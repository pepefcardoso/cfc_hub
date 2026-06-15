# GEMINI.md — Agent Instruction Manual for CFCHub

> **Read this file entirely before executing any task.**
> This file overrides any implicit assumption from training data.
> When in doubt between this file and a general convention, this file wins.

---

## 1. Project Identity

**CFCHub** is a multi-tenant SaaS platform for Brazilian *Centros de Formação de Condutores* (CFCs — driving schools).
It manages student enrollment, class scheduling, instructor/vehicle resource allocation, contracts, financial control, and regulatory document tracking.

| Attribute | Value |
|---|---|
| Domain locale | Brazilian Portuguese (pt-BR) — user-facing strings, email templates, validation messages |
| Code language | English — all identifiers, comments, commit messages, XML doc strings |
| Regulatory context | DETRAN, SENATRAN resolutions, CNH categories (A, B, C, D, E, ACC), LGPD (Lei 13.709/2018) |
| Documentation language | English — all `/docs` files including this one |

---

## 2. Non-Negotiable Technical Constraints

| Constraint | Rule |
|---|---|
| Runtime | .NET 10 only. No .NET 8 or 9 patterns. Use C# 13 features where they improve clarity. |
| ORM | EF Core 10 only. No Dapper, no raw ADO.NET except inside `IDbContextFactory` for bulk ops explicitly approved in `design.md`. |
| Database | PostgreSQL 16. Schema-per-tenant multi-tenancy (see `design.md#3`). Never use SQLite, even for tests. |
| Cache / Locks | Redis 7 via `StackExchange.Redis`. Never use in-memory `IDistributedCache` for any production code path. |
| Cloud | AWS only. S3 for file storage, SES for transactional email. No Azure Blob, no GCP Storage, no SendGrid. |
| Containers | Docker (Linux containers). `docker-compose` for local dev. ECS Fargate for production. |
| Testing | xUnit 2.x + FluentAssertions + Testcontainers. No NUnit, no MSTest. No in-memory DB for integration tests. |
| Auth | JWT Bearer RS256. Never HS256 in any environment. All tokens carry `tenant_id` and `role` claims. |
| Mocking | NSubstitute only. Never Moq. |
| PDF generation | QuestPDF only. No iTextSharp, no FastReport. |
| Mediator | MediatR 12.x. |
| Validation | FluentValidation 11.x. No DataAnnotations for business rules. |

---

## 3. Architecture Overview

```
CFCHub.sln
├── src/
│   ├── CFCHub.Api               # Minimal API host, middleware, DI composition root
│   ├── CFCHub.Application       # Use cases: Commands, Queries, Handlers (MediatR)
│   ├── CFCHub.Domain            # Entities, Value Objects, Domain Events, Specs, Interfaces
│   ├── CFCHub.Infrastructure    # EF Core, Redis, S3, SES, DETRAN client, OutboxWorker
│   └── CFCHub.Workers           # Hosted BackgroundService workers
├── tests/
│   ├── CFCHub.UnitTests         # Domain + Application — zero I/O, no Testcontainers
│   └── CFCHub.IntegrationTests  # Full stack — Testcontainers (real PostgreSQL + real Redis)
└── docs/
    ├── GEMINI.md                ← THIS FILE
    ├── conventions.md
    └── design.md
```

**Strict dependency flow:**
```
Api → Application → Domain ← Infrastructure
```
- `Domain` defines interfaces (`ISchedulingRepository`, `ISchedulingLockService`, etc.).
- `Infrastructure` implements them.
- `Application` depends only on `Domain` interfaces — never on `Infrastructure` types directly.
- `Api` wires everything in the DI root. It is the only layer allowed to reference `Infrastructure`.

---

## 4. Multi-Tenancy — Critical Rules

- **Tenant** = one CFC (driving school). One tenant = one PostgreSQL schema.
- `tenant_id` is resolved exclusively from the validated JWT claim — never from request body, query string, or header.
- Schema name format: `cfc_{tenant_slug}` (e.g., `cfc_autoescola_abc`).
- The `public` schema contains only: `tenants` table, `migrations_history`, and shared reference data (Brazilian states, CNH categories).
- **NEVER** write a query that can access data across tenant schemas.
- Tenant resolution: `JWT tenant_id claim → public.tenants lookup → schema name → SET search_path`.
- Migrations target the `__template` schema first. `TenantMigrationOrchestrator` propagates to all active tenant schemas on deploy.
- When generating a new migration, always use `--schema __template` flag. Never target a real tenant schema manually.

---

## 5. Scheduling Engine — Critical Rules

Double-booking is a **P0 defect**. The scheduling engine is the core domain.

**Lock acquisition (MUST follow this exact protocol):**

1. Acquire Redis `SETNX` locks in **deterministic order**: `instructor:{id}` → `vehicle:{id}` → `track:{id}`.
2. All locks use **30-second TTL**. No exceptions. If an operation needs more time, it is a bug.
3. If **any** lock fails to acquire: release all already-acquired locks → return `ConflictException` (409).
4. After acquiring all locks, re-validate availability in PostgreSQL using `SELECT FOR UPDATE` inside a transaction.
5. Release all locks in a `finally` block via `ISchedulingLockService` — never call `StackExchange.Redis` directly from Application or Domain.

**PostgreSQL exclusion constraint is the authoritative safety net.** Redis locks are a performance optimization (prevent thundering herd). Both must be present.

---

## 6. LGPD Compliance Mandates

> Violation of any rule below is a **critical security defect**, not a style issue.

1. **Encryption at rest**: fields classified as `SensitivePersonal` or `SensitiveSpecial` (see `design.md#6`) MUST be encrypted with AES-256-GCM via `IDataProtectionService` before persisting. Never store plaintext CPF, RG, date of birth, phone, address, or medical data in PostgreSQL columns.

2. **Audit log**: MUST be written for every Create/Update/Delete on: `Student`, `MedicalExam`, `Contract`, `Payment`. Implemented via `AuditInterceptor` (`ISaveChangesInterceptor`). The `audit_logs` table is append-only — no UPDATE or DELETE, enforced by PostgreSQL row security policy.

3. **Data minimization**: never log sensitive fields. Use `[Sensitive]` attribute on DTO properties. `SensitiveDataDestructuringPolicy` in Serilog scrubs them automatically. Verify before merging.

4. **Soft delete only**: never hard-delete `Student` or `Enrollment` records. Hard deletion requires a completed `DataErasureRequest` workflow (LGPD Art. 18 — Right to Erasure).

5. **Consent capture**: every `Enrollment` creation MUST persist a `ConsentRecord` with: policy version, SHA-256 hash of policy content, `DateTimeOffset` (UTC), client IP, user agent. `ConsentRecord` is immutable after creation.

6. **Field-level access control**: staff roles (`Receptionist`, `Instructor`, `Financial`, `Admin`) have field-level restrictions defined in `FieldAccessPolicy`. Never expose a restricted field outside its policy scope, regardless of API convenience.

7. **Medical data (LGPD Art. 11 — Sensitive Category)**: stored exclusively in the `cfchub-{env}-medical` S3 bucket (separate bucket, separate IAM policy). Pre-signed URL TTL for medical files: **maximum 15 minutes**. No exceptions.

8. **Data erasure flow**: anonymize name/cpf/email/phone → delete S3 objects under `/medical/{studentId}/` → soft-delete enrollment records → retain `audit_logs` and `payment_records` (fiscal retention: 5 years per Lei 9.613/1998).

9. **No PII in Redis values** without encryption. No PII in Redis key names — use `SHA-256(cpf)` as cache key component, never the raw CPF.

10. **No PII in email bodies**: SES templates use only pre-signed URLs or anonymized references. Never include CPF, RG, medical data, or financial details in email body.

---

## 7. Security Mandates

1. All API endpoints require authentication except: `POST /api/v1/auth/login`, `GET /api/v1/public/cfc/{slug}`, `GET /api/v1/public/qr/{code}`. Any other use of `[AllowAnonymous]` is forbidden.

2. `tenant_id` comes from the JWT only. Never trust it from any other source.

3. All Commands and Queries MUST have a corresponding FluentValidation validator. Invalid input is rejected before reaching domain logic.

4. File upload validation: read the first 16 bytes and validate magic bytes, not `Content-Type` header or file extension. Allowed types: `application/pdf` (`%PDF`), `image/jpeg` (`FFD8FF`), `image/png` (`89504E47`).

5. Pre-signed S3 URLs: max TTL = 300s for upload (`PutObject`), 3600s for regular download (`GetObject`), 900s for medical files.

6. Rate limiting: Redis sliding window per `{tenant}:{endpoint}:{user_id}`. See `conventions.md#rate-limiting` for per-endpoint limits.

7. API error responses: use `ProblemDetails` (RFC 7807) with `traceId` only. Never expose stack traces, inner exception messages, or database error details to the client.

8. Secrets: sourced exclusively from AWS Secrets Manager (production) or environment variables (local dev). Never in `appsettings.json`, `appsettings.Production.json`, or source code.

9. JWT private key rotation: use `CFCHUB_JWT_PRIVATE_KEY_ARN` from Secrets Manager. Key rotation does not require redeployment (resolved at runtime).

10. HTTP response headers required on all responses: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`. Configured globally in `SecurityHeadersMiddleware`.

---

## 8. Testing Mandates

- Domain rules: every rule in `CFCHub.Domain` MUST have a unit test. Coverage floor: **90%**. CI fails below this threshold.
- Scheduling conflicts: MUST have integration tests using Testcontainers with real PostgreSQL and real Redis — including concurrent booking simulation.
- LGPD operations: consent capture, audit log write, data erasure workflow, and field-level access policy MUST each have at least one integration test.
- Test data: use `Builder` pattern. Files live in `tests/CFCHub.UnitTests/Builders/` and `tests/CFCHub.IntegrationTests/Builders/`.
- Integration test isolation: each test class creates its own tenant schema in `InitializeAsync` and drops it in `DisposeAsync`. Tests MUST NOT share state.
- Outbox pattern: integration tests for outbox processing MUST verify atomicity (simulate crash between DB write and outbox dispatch; assert message is retried).
- Never use `Thread.Sleep` in tests. Use `await Task.Delay` with `CancellationToken` or polling with timeout via `FluentAssertions.Extensions`.

---

## 9. Task Execution Protocol

Follow this sequence for every task:

1. **Identify** which layer(s) are affected: `Domain`, `Application`, `Infrastructure`, `Api`, `Workers`.
2. **Read** the relevant section in `design.md` before writing any code.
3. **Read** `conventions.md` for naming, file placement, and the applicable pattern.
4. **Write tests first** when the task involves domain logic, scheduling rules, or LGPD-sensitive operations.
5. **Check migration rules**: if the task requires a DB schema change, generate the migration against `__template` schema only.
6. **Never modify** a migration already applied to production (marked `// [Applied: prod YYYY-MM-DD]`). Create a new migration instead.
7. **Never remove** a public API endpoint. Deprecate with `[Deprecated("reason")]` attribute + `Deprecated: true` response header.
8. **Self-check before output**:
   - Does the code compile cleanly?
   - Does any sensitive data appear in log statements?
   - Does any new endpoint lack authentication?
   - Does any new Redis operation lack a TTL?
   - Does any file operation bypass `IFileStorageService`?

---

## 10. Forbidden Actions

Hard stops — do not proceed and surface to the developer:

| Action | Reason |
|---|---|
| Raw SQL that references more than one tenant schema | Cross-tenant data leak |
| Storing a secret or credential in source code or `appsettings.*.json` | Security |
| Disabling or bypassing `AuditInterceptor` for any reason | LGPD compliance |
| Adding `[AllowAnonymous]` to any non-public endpoint | Security |
| Creating a Redis lock without a TTL | Deadlock risk |
| Returning stack trace or inner exception to the API client | Information disclosure |
| Generating a pre-signed S3 URL for medical files with TTL > 900s | LGPD / data exposure |
| Using `DateTime.Now` or `DateTime.UtcNow` directly | Use injected `ISystemClock` for testability |
| Using `Guid.NewGuid()` directly in domain entities | Use `IIdGenerator` for deterministic test IDs |
| Catching the base `Exception` class without re-throwing or fully logging | Swallows bugs silently |
| Storing PII in a Redis key name or unencrypted Redis value | LGPD violation |
| Hard-deleting a `Student` or `Enrollment` record without a `DataErasureRequest` | LGPD Art. 18 |
| Using offset-based pagination in any list endpoint | Performance at scale |
| Referencing another module's entity type directly (not by ID) | Module boundary violation |
