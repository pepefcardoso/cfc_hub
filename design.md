# design.md — CFCHub System Design

> Authoritative reference for architecture decisions.
> Read the relevant section before implementing any feature.
> ADRs (Architecture Decision Records) are embedded inline under each decision.

---

## 1. Architecture Philosophy

CFCHub uses a **Modular Monolith** with internal module boundaries enforced by code organization and build-time constraints.

**ADR-001: Modular Monolith over Microservices**

| Factor | Decision |
|---|---|
| Scale | ~11.000 CFCs, most with <500 students. No need for independent module scaling. |
| Ops overhead | A bootstrapped SaaS cannot afford the operational complexity of distributed services. |
| Data isolation | Schema-per-tenant provides LGPD isolation without distributed transactions. |
| Future migration | Module boundaries allow extraction to services if scale demands it, without upfront complexity. |

**Explicitly rejected:**
- Anemic domain model — business rules live in entities and domain services, not Application handlers.
- God DbContext — `AppDbContext` is scoped per tenant; its schema is set at request time.
- Service locator — all dependencies via constructor injection.
- Shared mutable state between modules — modules communicate only via MediatR in-process events.

---

## 2. Module Boundaries

```
┌──────────────────────────────────────────────────────────────────┐
│ MODULE: Scheduling                                               │
│ Owns: SchedulingSlot, Instructor, Vehicle, Track                 │
│ Publishes: SlotBooked, SlotCancelled, SlotCompleted              │
├──────────────────────────────────────────────────────────────────┤
│ MODULE: Enrollment                                               │
│ Owns: Student, Enrollment, LicenseCategory                       │
│ Publishes: StudentEnrolled, EnrollmentCompleted                  │
│ Subscribes: SlotCompleted (increments practical hours counter)   │
├──────────────────────────────────────────────────────────────────┤
│ MODULE: Contracts                                                │
│ Owns: Contract, ConsentRecord, SignatureRecord                   │
│ Publishes: ContractGenerationRequested, ContractSigned           │
│ Subscribes: StudentEnrolled (triggers contract generation)       │
├──────────────────────────────────────────────────────────────────┤
│ MODULE: Finance                                                  │
│ Owns: Payment, Installment, Invoice, DebtRecord                  │
│ Publishes: PaymentReceived, InvoiceOverdue                       │
│ Subscribes: StudentEnrolled (creates payment plan)               │
├──────────────────────────────────────────────────────────────────┤
│ MODULE: Compliance                                               │
│ Owns: DocumentRecord, DocumentExpiryAlert                        │
│ Publishes: DocumentExpiryAlertTriggered                          │
│ Subscribes: StudentEnrolled (registers CNH expiry tracking)      │
├──────────────────────────────────────────────────────────────────┤
│ MODULE: Identity (shared kernel)                                 │
│ Owns: StaffUser, Role, Permission, FieldAccessPolicy             │
│ Consumed by all modules for authorization checks                 │
└──────────────────────────────────────────────────────────────────┘
```

**Cross-module rule:** modules reference each other by **strongly typed IDs only**. No entity type from module A appears in the code of module B. Domain events carry only IDs and primitive values.

---

## 3. Multi-Tenant Strategy: Schema-Per-Tenant

**ADR-002: Schema-per-tenant over Row-Level Security**

| Factor | Schema-Per-Tenant | RLS |
|---|---|---|
| LGPD isolation | Strong — query bug cannot cross schemas | Weak — a missing `WHERE` clause exposes all data |
| Data portability (LGPD Art. 18) | `pg_dump -n cfc_abc` — trivial | Complex extraction logic required |
| Per-tenant backup | Native `pg_dump` per schema | Manual filtering required |
| Performance overhead | Zero at projected scale (<10.000 tenants) | Negligible but policy evaluation on every row |
| Migration complexity | Higher — orchestrated rollout | Simpler — single migration |

### Tenant Resolution Flow

```
Incoming HTTP Request
        │
        ▼
TenantResolutionMiddleware
        ├── Validate JWT signature (RS256)
        ├── Extract claim: tenant_id = "autoescola_abc"
        ├── Check Redis: prod:global:tenant:autoescola_abc (TTL 300s)
        │   ├── HIT  → load TenantContext from cache
        │   └── MISS → query public.tenants WHERE slug = 'autoescola_abc'
        │               → cache result → set TenantContext
        ├── Validate tenant.status = 'Active'
        └── Set ITenantContext { SchemaName = "cfc_autoescola_abc", TenantId = ... }
                │
                ▼
        AppDbContext (scoped per request)
                └── OnConfiguring: SET search_path TO cfc_autoescola_abc, public
```

### Schema Naming

- Format: `cfc_{slug}` — slug is lowercase alphanumeric + underscores.
- Example: `cfc_autoescola_abc`, `cfc_centro_piloto_sul`.
- Slug validation: `^[a-z0-9][a-z0-9_]{2,62}[a-z0-9]$` — enforced at tenant registration.

### Template Schema and Migrations

```
__template schema (canonical EF Core target)
        │
TenantMigrationOrchestrator (runs on deploy)
        ├── Apply pending migrations to __template
        └── For each tenant in public.tenants WHERE status = 'Active':
                └── Apply same migrations to cfc_{slug}
```

Migrations are generated with: `dotnet ef migrations add {Name} --schema __template`

New tenant provisioning:

```
POST /api/v1/admin/tenants
        │
TenantProvisioningService
        ├── INSERT INTO public.tenants
        ├── CREATE SCHEMA cfc_{slug}
        ├── Apply all migrations from __template to new schema
        └── Seed reference data (CNH categories, CFC configuration defaults)
```

---

## 4. Scheduling Engine Design

### Resource Model

A scheduling slot is a simultaneous reservation of three constrained resources:

```
Slot = Instructor(availability, category) ∩ Vehicle(availability, category) ∩ Track(type, availability)
```

**Instructor constraints:**
- Weekly availability template (days + time windows).
- Per-day overrides (vacation, sick leave, etc.).
- CNH categories they are licensed to teach.
- Maximum daily slots (contractual limit).

**Vehicle constraints:**
- Associated CNH category (A = motorcycle, B = car, C/D/E = truck/bus/articulated).
- Maintenance windows (no bookings during maintenance).

**Track constraints:**
- Type: `Maneuver`, `Road`, `Highway` — each required by different exam stages.
- Capacity: a track can host 1 session at a time (no concurrent use).
- CFC may have 1..N tracks.

**Slot duration:** 50 minutes, fixed. No partial slots.

### Distributed Lock Protocol

```
BookSlotCommand received
        │
        ▼
Step 1: Acquire locks in deterministic order (prevents deadlock)
        ├── SETNX {env}:{tenant}:sched:lock:instructor:{instructorId} 1 EX 30
        ├── SETNX {env}:{tenant}:sched:lock:vehicle:{vehicleId}      1 EX 30
        └── SETNX {env}:{tenant}:sched:lock:track:{trackId}          1 EX 30

        If any SETNX returns 0 (lock held):
        └── Release all acquired locks → throw ConflictException (409)

Step 2: Begin PostgreSQL transaction
        ├── SELECT FOR UPDATE on instructor slot range
        ├── SELECT FOR UPDATE on vehicle slot range
        ├── Validate no overlap (exclusion constraint also enforces this)
        ├── INSERT INTO scheduling_slots
        ├── INSERT INTO outbox_messages (type: 'SlotBooked', same transaction)
        └── COMMIT

Step 3: Release all Redis locks (finally block — always executes)
```

**Why double-check in DB after Redis lock?** Redis locks prevent concurrent racing requests. But the application process can crash after the DB write and before lock release. The next startup would find no lock but potentially conflicting data. PostgreSQL exclusion constraint is the authoritative guardrail.

### PostgreSQL Exclusion Constraint

```sql
-- Applied via migration against __template schema
ALTER TABLE scheduling_slots ADD CONSTRAINT no_instructor_double_booking
EXCLUDE USING gist (
    instructor_id WITH =,
    tstzrange(started_at, ended_at, '[)') WITH &&
) WHERE (status <> 'Cancelled');

ALTER TABLE scheduling_slots ADD CONSTRAINT no_vehicle_double_booking
EXCLUDE USING gist (
    vehicle_id WITH =,
    tstzrange(started_at, ended_at, '[)') WITH &&
) WHERE (status <> 'Cancelled');

ALTER TABLE scheduling_slots ADD CONSTRAINT no_track_double_booking
EXCLUDE USING gist (
    track_id WITH =,
    tstzrange(started_at, ended_at, '[)') WITH &&
) WHERE (status <> 'Cancelled');
```

Requires `btree_gist` extension (enabled in `__template` schema init migration).

### Availability Calculation

```
GetAvailableSlotsQuery(date, cnhCategory, instructorId?)
        │
        ▼
AvailabilityCalculatorService
        ├── 1. Load instructor weekly template (EF, cached 1h per instructor)
        ├── 2. Load per-day overrides for {date}
        ├── 3. Load booked slots for {date} (EF, AsNoTracking projection)
        ├── 4. Compute free 50-minute windows = template − booked − overrides
        ├── 5. For each free window:
        │   ├── Find available vehicle (category match, not in maintenance)
        │   └── Find available track (type required for student's stage)
        ├── 6. Cache result: prod:{tenant}:sched:avail:instructor:{id}:{date}  TTL=300s
        └── 7. Return list of AvailableSlot { StartedAt, InstructorId, VehicleId, TrackId }

Cache invalidation:
  On any slot INSERT/UPDATE/DELETE for (instructorId, date): DEL availability cache key
  Implemented in AuditableEntityInterceptor.SavedChangesAsync
```

---

## 5. Outbox Pattern Design

**ADR-003: Outbox for all async side effects**

All domain events that produce side effects (email, PDF, external webhook) MUST go through the outbox. Direct calls to SES or S3 from Command Handlers are forbidden — they break atomicity if the handler succeeds but the external call fails.

### Outbox Table (per-tenant schema)

```sql
CREATE TABLE outbox_messages (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    type            TEXT            NOT NULL,
    payload         JSONB           NOT NULL,
    status          TEXT            NOT NULL DEFAULT 'Pending',
    attempts        INT             NOT NULL DEFAULT 0,
    max_attempts    INT             NOT NULL DEFAULT 5,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now(),
    scheduled_after TIMESTAMPTZ     NOT NULL DEFAULT now(),
    processed_at    TIMESTAMPTZ,
    error           TEXT,
    error_details   JSONB
);

-- Status: Pending | Processing | Processed | Failed
-- Index for worker polling
CREATE INDEX idx_outbox_pending ON outbox_messages (status, scheduled_after)
    WHERE status = 'Pending';
```

### Outbox Worker Flow

```
OutboxWorker.ExecuteAsync (polling every 5s)
        │
        ▼
        ├── Acquire Redis lease: SETNX {env}:{tenant}:outbox:lease 1 EX 60
        │   └── If fails: skip this cycle (another instance is processing)
        │
        ├── SELECT * FROM outbox_messages
        │   WHERE status = 'Pending' AND scheduled_after <= now()
        │   ORDER BY created_at ASC LIMIT 10
        │   FOR UPDATE SKIP LOCKED
        │
        └── For each message:
            ├── UPDATE status = 'Processing'
            ├── Resolve handler: IOutboxMessageHandler<{type}>
            ├── await handler.HandleAsync(payload, ct)
            ├── On success: UPDATE status = 'Processed', processed_at = now()
            └── On failure:
                ├── attempts < max_attempts:
                │   UPDATE status = 'Pending', attempts++,
                │          scheduled_after = now() + exponential_backoff(attempts),
                │          error = message
                └── attempts >= max_attempts:
                    UPDATE status = 'Failed', error_details = full_exception
                    → _logger.LogCritical (triggers CloudWatch alarm)
```

### Outbox Message Types and Handlers

| Type | Handler | Side Effects |
|---|---|---|
| `ContractGenerationRequested` | `ContractGenerationHandler` | Generate PDF (QuestPDF) → S3 upload → SES email with pre-signed URL |
| `WelcomeEmailRequested` | `WelcomeEmailHandler` | SES email with login instructions |
| `SlotReminderRequested` | `SlotReminderHandler` | SES email 24h before slot |
| `PaymentReceiptRequested` | `PaymentReceiptHandler` | Generate PDF receipt → S3 upload → SES email |
| `DocumentExpiryAlertRequested` | `DocumentExpiryAlertHandler` | SES email to designated staff |
| `DataErasureCompleteNotified` | `ErasureNotificationHandler` | SES email with reference number (no PII in body) |

### Payload Design

Payloads MUST be self-contained. The handler must not query the DB to reconstruct context.

```json
// ContractGenerationRequested — good: self-contained
{
  "contractId": "...",
  "tenantId": "...",
  "studentName": "João da Silva",
  "studentEmail": "joao@example.com",
  "enrollmentDate": "2025-01-15",
  "cnhCategory": "B",
  "totalAmount": 2800.00,
  "templateKey": "contract-b-v3"
}

// Bad: handler would need to query DB to get student data
{
  "contractId": "...",
  "studentId": "..."
}
```

---

## 6. LGPD Architecture

### Data Classification Matrix

| Classification | LGPD Basis | Fields | Storage |
|---|---|---|---|
| `SensitiveSpecial` | Art. 11 | Medical exam data, health info, disability info | AES-256-GCM + S3 `/medical/` bucket |
| `SensitivePersonal` | Art. 5, VI | CPF, RG, date of birth, phone, full address | AES-256-GCM encrypted PostgreSQL column |
| `Personal` | Art. 5, I | Name, email, photo | Encrypted PostgreSQL column |
| `Operational` | N/A | Slot dates, payment amounts, CNH category | Plaintext |
| `Audit` | Art. 37 | Mutation records | Append-only, encrypted fields |

### Encryption Implementation

```csharp
// IDataProtectionService — key stored in AWS Secrets Manager per tenant
public interface IDataProtectionService
{
    string Encrypt(string plaintext, string tenantId);
    string Decrypt(string ciphertext, string tenantId);
}

// EF Core value converter for sensitive columns
public class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(IDataProtectionService dps, string tenantId)
        : base(
            v => dps.Encrypt(v, tenantId),
            v => dps.Decrypt(v, tenantId)) { }
}

// Applied in entity configuration
builder.Property(s => s.Cpf)
    .HasColumnName("cpf")
    .HasConversion(new EncryptedStringConverter(dataProtectionService, tenantId));
```

Key rotation: new keys are stored alongside old ones in Secrets Manager. `IDataProtectionService` tries new key first, falls back to old key for existing ciphertext. Old keys are retired after 30 days (all records re-encrypted on read).

### Audit Log

```sql
-- In public schema — shared across tenants for system-level audit
-- In tenant schema — business-level audit
CREATE TABLE audit_logs (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    occurred_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    actor_user_id   UUID        NOT NULL,
    actor_role      TEXT        NOT NULL,
    action          TEXT        NOT NULL,      -- Created | Updated | Deleted | Accessed | Exported
    entity_type     TEXT        NOT NULL,
    entity_id       UUID        NOT NULL,
    changed_fields  JSONB,                     -- {"cpf": {"from": "[encrypted]", "to": "[encrypted]"}}
    ip_address      INET        NOT NULL,
    user_agent      TEXT,
    trace_id        TEXT        NOT NULL
);

-- Row-level security: no application role can UPDATE or DELETE
CREATE POLICY audit_logs_insert_only ON audit_logs
    FOR INSERT TO app_role WITH CHECK (true);
-- No UPDATE or DELETE policy — effectively append-only
ALTER TABLE audit_logs ENABLE ROW LEVEL SECURITY;
```

### Data Erasure Workflow

```
PATCH /api/v1/students/{studentId}/erasure-requests
        │
        ▼
DataErasureRequest created (status: Pending)
        │
        ├── Check legal hold:
        │   ├── Active contract (unsigned or within cancellation window) → BLOCKED
        │   ├── Unpaid debt → BLOCKED
        │   └── Fiscal retention period active (payment records < 5 years) → PARTIAL
        │
        └── No full hold → DataErasureWorker (async, via outbox)
                ├── Anonymize Student:
                │   name       → "[REMOVIDO]"
                │   cpf        → SHA-256(cpf) (irreversible, for deduplication only)
                │   email      → "[REMOVIDO]"
                │   phone      → "[REMOVIDO]"
                │   address    → "[REMOVIDO]"
                ├── Delete S3 objects: medical/{tenantSlug}/{studentId}/*
                ├── Soft-delete Enrollment records (deleted_at = now())
                ├── RETAIN: audit_logs (legal requirement — cannot erase)
                ├── RETAIN: payment_records (fiscal: 5 years)
                └── UPDATE DataErasureRequest status = 'Completed'
                    → outbox: DataErasureCompleteNotified
```

### Consent Model

```csharp
// Immutable after creation — no Update method
public sealed class ConsentRecord : Entity<ConsentRecordId>
{
    public StudentId StudentId { get; private init; }
    public string PolicyVersion { get; private init; }     // "v2.1"
    public string PolicyContentHash { get; private init; } // SHA-256 of policy text at time of consent
    public DateTimeOffset ConsentedAt { get; private init; }
    public string IpAddress { get; private init; }
    public string UserAgent { get; private init; }
    public ConsentChannel Channel { get; private init; }   // Web | App | Paper

    private ConsentRecord() { } // EF Core

    public static ConsentRecord Capture(
        ConsentRecordId id,
        StudentId studentId,
        string policyVersion,
        string policyContentHash,
        DateTimeOffset consentedAt,
        string ipAddress,
        string userAgent,
        ConsentChannel channel) { ... }
}
```

---

## 7. AWS Integration Design

### S3 Bucket Structure

```
cfchub-{env}-documents/
└── {tenant_slug}/
    ├── contracts/
    │   └── {year}/{contractId}.pdf
    ├── receipts/
    │   └── {year}/{paymentId}.pdf
    └── student-photos/
        └── {studentId}.jpg

cfchub-{env}-medical/              ← SEPARATE BUCKET — stricter IAM policy
└── {tenant_slug}/
    └── {studentId}/
        └── {examId}.pdf
```

**ADR-004: Separate S3 bucket for medical files**

Medical files (LGPD Art. 11 — sensitive category) require a distinct bucket with:
- Separate IAM policy (only `cfchub-medical-service` role can access).
- No bucket policy allowing pre-signed URL generation by the general API role.
- S3 Object Lock enabled (WORM) — medical files cannot be deleted by the application.
- Pre-signed URL TTL capped at 900 seconds (enforced in `S3FileStorageService`).
- Deletion only via `DataErasureWorker` using a privileged IAM role.

### File Operation Pattern

```csharp
public interface IFileStorageService
{
    // Client uploads directly to S3 — file never passes through API server
    Task<PresignedUploadUrl> GenerateUploadUrlAsync(
        StorageTarget target,    // Documents | Medical
        string tenantSlug,
        string objectKey,
        string contentType,
        CancellationToken ct);

    // Worker uploads (e.g., generated PDFs) — uses IAM role credentials
    Task UploadAsync(
        StorageTarget target,
        string tenantSlug,
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken ct);

    // Returns pre-signed URL for client download
    Task<string> GenerateDownloadUrlAsync(
        StorageTarget target,
        string tenantSlug,
        string objectKey,
        CancellationToken ct);
}
```

TTL enforcement in `S3FileStorageService`:

```csharp
private TimeSpan GetDownloadUrlTtl(StorageTarget target) => target switch
{
    StorageTarget.Medical   => TimeSpan.FromMinutes(15),
    StorageTarget.Documents => TimeSpan.FromHours(1),
    _                       => TimeSpan.FromHours(1)
};
```

### SES Email Templates

| Template ID | Trigger | Personalization Variables |
|---|---|---|
| `cfchub-welcome` | Student enrolled | `student_name`, `login_url` |
| `cfchub-slot-reminder` | 24h before slot | `student_name`, `slot_date`, `instructor_name`, `cfc_address` |
| `cfchub-contract-ready` | Contract generated | `student_name`, `contract_url` (pre-signed, 24h), `expiry_notice` |
| `cfchub-payment-receipt` | Payment confirmed | `student_name`, `amount`, `receipt_url` (pre-signed, 24h), `reference` |
| `cfchub-doc-expiry-d30` | 30 days to expiry | `staff_name`, `document_type`, `expiry_date`, `student_name`, `action_url` |
| `cfchub-doc-expiry-d7` | 7 days to expiry | Same as D30 |
| `cfchub-erasure-complete` | Data erasure done | `reference_number` only — no PII |

Rules:
- Never include CPF, RG, medical data, or financial details in email body.
- Always use pre-signed URLs for file access. Embed the expiry time in the email text.
- Bounce and complaint events: SES → SNS → `POST /webhooks/ses/events` → updates `email_delivery_logs`.

---

## 8. Background Services Design

### Service Registry

| Service | Trigger | Tenant Scope | Redis Lease |
|---|---|---|---|
| `OutboxWorker` | Polling 5s | Per tenant | `outbox:lease` — TTL 60s |
| `DocumentExpiryWorker` | Daily 06:00 UTC | All tenants | `doc-expiry:lease:{date}` — TTL 24h |
| `SlotReminderWorker` | Polling 1h | All tenants | `slot-reminder:lease` — TTL 3600s |
| `TenantMigrationOrchestrator` | Deploy hook | All tenants | None (runs once, not recurring) |
| `DetranCacheInvalidator` | On-demand event | Per tenant | None |

### Multi-Instance Safety

ECS Fargate runs 2+ tasks. Workers use Redis `SETNX` leases to elect a single processor per cycle.

```csharp
public abstract class LeasedBackgroundService : BackgroundService
{
    protected abstract string LeaseKey { get; }
    protected abstract TimeSpan LeaseTtl { get; }
    protected abstract TimeSpan PollingInterval { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var leaseAcquired = await _redis.StringSetAsync(
                LeaseKey, Environment.MachineName, LeaseTtl, When.NotExists);

            if (leaseAcquired)
            {
                try   { await ProcessAsync(stoppingToken); }
                catch (Exception ex) { _logger.LogError(ex, "Worker {Worker} failed.", GetType().Name); }
                finally { await _redis.KeyDeleteAsync(LeaseKey); }
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    protected abstract Task ProcessAsync(CancellationToken ct);
}
```

### DocumentExpiryWorker Logic

```
DocumentExpiryWorker.ProcessAsync (daily)
        │
        └── For each active tenant:
                ├── Query: SELECT * FROM document_records
                │         WHERE expiry_date BETWEEN today AND today+30
                │         AND last_alert_sent_at < today - 1d  ← idempotent guard
                └── For each expiring document:
                        ├── Determine alert tier: D-30, D-15, D-7, D-1
                        ├── Find responsible staff (role: Receptionist or Admin)
                        └── INSERT INTO outbox_messages (DocumentExpiryAlertRequested)
                            UPDATE document_records SET last_alert_sent_at = now()
                            (same transaction → idempotent)
```

---

## 9. DETRAN Integration Design

**ADR-005: DETRAN integration via adapter pattern + screen scraping isolation**

DETRAN portals are not standardized across Brazilian states. Integration is state-specific.

```
Domain interface (stable contract):
IDetranClient
    ├── GetCnhStatusAsync(cpf: string, state: BrazilianState) → CnhStatusResult
    └── GetCnhPointsAsync(cpf: string, state: BrazilianState) → CnhPointsResult

Infrastructure router:
DetranHttpClient : IDetranClient
    └── StateDetranAdapterFactory
        ├── SpDetranAdapter     (São Paulo — REST API)
        ├── RjDetranAdapter     (Rio de Janeiro — REST API)
        ├── MgDetranAdapter     (Minas Gerais — HTML scraping)
        └── DefaultDetranAdapter (all others — Playwright scraping)
```

**Screen scraping isolation:** Playwright-based adapters run inside dedicated Docker containers (sidecar). The main API communicates via gRPC. This prevents browser crashes from affecting API availability.

**Caching:** All DETRAN results cached for 24 hours. CNH status does not change intra-day.

```
GetCnhStatusAsync(cpf, state)
        │
        ├── Check Redis: prod:{tenant}:detran:cnh:{sha256(cpf)}
        │   ├── HIT  → return cached result
        │   └── MISS → call adapter → cache result (TTL 86400s) → return
        │
        └── On adapter failure:
                ├── Log Warning (not Error — DETRAN downtime is common)
                └── Return CnhStatusResult.Unavailable (UI shows "consultar manualmente")
```

---

## 10. Deployment Architecture

```
                     Route 53 (*.cfchub.com.br)
                              │
                     ACM Certificate (wildcard)
                              │
                    Application Load Balancer
                     (host-based routing)
                    ┌──────────┴──────────┐
                    │                     │
              ECS Fargate           ECS Fargate
              API Service           Workers Service
              (2+ tasks)            (1 task)
              CFCHub.Api            CFCHub.Workers
                    │                     │
         ┌──────────┼─────────────────────┤
         │          │                     │
    RDS Aurora   ElastiCache          S3 Buckets
    PostgreSQL   Redis 7              (documents + medical)
    Multi-AZ     (cluster mode)
         │
    Secrets Manager
    (DB credentials, JWT keys,
     AWS SDK credentials,
     Data Protection keys)
```

### Environment Configuration

All secrets via AWS Secrets Manager or environment variables. Never in source code or `appsettings.*.json`.

```bash
# Required environment variables (set in ECS Task Definition)
CFCHUB_ENVIRONMENT=prod
CFCHUB_DB_CONNECTION_STRING=            # from Secrets Manager
CFCHUB_REDIS_CONNECTION_STRING=         # from Secrets Manager
CFCHUB_AWS_REGION=us-east-1
CFCHUB_S3_DOCUMENTS_BUCKET=cfchub-prod-documents
CFCHUB_S3_MEDICAL_BUCKET=cfchub-prod-medical
CFCHUB_SES_FROM_ADDRESS=noreply@cfchub.com.br
CFCHUB_JWT_PRIVATE_KEY_ARN=             # Secrets Manager ARN
CFCHUB_DATA_PROTECTION_KEY_PREFIX=cfchub/dpe/  # Secrets Manager prefix
```

### Health Checks

```
GET /health        → liveness (is the process alive?)
GET /health/ready  → readiness (are all dependencies reachable?)
                     Checks: PostgreSQL (public schema), Redis PING, S3 HeadBucket
```

ECS uses `/health` for liveness and `/health/ready` for readiness. Tasks failing readiness are removed from ALB target group without downtime.

---

## 11. Observability Design

### Structured Logging (Serilog)

```
Development  → Seq (localhost:5341)
Staging      → CloudWatch Logs (/cfchub/stg/api, /cfchub/stg/workers)
Production   → CloudWatch Logs (/cfchub/prod/api, /cfchub/prod/workers)
              + CloudWatch Alarms on Error/Critical level count
```

Required fields on every log entry (set in middleware):

```json
{
  "timestamp": "...",
  "level": "Information",
  "traceId": "00-abc-def-00",
  "tenantId": "autoescola_abc",
  "userId": "...",
  "environment": "prod",
  "message": "..."
}
```

### CloudWatch Alarms

| Alarm | Condition | Action |
|---|---|---|
| Outbox failure rate | `Failed` status count > 0 in 5 min | SNS → PagerDuty |
| API error rate | HTTP 5xx > 1% of requests in 5 min | SNS → PagerDuty |
| Scheduling conflict spike | 409 on scheduling > 10/min | SNS → email (non-critical) |
| Redis lock timeout | Lock TTL expired without release log | SNS → PagerDuty |
| Medical file access | Any access to medical bucket not via pre-signed URL | SNS → PagerDuty (security) |

### Distributed Tracing

OpenTelemetry SDK with `ActivitySource` in:
- HTTP request (auto via `Microsoft.AspNetCore.OpenTelemetry`).
- MediatR pipeline (custom `TracingBehavior`).
- EF Core commands (auto via `OpenTelemetry.Instrumentation.EntityFrameworkCore`).
- Redis operations (auto via `OpenTelemetry.Instrumentation.StackExchangeRedis`).
- S3/SES calls (auto via `OpenTelemetry.Instrumentation.AWS`).

Traces exported to AWS X-Ray in staging and production.
