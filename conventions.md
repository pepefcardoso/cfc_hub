# conventions.md — CFCHub Code Conventions

> Applies to all code in `src/` and `tests/`. AI agents must follow these conventions without exception.
> When this file conflicts with a general C# or .NET convention, this file wins.

---

## 1. Language Matrix

| Context | Language | Notes |
|---|---|---|
| Code identifiers | English | Classes, methods, variables, parameters, constants |
| Code comments | English | Inline and block comments |
| XML doc comments | English | `<summary>`, `<param>`, `<returns>` |
| Log messages | English | All `ILogger` calls |
| Commit messages | English | Conventional Commits format |
| User-facing strings | pt-BR | Resource files only — never hardcoded in C# |
| API error `detail` field | pt-BR | ProblemDetails `detail` property |
| Email template content | pt-BR | SES templates only |
| Exception messages (internal) | English | Never reaches the client |

---

## 2. C# Naming Conventions

### Casing Rules

| Target | Convention | Example |
|---|---|---|
| Classes | PascalCase | `SchedulingSlot`, `BookSlotCommandHandler` |
| Interfaces | `I` + PascalCase | `ISchedulingLockService`, `IFileStorageService` |
| Enums | PascalCase (type + members) | `SlotStatus.Confirmed`, `CnhCategory.B` |
| Methods | PascalCase | `GetAvailableSlotsAsync`, `AcquireLockAsync` |
| Properties | PascalCase | `InstructorId`, `StartedAt` |
| Private fields | `_camelCase` | `_redisDatabase`, `_logger` |
| Local variables | camelCase | `availableSlots`, `tenantSlug` |
| Method parameters | camelCase | `instructorId`, `cancellationToken` |
| Constants | PascalCase | `MaxLockTtlSeconds`, `DefaultPageSize` |
| Generic type params | `T` or descriptive | `TEntity`, `TId`, `TResult` |

### Required Suffixes

| Suffix | Applies to | Example |
|---|---|---|
| `Async` | Every async method | `BookSlotAsync`, `GetStudentByIdAsync` |
| `Exception` | Custom exception classes | `ConflictException`, `TenantNotFoundException` |
| `Handler` | MediatR handlers | `BookSlotCommandHandler`, `GetAvailableSlotsQueryHandler` |
| `Validator` | FluentValidation validators | `BookSlotCommandValidator` |
| `Repository` | Repository implementations | `SchedulingRepository`, `StudentRepository` |
| `Service` | Domain services | `SchedulingLockService`, `AvailabilityCalculatorService` |
| `Worker` | BackgroundService classes | `OutboxWorker`, `DocumentExpiryWorker` |
| `Configuration` | EF Core `IEntityTypeConfiguration<T>` | `SchedulingSlotConfiguration` |
| `Builder` | Test builder classes | `SchedulingSlotBuilder`, `StudentBuilder` |
| `Factory` | Factory classes | `TenantContextFactory`, `StateDetranAdapterFactory` |
| `Middleware` | ASP.NET Core middleware | `TenantResolutionMiddleware`, `SecurityHeadersMiddleware` |

### Naming Anti-Patterns (forbidden)

```csharp
// Wrong: Entity suffix on domain entities
public class SchedulingSlotEntity { }   // → SchedulingSlot

// Wrong: Manager, Helper, Utils
public class SchedulingManager { }      // → SchedulingService or specific use case
public class StringHelper { }           // → use extension methods with descriptive name

// Wrong: Base suffix on abstract classes visible to consumers
public abstract class BaseEntity { }    // → Entity<TId>

// Wrong: Impl suffix
public class SchedulingRepositoryImpl { } // → SchedulingRepository

// Wrong: Data suffix on DTOs
public class StudentData { }            // → StudentDto or StudentResult

// Wrong: abbreviations that reduce readability
public class SchdSlot { }              // → SchedulingSlot
public class InstrId { }               // → InstructorId
```

---

## 3. File and Folder Structure

### One Class Per File — No Exceptions
File name must exactly match the class/interface/enum name including casing.

### Layer Structure

```
src/CFCHub.Domain/
├── {ModuleName}/
│   ├── {EntityName}.cs
│   ├── {EntityName}Id.cs
│   ├── {EntityName}Status.cs          # enums
│   ├── I{EntityName}Repository.cs
│   ├── Events/
│   │   └── {EntityName}{EventVerb}Event.cs
│   └── Specifications/
│       └── {DescriptiveName}Spec.cs
└── Shared/
    ├── Entity.cs
    ├── AggregateRoot.cs
    ├── StronglyTypedId.cs
    ├── IDomainEvent.cs
    ├── ISystemClock.cs
    ├── IIdGenerator.cs
    └── Result.cs

src/CFCHub.Application/
└── {ModuleName}/
    ├── Commands/
    │   └── {VerbNoun}/
    │       ├── {VerbNoun}Command.cs
    │       ├── {VerbNoun}CommandHandler.cs
    │       └── {VerbNoun}CommandValidator.cs
    ├── Queries/
    │   └── Get{Noun}{Qualifier}/
    │       ├── Get{Noun}{Qualifier}Query.cs
    │       ├── Get{Noun}{Qualifier}QueryHandler.cs
    │       └── {Noun}{Qualifier}Result.cs
    └── Common/
        └── Behaviors/
            ├── ValidationBehavior.cs
            ├── LoggingBehavior.cs
            └── TenantBehavior.cs

src/CFCHub.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── TenantContextFactory.cs
│   ├── Configurations/
│   │   └── {EntityName}Configuration.cs
│   ├── Repositories/
│   │   └── {EntityName}Repository.cs
│   ├── Interceptors/
│   │   ├── AuditInterceptor.cs
│   │   └── AuditableEntityInterceptor.cs
│   └── Migrations/
│       └── {Timestamp}_{PascalCaseDescription}.cs
├── Caching/
│   ├── RedisLockService.cs
│   └── RedisKeys.cs                   # static class with key format constants
├── Storage/
│   └── S3FileStorageService.cs
├── Email/
│   └── SesEmailService.cs
└── ExternalServices/
    └── Detran/
        ├── IDetranClient.cs
        ├── DetranHttpClient.cs
        └── Adapters/
            ├── SpDetranAdapter.cs
            └── MgDetranAdapter.cs
```

---

## 4. Domain Entity Conventions

### Entity Base Classes

```csharp
// All entities inherit from Entity<TId>
public sealed class SchedulingSlot : Entity<SchedulingSlotId>
{
    // Properties: private setters only — mutate via methods
    public InstructorId InstructorId { get; private set; }
    public VehicleId VehicleId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset EndedAt { get; private set; }
    public SlotStatus Status { get; private set; }

    // Private constructor for EF Core
    private SchedulingSlot() { }

    // Static factory — only way to create a valid instance
    public static SchedulingSlot Book(
        SchedulingSlotId id,
        InstructorId instructorId,
        VehicleId vehicleId,
        TrackId trackId,
        DateTimeOffset startedAt,
        CnhCategory category)
    {
        // Enforce invariants here
        if (startedAt < DateTimeOffset.UtcNow)
            throw new UnprocessableException("Slot cannot be booked in the past.");

        var slot = new SchedulingSlot
        {
            Id = id,
            InstructorId = instructorId,
            VehicleId = vehicleId,
            StartedAt = startedAt,
            EndedAt = startedAt.AddMinutes(50),
            Status = SlotStatus.Confirmed
        };

        slot.AddDomainEvent(new SchedulingSlotBookedEvent(slot.Id, studentId));
        return slot;
    }

    // State transitions as explicit methods — never via property setters
    public void Cancel(string reason)
    {
        if (Status == SlotStatus.Completed)
            throw new UnprocessableException("Completed slots cannot be cancelled.");

        Status = SlotStatus.Cancelled;
        AddDomainEvent(new SchedulingSlotCancelledEvent(Id, reason));
    }
}
```

### Strongly Typed IDs

```csharp
// Correct
public sealed class SchedulingSlotId : StronglyTypedId<Guid>
{
    public SchedulingSlotId(Guid value) : base(value) { }
    public static SchedulingSlotId New() => new(Guid.NewGuid()); // only used in IIdGenerator impl
}

// Usage in domain
public SchedulingSlotId Id { get; private set; }  // compile-time type safety

// Wrong — raw Guid parameters are ambiguous
public static SchedulingSlot Book(Guid instructorId, Guid vehicleId, ...) { }
```

### Cross-Module References

```csharp
// Correct — reference by strongly typed ID only
public sealed class SchedulingSlot : Entity<SchedulingSlotId>
{
    public StudentId StudentId { get; private set; }      // ✓ ID reference
    public InstructorId InstructorId { get; private set; } // ✓ ID reference
}

// Wrong — direct entity reference across modules
public sealed class SchedulingSlot : Entity<SchedulingSlotId>
{
    public Student Student { get; private set; }          // ✗ cross-module entity
    public Instructor Instructor { get; private set; }    // ✗ cross-module entity
}
```

---

## 5. API Conventions

### Route Format

```
/api/v{version}/{resource}/{id?}/{sub-resource?}

POST   /api/v1/scheduling/slots
GET    /api/v1/scheduling/slots/{slotId}
PATCH  /api/v1/scheduling/slots/{slotId}/cancel
GET    /api/v1/students/{studentId}/slots
GET    /api/v1/instructors/{instructorId}/availability?date=2025-01-15
POST   /api/v1/students
DELETE /api/v1/students/{studentId}               # triggers DataErasureRequest, not hard delete
GET    /api/v1/public/qr/{code}                   # unauthenticated
```

Rules:
- Resources: `kebab-case` and plural (`scheduling-slots`, `medical-exams`).
- IDs: always path parameters, never query string.
- Actions (state transitions): `PATCH /{id}/{verb}` (`/slots/{id}/cancel`, `/enrollments/{id}/complete`).
- Never use verbs in resource names (`/getStudents`, `/createSlot`).

### Response Envelope

```json
{
  "data": { },
  "meta": {
    "traceId": "00-a1b2c3d4-e5f6a7b8-00",
    "timestamp": "2025-01-15T14:30:00.000Z"
  }
}
```

List responses include pagination:

```json
{
  "data": [ ],
  "meta": {
    "traceId": "...",
    "timestamp": "...",
    "nextCursor": "eyJpZCI6IjEyMzQ1In0=",
    "hasMore": true,
    "count": 20
  }
}
```

### Error Response (ProblemDetails — RFC 7807)

```json
{
  "type": "https://cfchub.com.br/errors/scheduling-conflict",
  "title": "Scheduling Conflict",
  "status": 409,
  "detail": "O instrutor já possui aula agendada neste horário.",
  "traceId": "00-a1b2c3d4-e5f6a7b8-00",
  "errors": {
    "instructorId": ["Instrutor indisponível no horário solicitado."]
  }
}
```

Error type URIs must be registered in `docs/error-types.md`.

### HTTP Status Code Mapping

| Scenario | Status |
|---|---|
| Successful creation | 201 Created + `Location` header |
| Successful update / state transition | 200 OK |
| Successful delete (erasure request created) | 202 Accepted |
| Validation error | 400 Bad Request |
| Unauthenticated | 401 Unauthorized |
| Insufficient role/permission | 403 Forbidden |
| Resource not found | 404 Not Found |
| Scheduling conflict / business constraint | 409 Conflict |
| Business rule violation (not a conflict) | 422 Unprocessable Entity |
| Server error | 500 Internal Server Error (no details) |

### Pagination

All list endpoints use **cursor-based pagination**:

```csharp
// Query parameter
public record GetAvailableSlotsQuery(
    DateOnly Date,
    CnhCategory? Category,
    string? Cursor,
    int Limit = 20) : IRequest<PagedResult<AvailableSlotResult>>;

// Cursor: base64-encoded JSON { "id": "...", "startedAt": "..." }
// Limit: default 20, max 100
```

Never use offset (`skip`/`take`) in production list endpoints.

---

## 6. EF Core Conventions

### DbContext

```csharp
// AppDbContext receives schema name from ITenantContext
public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(_tenantContext.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(BuildSoftDeleteFilter(entityType.ClrType));
        }
    }
}
```

### Entity Configuration

```csharp
// Each entity gets its own IEntityTypeConfiguration<T> class
public class SchedulingSlotConfiguration : IEntityTypeConfiguration<SchedulingSlot>
{
    public void Configure(EntityTypeBuilder<SchedulingSlot> builder)
    {
        builder.ToTable("scheduling_slots");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new SchedulingSlotId(value))
            .HasColumnName("id");

        builder.Property(s => s.Status)
            .HasConversion<string>()         // store enums as string, not int
            .HasColumnName("status");

        builder.Property(s => s.StartedAt).HasColumnName("started_at");
        builder.Property(s => s.EndedAt).HasColumnName("ended_at");

        // Ignore domain events — never persisted
        builder.Ignore(s => s.DomainEvents);
    }
}
```

### Table and Column Naming

| Element | Convention | Example |
|---|---|---|
| Table names | `snake_case`, plural | `scheduling_slots`, `medical_exams`, `outbox_messages` |
| Column names | `snake_case` | `instructor_id`, `started_at`, `created_at` |
| Primary key | `id` | `id UUID` |
| Foreign keys | `{entity}_id` | `instructor_id`, `student_id` |
| Timestamps | `created_at`, `updated_at` | UTC, `TIMESTAMPTZ` in PostgreSQL |
| Soft delete | `deleted_at` | nullable `TIMESTAMPTZ` |
| Enum columns | `TEXT` not `INT` | `status TEXT NOT NULL` |
| Boolean columns | `snake_case` prefixed `is_` | `is_active`, `is_verified` |

### Query Rules

```csharp
// Correct — read-only queries
var slots = await _context.SchedulingSlots
    .AsNoTracking()
    .Where(s => s.InstructorId == instructorId && s.StartedAt.Date == date)
    .Select(s => new SlotResult(s.Id, s.StartedAt, s.Status))  // projection, not full entity
    .ToListAsync(cancellationToken);

// Correct — bulk update without loading entities
await _context.SchedulingSlots
    .Where(s => s.StudentId == studentId && s.Status == SlotStatus.Pending)
    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, SlotStatus.Cancelled), ct);

// Wrong — loading entities for a read-only operation
var slots = await _context.SchedulingSlots.ToListAsync();  // no AsNoTracking
var slot = await _context.SchedulingSlots
    .Include(s => s.Instructor)
    .Include(s => s.Instructor.Vehicle)
    .Include(s => s.Instructor.Vehicle.Maintenance) // depth > 2 — use projection
    .FirstAsync(s => s.Id == id);
```

### Migration Rules

```csharp
// Migration class header — mark applied migrations
// [Applied: prod 2025-01-15] [Applied: stg 2025-01-14]
public partial class AddSchedulingExclusionConstraint : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) { ... }
    protected override void Down(MigrationBuilder migrationBuilder) { ... } // always implement Down
}
```

Never modify a migration with `[Applied]` tag. Create a new migration instead.

---

## 7. Redis Key Naming

### Format

```
{env}:{tenant}:{domain}:{resource}:{identifier}:{qualifier?}
```

| `{env}` values | `prod`, `stg`, `dev` |
|---|---|
| `{tenant}` values | tenant slug, e.g., `autoescola_abc` |
| Separator | `:` only — never `/` or `.` |

### Key Registry

```csharp
// src/CFCHub.Infrastructure/Caching/RedisKeys.cs
public static class RedisKeys
{
    // Scheduling locks — TTL 30s
    public static string SchedulingLockInstructor(string env, string tenant, Guid instructorId)
        => $"{env}:{tenant}:sched:lock:instructor:{instructorId}";

    public static string SchedulingLockVehicle(string env, string tenant, Guid vehicleId)
        => $"{env}:{tenant}:sched:lock:vehicle:{vehicleId}";

    public static string SchedulingLockTrack(string env, string tenant, Guid trackId)
        => $"{env}:{tenant}:sched:lock:track:{trackId}";

    // Availability cache — TTL 300s
    public static string InstructorAvailability(string env, string tenant, Guid instructorId, DateOnly date)
        => $"{env}:{tenant}:sched:avail:instructor:{instructorId}:{date:yyyy-MM-dd}";

    // DETRAN cache — TTL 86400s (24h)
    public static string DetranCnhStatus(string env, string tenant, string cpfHash)
        => $"{env}:{tenant}:detran:cnh:{cpfHash}";

    // Rate limiting — TTL per window
    public static string RateLimit(string env, string tenant, string endpointHash, string userId)
        => $"{env}:{tenant}:rl:{endpointHash}:{userId}";

    // Staff session — TTL 3600s
    public static string StaffSession(string env, string tenant, string jti)
        => $"{env}:{tenant}:session:{jti}";

    // Outbox worker lease — TTL 60s
    public static string OutboxWorkerLease(string env, string tenant)
        => $"{env}:{tenant}:outbox:lease";

    // Tenant resolution cache — TTL 300s
    public static string TenantResolution(string env, string slug)
        => $"{env}:global:tenant:{slug}";
}
```

Rules:
- Never build Redis keys inline in business code — always use `RedisKeys`.
- Never store raw CPF in a key. Use `Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cpf)))`.
- Never store PII in Redis values without encryption via `IDataProtectionService`.
- Every Redis write MUST specify a TTL — no persistent keys in application code.

---

## 8. Rate Limiting

Redis sliding window counter via `RateLimitMiddleware`.

| Endpoint Group | Window | Max Requests |
|---|---|---|
| `POST /api/v1/auth/*` | 15 min | 10 |
| `POST/PATCH /api/v1/scheduling/*` | 1 min | 30 |
| `GET /api/v1/scheduling/*` | 1 min | 120 |
| `GET /api/v1/*/detran/*` | 1 min | 5 |
| `GET /api/v1/public/*` | 1 min | 60 |
| All other endpoints | 1 min | 120 |

Response when rate limit exceeded: `429 Too Many Requests` with `Retry-After` header (seconds until window resets).

---

## 9. Error Handling

### Exception Hierarchy

```
CfcHubException (abstract, base)
├── ValidationException          → HTTP 400
├── UnauthorizedException        → HTTP 401
├── ForbiddenException           → HTTP 403
├── NotFoundException            → HTTP 404
│   └── TenantNotFoundException  → HTTP 404
├── ConflictException            → HTTP 409
│   └── SchedulingConflictException → HTTP 409
├── UnprocessableException       → HTTP 422
└── InfrastructureException      → HTTP 500 (details never exposed to client)
    ├── StorageException
    └── EmailDeliveryException
```

```csharp
// Correct usage in domain
public void Cancel(string reason)
{
    if (Status == SlotStatus.Completed)
        throw new UnprocessableException(
            "Completed slots cannot be cancelled.",
            errorCode: "SLOT_ALREADY_COMPLETED");
}

// Correct usage in application — expected failures use Result<T>
public async Task<Result<SlotResult>> Handle(BookSlotCommand command, CancellationToken ct)
{
    var lockAcquired = await _lockService.TryAcquireAsync(...);
    if (!lockAcquired)
        return Result<SlotResult>.Failure(Error.Conflict("SLOT_LOCK_FAILED", "Horário em conflito."));
    // ...
}

// Wrong — catching Exception base class without logging
try { ... }
catch (Exception) { return null; }  // ✗ swallows bugs
```

### GlobalExceptionMiddleware Mapping

```csharp
// Maps exceptions → ProblemDetails. Never expose inner exception details.
private static int MapStatusCode(CfcHubException ex) => ex switch
{
    ValidationException        => 400,
    UnauthorizedException      => 401,
    ForbiddenException         => 403,
    NotFoundException          => 404,
    ConflictException          => 409,
    UnprocessableException     => 422,
    InfrastructureException    => 500,
    _                          => 500
};
```

---

## 10. Logging

Serilog with Seq (dev) and CloudWatch (prod).

```csharp
// Correct — structured, no PII
_logger.LogInformation(
    "Scheduling slot booked. SlotId: {SlotId}, TenantId: {TenantId}, InstructorId: {InstructorId}",
    slot.Id, tenantId, instructorId);

_logger.LogWarning(
    "Redis lock retry. Resource: {Resource}, Attempt: {Attempt}",
    resourceKey, attempt);

_logger.LogError(ex,
    "S3 upload failed. Bucket: {Bucket}, Key: {Key}, TraceId: {TraceId}",
    bucket, key, traceId);

// Wrong — PII in log
_logger.LogInformation("Student {Cpf} booked slot", student.Cpf);  // ✗
_logger.LogInformation("Contract for {Email} generated", student.Email);  // ✗
```

### Sensitive Attribute

```csharp
// DTO with sensitive fields — Serilog destructuring policy scrubs [Sensitive] properties
public record CreateStudentRequest(
    string Name,
    [property: Sensitive] string Cpf,
    [property: Sensitive] string Email,
    [property: Sensitive] string Phone,
    DateOnly BirthDate
);
```

### Log Level Policy

| Level | Use when |
|---|---|
| `Debug` | Development diagnostics — MUST NOT appear in production logs |
| `Information` | Key business events: slot booked, contract generated, payment received |
| `Warning` | Recoverable issues: cache miss forcing DB query, lock retry (up to 3) |
| `Error` | Infrastructure failures, unhandled exceptions |
| `Critical` | Data integrity violations, security events, LGPD breaches |

---

## 11. Background Service Conventions

```csharp
// All workers follow this structure
public sealed class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxWorker> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessOutboxAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        // resolve scoped services here — workers are singletons
        // always propagate CancellationToken to all async calls
    }
}
```

Rules:
- Workers are `sealed` classes.
- Never inject scoped services directly into a worker constructor — use `IServiceScopeFactory`.
- Always propagate `CancellationToken` — never use `CancellationToken.None` in worker code.
- Log each worker cycle start/end at `Debug` level, errors at `Error` level.

---

## 12. Test Conventions

### Builder Pattern

```csharp
// tests/CFCHub.UnitTests/Builders/SchedulingSlotBuilder.cs
public sealed class SchedulingSlotBuilder
{
    private SchedulingSlotId _id = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    private InstructorId _instructorId = new(Guid.NewGuid());
    private DateTimeOffset _startedAt = DateTimeOffset.UtcNow.AddDays(1);

    public SchedulingSlotBuilder WithId(SchedulingSlotId id) { _id = id; return this; }
    public SchedulingSlotBuilder WithInstructor(InstructorId id) { _instructorId = id; return this; }
    public SchedulingSlotBuilder StartingAt(DateTimeOffset at) { _startedAt = at; return this; }

    public SchedulingSlot Build() => SchedulingSlot.Book(_id, _instructorId, ...);
}

// Usage in tests
var slot = new SchedulingSlotBuilder()
    .WithInstructor(instructorId)
    .StartingAt(tomorrow)
    .Build();
```

### Test Naming

```csharp
// Pattern: {MethodUnderTest}_{Scenario}_{ExpectedOutcome}
public void BookSlot_WhenInstructorUnavailable_ThrowsConflictException() { }
public void Cancel_WhenSlotAlreadyCompleted_ThrowsUnprocessableException() { }
public async Task Handle_WhenRedisLockFails_ReturnsConflictResult() { }
```

### Integration Test Isolation

```csharp
public sealed class SchedulingIntegrationTests : IAsyncLifetime
{
    private readonly string _tenantSchema = $"cfc_test_{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync($"CREATE SCHEMA {_tenantSchema}");
        await _migrationService.ApplyMigrationsAsync(_tenantSchema);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync($"DROP SCHEMA {_tenantSchema} CASCADE");
    }
}
```
