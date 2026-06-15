# CFCHub — Project Backlog

> **Agent instructions:** Read `GEMINI.md`, `design.md`, and `conventions.md` in full before starting any task.
> Every task references those files as the authoritative source. Acceptance criteria must all pass before marking a task done.
> Task dependencies are listed as `Depends on: TASK-XXX`. Never start a task before its dependencies are complete.

---

## Task Format

Each task contains:
- **Layer** — which project(s) are touched
- **Description** — what to implement and why
- **Files** — exact paths to create or modify
- **Acceptance Criteria** — verifiable checks an agent or reviewer can run
- **Depends on** — prerequisite task IDs

---

## EPIC 0 — Project Scaffolding

---

### TASK-001 — Create solution structure and project files

**Layer:** All  
**Description:**  
Create the .NET 10 solution with all five projects and two test projects. Use `dotnet new` commands. Add all NuGet package references defined in `GEMINI.md §2`. No source code yet — only solution and project scaffolding.

**Files to create:**
```
CFCHub.sln
src/CFCHub.Api/CFCHub.Api.csproj
src/CFCHub.Application/CFCHub.Application.csproj
src/CFCHub.Domain/CFCHub.Domain.csproj
src/CFCHub.Infrastructure/CFCHub.Infrastructure.csproj
src/CFCHub.Workers/CFCHub.Workers.csproj
tests/CFCHub.UnitTests/CFCHub.UnitTests.csproj
tests/CFCHub.IntegrationTests/CFCHub.IntegrationTests.csproj
.gitignore
.editorconfig
Directory.Build.props
Directory.Packages.props
```

**Package references (use `Directory.Packages.props` for central management):**
- `CFCHub.Domain`: no external dependencies — only `System.*`
- `CFCHub.Application`: `MediatR` 12.x, `FluentValidation` 11.x
- `CFCHub.Infrastructure`: `Microsoft.EntityFrameworkCore` 10.x, `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x, `StackExchange.Redis`, `AWSSDK.S3`, `AWSSDK.SimpleEmail`, `AWSSDK.SecretsManager`, `QuestPDF`
- `CFCHub.Api`: `Microsoft.AspNetCore.Authentication.JwtBearer`, `Serilog.AspNetCore`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`
- `CFCHub.Workers`: same as `CFCHub.Infrastructure` + `OpenTelemetry`
- `CFCHub.UnitTests`: `xunit`, `FluentAssertions`, `NSubstitute`
- `CFCHub.IntegrationTests`: `xunit`, `FluentAssertions`, `NSubstitute`, `Testcontainers.PostgreSql`, `Testcontainers.Redis`

**Project references (enforce strict dependency flow from `GEMINI.md §3`):**
- `CFCHub.Application` → `CFCHub.Domain`
- `CFCHub.Infrastructure` → `CFCHub.Application`, `CFCHub.Domain`
- `CFCHub.Api` → `CFCHub.Application`, `CFCHub.Infrastructure`
- `CFCHub.Workers` → `CFCHub.Application`, `CFCHub.Infrastructure`
- `CFCHub.UnitTests` → `CFCHub.Domain`, `CFCHub.Application`
- `CFCHub.IntegrationTests` → `CFCHub.Api`, `CFCHub.Infrastructure`

**Acceptance Criteria:**
- `dotnet build CFCHub.sln` succeeds with zero errors and zero warnings
- `CFCHub.Domain.csproj` has zero references to `CFCHub.Infrastructure`
- `CFCHub.Application.csproj` has zero references to `CFCHub.Infrastructure`
- `Directory.Packages.props` is the single source of truth for all package versions
- `.editorconfig` enforces `end_of_line = lf`, `indent_size = 4`, `charset = utf-8-bom` for C# files
- `Directory.Build.props` sets `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` for all projects

**Depends on:** —

---

### TASK-002 — Docker and docker-compose for local development

**Layer:** DevOps  
**Description:**  
Create `Dockerfile` for `CFCHub.Api` and `CFCHub.Workers`. Create `docker-compose.yml` for local development with all required services: PostgreSQL 16, Redis 7, Seq (log UI). No secrets in any file — use `.env.example` with documented placeholder values.

**Files to create:**
```
src/CFCHub.Api/Dockerfile
src/CFCHub.Workers/Dockerfile
docker-compose.yml
docker-compose.override.yml
.env.example
```

**`docker-compose.yml` services:**
- `postgres`: `postgres:16-alpine`, port 5432, volume `pgdata`, env `POSTGRES_DB=cfchub`, `POSTGRES_USER=cfchub`, `POSTGRES_PASSWORD` from `.env`
- `redis`: `redis:7-alpine`, port 6379, append-only enabled, volume `redisdata`
- `seq`: `datalust/seq:latest`, port 5341 (UI), port 80 internal, env `ACCEPT_EULA=Y`
- `api`: build from `src/CFCHub.Api/Dockerfile`, depends on `postgres`, `redis`
- `workers`: build from `src/CFCHub.Workers/Dockerfile`, depends on `postgres`, `redis`

**`Dockerfile` rules:**
- Multi-stage build: `sdk:10.0` for build, `aspnet:10.0` for runtime
- Run as non-root user (`dotnet` user)
- No secrets in Dockerfile or docker-compose.yml
- Health check on `/health` endpoint for the `api` service

**`.env.example`:**
```
POSTGRES_PASSWORD=changeme
CFCHUB_ENVIRONMENT=dev
CFCHUB_DB_CONNECTION_STRING=Host=postgres;Port=5432;Database=cfchub;Username=cfchub;Password=changeme
CFCHUB_REDIS_CONNECTION_STRING=redis:6379
CFCHUB_AWS_REGION=us-east-1
CFCHUB_S3_DOCUMENTS_BUCKET=cfchub-dev-documents
CFCHUB_S3_MEDICAL_BUCKET=cfchub-dev-medical
CFCHUB_SES_FROM_ADDRESS=noreply@cfchub.com.br
CFCHUB_JWT_PRIVATE_KEY_ARN=arn:aws:secretsmanager:us-east-1:000000000000:secret:cfchub-dev-jwt-key
CFCHUB_DATA_PROTECTION_KEY_PREFIX=cfchub/dpe/
```

**Acceptance Criteria:**
- `docker compose up -d` starts all services without errors
- PostgreSQL is reachable at `localhost:5432`
- Redis is reachable at `localhost:6379`
- Seq UI is accessible at `http://localhost:5341`
- `docker compose build api` and `docker compose build workers` complete without errors
- No password or secret appears in any versioned file — only `.env.example` with placeholder values

**Depends on:** TASK-001

---

### TASK-003 — Configure Serilog with Seq and CloudWatch sinks

**Layer:** `CFCHub.Api`, `CFCHub.Workers`  
**Description:**  
Configure Serilog in both `Api` and `Workers` projects. Dev environment writes to Seq. Staging and production write to CloudWatch Logs. All log entries must include `traceId`, `tenantId`, `userId`, and `environment` as enrichment properties (see `design.md §11`). Implement the `[Sensitive]` attribute and `SensitiveDataDestructuringPolicy` that scrubs annotated properties from log output.

**Files to create:**
```
src/CFCHub.Api/Logging/SensitiveAttribute.cs
src/CFCHub.Api/Logging/SensitiveDataDestructuringPolicy.cs
src/CFCHub.Api/Logging/LoggingConfiguration.cs
src/CFCHub.Workers/Logging/LoggingConfiguration.cs
```

**Files to modify:**
```
src/CFCHub.Api/Program.cs
src/CFCHub.Workers/Program.cs
```

**Implementation notes:**
- `SensitiveAttribute` is `[AttributeUsage(AttributeTargets.Property)]`
- `SensitiveDataDestructuringPolicy : IDestructuringPolicy` — inspect properties with `[Sensitive]`; replace value with `"[REDACTED]"` in the destructured object
- `LoggingConfiguration.ConfigureSerilog(WebApplicationBuilder builder)` — reads `CFCHUB_ENVIRONMENT` env var to decide sink:
  - `dev` → `WriteTo.Seq("http://seq:5341")`
  - `stg`, `prod` → `WriteTo.AmazonCloudWatch(...)` (log group: `/cfchub/{env}/api`)
- Enrich with `FromLogContext()`, `WithMachineName()`, `WithProperty("environment", env)`
- Minimum log level: `Debug` in dev, `Information` in stg/prod
- `Debug` level MUST be filtered out on stg/prod (`MinimumLevel.Override`)

**Acceptance Criteria:**
- A DTO with `[Sensitive]` on `Cpf` property logs `"cpf": "[REDACTED]"` when destructured via `{@dto}` in a log statement
- Running locally, log entries appear in Seq at `http://localhost:5341`
- Every log entry has `environment`, `machineName` properties
- `Debug`-level logs do not appear in production configuration
- Unit test: `SensitiveDataDestructuringPolicy_WhenPropertyHasSensitiveAttribute_ReplacesValueWithRedacted` passes

**Depends on:** TASK-001, TASK-002

---

### TASK-004 — Configure OpenTelemetry SDK

**Layer:** `CFCHub.Api`, `CFCHub.Workers`  
**Description:**  
Configure OpenTelemetry with auto-instrumentation for ASP.NET Core, EF Core, Redis, and AWS SDK. Export traces to AWS X-Ray in staging/production, and to console/OTLP in development. Define a custom `ActivitySource` for MediatR pipeline tracing.

**Files to create:**
```
src/CFCHub.Api/Telemetry/TelemetryConfiguration.cs
src/CFCHub.Application/Common/Telemetry/AppActivitySource.cs
```

**Files to modify:**
```
src/CFCHub.Api/Program.cs
src/CFCHub.Workers/Program.cs
```

**Implementation notes:**
- `AppActivitySource.Instance` is a static `ActivitySource("CFCHub.Application")` — used by `TracingBehavior` (TASK-065)
- `TelemetryConfiguration.AddCfcHubTelemetry(IServiceCollection services, IConfiguration config)`:
  - `AddOpenTelemetry().WithTracing(...)`:
    - `.AddAspNetCoreInstrumentation(opt => opt.RecordException = true)`
    - `.AddEntityFrameworkCoreInstrumentation()`
    - `.AddRedisInstrumentation()`
    - `.AddAWSInstrumentation()`
    - `.AddSource("CFCHub.Application")`
    - Dev: `.AddConsoleExporter()`
    - Stg/Prod: `.AddAWSXRayExporter()`
- Set `X-Ray` trace ID format via `AWSXRayIdGenerator`

**Acceptance Criteria:**
- `dotnet build` succeeds after changes
- In dev, a request to `GET /health` produces a console-exported trace span with `http.method = GET`
- `AppActivitySource.Instance.HasListeners()` returns `true` at runtime after DI registration
- No hardcoded AWS credentials — SDK uses IAM role / environment credentials

**Depends on:** TASK-001, TASK-002

---

### TASK-005 — GitHub Actions CI pipeline

**Layer:** DevOps  
**Description:**  
Create a GitHub Actions workflow that builds, tests, and enforces quality gates on every push to `main` and every pull request. The pipeline must fail if unit test coverage drops below 90% on `CFCHub.Domain`.

**Files to create:**
```
.github/workflows/ci.yml
.github/workflows/cd-stg.yml
```

**`ci.yml` jobs:**
1. `build` — `dotnet build CFCHub.sln --no-incremental -warnaserror`
2. `unit-tests` — `dotnet test tests/CFCHub.UnitTests --collect:"XPlat Code Coverage"`, report coverage with `reportgenerator`, fail if `CFCHub.Domain` line coverage < 90%
3. `integration-tests` — `dotnet test tests/CFCHub.IntegrationTests` (requires Docker services via `services:` block with `postgres:16` and `redis:7`)
4. `lint` — `dotnet format --verify-no-changes`

**`cd-stg.yml`:** triggers on merge to `main`, builds Docker images, pushes to ECR, updates ECS service. Uses OIDC role assumption (no stored AWS secrets).

**Acceptance Criteria:**
- CI pipeline runs on every PR and push to `main`
- Build step fails if any C# warning is present (`TreatWarningsAsErrors`)
- Coverage gate: pipeline fails if `CFCHub.Domain` coverage < 90%
- Integration tests run against real PostgreSQL 16 and Redis 7 containers, not mocks
- CD pipeline assumes AWS role via OIDC — no `AWS_ACCESS_KEY_ID` secret stored in GitHub

**Depends on:** TASK-001, TASK-002

---

### TASK-006 — Create `docs/error-types.md` error URI registry

**Layer:** Documentation  
**Description:**  
Create the error type URI registry referenced in `conventions.md §5`. Every `ProblemDetails.type` URI used in the API must be registered here. AI agents implementing API endpoints must add their error types to this file.

**Files to create:**
```
docs/error-types.md
```

**Format:**
```markdown
| URI | HTTP Status | Error Code | pt-BR Detail |
|-----|-------------|------------|--------------|
| https://cfchub.com.br/errors/scheduling-conflict | 409 | SCHEDULING_CONFLICT | O instrutor já possui aula agendada neste horário. |
| https://cfchub.com.br/errors/tenant-not-found | 404 | TENANT_NOT_FOUND | Tenant não encontrado. |
...
```

**Pre-populate with all exception types from `conventions.md §9`:**
- `validation-error` (400)
- `unauthorized` (401)
- `forbidden` (403)
- `not-found` (404)
- `tenant-not-found` (404)
- `conflict` (409)
- `scheduling-conflict` (409)
- `unprocessable` (422)
- `internal-error` (500)

**Acceptance Criteria:**
- File exists at `docs/error-types.md`
- All exception types from the hierarchy in `conventions.md §9` are registered
- Each entry has a unique URI following the `https://cfchub.com.br/errors/{kebab-case}` pattern

**Depends on:** TASK-001

---

## EPIC 1 — Domain: Shared Kernel

---

### TASK-007 — Implement base domain primitives

**Layer:** `CFCHub.Domain`  
**Description:**  
Implement the shared kernel base classes that all domain entities and value objects inherit from. These are the foundation for the entire domain model.

**Files to create:**
```
src/CFCHub.Domain/Shared/Entity.cs
src/CFCHub.Domain/Shared/AggregateRoot.cs
src/CFCHub.Domain/Shared/StronglyTypedId.cs
src/CFCHub.Domain/Shared/IDomainEvent.cs
src/CFCHub.Domain/Shared/ValueObject.cs
src/CFCHub.Domain/Shared/ISoftDeletable.cs
src/CFCHub.Domain/Shared/IAuditable.cs
```

**Implementation notes:**
- `Entity<TId>` — has `TId Id`, `IReadOnlyList<IDomainEvent> DomainEvents`, `AddDomainEvent(IDomainEvent)`, `ClearDomainEvents()`; abstract; no public setters; `Equals` based on `Id`
- `AggregateRoot<TId> : Entity<TId>` — marker class for aggregate roots; same as `Entity` but conceptually distinct
- `StronglyTypedId<TValue>` — wraps `TValue Value`; `record struct` for value semantics; implicit conversion to `TValue`; `ToString()` returns `Value.ToString()`
- `IDomainEvent` — empty marker interface; implement `DateTimeOffset OccurredAt { get; }`
- `ValueObject` — abstract; overrides `Equals`/`GetHashCode` via abstract `GetEqualityComponents()`
- `ISoftDeletable` — `DateTimeOffset? DeletedAt { get; }`, `bool IsDeleted => DeletedAt.HasValue`
- `IAuditable` — `DateTimeOffset CreatedAt { get; }`, `DateTimeOffset? UpdatedAt { get; }`

**Acceptance Criteria:**
- `dotnet build` succeeds
- `Entity<TId>` equality: two instances with same `Id` are equal regardless of reference
- `StronglyTypedId<Guid>` implicit cast to `Guid` works
- `AggregateRoot` `AddDomainEvent` appends to the internal list; `ClearDomainEvents` empties it
- Unit test: `Entity_WithSameId_AreEqual` passes
- Unit test: `StronglyTypedId_ImplicitConversion_ReturnsUnderlyingValue` passes
- `CFCHub.Domain` has zero external package references

**Depends on:** TASK-001

---

### TASK-008 — Implement Result<T> and Error types

**Layer:** `CFCHub.Domain`  
**Description:**  
Implement the `Result<T>` type used by Application handlers to return expected failures without throwing exceptions. See `conventions.md §9` for usage pattern.

**Files to create:**
```
src/CFCHub.Domain/Shared/Result.cs
src/CFCHub.Domain/Shared/Error.cs
src/CFCHub.Domain/Shared/PagedResult.cs
```

**Implementation notes:**
- `Result<T>` — `record` with `bool IsSuccess`, `T? Value`, `Error? Error`; static factories `Success(T value)` and `Failure(Error error)`; implicit operators for `T → Result<T>` and `Error → Result<T>`
- `Error` — `record` with `string Code`, `string Description`, `ErrorType Type`; static factories: `Error.NotFound(code, desc)`, `Error.Conflict(...)`, `Error.Validation(...)`, `Error.Unauthorized(...)`, `Error.Forbidden(...)`, `Error.Unexpected(...)`
- `ErrorType` — enum: `NotFound`, `Conflict`, `Validation`, `Unauthorized`, `Forbidden`, `Unexpected`
- `PagedResult<T>` — wraps `IReadOnlyList<T> Items`, `string? NextCursor`, `bool HasMore`, `int Count`

**Acceptance Criteria:**
- `Result<int>.Success(42).IsSuccess` is `true`
- `Result<int>.Failure(Error.Conflict("X", "y")).IsSuccess` is `false`
- `Error.NotFound("STUDENT_NOT_FOUND", "desc").Type == ErrorType.NotFound`
- Implicit operator: `int value = 1; Result<int> r = value; r.IsSuccess == true`
- Unit tests for all factory methods pass

**Depends on:** TASK-007

---

### TASK-009 — Implement ISystemClock, IIdGenerator and domain exception hierarchy

**Layer:** `CFCHub.Domain`  
**Description:**  
Implement infrastructure abstraction interfaces that domain code uses for time and ID generation (testability), plus the full custom exception hierarchy. See `conventions.md §9` and `GEMINI.md §10` (forbidden: `DateTime.UtcNow` and `Guid.NewGuid()` directly in domain).

**Files to create:**
```
src/CFCHub.Domain/Shared/ISystemClock.cs
src/CFCHub.Domain/Shared/IIdGenerator.cs
src/CFCHub.Domain/Shared/Exceptions/CfcHubException.cs
src/CFCHub.Domain/Shared/Exceptions/ValidationException.cs
src/CFCHub.Domain/Shared/Exceptions/UnauthorizedException.cs
src/CFCHub.Domain/Shared/Exceptions/ForbiddenException.cs
src/CFCHub.Domain/Shared/Exceptions/NotFoundException.cs
src/CFCHub.Domain/Shared/Exceptions/TenantNotFoundException.cs
src/CFCHub.Domain/Shared/Exceptions/ConflictException.cs
src/CFCHub.Domain/Shared/Exceptions/SchedulingConflictException.cs
src/CFCHub.Domain/Shared/Exceptions/UnprocessableException.cs
src/CFCHub.Domain/Shared/Exceptions/InfrastructureException.cs
src/CFCHub.Domain/Shared/Exceptions/StorageException.cs
src/CFCHub.Domain/Shared/Exceptions/EmailDeliveryException.cs
```

**Implementation notes:**
- `ISystemClock` — `DateTimeOffset UtcNow { get; }`
- `IIdGenerator` — `TId NewId<TId>() where TId : StronglyTypedId<Guid>` — implementations call `Guid.NewGuid()` internally; domain code always calls this interface
- `CfcHubException(string message, string errorCode)` — abstract; has `string ErrorCode` property
- Each concrete exception matches the HTTP status from `conventions.md §9`; they carry `ErrorCode` for the `ProblemDetails.type` URI suffix

**Acceptance Criteria:**
- `SchedulingConflictException` is-a `ConflictException` is-a `CfcHubException`
- `StorageException` is-a `InfrastructureException` is-a `CfcHubException`
- All exception classes have at least a `(string message, string errorCode)` constructor
- `ISystemClock` and `IIdGenerator` interfaces are in `CFCHub.Domain`, not `CFCHub.Infrastructure`
- Unit test: exception hierarchy tree matches `conventions.md §9` exactly

**Depends on:** TASK-007

---

### TASK-010 — Implement cursor-based pagination infrastructure

**Layer:** `CFCHub.Domain`, `CFCHub.Application`  
**Description:**  
Implement cursor encoding/decoding and query base types used by all list endpoints. Offset pagination is forbidden (`GEMINI.md §10`).

**Files to create:**
```
src/CFCHub.Domain/Shared/Pagination/Cursor.cs
src/CFCHub.Application/Common/Pagination/CursorEncoder.cs
src/CFCHub.Application/Common/Pagination/PaginatedQuery.cs
```

**Implementation notes:**
- `Cursor` — `record` with `Guid Id` and `DateTimeOffset Timestamp`; `static Cursor Parse(string encoded)` decodes base64 JSON; `string Encode()` encodes to base64 JSON; handle malformed cursor as `ValidationException`
- `CursorEncoder` — static helper: `string Encode(Guid id, DateTimeOffset ts)` and `Cursor Decode(string encoded)`
- `PaginatedQuery<TResult>` — base record: `string? Cursor`, `int Limit = 20`; `Limit` clamped to max 100 by FluentValidation in each query's validator

**Acceptance Criteria:**
- `CursorEncoder.Encode(id, ts)` produces a base64 string
- `CursorEncoder.Decode(encoded)` round-trips back to same `id` and `ts`
- Malformed cursor string throws `ValidationException` with error code `INVALID_CURSOR`
- `Limit > 100` returns `ValidationException` in query validators
- Unit tests: encode/decode round-trip, malformed cursor handling

**Depends on:** TASK-008, TASK-009

---

## EPIC 2 — Domain: Identity Module

---

### TASK-011 — Implement Identity module domain entities

**Layer:** `CFCHub.Domain`  
**Description:**  
Implement the Identity module entities: `StaffUser`, `Role`, and `Permission`. These represent the staff members of a CFC with role-based access. Field-level access control is enforced via `FieldAccessPolicy` (see `design.md §2`).

**Files to create:**
```
src/CFCHub.Domain/Identity/StaffUser.cs
src/CFCHub.Domain/Identity/StaffUserId.cs
src/CFCHub.Domain/Identity/StaffUserStatus.cs
src/CFCHub.Domain/Identity/Role.cs
src/CFCHub.Domain/Identity/RoleType.cs
src/CFCHub.Domain/Identity/Permission.cs
src/CFCHub.Domain/Identity/PermissionType.cs
src/CFCHub.Domain/Identity/IStaffUserRepository.cs
src/CFCHub.Domain/Identity/Events/StaffUserCreatedEvent.cs
src/CFCHub.Domain/Identity/Events/StaffUserRoleChangedEvent.cs
```

**Implementation notes:**
- `StaffUser : AggregateRoot<StaffUserId>` — properties: `string Name`, `string Email` (encrypted), `string PasswordHash`, `RoleType Role`, `StaffUserStatus Status`, `DateTimeOffset? LastLoginAt`; methods: `static Create(...)`, `ChangeRole(RoleType newRole)`, `RecordLogin(ISystemClock)`, `Deactivate()`
- `StaffUserStatus` enum: `Active`, `Inactive`, `Locked`
- `RoleType` enum: `Admin`, `Receptionist`, `Instructor`, `Financial`
- `PermissionType` enum: values covering read/write per module (e.g., `ReadStudents`, `WriteStudents`, `ReadFinance`, `WriteFinance`, `ManageInstructors`, etc.)
- `IStaffUserRepository` — `GetByIdAsync`, `GetByEmailAsync`, `AddAsync`, `UpdateAsync`, cursor-paginated `ListAsync`
- Password hash: `StaffUser` stores hash only — never plain text; hashing happens in `Application` layer via `IPasswordHasher`

**Acceptance Criteria:**
- `StaffUser.Create(...)` raises `StaffUserCreatedEvent`
- `StaffUser.ChangeRole(...)` raises `StaffUserRoleChangedEvent`
- `StaffUser` does not store plain text password — only `PasswordHash`
- `StaffUser.Email` is marked for encryption (no plaintext column — verified in TASK-043)
- Unit tests: `Create_WithValidData_RaisesCreatedEvent`, `ChangeRole_WhenUserInactive_ThrowsUnprocessableException`

**Depends on:** TASK-007, TASK-008, TASK-009

---

### TASK-012 — Implement FieldAccessPolicy domain service

**Layer:** `CFCHub.Domain`  
**Description:**  
Implement `FieldAccessPolicy` which enforces which fields a given role can read or write. This is the LGPD field-level access control mechanism referenced in `GEMINI.md §6.6`.

**Files to create:**
```
src/CFCHub.Domain/Identity/FieldAccessPolicy.cs
src/CFCHub.Domain/Identity/IFieldAccessPolicyService.cs
src/CFCHub.Domain/Identity/FieldAccess.cs
```

**Implementation notes:**
- `FieldAccess` — enum: `Allowed`, `Denied`
- `FieldAccessPolicy` — value object; encapsulates a dictionary of `(RoleType, string fieldName) → FieldAccess`; built from a static configuration (not DB-driven); method: `FieldAccess Check(RoleType role, string fieldName)`
- Hardcoded policy for initial version:
  - `Receptionist`: can read `Student.Name`, `Student.Email`, `Student.Phone`; cannot read `Student.Cpf`, `Student.Rg`, `MedicalExam.*`
  - `Instructor`: can read `Student.Name`; no financial or medical data
  - `Financial`: can read all finance fields; cannot read medical data
  - `Admin`: full access to all fields
- `IFieldAccessPolicyService` — `FieldAccess CheckAccess(RoleType role, string fieldName)` — Application layer uses this before exposing fields in query results

**Acceptance Criteria:**
- `Receptionist` role cannot access `Student.Cpf` — `Check(Receptionist, "Cpf") == Denied`
- `Admin` role can access all fields
- `Financial` role cannot access `MedicalExam` fields
- Unit tests: `CheckAccess_Receptionist_CannotReadCpf`, `CheckAccess_Admin_CanReadAllFields`

**Depends on:** TASK-011

---

## EPIC 3 — Domain: Scheduling Module

---

### TASK-013 — Implement Instructor, Vehicle, Track domain entities

**Layer:** `CFCHub.Domain`  
**Description:**  
Implement the three resource entities of the Scheduling module. These represent the constrained resources that must be simultaneously available for a slot to be bookable (see `design.md §4`).

**Files to create:**
```
src/CFCHub.Domain/Scheduling/Instructor.cs
src/CFCHub.Domain/Scheduling/InstructorId.cs
src/CFCHub.Domain/Scheduling/InstructorAvailabilityTemplate.cs
src/CFCHub.Domain/Scheduling/DayAvailabilityOverride.cs
src/CFCHub.Domain/Scheduling/Vehicle.cs
src/CFCHub.Domain/Scheduling/VehicleId.cs
src/CFCHub.Domain/Scheduling/VehicleStatus.cs
src/CFCHub.Domain/Scheduling/Track.cs
src/CFCHub.Domain/Scheduling/TrackId.cs
src/CFCHub.Domain/Scheduling/TrackType.cs
src/CFCHub.Domain/Scheduling/CnhCategory.cs
```

**Implementation notes:**
- `Instructor : AggregateRoot<InstructorId>` — `StaffUserId LinkedUserId`, `string Name`, `IReadOnlyList<CnhCategory> TeachableCategories`, `int MaxDailySlots`, `InstructorAvailabilityTemplate WeeklyTemplate`; methods: `SetAvailabilityTemplate(...)`, `AddDayOverride(DayAvailabilityOverride)`, `IsAvailableAt(DateTimeOffset, ISystemClock)`
- `InstructorAvailabilityTemplate` — value object: list of `(DayOfWeek, TimeOnly Start, TimeOnly End)` windows
- `DayAvailabilityOverride` — value object: specific date override (available: false for vacations, or custom windows)
- `Vehicle : AggregateRoot<VehicleId>` — `string LicensePlate`, `CnhCategory Category`, `VehicleStatus Status`, `DateTimeOffset? MaintenanceUntil`; method: `IsAvailableAt(DateTimeOffset)`
- `VehicleStatus` enum: `Active`, `InMaintenance`, `Retired`
- `Track : AggregateRoot<TrackId>` — `string Name`, `TrackType Type`; `TrackType` enum: `Maneuver`, `Road`, `Highway`
- `CnhCategory` enum: `A`, `B`, `C`, `D`, `E`, `ACC`
- Cross-module note: `Instructor.LinkedUserId` is a `StaffUserId` — only the ID, never the entity

**Acceptance Criteria:**
- `Vehicle.IsAvailableAt(time)` returns `false` when `MaintenanceUntil > time`
- `Instructor.IsAvailableAt(time)` returns `false` for times outside `WeeklyTemplate` windows
- `DayAvailabilityOverride` with `Available = false` overrides the weekly template
- Unit tests: `IsAvailableAt_WhenInMaintenance_ReturnsFalse`, `IsAvailableAt_WithDayOverride_ReturnsFalse`

**Depends on:** TASK-007, TASK-009

---

### TASK-014 — Implement SchedulingSlot aggregate

**Layer:** `CFCHub.Domain`  
**Description:**  
Implement the `SchedulingSlot` aggregate — the core domain object of CFCHub. This is a P0 component. Enforce all business rules in the entity itself. Slot duration is always 50 minutes (see `design.md §4`).

**Files to create:**
```
src/CFCHub.Domain/Scheduling/SchedulingSlot.cs
src/CFCHub.Domain/Scheduling/SchedulingSlotId.cs
src/CFCHub.Domain/Scheduling/SlotStatus.cs
src/CFCHub.Domain/Scheduling/Events/SchedulingSlotBookedEvent.cs
src/CFCHub.Domain/Scheduling/Events/SchedulingSlotCancelledEvent.cs
src/CFCHub.Domain/Scheduling/Events/SchedulingSlotCompletedEvent.cs
```

**Implementation notes:**
- `SchedulingSlot : AggregateRoot<SchedulingSlotId>` — properties: `InstructorId InstructorId`, `VehicleId VehicleId`, `TrackId TrackId`, `StudentId StudentId` (cross-module: ID only), `CnhCategory Category`, `DateTimeOffset StartedAt`, `DateTimeOffset EndedAt` (always `StartedAt + 50 min`), `SlotStatus Status`, `string? CancellationReason`
- `SlotStatus` enum: `Confirmed`, `Cancelled`, `Completed`, `NoShow`
- Static factory `Book(id, instructorId, vehicleId, trackId, studentId, startedAt, category, ISystemClock)`:
  - Guard: `startedAt < clock.UtcNow` → throw `UnprocessableException("SLOT_IN_PAST")`
  - Guard: `startedAt` not on a 50-minute boundary (slots are :00 and :50) → `UnprocessableException("INVALID_SLOT_TIME")`
  - Sets `EndedAt = StartedAt.AddMinutes(50)`
  - Raises `SchedulingSlotBookedEvent`
- `Cancel(string reason)` — guard: `Status == Completed` → `UnprocessableException("SLOT_ALREADY_COMPLETED")`; sets `Status = Cancelled`; raises `SchedulingSlotCancelledEvent`
- `Complete()` — guard: `Status != Confirmed` → `UnprocessableException`; sets `Status = Completed`; raises `SchedulingSlotCompletedEvent`
- `MarkNoShow()` — guard: `Status != Confirmed`; sets `Status = NoShow`

**Acceptance Criteria:**
- `Book(...)` with past `startedAt` throws `UnprocessableException` with code `SLOT_IN_PAST`
- `Cancel(...)` after `Complete()` throws `UnprocessableException` with code `SLOT_ALREADY_COMPLETED`
- `EndedAt` is always exactly `StartedAt + 50 minutes`
- `Book(...)` raises exactly one `SchedulingSlotBookedEvent`
- Unit tests: `Book_WithPastTime_ThrowsUnprocessable`, `Cancel_WhenCompleted_ThrowsUnprocessable`, `Book_RaisesSlotBookedEvent`, `Complete_WhenConfirmed_SetsStatusCompleted`

**Depends on:** TASK-013

---

### TASK-015 — Implement Scheduling module repository and service interfaces

**Layer:** `CFCHub.Domain`  
**Description:**  
Define the interfaces the Application layer depends on for scheduling operations. These are implemented in `CFCHub.Infrastructure` but owned by `CFCHub.Domain`.

**Files to create:**
```
src/CFCHub.Domain/Scheduling/ISchedulingRepository.cs
src/CFCHub.Domain/Scheduling/ISchedulingLockService.cs
src/CFCHub.Domain/Scheduling/IAvailabilityCalculatorService.cs
src/CFCHub.Domain/Scheduling/AvailableSlot.cs
src/CFCHub.Domain/Scheduling/Specifications/SlotOverlapSpec.cs
```

**Implementation notes:**
- `ISchedulingRepository`:
  - `GetSlotByIdAsync(SchedulingSlotId id, CancellationToken ct)`
  - `GetOverlappingSlotAsync(InstructorId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)` — for SELECT FOR UPDATE pre-check
  - `AddAsync(SchedulingSlot slot, CancellationToken ct)`
  - `GetByInstructorAsync(InstructorId, DateOnly date, CancellationToken ct)` — returns projections
  - `GetByStudentAsync(StudentId, string? cursor, int limit, CancellationToken ct)` — cursor paginated
- `ISchedulingLockService` — `TryAcquireAsync(string key, CancellationToken ct)`, `ReleaseAsync(string key, CancellationToken ct)`, `AcquireAllAsync(IEnumerable<string> keys, CancellationToken ct)` (acquires in deterministic order, releases all on failure)
- `IAvailabilityCalculatorService` — `GetAvailableSlotsAsync(DateOnly date, CnhCategory category, InstructorId? instructorId, string? cursor, int limit, CancellationToken ct)`
- `AvailableSlot` — record: `DateTimeOffset StartedAt`, `InstructorId InstructorId`, `VehicleId VehicleId`, `TrackId TrackId`
- `SlotOverlapSpec` — specification: `bool IsSatisfiedBy(SchedulingSlot existing, DateTimeOffset newStart)` — checks `[startedAt, endedAt)` overlap

**Acceptance Criteria:**
- All interfaces in `CFCHub.Domain` — no `Infrastructure` references
- `SlotOverlapSpec` correctly identifies overlap and non-overlap for boundary cases
- Unit test: `SlotOverlapSpec_WithAdjacentSlots_ReturnsFalse`, `SlotOverlapSpec_WithOverlappingSlots_ReturnsTrue`

**Depends on:** TASK-014

---

## EPIC 4 — Domain: Enrollment Module

---

### TASK-016 — Implement Student aggregate

**Layer:** `CFCHub.Domain`  
**Description:**  
Implement the `Student` aggregate. This entity contains LGPD-sensitive personal data. All PII fields must be marked for encryption in EF configuration (TASK-045). Soft delete only — hard delete requires a `DataErasureRequest`.

**Files to create:**
```
src/CFCHub.Domain/Enrollment/Student.cs
src/CFCHub.Domain/Enrollment/StudentId.cs
src/CFCHub.Domain/Enrollment/StudentStatus.cs
src/CFCHub.Domain/Enrollment/Address.cs
src/CFCHub.Domain/Enrollment/Events/StudentCreatedEvent.cs
src/CFCHub.Domain/Enrollment/Events/StudentAnonymizedEvent.cs
src/CFCHub.Domain/Enrollment/IStudentRepository.cs
```

**Implementation notes:**
- `Student : AggregateRoot<StudentId>` implements `ISoftDeletable`, `IAuditable`
- PII fields (will be AES-256-GCM encrypted via EF value converter): `string Name`, `string Cpf`, `string? Rg`, `string Email`, `string Phone`, `DateOnly BirthDate`, `Address HomeAddress`
- `Address` — value object: `string Street`, `string Number`, `string? Complement`, `string District`, `string City`, `string State`, `string ZipCode`
- `StudentStatus` enum: `Active`, `Inactive`, `PendingErasure`
- `static Create(id, name, cpf, email, phone, birthDate, address, ISystemClock, IIdGenerator)` — validates CPF format (11 digits); raises `StudentCreatedEvent`
- `Anonymize()` — sets `Name = "[REMOVIDO]"`, `Cpf = SHA256(cpf)`, `Email = "[REMOVIDO]"`, `Phone = "[REMOVIDO]"`, `HomeAddress = Address.Empty`; raises `StudentAnonymizedEvent`; cannot be called if `Status != PendingErasure`
- `SoftDelete()` — sets `DeletedAt = clock.UtcNow`
- `IStudentRepository` — `GetByIdAsync`, `GetByCpfAsync`, `AddAsync`, `UpdateAsync`, cursor-paginated `ListAsync`

**Acceptance Criteria:**
- `Create` with invalid CPF format (not 11 digits) throws `ValidationException`
- `Anonymize()` when `Status != PendingErasure` throws `UnprocessableException`
- After `Anonymize()`, `Cpf` is a SHA-256 hex string (64 chars), not the original value
- `Student` implements `ISoftDeletable` — no hard-delete method exists
- Unit tests: `Create_WithInvalidCpf_ThrowsValidation`, `Anonymize_WhenNotPendingErasure_ThrowsUnprocessable`, `Anonymize_SetsCpfToHash`

**Depends on:** TASK-007, TASK-008, TASK-009

---

### TASK-017 — Implement Enrollment aggregate and ConsentRecord entity

**Layer:** `CFCHub.Domain`  
**Description:**  
Implement `Enrollment` (the student's enrollment in a CNH category course) and `ConsentRecord` (immutable LGPD consent capture, see `design.md §6`). `ConsentRecord` has no update method by design.

**Files to create:**
```
src/CFCHub.Domain/Enrollment/Enrollment.cs
src/CFCHub.Domain/Enrollment/EnrollmentId.cs
src/CFCHub.Domain/Enrollment/EnrollmentStatus.cs
src/CFCHub.Domain/Enrollment/ConsentRecord.cs
src/CFCHub.Domain/Enrollment/ConsentRecordId.cs
src/CFCHub.Domain/Enrollment/ConsentChannel.cs
src/CFCHub.Domain/Enrollment/Events/StudentEnrolledEvent.cs
src/CFCHub.Domain/Enrollment/Events/EnrollmentCompletedEvent.cs
src/CFCHub.Domain/Enrollment/IEnrollmentRepository.cs
```

**Implementation notes:**
- `Enrollment : AggregateRoot<EnrollmentId>` implements `ISoftDeletable`, `IAuditable`
- Properties: `StudentId StudentId`, `CnhCategory Category`, `EnrollmentStatus Status`, `int TheoryHoursCompleted`, `int PracticalHoursCompleted`, `DateTimeOffset? CompletedAt`
- `EnrollmentStatus` enum: `Active`, `Suspended`, `Completed`, `Cancelled`
- `static Enroll(id, studentId, category, ISystemClock)` — raises `StudentEnrolledEvent`
- `IncrementPracticalHours()` — called when `SchedulingSlotCompleted` event is received; increments counter; if threshold reached raises `EnrollmentCompletedEvent`
- `Complete()` — sets `Status = Completed`, `CompletedAt = clock.UtcNow`; raises `EnrollmentCompletedEvent`
- `ConsentRecord : Entity<ConsentRecordId>` — all properties `private init` (immutable); `static Capture(id, studentId, policyVersion, policyContentHash, consentedAt, ipAddress, userAgent, channel)` — the only way to create; no update method; no domain events needed (captured atomically with enrollment)
- `ConsentChannel` enum: `Web`, `App`, `Paper`
- `IEnrollmentRepository` — `GetByIdAsync`, `GetByStudentIdAsync`, `AddAsync`, cursor-paginated `ListAsync`

**Acceptance Criteria:**
- `ConsentRecord` has no `Update`, `Change`, or setter methods
- `Enrollment.Enroll(...)` raises exactly one `StudentEnrolledEvent`
- `Enrollment.IncrementPracticalHours()` does not raise `EnrollmentCompletedEvent` below the category threshold
- Soft-delete implemented: no hard-delete method
- Unit tests: `Enroll_RaisesStudentEnrolledEvent`, `ConsentRecord_IsImmutable_NoUpdateMethod`, `IncrementPracticalHours_WhenBelowThreshold_DoesNotComplete`

**Depends on:** TASK-016

---

## EPIC 5 — Domain: Contracts Module

---

### TASK-018 — Implement Contract aggregate and SignatureRecord

**Layer:** `CFCHub.Domain`  
**Description:**  
Implement the `Contract` aggregate which represents the legally binding enrollment contract. Generated via the outbox when `StudentEnrolled` event fires.

**Files to create:**
```
src/CFCHub.Domain/Contracts/Contract.cs
src/CFCHub.Domain/Contracts/ContractId.cs
src/CFCHub.Domain/Contracts/ContractStatus.cs
src/CFCHub.Domain/Contracts/SignatureRecord.cs
src/CFCHub.Domain/Contracts/SignatureRecordId.cs
src/CFCHub.Domain/Contracts/Events/ContractGenerationRequestedEvent.cs
src/CFCHub.Domain/Contracts/Events/ContractSignedEvent.cs
src/CFCHub.Domain/Contracts/IContractRepository.cs
```

**Implementation notes:**
- `Contract : AggregateRoot<ContractId>` — properties: `StudentId StudentId`, `EnrollmentId EnrollmentId`, `ContractStatus Status`, `string? S3Key` (set when PDF is generated), `DateTimeOffset? SignedAt`, `string? TemplateKey`
- `ContractStatus` enum: `Pending`, `Generated`, `Signed`, `Cancelled`
- `static Create(id, studentId, enrollmentId, templateKey, ISystemClock)` — `Status = Pending`; raises `ContractGenerationRequestedEvent` (goes to outbox → PDF generation)
- `MarkGenerated(string s3Key)` — called by `ContractGenerationHandler`; sets `S3Key`, `Status = Generated`
- `Sign(SignatureRecord signature, ISystemClock)` — guard: `Status != Generated`; attaches `SignatureRecord`; raises `ContractSignedEvent`
- `SignatureRecord : Entity<SignatureRecordId>` — `ContractId ContractId`, `string SignatureHash`, `string IpAddress`, `DateTimeOffset SignedAt`; no update method
- `IContractRepository` — `GetByIdAsync`, `GetByStudentIdAsync`, `AddAsync`, `UpdateAsync`

**Acceptance Criteria:**
- `Create(...)` raises `ContractGenerationRequestedEvent`
- `Sign(...)` when `Status == Pending` throws `UnprocessableException`
- `Sign(...)` when `Status == Generated` raises `ContractSignedEvent`
- `SignatureRecord` has no update method
- Unit tests: `Create_RaisesContractGenerationRequested`, `Sign_WhenPending_ThrowsUnprocessable`, `Sign_WhenGenerated_RaisesContractSigned`

**Depends on:** TASK-017

---

## EPIC 6 — Domain: Finance Module

---

### TASK-019 — Implement Payment, Installment, and Invoice domain entities

**Layer:** `CFCHub.Domain`  
**Description:**  
Implement the Finance module domain objects. A `Payment` is a single financial transaction; an `Installment` is a planned payment in a payment plan; an `Invoice` tracks overdue status.

**Files to create:**
```
src/CFCHub.Domain/Finance/Payment.cs
src/CFCHub.Domain/Finance/PaymentId.cs
src/CFCHub.Domain/Finance/PaymentStatus.cs
src/CFCHub.Domain/Finance/PaymentMethod.cs
src/CFCHub.Domain/Finance/Installment.cs
src/CFCHub.Domain/Finance/InstallmentId.cs
src/CFCHub.Domain/Finance/InstallmentStatus.cs
src/CFCHub.Domain/Finance/Invoice.cs
src/CFCHub.Domain/Finance/InvoiceId.cs
src/CFCHub.Domain/Finance/Money.cs
src/CFCHub.Domain/Finance/Events/PaymentReceivedEvent.cs
src/CFCHub.Domain/Finance/Events/InvoiceOverdueEvent.cs
src/CFCHub.Domain/Finance/IPaymentRepository.cs
```

**Implementation notes:**
- `Money` — value object: `decimal Amount`, `string Currency = "BRL"`; operators for `+`, `-`, comparison; guard: `Amount < 0` throws `UnprocessableException`
- `Payment : AggregateRoot<PaymentId>` implements `IAuditable` — `StudentId StudentId`, `EnrollmentId EnrollmentId`, `Money Amount`, `PaymentStatus Status`, `PaymentMethod Method`, `DateTimeOffset? PaidAt`, `string? ReceiptS3Key`
- `PaymentStatus` enum: `Pending`, `Confirmed`, `Refunded`, `Cancelled`
- `PaymentMethod` enum: `Pix`, `CreditCard`, `BankSlip`, `Cash`
- `static Create(...)`, `Confirm(ISystemClock)` → raises `PaymentReceivedEvent`, `Refund(string reason)` → guard: only `Confirmed` can be refunded
- `Installment : Entity<InstallmentId>` — `EnrollmentId EnrollmentId`, `Money Amount`, `DateOnly DueDate`, `InstallmentStatus Status`; `MarkPaid(PaymentId paymentId)`, `MarkOverdue()` → raises `InvoiceOverdueEvent`
- `InstallmentStatus` enum: `Pending`, `Paid`, `Overdue`
- `IPaymentRepository` — `GetByIdAsync`, `GetByStudentIdAsync`, `AddAsync`, `UpdateAsync`, cursor-paginated `ListAsync`

**Acceptance Criteria:**
- `Money` with negative amount throws `UnprocessableException`
- `Payment.Refund(...)` when `Status != Confirmed` throws `UnprocessableException`
- `Payment.Confirm(...)` raises `PaymentReceivedEvent`
- `Installment.MarkOverdue()` raises `InvoiceOverdueEvent`
- Unit tests: `Money_NegativeAmount_ThrowsUnprocessable`, `Payment_Refund_WhenPending_ThrowsUnprocessable`, `Payment_Confirm_RaisesPaymentReceived`

**Depends on:** TASK-017

---

## EPIC 7 — Domain: Compliance Module

---

### TASK-020 — Implement Compliance module domain entities

**Layer:** `CFCHub.Domain`  
**Description:**  
Implement the Compliance module which tracks student document expiry dates (e.g., medical exam validity, CNH renewal deadlines) and triggers alerts. See `design.md §8`.

**Files to create:**
```
src/CFCHub.Domain/Compliance/DocumentRecord.cs
src/CFCHub.Domain/Compliance/DocumentRecordId.cs
src/CFCHub.Domain/Compliance/DocumentType.cs
src/CFCHub.Domain/Compliance/DocumentExpiryAlert.cs
src/CFCHub.Domain/Compliance/DocumentExpiryAlertId.cs
src/CFCHub.Domain/Compliance/AlertTier.cs
src/CFCHub.Domain/Compliance/Events/DocumentExpiryAlertRequestedEvent.cs
src/CFCHub.Domain/Compliance/IDocumentRepository.cs
```

**Implementation notes:**
- `DocumentRecord : AggregateRoot<DocumentRecordId>` implements `IAuditable` — `StudentId StudentId`, `DocumentType Type`, `DateOnly ExpiryDate`, `string? S3Key` (for uploaded medical PDFs), `DateTimeOffset? LastAlertSentAt`
- `DocumentType` enum: `MedicalExam`, `CnhRenewal`, `VehicleInspection`, `CourseCompletion`
- `MarkAlertSent(AlertTier tier, ISystemClock)` — updates `LastAlertSentAt = clock.UtcNow`; raises `DocumentExpiryAlertRequestedEvent`
- Guard in `MarkAlertSent`: if `LastAlertSentAt` was within 24h, throw `UnprocessableException("ALERT_ALREADY_SENT_TODAY")` — idempotency guard from `design.md §8`
- `AlertTier` enum: `D30`, `D15`, `D7`, `D1`
- `IDocumentRepository` — `GetExpiringAsync(DateOnly from, DateOnly to, CancellationToken ct)`, `GetByStudentIdAsync`, `AddAsync`, `UpdateAsync`

**Acceptance Criteria:**
- `MarkAlertSent` within 24h of previous alert throws `UnprocessableException("ALERT_ALREADY_SENT_TODAY")`
- `MarkAlertSent` raises `DocumentExpiryAlertRequestedEvent`
- `IDocumentRepository.GetExpiringAsync` signature accepts date range only (not an alert tier)
- Unit tests: `MarkAlertSent_WhenSentToday_ThrowsUnprocessable`, `MarkAlertSent_RaisesEvent`

**Depends on:** TASK-016

---

## EPIC 8 — Infrastructure: Persistence Foundation

---

### TASK-021 — Implement ITenantContext and TenantContext

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement the tenant context abstraction. `ITenantContext` is what `AppDbContext` and repositories use to scope queries. `TenantContext` is a scoped service populated by `TenantResolutionMiddleware`.

**Files to create:**
```
src/CFCHub.Infrastructure/Persistence/ITenantContext.cs
src/CFCHub.Infrastructure/Persistence/TenantContext.cs
```

**Implementation notes:**
- `ITenantContext` (in `CFCHub.Domain/Shared` or `CFCHub.Application/Common`) — `string SchemaName { get; }`, `string TenantSlug { get; }`, `Guid TenantId { get; }`, `bool IsResolved { get; }`
- `TenantContext : ITenantContext` — mutable; `Resolve(string schemaName, string slug, Guid tenantId)` sets values and `IsResolved = true`
- Registered as `Scoped` in DI
- Note: `ITenantContext` interface belongs in `CFCHub.Application` (not `Infrastructure`) to avoid the `Domain → Infrastructure` dependency inversion violation

**Acceptance Criteria:**
- `TenantContext.IsResolved` is `false` before `Resolve(...)` is called
- `AppDbContext` (TASK-022) uses `ITenantContext.SchemaName` to set `search_path`
- Accessing `SchemaName` when `IsResolved == false` throws `InfrastructureException("TENANT_NOT_RESOLVED")`
- Registered as `Scoped` — new instance per HTTP request

**Depends on:** TASK-009

---

### TASK-022 — Implement AppDbContext

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement the EF Core `DbContext` with schema-per-tenant multi-tenancy. The schema is set at construction time via `ITenantContext`. Global query filters enforce soft deletes. All entity configurations are applied from assembly scanning.

**Files to create:**
```
src/CFCHub.Infrastructure/Persistence/AppDbContext.cs
src/CFCHub.Infrastructure/Persistence/AppDbContextFactory.cs
```

**Implementation notes:**
- `AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)` — stores `tenantContext`
- `OnModelCreating`: `modelBuilder.HasDefaultSchema(tenantContext.SchemaName)`; `modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)`; apply global soft-delete query filter for all `ISoftDeletable` entity types
- `DbSet<>` for all entities: `SchedulingSlots`, `Instructors`, `Vehicles`, `Tracks`, `Students`, `Enrollments`, `ConsentRecords`, `Contracts`, `Payments`, `Installments`, `DocumentRecords`, `OutboxMessages`, `AuditLogs`, `StaffUsers`, `DataErasureRequests`
- `AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>` — for EF tooling (migration generation); creates a context targeting `__template` schema with connection string from env var `CFCHUB_DB_CONNECTION_STRING`
- Override `SaveChangesAsync` to set `CreatedAt`/`UpdatedAt` on `IAuditable` entities

**Acceptance Criteria:**
- `OnModelCreating` sets `HasDefaultSchema` to the tenant's schema name, not hardcoded
- Global query filter: `ISoftDeletable` entities with `DeletedAt != null` are excluded automatically
- `SaveChangesAsync` sets `CreatedAt` on new `IAuditable` entities and `UpdatedAt` on modified ones
- `AppDbContextFactory` can be used for `dotnet ef migrations add` without running the API

**Depends on:** TASK-021, TASK-007

---

### TASK-023 — Implement AuditInterceptor

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement the EF Core save-changes interceptor that writes to `audit_logs` for every Create/Update/Delete on audited entities. This is a LGPD mandate — disabling it is a critical security defect (see `GEMINI.md §6.2`).

**Files to create:**
```
src/CFCHub.Infrastructure/Persistence/Interceptors/AuditInterceptor.cs
```

**Implementation notes:**
- `AuditInterceptor : SaveChangesInterceptor` — override `SavingChangesAsync`
- Audited entity types: `Student`, `MedicalExam`-related docs, `Contract`, `Payment`, `Enrollment`
- For each tracked entry in `Added`, `Modified`, `Deleted` states on audited types:
  - Build `AuditLog` record: `occurred_at`, `actor_user_id` (from `ITenantContext` + `ICurrentUserService`), `actor_role`, `action` (Created/Updated/Deleted), `entity_type` (type name), `entity_id` (primary key), `changed_fields` (JSONB: `{fieldName: {from: oldVal, to: newVal}}`), `ip_address`, `user_agent`, `trace_id`
  - PII field values in `changed_fields` must be stored as `"[encrypted]"` — never plaintext
- Add `AuditLog` entries to the context's change tracker (they are persisted in the same transaction)
- Also invalidate Redis availability cache for `SchedulingSlot` changes (DEL `sched:avail:instructor:{id}:{date}`)
- `ICurrentUserService` interface (in `CFCHub.Application`): `Guid UserId { get; }`, `RoleType Role { get; }`, `string IpAddress { get; }`, `string? UserAgent { get; }`, `string TraceId { get; }`

**Acceptance Criteria:**
- Every `Student` update produces an `AuditLog` entry in the same DB transaction
- `AuditLog` `changed_fields` never contains plaintext CPF, email, or phone
- Interceptor is registered as a singleton in DI and added to `DbContextOptionsBuilder`
- Integration test: `AuditInterceptor_OnStudentUpdate_WritesAuditLog` passes
- `AuditLog` table has row-level security — no UPDATE or DELETE possible (verified in migration)

**Depends on:** TASK-022, TASK-011

---

### TASK-024 — Implement IDataProtectionService (AES-256-GCM encryption)

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement the LGPD field-level encryption service. All PII fields are encrypted before being stored in PostgreSQL. Keys are stored in AWS Secrets Manager per tenant prefix. See `design.md §6`.

**Files to create:**
```
src/CFCHub.Infrastructure/Security/DataProtectionService.cs
src/CFCHub.Infrastructure/Persistence/ValueConverters/EncryptedStringConverter.cs
```

**Implementation notes for `DataProtectionService`:**
- `IDataProtectionService` interface belongs in `CFCHub.Application/Common/Security/`
- `Encrypt(string plaintext, string tenantId)`:
  - Load key from AWS Secrets Manager key at `{CFCHUB_DATA_PROTECTION_KEY_PREFIX}{tenantId}`
  - Generate random 12-byte nonce
  - AES-256-GCM encrypt
  - Return base64(`nonce + ciphertext + tag`)
  - Cache key material in memory (TTL: 300s) — never in Redis (LGPD: no PII in Redis)
- `Decrypt(string ciphertext, string tenantId)`:
  - Load same key
  - Parse nonce + ciphertext + tag
  - AES-256-GCM decrypt
  - Key rotation: try current key; if `AuthenticationTagMismatchException`, try previous key (fetched by adding `/prev` suffix to secret name)
- `EncryptedStringConverter : ValueConverter<string, string>` — wraps `IDataProtectionService`; applied in EF entity configurations for PII columns

**Acceptance Criteria:**
- Round-trip: `Decrypt(Encrypt(plaintext, tenantId), tenantId) == plaintext`
- Decryption with wrong key throws `SecurityException`, not exposes plaintext
- Key material is NOT logged (verify via `SensitiveDataDestructuringPolicy`)
- Key rotation: `Decrypt` succeeds after key rotation if old ciphertext uses previous key
- Unit test with mock `ISecretsManagerService`: `Encrypt_ThenDecrypt_RoundTrips`

**Depends on:** TASK-009, TASK-021

---

### TASK-025 — Create initial database migration (template schema)

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Create the first EF Core migration targeting the `__template` schema. This migration creates all tables, indexes, exclusion constraints, outbox table, audit log with RLS, and enables the `btree_gist` PostgreSQL extension. See `design.md §3, §4, §5, §6`.

**Files to create:**
```
src/CFCHub.Infrastructure/Persistence/Migrations/{Timestamp}_InitialCreate.cs
src/CFCHub.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs
```

**Migration must include:**
- `CREATE EXTENSION IF NOT EXISTS btree_gist` (for exclusion constraints)
- All tables from all modules: `staff_users`, `scheduling_slots`, `instructors`, `vehicles`, `tracks`, `instructor_availability_templates`, `day_availability_overrides`, `students`, `enrollments`, `consent_records`, `contracts`, `signature_records`, `payments`, `installments`, `document_records`, `outbox_messages`, `audit_logs`, `data_erasure_requests`
- PostgreSQL exclusion constraints on `scheduling_slots` for instructor, vehicle, and track double-booking (see `design.md §4`)
- Indexes: `idx_outbox_pending` (partial index on `status = 'Pending'`), `idx_scheduling_slots_instructor_date`, `idx_students_cpf` (on encrypted column — for search by hash), `idx_audit_logs_entity`
- Row-level security on `audit_logs`: INSERT-only, no UPDATE/DELETE
- All enum columns stored as `TEXT`
- All timestamps as `TIMESTAMPTZ`
- All IDs as `UUID`
- `Down()` method must be fully implemented (drop all in reverse)
- Migration header comment: `// Target: __template schema — apply to tenant schemas via TenantMigrationOrchestrator`

**Acceptance Criteria:**
- `dotnet ef migrations add InitialCreate --schema __template` produces valid migration file (agent may generate manually based on entity configurations from TASK-043–048)
- Migration `Up()` runs without error against a fresh PostgreSQL 16 database
- `scheduling_slots` has three exclusion constraints (instructor, vehicle, track)
- `audit_logs` RLS policy prevents UPDATE/DELETE from `app_role`
- `outbox_messages` has partial index on `status = 'Pending'`
- Migration `Down()` cleanly reverses all changes

**Depends on:** TASK-022, TASK-043, TASK-044, TASK-045, TASK-046, TASK-047, TASK-048

---

### TASK-026 — Implement TenantMigrationOrchestrator

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement the service that applies pending migrations to all active tenant schemas on deploy. Also handles new tenant schema provisioning. See `design.md §3`.

**Files to create:**
```
src/CFCHub.Infrastructure/Persistence/TenantMigrationOrchestrator.cs
src/CFCHub.Infrastructure/Persistence/TenantProvisioningService.cs
```

**Implementation notes for `TenantMigrationOrchestrator`:**
- On startup (called from `Program.cs` before app starts accepting requests):
  1. Apply pending migrations to `__template` schema
  2. Query `public.tenants WHERE status = 'Active'` for all tenant slugs
  3. For each tenant: `SET search_path TO cfc_{slug}; apply pending migrations`
  4. Log each migration applied per tenant at `Information` level

**Implementation notes for `TenantProvisioningService`:**
- `ProvisionAsync(string slug, Guid tenantId, CancellationToken ct)`:
  1. `CREATE SCHEMA cfc_{slug}`
  2. Copy and apply all migrations from `__template` to `cfc_{slug}`
  3. Seed reference data: CNH categories, default CFC configuration
  4. `INSERT INTO public.tenants (id, slug, schema_name, status) VALUES (...)`

**Acceptance Criteria:**
- `TenantMigrationOrchestrator` runs before the API starts listening
- Each tenant schema gets all migrations applied idempotently
- `TenantProvisioningService.ProvisionAsync` is atomic — if migration fails, schema is dropped and transaction rolled back
- Integration test: provisioning a new tenant creates an accessible schema with all tables

**Depends on:** TASK-025

---

## EPIC 9 — Infrastructure: EF Core Configurations

---

### TASK-027 — EF Core configurations: Identity module

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement EF Core `IEntityTypeConfiguration<T>` for all Identity module entities. Apply `EncryptedStringConverter` to PII fields.

**Files to create:**
```
src/CFCHub.Infrastructure/Persistence/Configurations/StaffUserConfiguration.cs
```

**Implementation notes:**
- `StaffUserConfiguration`: table `staff_users`; all column names in `snake_case`; `Status` stored as `TEXT`; `Role` stored as `TEXT`; `Email` column uses `EncryptedStringConverter`; `Ignore(u => u.DomainEvents)`;  unique index on `email` (note: index is on the encrypted value — uniqueness at DB level ensures no duplicate emails per tenant)

**Acceptance Criteria:**
- `Email` column uses `EncryptedStringConverter`
- `Status` and `Role` stored as `TEXT` not `INT`
- No navigation properties to other modules

**Depends on:** TASK-022, TASK-024

---

### TASK-028 — EF Core configurations: Scheduling module

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement EF Core configurations for all Scheduling module entities. Add PostgreSQL exclusion constraints via `HasAnnotation`. `SchedulingSlotId`, `InstructorId`, `VehicleId`, `TrackId` use strongly typed ID converters.

**Files to create:**
```
src/CFCHub.Infrastructure/Persistence/Configurations/SchedulingSlotConfiguration.cs
src/CFCHub.Infrastructure/Persistence/Configurations/InstructorConfiguration.cs
src/CFCHub.Infrastructure/Persistence/Configurations/InstructorAvailabilityTemplateConfiguration.cs
src/CFCHub.Infrastructure/Persistence/Configurations/VehicleConfiguration.cs
src/CFCHub.Infrastructure/Persistence/Configurations/TrackConfiguration.cs
```

**Implementation notes:**
- `SchedulingSlotConfiguration`: table `scheduling_slots`; strongly typed ID converters for all ID properties; `Status` as `TEXT`; `Category` as `TEXT`; `DomainEvents` ignored; exclusion constraints added via `migrationBuilder.Sql(...)` in migration (not via Fluent API — EF doesn't support gist natively)
- `InstructorConfiguration`: owns `InstructorAvailabilityTemplate` as owned entity; `TeachableCategories` stored as `TEXT[]` PostgreSQL array
- `VehicleConfiguration`: `Status` as `TEXT`; `Category` as `TEXT`
- `TrackConfiguration`: `Type` as `TEXT`

**Acceptance Criteria:**
- All ID properties use strongly typed converters (not raw `Guid`)
- `Status`, `Category`, `Type` stored as `TEXT`
- No navigation properties to Enrollment/Finance/Contracts modules
- `InstructorAvailabilityTemplate` columns in `instructors` table (owned entity, same table)

**Depends on:** TASK-022, TASK-013, TASK-014

---

### TASK-029 — EF Core configurations: Enrollment module

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement EF Core configurations for Student, Enrollment, and ConsentRecord. Apply `EncryptedStringConverter` to all LGPD-sensitive fields.

**Files to create:**
```
src/CFCHub.Infrastructure/Persistence/Configurations/StudentConfiguration.cs
src/CFCHub.Infrastructure/Persistence/Configurations/EnrollmentConfiguration.cs
src/CFCHub.Infrastructure/Persistence/Configurations/ConsentRecordConfiguration.cs
```

**Implementation notes:**
- `StudentConfiguration`: table `students`; `EncryptedStringConverter` on `Name`, `Cpf`, `Rg`, `Email`, `Phone`; `Address` owned entity → columns `home_street`, `home_number`, `home_complement`, `home_district`, `home_city`, `home_state`, `home_zip`; global soft-delete filter via `ISoftDeletable`
- `EnrollmentConfiguration`: table `enrollments`; `Status` as `TEXT`; `Category` as `TEXT`; soft-delete
- `ConsentRecordConfiguration`: table `consent_records`; `Channel` as `TEXT`; no UPDATE — enforced by no `UpdateAsync` in repository

**Acceptance Criteria:**
- `Name`, `Cpf`, `Rg`, `Email`, `Phone` columns use `EncryptedStringConverter`
- Soft-delete filter: EF queries exclude `deleted_at IS NOT NULL` records automatically
- `Address` is stored as owned entity (flat columns in `students` table, no join table)
- Unit test: saving a `Student` stores encrypted `Cpf`, not plaintext

**Depends on:** TASK-022, TASK-024, TASK-016

---

### TASK-030 — EF Core configurations: Contracts, Finance, Compliance modules

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement EF Core configurations for the remaining three modules.

**Files to create:**
```
src/CFCHub.Infrastructure/Persistence/Configurations/ContractConfiguration.cs
src/CFCHub.Infrastructure/Persistence/Configurations/SignatureRecordConfiguration.cs
src/CFCHub.Infrastructure/Persistence/Configurations/PaymentConfiguration.cs
src/CFCHub.Infrastructure/Persistence/Configurations/InstallmentConfiguration.cs
src/CFCHub.Infrastructure/Persistence/Configurations/DocumentRecordConfiguration.cs
src/CFCHub.Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs
src/CFCHub.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs
src/CFCHub.Infrastructure/Persistence/Configurations/DataErasureRequestConfiguration.cs
```

**Implementation notes:**
- `ContractConfiguration`: table `contracts`; `Status` as `TEXT`
- `PaymentConfiguration`: table `payments`; `Money` owned entity → columns `amount`, `currency`; `Status` as `TEXT`; `Method` as `TEXT`; `IAuditable` fields
- `OutboxMessageConfiguration`: table `outbox_messages`; `Payload` as `JSONB` (`HasColumnType("jsonb")`); `Status` as `TEXT`; partial index on `status = 'Pending'` via `HasAnnotation`
- `AuditLogConfiguration`: table `audit_logs`; `ChangedFields` as `JSONB`; no `DeleteBehavior` references — append-only by design

**Acceptance Criteria:**
- `OutboxMessage.Payload` uses `jsonb` column type
- `Money` stored as flat columns `amount NUMERIC(18,2)` and `currency TEXT`
- No navigation properties crossing module boundaries

**Depends on:** TASK-022, TASK-018, TASK-019, TASK-020

---

## EPIC 10 — Infrastructure: Repositories

---

### TASK-031 — Implement Scheduling repositories

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement all three repository interfaces from the Scheduling module domain. Use cursor-based pagination. All read queries use `AsNoTracking()` with projections.

**Files to create:**
```
src/CFCHub.Infrastructure/Persistence/Repositories/SchedulingRepository.cs
src/CFCHub.Infrastructure/Persistence/Repositories/InstructorRepository.cs
src/CFCHub.Infrastructure/Persistence/Repositories/VehicleRepository.cs
```

**Implementation notes:**
- `SchedulingRepository : ISchedulingRepository` — `GetOverlappingSlotAsync`: uses `SELECT FOR UPDATE` via `FromSqlRaw` with a range overlap query (not LINQ — EF can't express `FOR UPDATE`)
- `GetByInstructorAsync`: `AsNoTracking()`, `.Select(s => new SlotProjection(...))` — never return full entity on read-only paths
- `GetByStudentAsync`: cursor-based — `WHERE id > cursorId AND started_at >= cursorTimestamp ORDER BY started_at, id LIMIT n+1` to detect `HasMore`
- `InstructorRepository`: `GetByIdAsync` includes `WeeklyTemplate` (owned entity, same row); cursor-paginated `ListAsync` with `AsNoTracking` projection

**Acceptance Criteria:**
- All read queries use `AsNoTracking()` and projections (not full entity loads)
- `GetByStudentAsync` uses cursor pagination, not `Skip`/`Take`
- `GetOverlappingSlotAsync` uses raw SQL with `FOR UPDATE` for correct locking semantics
- No `Include()` chains deeper than 1 level
- Integration test: `GetByStudentAsync_WithCursor_ReturnsPaginatedResults`

**Depends on:** TASK-022, TASK-028, TASK-015

---

### TASK-032 — Implement Enrollment, Contract, Finance, Compliance repositories

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement remaining module repositories: `StudentRepository`, `EnrollmentRepository`, `ContractRepository`, `PaymentRepository`, `DocumentRepository`.

**Files to create:**
```
src/CFCHub.Infrastructure/Persistence/Repositories/StudentRepository.cs
src/CFCHub.Infrastructure/Persistence/Repositories/EnrollmentRepository.cs
src/CFCHub.Infrastructure/Persistence/Repositories/ContractRepository.cs
src/CFCHub.Infrastructure/Persistence/Repositories/PaymentRepository.cs
src/CFCHub.Infrastructure/Persistence/Repositories/DocumentRepository.cs
src/CFCHub.Infrastructure/Persistence/Repositories/StaffUserRepository.cs
```

**Implementation notes:**
- `StudentRepository.GetByCpfAsync`: search by encrypted CPF — since CPF is encrypted at rest, query by SHA-256 hash index (store `cpf_hash TEXT` alongside encrypted `cpf` for lookup; `cpf_hash = SHA-256(plaintext_cpf)` — set in `StudentConfiguration`)
- All list methods: cursor-based, `AsNoTracking()`, projections
- `DocumentRepository.GetExpiringAsync(DateOnly from, DateOnly to, ct)`: query `expiry_date BETWEEN from AND to AND last_alert_sent_at < CURRENT_DATE - INTERVAL '1 day'`

**Acceptance Criteria:**
- `StudentRepository.GetByCpfAsync` does not decrypt all CPFs in a table scan — uses `cpf_hash` index
- All list methods return `PagedResult<T>` with cursor
- No `Thread.Sleep` anywhere — all async with proper `CancellationToken` propagation
- Integration test: `GetByCpfAsync_FindsStudentByCpfHash`

**Depends on:** TASK-022, TASK-029, TASK-030

---

## EPIC 11 — Infrastructure: Caching (Redis)

---

### TASK-033 — Implement RedisKeys static class

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement the centralized Redis key registry. All Redis key construction in the application must go through this class — no inline key building (see `conventions.md §7`).

**Files to create:**
```
src/CFCHub.Infrastructure/Caching/RedisKeys.cs
```

**Implementation notes:**
- Static class with static methods only — no instances
- Implement all key formats from `conventions.md §7`:
  - `SchedulingLockInstructor`, `SchedulingLockVehicle`, `SchedulingLockTrack`
  - `InstructorAvailability`
  - `DetranCnhStatus`
  - `RateLimit`
  - `StaffSession`
  - `OutboxWorkerLease`
  - `TenantResolution`
- `static string CpfHash(string cpf)` — helper: `Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cpf)))` — used in `DetranCnhStatus` and student search

**Acceptance Criteria:**
- Every key method includes `env` and `tenant` parameters — no global keys except `TenantResolution`
- No key method returns a key with the raw CPF string in it
- `CpfHash` produces a 64-char lowercase hex string
- Unit test: all key methods produce strings matching the format from `conventions.md §7`

**Depends on:** TASK-001

---

### TASK-034 — Implement RedisLockService (ISchedulingLockService)

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement the distributed lock service used by the Scheduling module. Must follow the exact protocol from `design.md §4` and `GEMINI.md §5`. TTL is always 30 seconds.

**Files to create:**
```
src/CFCHub.Infrastructure/Caching/RedisLockService.cs
```

**Implementation notes:**
- `RedisLockService : ISchedulingLockService`
- `TryAcquireAsync(string key, CancellationToken ct)` — `SETNX key 1 EX 30`; returns `bool`
- `ReleaseAsync(string key, CancellationToken ct)` — `DEL key`; log at `Debug` if key was already expired (no-op, not an error)
- `AcquireAllAsync(IEnumerable<string> keys, CancellationToken ct)` — acquire keys in sorted order (deterministic to prevent deadlock); if any `TryAcquireAsync` returns `false`, call `ReleaseAsync` on all previously acquired keys, then return `false`; never call `StackExchange.Redis` directly from Application layer — this service is the only place
- All locks: TTL exactly 30 seconds — no exceptions; any attempt to set a different TTL must throw `ArgumentException`

**Acceptance Criteria:**
- `AcquireAllAsync` acquires keys in lexicographically sorted order
- If third lock fails, first two are released before returning `false`
- TTL is always 30 seconds — hardcoded constant `MaxLockTtlSeconds = 30`
- Unit test with mocked `IDatabase`: `AcquireAll_ThirdLockFails_ReleasesFirstTwo`

**Depends on:** TASK-033, TASK-015

---

### TASK-035 — Implement Redis availability cache and tenant resolution cache

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement Redis caching for instructor availability results (TTL 300s) and tenant resolution (TTL 300s). Cache invalidation for scheduling slots on any slot change.

**Files to create:**
```
src/CFCHub.Infrastructure/Caching/AvailabilityCacheService.cs
src/CFCHub.Infrastructure/Caching/TenantCacheService.cs
```

**Implementation notes:**
- `AvailabilityCacheService` — `GetAsync(instructorId, date)`, `SetAsync(instructorId, date, slots)`, `InvalidateAsync(instructorId, date)` — serializes `IReadOnlyList<AvailableSlot>` as JSON; TTL 300s
- `TenantCacheService` — `GetAsync(slug)`, `SetAsync(slug, tenantContext)` — JSON serialization; TTL 300s
- Cache invalidation called from `AuditInterceptor` (TASK-023) after any `SchedulingSlot` insert/update/delete — must call `InvalidateAsync` for the affected instructor+date combination

**Acceptance Criteria:**
- Availability cache stores serialized `AvailableSlot[]` as JSON string
- Cache miss returns `null` (caller falls through to DB)
- Slot insertion triggers `InvalidateAsync` for the affected instructor+date
- All Redis writes include explicit TTL — no permanent keys
- No PII in cache values

**Depends on:** TASK-033, TASK-023

---

## EPIC 12 — Infrastructure: AWS Integration

---

### TASK-036 — Implement S3FileStorageService

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement `IFileStorageService` using AWS S3. Enforce TTL rules: documents max 3600s, medical files max 900s. Medical files use a separate bucket. See `design.md §7`.

**Files to create:**
```
src/CFCHub.Infrastructure/Storage/S3FileStorageService.cs
```

**File validation rule (upload path):**
- Read first 16 bytes of uploaded file; validate magic bytes before any S3 operation:
  - PDF: `25 50 44 46` (`%PDF`)
  - JPEG: `FF D8 FF`
  - PNG: `89 50 4E 47`
- Reject with `StorageException("INVALID_FILE_TYPE")` if magic bytes don't match

**TTL enforcement:**
```csharp
private TimeSpan GetDownloadUrlTtl(StorageTarget target) => target switch
{
    StorageTarget.Medical   => TimeSpan.FromMinutes(15),
    StorageTarget.Documents => TimeSpan.FromHours(1),
    _                       => TimeSpan.FromHours(1)
};
```
The method must throw `ArgumentOutOfRangeException` if anyone attempts to override TTL above the cap.

**Acceptance Criteria:**
- `GenerateDownloadUrlAsync(Medical, ...)` never generates a URL with TTL > 900s — throws if attempted
- `GenerateUploadUrlAsync` for non-PDF/JPEG/PNG content type is rejected before reaching S3
- Bucket names are read from env vars — never hardcoded
- Unit test with mock `IAmazonS3`: `GenerateDownloadUrl_MedicalTarget_TTLIs900s`

**Depends on:** TASK-009

---

### TASK-037 — Implement SesEmailService

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement `IEmailService` using AWS SES templated email sending. All emails use pre-registered SES templates. Email body must never contain PII (see `GEMINI.md §6.10`).

**Files to create:**
```
src/CFCHub.Infrastructure/Email/SesEmailService.cs
src/CFCHub.Infrastructure/Email/IEmailService.cs
src/CFCHub.Infrastructure/Email/EmailMessage.cs
src/CFCHub.Infrastructure/Email/SesEventWebhookHandler.cs
```

**Implementation notes:**
- `IEmailService` in `CFCHub.Application/Common/Email/`
- `EmailMessage` — `record`: `string TemplateId`, `string ToAddress`, `Dictionary<string, string> TemplateData`
- `SesEmailService.SendAsync(EmailMessage message, CancellationToken ct)` — calls `SendTemplatedEmailAsync`; validates `ToAddress` is not empty; catches `SES` exceptions → wraps as `EmailDeliveryException`
- `SesEventWebhookHandler` — processes bounce/complaint SNS events from `POST /webhooks/ses/events`; updates `email_delivery_logs` table
- Available SES template IDs (see `design.md §7`): `cfchub-welcome`, `cfchub-slot-reminder`, `cfchub-contract-ready`, `cfchub-payment-receipt`, `cfchub-doc-expiry-d30`, `cfchub-doc-expiry-d7`, `cfchub-erasure-complete`

**Acceptance Criteria:**
- `SendAsync` uses SES template rendering — never constructs HTML email body in code
- Template data passed to SES never contains CPF, RG, medical data fields
- SES exceptions are caught and re-thrown as `EmailDeliveryException` (not exposed to client)
- `SesEventWebhookHandler` processes bounce events and logs to `email_delivery_logs`

**Depends on:** TASK-009

---

### TASK-038 — Implement AWS Secrets Manager integration

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement the service that fetches secrets from AWS Secrets Manager at runtime. Used by `DataProtectionService` for encryption keys and JWT key loading. Secrets are cached in memory for 5 minutes.

**Files to create:**
```
src/CFCHub.Infrastructure/Security/SecretsManagerService.cs
src/CFCHub.Infrastructure/Security/ISecretsManagerService.cs
```

**Implementation notes:**
- `ISecretsManagerService` in `CFCHub.Application/Common/Security/`
- `GetSecretAsync(string arn, CancellationToken ct)` — calls `GetSecretValueAsync`; caches result in `ConcurrentDictionary<string, (string value, DateTimeOffset cachedAt)>` with 5-minute TTL; returns `string` secret value
- On local dev: if ARN starts with `arn:aws:secretsmanager:us-east-1:000000000000`, fall back to environment variable lookup (for LocalStack compatibility)
- Never log the secret value — log only the ARN (masked: show first 20 chars)

**Acceptance Criteria:**
- Secrets are cached in memory for 5 minutes — second call within 5 min does not hit AWS
- Secret value is never logged (SensitiveDataDestructuringPolicy covers it, but also manual guard)
- On local dev with mock ARN, falls back to env var
- Unit test with mock `IAmazonSecretsManager`: `GetSecretAsync_CallsOnceWithinCacheTtl`

**Depends on:** TASK-009

---

### TASK-039 — Implement IDetranClient with adapter pattern

**Layer:** `CFCHub.Infrastructure`  
**Description:**  
Implement the DETRAN integration following the adapter pattern from `design.md §9`. Start with SP (REST API) and MG (HTML scraping sidecar) adapters. All results cached 24h in Redis.

**Files to create:**
```
src/CFCHub.Infrastructure/ExternalServices/Detran/IDetranClient.cs
src/CFCHub.Infrastructure/ExternalServices/Detran/DetranHttpClient.cs
src/CFCHub.Infrastructure/ExternalServices/Detran/CnhStatusResult.cs
src/CFCHub.Infrastructure/ExternalServices/Detran/Adapters/SpDetranAdapter.cs
src/CFCHub.Infrastructure/ExternalServices/Detran/Adapters/MgDetranAdapter.cs
src/CFCHub.Infrastructure/ExternalServices/Detran/Adapters/DefaultDetranAdapter.cs
src/CFCHub.Infrastructure/ExternalServices/Detran/StateDetranAdapterFactory.cs
```

**Implementation notes:**
- `IDetranClient` belongs in `CFCHub.Domain/Compliance/` — domain defines the contract
- `CnhStatusResult` — `record`: `bool IsAvailable`, `string? Status`, `DateOnly? ExpiryDate`, `int? Points`; static `Unavailable` property for failure case
- `DetranHttpClient : IDetranClient` — resolves adapter via `StateDetranAdapterFactory`; checks Redis cache `DetranCnhStatus(env, tenant, cpfHash)` before calling adapter; caches result TTL 86400s; on adapter failure → `LogWarning` (not `Error`) and return `CnhStatusResult.Unavailable`
- `StateDetranAdapterFactory` — maps `BrazilianState` enum to the correct adapter
- `SpDetranAdapter` — HTTP GET to SP DETRAN REST API; typed `HttpClient` with named client `"detran-sp"`
- `DefaultDetranAdapter` — calls Playwright sidecar via gRPC; `MgDetranAdapter` is same pattern
- CPF key in Redis: `RedisKeys.DetranCnhStatus(env, tenant, RedisKeys.CpfHash(cpf))`

**Acceptance Criteria:**
- `GetCnhStatusAsync` checks Redis before calling adapter
- Cache hit returns without calling adapter (verified by mock assertion)
- Adapter failure returns `CnhStatusResult.Unavailable` — does not throw to caller
- CPF never appears in Redis key or value — only SHA-256 hash

**Depends on:** TASK-033, TASK-035, TASK-020

---

## EPIC 13 — Application: Pipeline Behaviors

---

### TASK-040 — Implement MediatR pipeline behaviors

**Layer:** `CFCHub.Application`  
**Description:**  
Implement the four pipeline behaviors that apply cross-cutting concerns to all MediatR commands and queries: validation, logging, tenant context, and tracing. Register in correct order.

**Files to create:**
```
src/CFCHub.Application/Common/Behaviors/ValidationBehavior.cs
src/CFCHub.Application/Common/Behaviors/LoggingBehavior.cs
src/CFCHub.Application/Common/Behaviors/TenantBehavior.cs
src/CFCHub.Application/Common/Behaviors/TracingBehavior.cs
```

**Registration order** (applied in this sequence):
1. `LoggingBehavior` — log request start/end with handler name, duration; no PII
2. `TenantBehavior` — guard: `ITenantContext.IsResolved == false` → throw `UnauthorizedException("TENANT_NOT_RESOLVED")`
3. `ValidationBehavior` — run FluentValidation; collect all failures; if any → throw `ValidationException` with all failures
4. `TracingBehavior` — wrap handler in `ActivitySource.StartActivity(handlerName)` for OpenTelemetry

**`ValidationBehavior` implementation:**
- `IPipelineBehavior<TRequest, TResponse>` where `TRequest : IRequest<TResponse>`
- Resolve all `IValidator<TRequest>` from DI; run concurrently via `Task.WhenAll`; collect failures; throw `ValidationException` with list of `{PropertyName, ErrorMessage, ErrorCode}` tuples

**`LoggingBehavior`:**
- Log `Information` at start: `"Handling {RequestName}. TenantId: {TenantId}"` — no request payload logging (could contain PII)
- Log `Information` at end: `"Handled {RequestName} in {ElapsedMs}ms"`
- Log `Warning` on exception (let it bubble up)

**Acceptance Criteria:**
- `ValidationBehavior` collects ALL validation failures — not just the first
- `TenantBehavior` blocks requests with unresolved tenant before validation runs
- `TracingBehavior` creates a child span under the HTTP request span
- Behaviors registered in order: Logging → Tenant → Validation → Tracing
- Unit test: `ValidationBehavior_WithMultipleFailures_CollectsAll`

**Depends on:** TASK-009, TASK-010, TASK-021

---

## EPIC 14 — Application: Identity Use Cases

---

### TASK-041 — Implement Login command

**Layer:** `CFCHub.Application`  
**Description:**  
Implement the `LoginCommand` use case. Validates credentials, generates RS256 JWT with `tenant_id` and `role` claims. Rate limiting is handled by middleware (TASK-056), not here.

**Files to create:**
```
src/CFCHub.Application/Identity/Commands/Login/LoginCommand.cs
src/CFCHub.Application/Identity/Commands/Login/LoginCommandHandler.cs
src/CFCHub.Application/Identity/Commands/Login/LoginCommandValidator.cs
src/CFCHub.Application/Identity/Commands/Login/LoginResult.cs
src/CFCHub.Application/Common/Security/IPasswordHasher.cs
src/CFCHub.Application/Common/Security/IJwtTokenService.cs
```

**Implementation notes:**
- `LoginCommand : IRequest<Result<LoginResult>>` — `string Email`, `string Password`
- `LoginCommandHandler`:
  1. `GetByEmailAsync` → `NotFoundException` if not found (but return same error as wrong password to prevent user enumeration)
  2. `IPasswordHasher.Verify(command.Password, user.PasswordHash)` → `UnauthorizedException` if fails
  3. Check `StaffUserStatus == Active` → `ForbiddenException("ACCOUNT_INACTIVE")` if not
  4. `user.RecordLogin(clock)`
  5. `IJwtTokenService.GenerateToken(user, tenantContext)` → JWT
  6. Cache session: `RedisKeys.StaffSession(env, tenant, jti)` TTL 3600s
  7. Return `LoginResult { AccessToken, ExpiresAt, StaffUserId, Role }`
- `IJwtTokenService.GenerateToken` — RS256; claims: `sub`, `jti`, `tenant_id`, `role`, `iat`, `exp` (1h); private key from `ISecretsManagerService` via `CFCHUB_JWT_PRIVATE_KEY_ARN`
- `LoginCommandValidator`: `Email` required + email format; `Password` required, min 8 chars

**Acceptance Criteria:**
- Wrong password and unknown email return the same error (no user enumeration)
- JWT uses RS256 — never HS256
- JWT carries `tenant_id` and `role` claims
- Inactive user gets `ForbiddenException`, not `UnauthorizedException`
- Session cached in Redis with TTL 3600s
- Unit tests: `Handle_WithWrongPassword_ReturnsUnauthorized`, `Handle_WithInactiveUser_ReturnsForbidden`, `Handle_WithValidCredentials_ReturnsJwt`

**Depends on:** TASK-040, TASK-011, TASK-038

---

### TASK-042 — Implement StaffUser management commands and queries

**Layer:** `CFCHub.Application`  
**Description:**  
Implement CRUD use cases for staff user management. Only `Admin` role can manage staff.

**Files to create:**
```
src/CFCHub.Application/Identity/Commands/CreateStaffUser/CreateStaffUserCommand.cs
src/CFCHub.Application/Identity/Commands/CreateStaffUser/CreateStaffUserCommandHandler.cs
src/CFCHub.Application/Identity/Commands/CreateStaffUser/CreateStaffUserCommandValidator.cs
src/CFCHub.Application/Identity/Commands/ChangeStaffUserRole/ChangeStaffUserRoleCommand.cs
src/CFCHub.Application/Identity/Commands/ChangeStaffUserRole/ChangeStaffUserRoleCommandHandler.cs
src/CFCHub.Application/Identity/Commands/DeactivateStaffUser/DeactivateStaffUserCommand.cs
src/CFCHub.Application/Identity/Commands/DeactivateStaffUser/DeactivateStaffUserCommandHandler.cs
src/CFCHub.Application/Identity/Queries/GetStaffUsers/GetStaffUsersQuery.cs
src/CFCHub.Application/Identity/Queries/GetStaffUsers/GetStaffUsersQueryHandler.cs
src/CFCHub.Application/Identity/Queries/GetStaffUsers/StaffUserResult.cs
```

**Implementation notes:**
- `CreateStaffUserCommandHandler`: hash password via `IPasswordHasher.Hash`; check duplicate email via `GetByEmailAsync`; raise `ConflictException` if exists; add to repository; outbox → `WelcomeEmailRequested`
- All handlers: check `ICurrentUserService.Role == Admin` → `ForbiddenException` if not
- `GetStaffUsersQueryHandler`: cursor-paginated; return `StaffUserResult` (no `PasswordHash` in result); apply `IFieldAccessPolicyService` — but all roles can see their own user list (Admin only)
- Password policy: min 12 chars, at least 1 uppercase, 1 number, 1 special char — enforced in validator

**Acceptance Criteria:**
- `CreateStaffUser` by non-Admin role returns `ForbiddenException`
- Duplicate email returns `ConflictException`
- `PasswordHash` never appears in any result DTO
- Password policy enforced by FluentValidation
- Unit test: `CreateStaffUser_WhenNotAdmin_ThrowsForbidden`, `CreateStaffUser_WithDuplicateEmail_ThrowsConflict`

**Depends on:** TASK-041, TASK-040

---

### TASK-043 — Implement Tenant provisioning command

**Layer:** `CFCHub.Application`  
**Description:**  
Implement the admin-only command to register a new CFC tenant. This is the onboarding entry point for new customers.

**Files to create:**
```
src/CFCHub.Application/Admin/Commands/RegisterTenant/RegisterTenantCommand.cs
src/CFCHub.Application/Admin/Commands/RegisterTenant/RegisterTenantCommandHandler.cs
src/CFCHub.Application/Admin/Commands/RegisterTenant/RegisterTenantCommandValidator.cs
src/CFCHub.Application/Admin/Commands/RegisterTenant/RegisterTenantResult.cs
src/CFCHub.Application/Admin/Queries/GetTenant/GetTenantQuery.cs
src/CFCHub.Application/Admin/Queries/GetTenant/GetTenantQueryHandler.cs
src/CFCHub.Application/Admin/Queries/GetTenant/TenantResult.cs
```

**Implementation notes:**
- `RegisterTenantCommand` — `string Name`, `string Slug`, `string ContactEmail`, `string CnpJ`; requires `DETRAN_ADMIN` super-admin role (separate from tenant `Admin` role)
- Slug validation: regex `^[a-z0-9][a-z0-9_]{2,62}[a-z0-9]$` (from `design.md §3`)
- `RegisterTenantCommandHandler`:
  1. Check slug uniqueness in `public.tenants`
  2. Call `TenantProvisioningService.ProvisionAsync` (TASK-026)
  3. Return `RegisterTenantResult { TenantId, SchemaName, Slug }`
- `GetTenantQuery` — queried against `public.tenants`, not a tenant schema

**Acceptance Criteria:**
- Invalid slug format returns `ValidationException`
- Duplicate slug returns `ConflictException`
- `RegisterTenantCommandHandler` calls `TenantProvisioningService.ProvisionAsync` — verified by test
- Super-admin role check is enforced

**Depends on:** TASK-026, TASK-040

---

## EPIC 15 — Application: Scheduling Use Cases

---

### TASK-044 — Implement GetAvailableSlots query

**Layer:** `CFCHub.Application`  
**Description:**  
Implement the availability query. This is read-heavy and cache-aware. Results are cached in Redis for 300s.

**Files to create:**
```
src/CFCHub.Application/Scheduling/Queries/GetAvailableSlots/GetAvailableSlotsQuery.cs
src/CFCHub.Application/Scheduling/Queries/GetAvailableSlots/GetAvailableSlotsQueryHandler.cs
src/CFCHub.Application/Scheduling/Queries/GetAvailableSlots/GetAvailableSlotsQueryValidator.cs
src/CFCHub.Application/Scheduling/Queries/GetAvailableSlots/AvailableSlotResult.cs
src/CFCHub.Infrastructure/Scheduling/AvailabilityCalculatorService.cs
```

**Implementation notes:**
- `GetAvailableSlotsQuery : IRequest<PagedResult<AvailableSlotResult>>` — `DateOnly Date`, `CnhCategory? Category`, `InstructorId? InstructorId`, `string? Cursor`, `int Limit = 20`
- Handler flow per `design.md §4`:
  1. Check `AvailabilityCacheService.GetAsync(instructorId, date)` — cache hit → return
  2. Load instructor weekly template from repository
  3. Load per-day overrides for `date`
  4. Load booked slots for `date` (`AsNoTracking` projection)
  5. Compute free 50-min windows
  6. For each window: find available vehicle + track
  7. `AvailabilityCacheService.SetAsync(...)` — TTL 300s
  8. Apply cursor pagination to results before returning
- `GetAvailableSlotsQueryValidator`: `Date` must not be in the past; `Limit` ≤ 100

**Acceptance Criteria:**
- Cache hit skips DB query (verified by mock assertion in unit test)
- `Date` in the past returns `ValidationException`
- Results are cursor-paginated (no offset)
- `AvailableSlotResult` contains `StartedAt`, `InstructorId`, `InstructorName`, `VehicleId`, `TrackId`, `TrackType`

**Depends on:** TASK-035, TASK-040, TASK-015, TASK-031

---

### TASK-045 — Implement BookSlot command

**Layer:** `CFCHub.Application`  
**Description:**  
Implement the core booking command. This is the highest-risk use case — must follow the exact distributed lock protocol from `design.md §4` and `GEMINI.md §5`. Double booking is a P0 defect.

**Files to create:**
```
src/CFCHub.Application/Scheduling/Commands/BookSlot/BookSlotCommand.cs
src/CFCHub.Application/Scheduling/Commands/BookSlot/BookSlotCommandHandler.cs
src/CFCHub.Application/Scheduling/Commands/BookSlot/BookSlotCommandValidator.cs
src/CFCHub.Application/Scheduling/Commands/BookSlot/BookSlotResult.cs
```

**Exact implementation protocol (from `design.md §4`):**

```
Step 1 — Acquire Redis locks in deterministic order:
  keys = [instructor:{id}, vehicle:{id}, track:{id}] sorted lexicographically
  ISchedulingLockService.AcquireAllAsync(keys, ct)
  If fails → return Result.Failure(Error.Conflict("SLOT_LOCK_FAILED", "..."))

Step 2 — Begin DB transaction:
  SELECT FOR UPDATE on instructor slot range
  SELECT FOR UPDATE on vehicle slot range
  Validate no overlap (ISchedulingRepository.GetOverlappingSlotAsync)
  If overlap found → release locks → return Conflict result
  SchedulingSlot.Book(...)
  INSERT scheduling_slot
  INSERT outbox_message (type: SlotBooked, payload: self-contained)
  COMMIT

Step 3 — Release all locks (finally block — always executes):
  ISchedulingLockService.ReleaseAllAsync(keys, ct)
```

**`BookSlotCommandValidator`:** `StartedAt` must be in the future; `InstructorId`, `VehicleId`, `TrackId`, `StudentId` required; `Category` required

**Acceptance Criteria:**
- Lock acquisition fails → `Result.Failure(Error.Conflict(...))` without DB write
- DB overlap found after lock acquisition → rollback + release locks
- Lock release always happens in `finally` block — even if handler throws
- `OutboxMessage` inserted in same transaction as `SchedulingSlot`
- Integration test: `BookSlot_ConcurrentRequests_OnlyOneSucceeds` (Testcontainers + real Redis + real PostgreSQL)
- Integration test: `BookSlot_VerifiesExclusionConstraint_OnSimultaneousConcurrentInserts`

**Depends on:** TASK-034, TASK-031, TASK-014, TASK-040

---

### TASK-046 — Implement CancelSlot, CompleteSlot, and slot query commands

**Layer:** `CFCHub.Application`  
**Description:**  
Implement remaining scheduling commands and queries.

**Files to create:**
```
src/CFCHub.Application/Scheduling/Commands/CancelSlot/CancelSlotCommand.cs
src/CFCHub.Application/Scheduling/Commands/CancelSlot/CancelSlotCommandHandler.cs
src/CFCHub.Application/Scheduling/Commands/CancelSlot/CancelSlotCommandValidator.cs
src/CFCHub.Application/Scheduling/Commands/CompleteSlot/CompleteSlotCommand.cs
src/CFCHub.Application/Scheduling/Commands/CompleteSlot/CompleteSlotCommandHandler.cs
src/CFCHub.Application/Scheduling/Commands/MarkNoShow/MarkNoShowCommand.cs
src/CFCHub.Application/Scheduling/Commands/MarkNoShow/MarkNoShowCommandHandler.cs
src/CFCHub.Application/Scheduling/Queries/GetSlotsByInstructor/GetSlotsByInstructorQuery.cs
src/CFCHub.Application/Scheduling/Queries/GetSlotsByInstructor/GetSlotsByInstructorQueryHandler.cs
src/CFCHub.Application/Scheduling/Queries/GetSlotsByInstructor/SlotResult.cs
src/CFCHub.Application/Scheduling/Queries/GetSlotsByStudent/GetSlotsByStudentQuery.cs
src/CFCHub.Application/Scheduling/Queries/GetSlotsByStudent/GetSlotsByStudentQueryHandler.cs
```

**Implementation notes:**
- `CancelSlotCommandHandler`: load slot; verify `StudentId` matches `ICurrentUserService` OR role is `Admin`/`Receptionist`; call `slot.Cancel(reason)`; save; publish `SlotCancelledEvent` → outbox invalidates availability cache
- `CompleteSlotCommandHandler`: only `Instructor` or `Admin` can complete; publishes `SlotCompletedEvent` → outbox; downstream Enrollment handler increments practical hours
- All query handlers: `AsNoTracking()` projections; cursor-paginated
- `SlotResult`: includes `Status`, `StartedAt`, `InstructorName`, `VehicleId`, `TrackType`; does NOT include student PII

**Acceptance Criteria:**
- Student can only cancel their own slot; Instructor/Admin can cancel any
- `CompleteSlot` triggers `SchedulingSlotCompletedEvent` → Enrollment `IncrementPracticalHours`
- Availability cache is invalidated after cancel/complete
- Unit tests: `CancelSlot_ByOtherStudent_ThrowsForbidden`, `CompleteSlot_PublishesCompletedEvent`

**Depends on:** TASK-045, TASK-031

---

## EPIC 16 — Application: Enrollment Use Cases

---

### TASK-047 — Implement CreateStudent command

**Layer:** `CFCHub.Application`  
**Description:**  
Implement the student creation command with LGPD consent capture. Consent is mandatory — no student can be created without it.

**Files to create:**
```
src/CFCHub.Application/Enrollment/Commands/CreateStudent/CreateStudentCommand.cs
src/CFCHub.Application/Enrollment/Commands/CreateStudent/CreateStudentCommandHandler.cs
src/CFCHub.Application/Enrollment/Commands/CreateStudent/CreateStudentCommandValidator.cs
src/CFCHub.Application/Enrollment/Commands/CreateStudent/CreateStudentResult.cs
```

**`CreateStudentCommand`:** `string Name`, `string Cpf`, `string? Rg`, `string Email`, `string Phone`, `DateOnly BirthDate`, `AddressRequest HomeAddress`, `string PolicyVersion`, `string PolicyContentHash`, `ConsentChannel ConsentChannel`

**Handler flow:**
1. Validate CPF format (11 digits, Luhn-like algorithm for Brazilian CPF validation)
2. Check `IStudentRepository.GetByCpfAsync(SHA256(cpf))` → `ConflictException("STUDENT_ALREADY_EXISTS")` if found
3. `Student.Create(...)` — raises `StudentCreatedEvent`
4. `ConsentRecord.Capture(...)` — atomic with student creation (same transaction)
5. `AddAsync(student)` + `AddAsync(consentRecord)` — same `SaveChangesAsync` call
6. Outbox: `WelcomeEmailRequested`
7. Return `CreateStudentResult { StudentId }`

**`CreateStudentCommandValidator`:**
- Brazilian CPF algorithm validation
- `BirthDate` between 16 and 100 years ago
- `Email` format valid
- `Phone` format: `+55` followed by 10 or 11 digits
- `PolicyVersion` and `PolicyContentHash` required (LGPD consent mandatory)

**Acceptance Criteria:**
- `ConsentRecord` created in same transaction as `Student` — if `Student` insert fails, `ConsentRecord` is also rolled back
- Duplicate CPF (by hash) returns `ConflictException`
- Invalid CPF algorithm returns `ValidationException`
- `ConsentRecord.IpAddress` sourced from `ICurrentUserService.IpAddress`
- Unit test: `CreateStudent_WithInvalidCpfAlgorithm_ThrowsValidation`, `CreateStudent_WithDuplicateCpf_ThrowsConflict`, `CreateStudent_CreatesConsentRecordAtomically`

**Depends on:** TASK-017, TASK-040, TASK-032

---

### TASK-048 — Implement EnrollStudent command and related queries

**Layer:** `CFCHub.Application`  
**Description:**  
Implement student enrollment in a CNH category course. Triggers downstream events to Contracts and Finance modules.

**Files to create:**
```
src/CFCHub.Application/Enrollment/Commands/EnrollStudent/EnrollStudentCommand.cs
src/CFCHub.Application/Enrollment/Commands/EnrollStudent/EnrollStudentCommandHandler.cs
src/CFCHub.Application/Enrollment/Commands/EnrollStudent/EnrollStudentCommandValidator.cs
src/CFCHub.Application/Enrollment/Queries/GetStudent/GetStudentQuery.cs
src/CFCHub.Application/Enrollment/Queries/GetStudent/GetStudentQueryHandler.cs
src/CFCHub.Application/Enrollment/Queries/GetStudent/StudentResult.cs
src/CFCHub.Application/Enrollment/Queries/GetStudents/GetStudentsQuery.cs
src/CFCHub.Application/Enrollment/Queries/GetStudents/GetStudentsQueryHandler.cs
```

**`EnrollStudentCommandHandler` flow:**
1. Check student exists and `Status == Active`
2. Check no active enrollment in same `Category` → `ConflictException("ENROLLMENT_ALREADY_EXISTS")`
3. `Enrollment.Enroll(...)` — raises `StudentEnrolledEvent`
4. Outbox: `ContractGenerationRequested` (Contract module picks this up)
5. Outbox: `PaymentPlanCreationRequested` (Finance module picks this up)
6. Outbox: `DocumentTrackingRegistered` (Compliance module picks this up)

**`GetStudentQueryHandler` — field-level access control:**
- Load student from DB
- For each PII field: check `IFieldAccessPolicyService.CheckAccess(currentRole, fieldName)`
- Replace denied fields with `null` in the result DTO
- `StudentResult` has nullable PII fields: `string? Cpf`, `string? Rg`, `string? Phone`

**Acceptance Criteria:**
- Double enrollment in same category returns `ConflictException`
- `GetStudent` result has `Cpf = null` for `Receptionist` role
- All three outbox messages written in same transaction as enrollment
- Unit test: `EnrollStudent_WithExistingEnrollmentInSameCategory_ThrowsConflict`
- Unit test: `GetStudent_AsReceptionist_CpfIsNull`

**Depends on:** TASK-047, TASK-017, TASK-012, TASK-040

---

### TASK-049 — Implement DataErasureRequest command (LGPD Art. 18)

**Layer:** `CFCHub.Application`  
**Description:**  
Implement the student data erasure request workflow as required by LGPD Art. 18. This is a legal mandate.

**Files to create:**
```
src/CFCHub.Domain/Enrollment/DataErasureRequest.cs
src/CFCHub.Domain/Enrollment/DataErasureRequestId.cs
src/CFCHub.Domain/Enrollment/DataErasureRequestStatus.cs
src/CFCHub.Application/Enrollment/Commands/RequestDataErasure/RequestDataErasureCommand.cs
src/CFCHub.Application/Enrollment/Commands/RequestDataErasure/RequestDataErasureCommandHandler.cs
src/CFCHub.Application/Enrollment/Commands/RequestDataErasure/RequestDataErasureCommandValidator.cs
```

**`DataErasureRequest` entity:**
- `StudentId`, `Status` (`Pending`, `Processing`, `Completed`, `Blocked`), `BlockReason?`, `CompletedAt?`
- `static Create(id, studentId, ISystemClock)` — status `Pending`
- `Block(string reason)` — sets `Status = Blocked`, `BlockReason = reason`
- `Complete(ISystemClock)` — sets `Status = Completed`

**`RequestDataErasureCommandHandler` flow:**
1. Load student; check legal holds:
   - Active unsigned contract → `Block("ACTIVE_CONTRACT")`
   - Unpaid debt (any `Installment` with `Status == Overdue`) → `Block("UNPAID_DEBT")`
   - Payment records < 5 years old → partial block (erasure proceeds but payment records retained)
2. If full block → save `DataErasureRequest(Blocked)`, return `202 Accepted` with `BlockReason`
3. If no full block → save `DataErasureRequest(Pending)`, set `Student.Status = PendingErasure`, enqueue `DataErasureWorker` via outbox
4. HTTP response: `202 Accepted` (processing is async)

**Acceptance Criteria:**
- Student with unsigned contract gets `Status = Blocked`
- Student with overdue debt gets `Status = Blocked`
- Student with no legal holds gets `Status = Pending` and outbox message enqueued
- `Student.Status` set to `PendingErasure` atomically with `DataErasureRequest` creation
- Unit test: `RequestErasure_WithActiveContract_BlocksRequest`, `RequestErasure_WithNoHolds_EnqueuesWorker`

**Depends on:** TASK-047, TASK-018, TASK-019, TASK-040

---

## EPIC 17 — Application: Contracts, Finance, and Compliance Use Cases

---

### TASK-050 — Implement Contract use cases

**Layer:** `CFCHub.Application`  
**Description:**  
Implement contract generation trigger (event-driven) and contract signing command. Contract generation itself happens in the `ContractGenerationHandler` outbox handler (TASK-071).

**Files to create:**
```
src/CFCHub.Application/Contracts/Commands/SignContract/SignContractCommand.cs
src/CFCHub.Application/Contracts/Commands/SignContract/SignContractCommandHandler.cs
src/CFCHub.Application/Contracts/Commands/SignContract/SignContractCommandValidator.cs
src/CFCHub.Application/Contracts/Queries/GetContract/GetContractQuery.cs
src/CFCHub.Application/Contracts/Queries/GetContract/GetContractQueryHandler.cs
src/CFCHub.Application/Contracts/Queries/GetContract/ContractResult.cs
src/CFCHub.Application/Enrollment/EventHandlers/StudentEnrolledEventHandler.cs
```

**Implementation notes:**
- `StudentEnrolledEventHandler : INotificationHandler<StudentEnrolledEvent>` — creates `Contract.Create(...)` which raises `ContractGenerationRequestedEvent` → this goes to outbox as `ContractGenerationRequested`; all in same transaction as enrollment
- `SignContractCommandHandler` — load contract; create `SignatureRecord`; call `contract.Sign(...)`; save; fire `ContractSignedEvent` outbox
- `GetContractQueryHandler` — returns `ContractResult` with `S3Key` → handler generates a pre-signed download URL (TTL 3600s); embeds URL in result instead of S3 key

**Acceptance Criteria:**
- `StudentEnrolledEvent` triggers contract creation in same transaction
- `GetContract` returns a pre-signed URL, not the S3 key
- `Sign` when contract not yet generated (`Status != Generated`) returns `UnprocessableException`
- Unit test: `StudentEnrolled_CreatesContract`, `SignContract_WhenPending_ThrowsUnprocessable`

**Depends on:** TASK-048, TASK-018, TASK-036

---

### TASK-051 — Implement Finance use cases

**Layer:** `CFCHub.Application`  
**Description:**  
Implement payment plan creation (event-driven from `StudentEnrolled`), payment recording, and finance queries.

**Files to create:**
```
src/CFCHub.Application/Finance/Commands/RecordPayment/RecordPaymentCommand.cs
src/CFCHub.Application/Finance/Commands/RecordPayment/RecordPaymentCommandHandler.cs
src/CFCHub.Application/Finance/Commands/RecordPayment/RecordPaymentCommandValidator.cs
src/CFCHub.Application/Finance/Queries/GetPaymentPlan/GetPaymentPlanQuery.cs
src/CFCHub.Application/Finance/Queries/GetPaymentPlan/GetPaymentPlanQueryHandler.cs
src/CFCHub.Application/Finance/Queries/GetPaymentPlan/PaymentPlanResult.cs
src/CFCHub.Application/Finance/Queries/GetOverdueInstallments/GetOverdueInstallmentsQuery.cs
src/CFCHub.Application/Finance/Queries/GetOverdueInstallments/GetOverdueInstallmentsQueryHandler.cs
src/CFCHub.Application/Enrollment/EventHandlers/StudentEnrolledPaymentPlanHandler.cs
```

**Implementation notes:**
- `StudentEnrolledPaymentPlanHandler : INotificationHandler<StudentEnrolledEvent>` — creates `Installment` records based on CFC payment plan configuration (e.g., 12 monthly installments); same transaction
- `RecordPaymentCommandHandler`: load `Installment`; call `payment.Confirm(clock)`; `installment.MarkPaid(paymentId)`; outbox → `PaymentReceiptRequested`; `AuditLog` entry (via interceptor)
- `GetOverdueInstallmentsQuery` — `Financial` or `Admin` role only; cursor paginated

**Acceptance Criteria:**
- `StudentEnrolled` creates installments in same transaction
- `RecordPayment` triggers `PaymentReceiptRequested` outbox message
- `GetOverdueInstallments` by `Receptionist` returns `ForbiddenException`
- Unit test: `RecordPayment_Confirm_EnqueuesReceipt`

**Depends on:** TASK-048, TASK-019, TASK-040

---

### TASK-052 — Implement Compliance use cases and DETRAN query

**Layer:** `CFCHub.Application`  
**Description:**  
Implement document registration, expiry tracking queries, and CNH status lookup via DETRAN.

**Files to create:**
```
src/CFCHub.Application/Compliance/Commands/RegisterDocument/RegisterDocumentCommand.cs
src/CFCHub.Application/Compliance/Commands/RegisterDocument/RegisterDocumentCommandHandler.cs
src/CFCHub.Application/Compliance/Queries/GetExpiringDocuments/GetExpiringDocumentsQuery.cs
src/CFCHub.Application/Compliance/Queries/GetExpiringDocuments/GetExpiringDocumentsQueryHandler.cs
src/CFCHub.Application/Compliance/Queries/GetExpiringDocuments/ExpiringDocumentResult.cs
src/CFCHub.Application/Compliance/Queries/GetCnhStatus/GetCnhStatusQuery.cs
src/CFCHub.Application/Compliance/Queries/GetCnhStatus/GetCnhStatusQueryHandler.cs
src/CFCHub.Application/Compliance/Queries/GetCnhStatus/CnhStatusResult.cs
```

**Implementation notes:**
- `RegisterDocumentCommandHandler` — for `MedicalExam` type: generate pre-signed S3 upload URL for `cfchub-{env}-medical` bucket; return URL to client; client uploads directly to S3; separate `PATCH` endpoint records the `S3Key` after upload confirmation
- `GetCnhStatusQueryHandler` — calls `IDetranClient.GetCnhStatusAsync`; if `IsAvailable == false`, return `CnhStatusResult { Status = "Consultar manualmente" }` — never throw for DETRAN unavailability
- Rate limit for DETRAN endpoints: 5 req/min per tenant (enforced in middleware, not handler)

**Acceptance Criteria:**
- Medical document upload URL uses `cfchub-{env}-medical` bucket (separate from documents bucket)
- DETRAN unavailability returns a graceful result, not an exception
- `GetCnhStatus` uses SHA-256 of CPF for cache key — never raw CPF

**Depends on:** TASK-039, TASK-020, TASK-036, TASK-040

---

## EPIC 18 — API: Middleware

---

### TASK-053 — Implement TenantResolutionMiddleware

**Layer:** `CFCHub.Api`  
**Description:**  
Implement the middleware that validates the JWT, extracts `tenant_id`, resolves the tenant schema, and populates `ITenantContext`. See `design.md §3`.

**Files to create:**
```
src/CFCHub.Api/Middleware/TenantResolutionMiddleware.cs
src/CFCHub.Infrastructure/Identity/JwtValidationService.cs
```

**Resolution flow (from `design.md §3`):**
1. Skip resolution for unauthenticated routes: `POST /api/v1/auth/login`, `GET /api/v1/public/**`, `GET /health/**`, `POST /webhooks/**`
2. Validate JWT signature (RS256) — public key from `ISecretsManagerService`
3. Extract `tenant_id` claim
4. Check Redis `TenantResolution(env, slug)` — TTL 300s
5. On cache miss: query `public.tenants WHERE slug = '{slug}' AND status = 'Active'`
6. If not found → `TenantNotFoundException`; if `status != Active` → `ForbiddenException("TENANT_SUSPENDED")`
7. Cache result; call `ITenantContext.Resolve(schemaName, slug, tenantId)`
8. Set `AppDbContext.Database.ExecuteSqlRaw("SET search_path TO {schemaName}, public")`
9. Populate `ICurrentUserService` from JWT claims

**Acceptance Criteria:**
- Unauthenticated routes bypass tenant resolution
- Suspended tenant gets `403 Forbidden`
- `ITenantContext.IsResolved` is `true` after middleware
- Cache hit skips `public.tenants` DB query
- Integration test: `TenantResolution_WithValidJwt_PopulatesTenantContext`

**Depends on:** TASK-021, TASK-035, TASK-038, TASK-041

---

### TASK-054 — Implement SecurityHeadersMiddleware and GlobalExceptionMiddleware

**Layer:** `CFCHub.Api`  
**Description:**  
Implement middleware for security headers (OWASP) and global exception-to-ProblemDetails mapping.

**Files to create:**
```
src/CFCHub.Api/Middleware/SecurityHeadersMiddleware.cs
src/CFCHub.Api/Middleware/GlobalExceptionMiddleware.cs
```

**`SecurityHeadersMiddleware`** — add to every response:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: no-referrer`
- `Content-Security-Policy: default-src 'none'` (API-only, no UI)

**`GlobalExceptionMiddleware`** — maps exceptions to `ProblemDetails` (RFC 7807):
- `CfcHubException` subtypes → use `MapStatusCode` switch from `conventions.md §9`
- `ProblemDetails.type` → registered URI from `docs/error-types.md`
- `ProblemDetails.detail` → pt-BR user-facing message (from resources)
- `ProblemDetails.traceId` → `Activity.Current?.Id`
- NEVER expose: stack trace, inner exception message, SQL error details
- Unhandled exceptions → `500` with generic `"detail": "Erro interno. Tente novamente."` (pt-BR)

**Acceptance Criteria:**
- Every response has the three security headers
- `ValidationException` returns `400` with `errors` dictionary (field → messages)
- `SchedulingConflictException` returns `409`
- Stack traces never appear in response body (verified by integration test)
- `ProblemDetails.detail` is in pt-BR

**Depends on:** TASK-009

---

### TASK-055 — Implement RateLimitMiddleware

**Layer:** `CFCHub.Api`  
**Description:**  
Implement Redis sliding window rate limiter per `{tenant}:{endpoint}:{user_id}`. Enforce limits from `conventions.md §8`.

**Files to create:**
```
src/CFCHub.Api/Middleware/RateLimitMiddleware.cs
src/CFCHub.Infrastructure/Caching/RedisRateLimiter.cs
```

**Implementation notes:**
- Sliding window algorithm using Redis `MULTI/EXEC`: `ZADD key now now; ZREMRANGEBYSCORE key -inf (now-window); ZCARD key`
- Key format: `RedisKeys.RateLimit(env, tenant, SHA256(routeTemplate), userId)` — never raw CPF
- Endpoint groups and limits from `conventions.md §8`:
  - `POST /api/v1/auth/*` → 10 req / 15 min
  - `POST|PATCH /api/v1/scheduling/*` → 30 req / 1 min
  - `GET /api/v1/scheduling/*` → 120 req / 1 min
  - `GET /api/v1/*/detran/*` → 5 req / 1 min
  - `GET /api/v1/public/*` → 60 req / 1 min
  - All others → 120 req / 1 min
- On limit exceeded: `429 Too Many Requests` with `Retry-After: {seconds}` header

**Acceptance Criteria:**
- 11th auth request within 15 min returns `429`
- `Retry-After` header is present and accurate (seconds until window resets)
- Key uses `SHA256(routeTemplate)` — not the raw path (to avoid PII in path params)
- Integration test: `RateLimit_AuthEndpoint_Blocks11thRequest`

**Depends on:** TASK-033, TASK-053

---

### TASK-056 — Implement SES webhook endpoint and bounce handler

**Layer:** `CFCHub.Api`  
**Description:**  
Implement the inbound SES event webhook that handles bounces and complaints. This endpoint is unauthenticated (SNS-signed) but must verify the SNS signature.

**Files to create:**
```
src/CFCHub.Api/Endpoints/Webhooks/SesWebhookEndpoints.cs
src/CFCHub.Domain/Shared/Email/EmailDeliveryLog.cs
src/CFCHub.Domain/Shared/Email/EmailDeliveryLogId.cs
src/CFCHub.Infrastructure/Persistence/Configurations/EmailDeliveryLogConfiguration.cs
```

**Implementation notes:**
- `POST /webhooks/ses/events` — no auth, but verify `x-amz-sns-message-type` and SNS signature
- Parse SNS notification payload; extract SES event type (`Bounce`, `Complaint`, `Delivery`)
- Insert into `email_delivery_logs` table: `id`, `ses_message_id`, `event_type`, `timestamp`, `recipient_address_hash` (SHA-256 of email — not plaintext), `bounce_type?`, `occurred_at`
- Add `email_delivery_logs` table to migration

**Acceptance Criteria:**
- Endpoint is publicly accessible (no JWT required)
- SNS signature verified before processing — unverified requests return `403`
- Email address never stored in plaintext in `email_delivery_logs` — only SHA-256 hash

**Depends on:** TASK-025, TASK-037

---

## EPIC 19 — API: Endpoints

---

### TASK-057 — Implement Auth and Identity endpoints

**Layer:** `CFCHub.Api`  
**Description:**  
Implement Minimal API endpoints for authentication and staff user management.

**Files to create:**
```
src/CFCHub.Api/Endpoints/Auth/AuthEndpoints.cs
src/CFCHub.Api/Endpoints/Identity/StaffUserEndpoints.cs
```

**Routes:**
```
POST   /api/v1/auth/login                 → LoginCommand        (anonymous)
POST   /api/v1/staff-users                → CreateStaffUserCommand
GET    /api/v1/staff-users                → GetStaffUsersQuery  (cursor paginated)
PATCH  /api/v1/staff-users/{userId}/role  → ChangeStaffUserRoleCommand
PATCH  /api/v1/staff-users/{userId}/deactivate → DeactivateStaffUserCommand
```

**Response format** (all endpoints): wrap in `{ "data": {...}, "meta": { "traceId": "...", "timestamp": "..." } }`

**Acceptance Criteria:**
- `POST /auth/login` has `[AllowAnonymous]` — no JWT required
- No other endpoint uses `[AllowAnonymous]`
- `POST /staff-users` returns `201 Created` with `Location: /api/v1/staff-users/{id}`
- List endpoint returns cursor-paginated response with `nextCursor` and `hasMore` in `meta`
- Rate limit applied: auth endpoint group (10/15min)

**Depends on:** TASK-041, TASK-042, TASK-053, TASK-054, TASK-055

---

### TASK-058 — Implement Scheduling endpoints

**Layer:** `CFCHub.Api`  
**Description:**  
Implement Minimal API endpoints for scheduling operations.

**Files to create:**
```
src/CFCHub.Api/Endpoints/Scheduling/SchedulingEndpoints.cs
src/CFCHub.Api/Endpoints/Scheduling/InstructorEndpoints.cs
```

**Routes:**
```
GET    /api/v1/scheduling/slots/available          → GetAvailableSlotsQuery
POST   /api/v1/scheduling/slots                    → BookSlotCommand
GET    /api/v1/scheduling/slots/{slotId}           → GetSlotByIdQuery
PATCH  /api/v1/scheduling/slots/{slotId}/cancel    → CancelSlotCommand
PATCH  /api/v1/scheduling/slots/{slotId}/complete  → CompleteSlotCommand
PATCH  /api/v1/scheduling/slots/{slotId}/no-show   → MarkNoShowCommand
GET    /api/v1/students/{studentId}/slots          → GetSlotsByStudentQuery
GET    /api/v1/instructors/{instructorId}/slots    → GetSlotsByInstructorQuery
GET    /api/v1/instructors/{instructorId}/availability → GetAvailableSlotsQuery (filtered)
```

**Acceptance Criteria:**
- `POST /scheduling/slots` returns `201 Created`
- `PATCH .../cancel`, `.../complete`, `.../no-show` return `200 OK`
- All scheduling POST/PATCH endpoints have rate limit: 30/min
- All scheduling GET endpoints have rate limit: 120/min

**Depends on:** TASK-044, TASK-045, TASK-046, TASK-053, TASK-055

---

### TASK-059 — Implement Enrollment, Contract, Finance, Compliance endpoints

**Layer:** `CFCHub.Api`  
**Description:**  
Implement all remaining domain endpoints.

**Files to create:**
```
src/CFCHub.Api/Endpoints/Enrollment/StudentEndpoints.cs
src/CFCHub.Api/Endpoints/Enrollment/EnrollmentEndpoints.cs
src/CFCHub.Api/Endpoints/Contracts/ContractEndpoints.cs
src/CFCHub.Api/Endpoints/Finance/PaymentEndpoints.cs
src/CFCHub.Api/Endpoints/Compliance/DocumentEndpoints.cs
src/CFCHub.Api/Endpoints/Compliance/DetranEndpoints.cs
src/CFCHub.Api/Endpoints/Public/PublicEndpoints.cs
```

**Routes:**
```
POST   /api/v1/students                              → CreateStudentCommand
GET    /api/v1/students                              → GetStudentsQuery
GET    /api/v1/students/{studentId}                  → GetStudentQuery
DELETE /api/v1/students/{studentId}                  → RequestDataErasureCommand (→ 202 Accepted)
POST   /api/v1/students/{studentId}/enrollments      → EnrollStudentCommand
GET    /api/v1/students/{studentId}/enrollments      → GetEnrollmentsQuery
PATCH  /api/v1/contracts/{contractId}/sign           → SignContractCommand
GET    /api/v1/contracts/{contractId}                → GetContractQuery
POST   /api/v1/payments                              → RecordPaymentCommand
GET    /api/v1/students/{studentId}/payment-plan     → GetPaymentPlanQuery
GET    /api/v1/compliance/documents/expiring         → GetExpiringDocumentsQuery
POST   /api/v1/compliance/documents                  → RegisterDocumentCommand
GET    /api/v1/students/{studentId}/cnh-status       → GetCnhStatusQuery
GET    /api/v1/public/cfc/{slug}                     → GetPublicCfcInfoQuery (anonymous)
GET    /api/v1/public/qr/{code}                      → GetQrCodeInfoQuery   (anonymous)
```

**Acceptance Criteria:**
- `DELETE /students/{id}` returns `202 Accepted` (not `200` or `204`)
- `GET /students/{studentId}/cnh-status` rate limited: 5/min
- Public endpoints (`/public/**`) have `[AllowAnonymous]` and rate limit 60/min
- All list endpoints paginated with cursor in `meta`

**Depends on:** TASK-047, TASK-048, TASK-049, TASK-050, TASK-051, TASK-052, TASK-053, TASK-055

---

### TASK-060 — Implement Health Check endpoints

**Layer:** `CFCHub.Api`, `CFCHub.Workers`  
**Description:**  
Implement liveness and readiness health check endpoints as specified in `design.md §10`.

**Files to create:**
```
src/CFCHub.Api/Endpoints/Health/HealthEndpoints.cs
src/CFCHub.Infrastructure/Health/PostgreSqlHealthCheck.cs
src/CFCHub.Infrastructure/Health/RedisHealthCheck.cs
src/CFCHub.Infrastructure/Health/S3HealthCheck.cs
```

**Routes:**
```
GET /health        → liveness (just returns 200 if process is alive)
GET /health/ready  → readiness: checks PostgreSQL (public schema), Redis PING, S3 HeadBucket
```

**Readiness check:** `AllHealthy` → `200 OK`; any failure → `503 Service Unavailable` with `{ "dependencies": { "postgres": "Unhealthy", "redis": "Healthy", "s3": "Healthy" } }`

**Acceptance Criteria:**
- `GET /health` returns `200` without DB/Redis dependency
- `GET /health/ready` returns `503` when PostgreSQL is unreachable
- Health endpoints are anonymous (no JWT)
- Health endpoints are excluded from rate limiting and tenant resolution

**Depends on:** TASK-053, TASK-054

---

### TASK-061 — Implement API DI composition root and JWT setup

**Layer:** `CFCHub.Api`  
**Description:**  
Implement `Program.cs` — the DI composition root. This is the only layer allowed to reference `CFCHub.Infrastructure`. Configure JWT RS256 bearer authentication.

**Files to create:**
```
src/CFCHub.Api/Program.cs
src/CFCHub.Api/DependencyInjection/InfrastructureExtensions.cs
src/CFCHub.Api/DependencyInjection/ApplicationExtensions.cs
src/CFCHub.Api/DependencyInjection/AuthExtensions.cs
```

**Registration checklist:**
- `MediatR` with all behaviors in correct order (TASK-040)
- All FluentValidation validators from `CFCHub.Application`
- All repositories as `Scoped`
- `ITenantContext` / `TenantContext` as `Scoped`
- `ICurrentUserService` as `Scoped`
- `ISchedulingLockService` / `RedisLockService` as `Singleton` (StackExchange.Redis connection is singleton)
- `IDataProtectionService` as `Singleton`
- `IFileStorageService` as `Singleton`
- `IEmailService` as `Singleton`
- `ISystemClock` / `SystemClock` as `Singleton`
- `IIdGenerator` / `GuidIdGenerator` as `Singleton`
- Serilog, OpenTelemetry, Health Checks
- JWT Bearer RS256 — public key loaded from `ISecretsManagerService` at startup; `TokenValidationParameters`: `ValidateIssuerSigningKey = true`, `ValidAlgorithms = ["RS256"]`

**`TenantMigrationOrchestrator` invocation:**
```csharp
// In Program.cs, before app.Run():
await using var scope = app.Services.CreateAsyncScope();
var orchestrator = scope.ServiceProvider.GetRequiredService<TenantMigrationOrchestrator>();
await orchestrator.RunAsync(CancellationToken.None);
```

**Acceptance Criteria:**
- `dotnet build` produces zero warnings
- `dotnet run` starts the API and `GET /health` returns `200`
- JWT HS256 is not registered — only RS256
- `CFCHub.Infrastructure` is not referenced by `CFCHub.Application` or `CFCHub.Domain`
- Integration test: `Program_WithValidJwt_Returns200`

**Depends on:** TASK-040, TASK-041, TASK-053, TASK-054, TASK-055, TASK-060, TASK-026

---

## EPIC 20 — Background Workers

---

### TASK-062 — Implement LeasedBackgroundService base class

**Layer:** `CFCHub.Workers`  
**Description:**  
Implement the abstract base class for all leased workers. Uses Redis `SETNX` to elect a single processor across ECS tasks. See `design.md §8`.

**Files to create:**
```
src/CFCHub.Workers/Common/LeasedBackgroundService.cs
```

**Implementation** (from `design.md §8`):
```csharp
public abstract class LeasedBackgroundService : BackgroundService
{
    protected abstract string LeaseKey { get; }
    protected abstract TimeSpan LeaseTtl { get; }
    protected abstract TimeSpan PollingInterval { get; }
    protected abstract Task ProcessAsync(CancellationToken ct);

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
}
```

- Workers are `sealed` classes
- Never inject scoped services directly — use `IServiceScopeFactory`
- Always propagate `CancellationToken`

**Acceptance Criteria:**
- `ProcessAsync` is only called when lease is acquired
- Lease is always released in `finally` block
- Lease key uses `RedisKeys` registry — no inline key construction
- Log cycle start at `Debug`, errors at `Error`

**Depends on:** TASK-033, TASK-009

---

### TASK-063 — Implement OutboxWorker

**Layer:** `CFCHub.Workers`  
**Description:**  
Implement the outbox polling worker. This is the core reliability mechanism for all async side effects. See `design.md §5`.

**Files to create:**
```
src/CFCHub.Workers/Outbox/OutboxWorker.cs
src/CFCHub.Workers/Outbox/IOutboxMessageHandler.cs
src/CFCHub.Workers/Outbox/OutboxMessageDispatcher.cs
src/CFCHub.Domain/Shared/Outbox/OutboxMessage.cs
src/CFCHub.Domain/Shared/Outbox/OutboxMessageId.cs
src/CFCHub.Domain/Shared/Outbox/OutboxMessageStatus.cs
```

**`OutboxWorker`** (`LeasedBackgroundService`):
- `LeaseKey = RedisKeys.OutboxWorkerLease(env, tenant)`, TTL 60s, polling 5s
- `ProcessAsync`: SELECT 10 `Pending` messages (`scheduled_after <= now()`) with `FOR UPDATE SKIP LOCKED`; for each → resolve `IOutboxMessageHandler<TType>` from DI; call `HandleAsync(payload, ct)`
- On success: `UPDATE status = 'Processed'`
- On failure (attempts < max_attempts): exponential backoff `scheduled_after = now() + 2^attempts seconds`; `UPDATE status = 'Pending'`
- On exhausted retries: `UPDATE status = 'Failed'`; `LogCritical` → triggers CloudWatch alarm

**`IOutboxMessageHandler` interface:**
```csharp
public interface IOutboxMessageHandler<T> where T : class
{
    Task HandleAsync(T payload, CancellationToken ct);
}
```

**Acceptance Criteria:**
- `FOR UPDATE SKIP LOCKED` used — concurrent workers don't process same message
- Message processed in a transaction: status update is atomic
- Exponential backoff: attempt 1 → 2s, attempt 2 → 4s, attempt 3 → 8s, ..., up to `max_attempts = 5`
- Integration test: `OutboxWorker_OnHandlerCrash_RetriesWithBackoff`
- Integration test: `OutboxWorker_AtomicityTest_CrashBetweenWriteAndDispatch_MessageIsRetried`

**Depends on:** TASK-062, TASK-022, TASK-030

---

### TASK-064 — Implement DocumentExpiryWorker and SlotReminderWorker

**Layer:** `CFCHub.Workers`  
**Description:**  
Implement the daily document expiry alert worker and the slot reminder worker.

**Files to create:**
```
src/CFCHub.Workers/Compliance/DocumentExpiryWorker.cs
src/CFCHub.Workers/Scheduling/SlotReminderWorker.cs
```

**`DocumentExpiryWorker`** (`LeasedBackgroundService`):
- Runs daily at 06:00 UTC; lease TTL 24h; lease key: `RedisKeys.DocExpiryLease(env, date)`
- For each active tenant: query `GetExpiringAsync(today, today + 30 days)`
- Idempotency guard: `last_alert_sent_at < today - 1 day` (from `design.md §8`)
- Determine `AlertTier` based on days until expiry
- For each expiring doc: `document.MarkAlertSent(tier, clock)` → raises `DocumentExpiryAlertRequestedEvent` → insert outbox message → update `last_alert_sent_at` (same transaction)

**`SlotReminderWorker`** (`LeasedBackgroundService`):
- Polling interval 1h; lease TTL 3600s
- Query slots where `started_at BETWEEN now() + 23h AND now() + 25h` and `status = 'Confirmed'` and no reminder sent
- Insert outbox message `SlotReminderRequested` per slot
- Mark reminder sent (add `reminder_sent_at TIMESTAMPTZ` to `scheduling_slots` — add migration)

**Acceptance Criteria:**
- `DocumentExpiryWorker` idempotency: running twice on same day does not send duplicate alerts
- `SlotReminderWorker` does not send duplicate reminders (idempotency via `reminder_sent_at`)
- Both workers only run on lease holder — other instances skip cycle
- Integration test: `DocumentExpiryWorker_WithExpiringDoc_EnqueuesAlert`

**Depends on:** TASK-062, TASK-052, TASK-046

---

### TASK-065 — Implement DataErasureWorker

**Layer:** `CFCHub.Workers`  
**Description:**  
Implement the LGPD data erasure worker. Processes `DataErasureRequest` records with `Status = Pending`. See `design.md §6`.

**Files to create:**
```
src/CFCHub.Workers/Compliance/DataErasureWorker.cs
```

**`DataErasureWorker.ProcessAsync` flow (from `design.md §6`):**
1. SELECT `DataErasureRequest WHERE status = 'Pending'` — one at a time, `FOR UPDATE SKIP LOCKED`
2. Load `Student`
3. `student.Anonymize()` — sets `[REMOVIDO]` values
4. Delete S3 objects under `medical/{tenantSlug}/{studentId}/*` (using privileged IAM role via env var `CFCHUB_MEDICAL_ERASURE_ROLE_ARN`)
5. Soft-delete all `Enrollment` records (`deleted_at = now()`)
6. RETAIN `audit_logs` (legal requirement)
7. RETAIN `payment_records` (fiscal: 5 years)
8. `dataErasureRequest.Complete(clock)`
9. Insert outbox message `DataErasureCompleteNotified`
10. Commit transaction

**Acceptance Criteria:**
- `student.Anonymize()` and soft-delete enrollments happen in same DB transaction
- S3 deletion is separate (not transactional) — log each deletion
- If S3 deletion fails: log `Error` but do NOT roll back DB changes; medical file orphan is acceptable and auditable
- `AuditLog` entries are NEVER deleted
- `payment_records` older than 5 years are deleted; newer ones are retained
- Integration test: `DataErasureWorker_AnonymizesStudent_RetainsPaidInvoices`

**Depends on:** TASK-062, TASK-049, TASK-016, TASK-036

---

## EPIC 21 — PDF Generation (Outbox Handlers)

---

### TASK-066 — Implement ContractGenerationHandler (QuestPDF)

**Layer:** `CFCHub.Workers`  
**Description:**  
Implement the outbox message handler that generates the contract PDF and emails it to the student. Uses QuestPDF. See `design.md §5`.

**Files to create:**
```
src/CFCHub.Workers/Outbox/Handlers/ContractGenerationHandler.cs
src/CFCHub.Workers/Pdf/ContractDocument.cs
src/CFCHub.Workers/Pdf/PdfConfiguration.cs
```

**`ContractGenerationHandler : IOutboxMessageHandler<ContractGenerationRequestedPayload>`:**
1. Deserialize payload (self-contained — no DB query needed per `design.md §5`)
2. Generate PDF: `ContractDocument.Generate(payload)` using QuestPDF
3. Upload to S3: `IFileStorageService.UploadAsync(Documents, tenantSlug, "contracts/{year}/{contractId}.pdf", pdfStream, "application/pdf", ct)`
4. Update `Contract.MarkGenerated(s3Key)` in DB
5. Insert outbox message `ContractReadyNotified` (triggers email with pre-signed URL)

**`ContractDocument`** (QuestPDF):
- CFC logo placeholder (loaded from S3 or config)
- Student name, CNH category, enrollment date, total amount
- LGPD consent clause (mandatory, with policy version and hash)
- Signature fields
- QuestPDF license: Community or Professional — set in `PdfConfiguration.Configure()` called at startup

**Acceptance Criteria:**
- Generated PDF is a valid PDF/A document (verifiable with `qpdf --check`)
- PDF is uploaded to `documents` bucket, not `medical` bucket
- S3 key format: `{tenantSlug}/contracts/{year}/{contractId}.pdf`
- Payload is used directly — no extra DB query in handler
- QuestPDF license configured at startup

**Depends on:** TASK-063, TASK-036, TASK-037

---

### TASK-067 — Implement remaining outbox message handlers

**Layer:** `CFCHub.Workers`  
**Description:**  
Implement all other outbox message handlers: welcome email, slot reminder, payment receipt (PDF), document expiry alert, erasure completion notification.

**Files to create:**
```
src/CFCHub.Workers/Outbox/Handlers/WelcomeEmailHandler.cs
src/CFCHub.Workers/Outbox/Handlers/SlotReminderHandler.cs
src/CFCHub.Workers/Outbox/Handlers/PaymentReceiptHandler.cs
src/CFCHub.Workers/Pdf/PaymentReceiptDocument.cs
src/CFCHub.Workers/Outbox/Handlers/DocumentExpiryAlertHandler.cs
src/CFCHub.Workers/Outbox/Handlers/ErasureNotificationHandler.cs
```

**SES template IDs per handler:**
- `WelcomeEmailHandler` → `cfchub-welcome`; data: `{ student_name, login_url }` — NO CPF or PII
- `SlotReminderHandler` → `cfchub-slot-reminder`; data: `{ student_name, slot_date, instructor_name, cfc_address }` — NO CPF
- `PaymentReceiptHandler` → generates PDF receipt → S3 → `cfchub-payment-receipt` with pre-signed URL (TTL 24h for regular receipt)
- `DocumentExpiryAlertHandler` → `cfchub-doc-expiry-d30` or `cfchub-doc-expiry-d7` based on `AlertTier`; recipient is `Receptionist`/`Admin` staff member
- `ErasureNotificationHandler` → `cfchub-erasure-complete`; data: `{ reference_number }` ONLY — zero PII

**Acceptance Criteria:**
- No handler includes CPF, RG, medical data, or financial detail amounts in SES template data
- `ErasureNotificationHandler` sends only `reference_number`
- `DocumentExpiryAlertHandler` sends to staff, not to the student
- All handlers are registered in DI as `IOutboxMessageHandler<T>` implementations
- Unit test per handler: payload deserialization + correct SES template ID used

**Depends on:** TASK-063, TASK-037, TASK-066

---

## EPIC 22 — Unit Tests

---

### TASK-068 — Unit tests: Domain — Scheduling module

**Layer:** `CFCHub.UnitTests`  
**Description:**  
Implement all unit tests for the Scheduling domain module. Coverage target: 90%+ on `CFCHub.Domain.Scheduling`.

**Files to create:**
```
tests/CFCHub.UnitTests/Scheduling/SchedulingSlotTests.cs
tests/CFCHub.UnitTests/Scheduling/InstructorAvailabilityTests.cs
tests/CFCHub.UnitTests/Scheduling/SlotOverlapSpecTests.cs
tests/CFCHub.UnitTests/Builders/SchedulingSlotBuilder.cs
tests/CFCHub.UnitTests/Builders/InstructorBuilder.cs
tests/CFCHub.UnitTests/Builders/VehicleBuilder.cs
```

**Required test cases:**
- `Book_WithPastStartTime_ThrowsUnprocessableException`
- `Book_WithValidData_SetsEndedAtTo50MinutesAfterStart`
- `Book_WithValidData_RaisesSlotBookedEvent`
- `Cancel_WhenConfirmed_SetsStatusToCancelled`
- `Cancel_WhenCompleted_ThrowsUnprocessableException`
- `Cancel_WhenCancelled_ThrowsUnprocessableException`
- `Complete_WhenConfirmed_RaisesSlotCompletedEvent`
- `MarkNoShow_WhenConfirmed_SetsStatusToNoShow`
- `SlotOverlapSpec_WithOverlappingSlots_ReturnsTrue`
- `SlotOverlapSpec_WithAdjacentSlots_ReturnsFalse` (boundary: `[10:00, 10:50)` and `[10:50, 11:40)` do NOT overlap)
- `InstructorAvailability_WithDayOverride_OverridesWeeklyTemplate`
- `Vehicle_IsAvailableAt_WhenInMaintenance_ReturnsFalse`

**Acceptance Criteria:**
- All tests pass
- Coverage on `CFCHub.Domain.Scheduling` ≥ 90%
- No `Thread.Sleep` — all tests synchronous or use `Task.FromResult`
- Builder pattern used for test data construction

**Depends on:** TASK-013, TASK-014, TASK-015

---

### TASK-069 — Unit tests: Domain — Enrollment, Contracts, Finance modules

**Layer:** `CFCHub.UnitTests`  
**Description:**  
Implement unit tests for Enrollment, Contracts, and Finance domain modules.

**Files to create:**
```
tests/CFCHub.UnitTests/Enrollment/StudentTests.cs
tests/CFCHub.UnitTests/Enrollment/EnrollmentTests.cs
tests/CFCHub.UnitTests/Enrollment/ConsentRecordTests.cs
tests/CFCHub.UnitTests/Contracts/ContractTests.cs
tests/CFCHub.UnitTests/Finance/PaymentTests.cs
tests/CFCHub.UnitTests/Finance/MoneyTests.cs
tests/CFCHub.UnitTests/Builders/StudentBuilder.cs
tests/CFCHub.UnitTests/Builders/EnrollmentBuilder.cs
tests/CFCHub.UnitTests/Builders/ContractBuilder.cs
tests/CFCHub.UnitTests/Builders/PaymentBuilder.cs
```

**Required test cases:**
- `Student_Create_WithInvalidCpfAlgorithm_ThrowsValidationException`
- `Student_Anonymize_WhenNotPendingErasure_ThrowsUnprocessable`
- `Student_Anonymize_SetsCpfToSha256Hash`
- `ConsentRecord_IsImmutable_HasNoUpdateMethod` (reflection check)
- `Enrollment_IncrementPracticalHours_WhenBelowThreshold_DoesNotComplete`
- `Contract_Sign_WhenPending_ThrowsUnprocessable`
- `Contract_Sign_WhenGenerated_RaisesContractSignedEvent`
- `Payment_Confirm_RaisesPaymentReceivedEvent`
- `Payment_Refund_WhenPending_ThrowsUnprocessable`
- `Money_NegativeAmount_ThrowsUnprocessable`

**Acceptance Criteria:**
- All tests pass
- Coverage on each domain module ≥ 90%
- Reflection test confirms `ConsentRecord` has no public `Update*` methods

**Depends on:** TASK-016, TASK-017, TASK-018, TASK-019

---

### TASK-070 — Unit tests: Application — Scheduling handlers

**Layer:** `CFCHub.UnitTests`  
**Description:**  
Implement unit tests for the `BookSlot`, `CancelSlot`, `CompleteSlot`, and `GetAvailableSlots` handlers. Use NSubstitute for mocking.

**Files to create:**
```
tests/CFCHub.UnitTests/Application/Scheduling/BookSlotCommandHandlerTests.cs
tests/CFCHub.UnitTests/Application/Scheduling/CancelSlotCommandHandlerTests.cs
tests/CFCHub.UnitTests/Application/Scheduling/GetAvailableSlotsQueryHandlerTests.cs
```

**Required test cases:**
- `Handle_WhenLockFails_ReturnsConflictResult`
- `Handle_WhenOverlapFoundAfterLock_ReleasesLocksAndReturnsConflict`
- `Handle_WithValidData_InsertsSlotAndOutboxMessage`
- `Handle_LockAlwaysReleasedInFinallyBlock` (even when handler throws)
- `CancelSlot_ByOtherStudent_ReturnsForbidden`
- `GetAvailableSlots_CacheHit_SkipsRepositoryCall`
- `GetAvailableSlots_CacheMiss_CallsRepositoryAndCachesResult`

**Mock targets:** `ISchedulingLockService`, `ISchedulingRepository`, `IAvailabilityCalculatorService`, `AvailabilityCacheService`, `ICurrentUserService`

**Acceptance Criteria:**
- All mocks use NSubstitute — never Moq
- `Handle_LockAlwaysReleasedInFinallyBlock` passes by verifying `ReleaseAllAsync` called even when subsequent code throws
- All tests pass

**Depends on:** TASK-044, TASK-045, TASK-046

---

### TASK-071 — Unit tests: Application — Enrollment, LGPD handlers

**Layer:** `CFCHub.UnitTests`  
**Description:**  
Implement unit tests for enrollment creation, consent capture, field-level access, and erasure request handlers.

**Files to create:**
```
tests/CFCHub.UnitTests/Application/Enrollment/CreateStudentCommandHandlerTests.cs
tests/CFCHub.UnitTests/Application/Enrollment/EnrollStudentCommandHandlerTests.cs
tests/CFCHub.UnitTests/Application/Enrollment/RequestDataErasureCommandHandlerTests.cs
tests/CFCHub.UnitTests/Application/Enrollment/GetStudentQueryHandlerTests.cs
```

**Required test cases:**
- `CreateStudent_WithInvalidCpfAlgorithm_ThrowsValidation`
- `CreateStudent_WithDuplicateCpf_ThrowsConflict`
- `CreateStudent_CreatesConsentRecordInSameUnitOfWork`
- `EnrollStudent_WithExistingEnrollment_ThrowsConflict`
- `EnrollStudent_PublishesThreeOutboxMessages` (contract, payment plan, compliance)
- `RequestErasure_WithActiveContract_BlocksRequest`
- `GetStudent_AsReceptionist_CpfIsNull`
- `GetStudent_AsAdmin_CpfIsPresent`

**Acceptance Criteria:**
- `CreateStudent_CreatesConsentRecordInSameUnitOfWork` verifies both saves happen before `SaveChangesAsync`
- All tests pass with NSubstitute mocks

**Depends on:** TASK-047, TASK-048, TASK-049

---

## EPIC 23 — Integration Tests

---

### TASK-072 — Integration tests: Scheduling concurrent booking

**Layer:** `CFCHub.IntegrationTests`  
**Description:**  
Implement the highest-priority integration tests: concurrent booking simulation proving the lock protocol and PostgreSQL exclusion constraint work correctly together.

**Files to create:**
```
tests/CFCHub.IntegrationTests/Scheduling/ConcurrentBookingTests.cs
tests/CFCHub.IntegrationTests/Common/IntegrationTestBase.cs
tests/CFCHub.IntegrationTests/Builders/SchedulingIntegrationBuilder.cs
```

**`IntegrationTestBase`:**
- Implements `IAsyncLifetime`
- `InitializeAsync`: spin up Testcontainers PostgreSQL 16 + Redis 7; run all migrations against a unique test tenant schema `cfc_test_{Guid.NewGuid():N}` (from `conventions.md §12`)
- `DisposeAsync`: `DROP SCHEMA {schema} CASCADE`

**Test cases:**
- `BookSlot_ConcurrentRequests_OnlyOneSucceeds` — fire 10 concurrent `BookSlotCommand` for same instructor+vehicle+track+time; assert exactly 1 `Result.IsSuccess == true`, 9 conflict results; assert DB has exactly 1 slot
- `BookSlot_ViaExclusionConstraint_BothRedisLocksBypassed` — simulate Redis bypass; send 2 simultaneous `INSERT` commands; assert PostgreSQL raises exclusion constraint violation; verify no data corruption
- `BookSlot_LocksReleasedAfterSuccess` — after successful booking, lock keys don't exist in Redis
- `BookSlot_LocksReleasedAfterFailure` — after failed booking (overlap), lock keys don't exist in Redis

**Acceptance Criteria:**
- Tests use real PostgreSQL 16 (Testcontainers) — never SQLite or InMemory
- Tests use real Redis 7 (Testcontainers)
- Each test class creates its own isolated tenant schema
- `Thread.Sleep` not used — use `Task.WhenAll` for concurrency
- All four test cases pass consistently (run 3 times to check for flakiness)

**Depends on:** TASK-045, TASK-025

---

### TASK-073 — Integration tests: LGPD operations

**Layer:** `CFCHub.IntegrationTests`  
**Description:**  
Integration tests verifying all LGPD mandates work correctly end-to-end.

**Files to create:**
```
tests/CFCHub.IntegrationTests/Lgpd/ConsentCaptureTests.cs
tests/CFCHub.IntegrationTests/Lgpd/AuditLogTests.cs
tests/CFCHub.IntegrationTests/Lgpd/DataErasureTests.cs
tests/CFCHub.IntegrationTests/Lgpd/FieldAccessPolicyTests.cs
```

**Required test cases:**
- `CreateStudent_WithoutConsent_Fails` — `ConsentRecord` creation is mandatory
- `CreateStudent_ConsentRecord_IsImmutableInDb` — verify no UPDATE on `consent_records` is possible
- `UpdateStudent_WritesAuditLog` — after student update, `audit_logs` table has 1 new row
- `AuditLog_ContainsNoPlaintextPii` — audit log `changed_fields` JSONB does not contain plaintext CPF
- `AuditLog_RowLevelSecurity_CannotUpdate` — direct `UPDATE audit_logs` returns PostgreSQL error
- `DataErasure_AnonymizesStudent` — after erasure, `Student.Name == "[REMOVIDO]"` and `Cpf` is a 64-char hex
- `DataErasure_DeletesMedicalS3Objects` — mock S3; verify `DeleteObjectsAsync` called for medical prefix
- `DataErasure_RetainsPaidPayments` — payment records after erasure remain in DB
- `GetStudent_AsReceptionist_CpfNotInResponse` — field-level access enforced at API level

**Acceptance Criteria:**
- All tests use Testcontainers (real PostgreSQL + Redis)
- `AuditLog_RowLevelSecurity_CannotUpdate` fails at DB level (PostgreSQL RLS)
- All tests pass

**Depends on:** TASK-047, TASK-023, TASK-049, TASK-065

---

### TASK-074 — Integration tests: Outbox atomicity and worker processing

**Layer:** `CFCHub.IntegrationTests`  
**Description:**  
Integration tests verifying the outbox pattern atomicity guarantees — the core reliability mechanism.

**Files to create:**
```
tests/CFCHub.IntegrationTests/Outbox/OutboxAtomicityTests.cs
tests/CFCHub.IntegrationTests/Outbox/OutboxWorkerTests.cs
```

**Required test cases:**
- `BookSlot_OutboxMessage_InSameTransactionAsSlot` — roll back the transaction; assert neither `scheduling_slots` nor `outbox_messages` has the record
- `OutboxWorker_ProcessMessage_MarksAsProcessed`
- `OutboxWorker_OnHandlerException_RetriesWithBackoff` — handler throws on first 2 attempts, succeeds on 3rd; verify `attempts = 3` in DB
- `OutboxWorker_OnMaxAttemptsReached_MarksFailed` — handler always throws; after 5 attempts, `status = 'Failed'`
- `OutboxWorker_ForUpdateSkipLocked_NoDuplicateProcessing` — two worker instances simultaneously; assert message processed exactly once

**Acceptance Criteria:**
- `BookSlot_OutboxMessage_InSameTransactionAsSlot` uses `TransactionScope` + forced rollback
- All tests use Testcontainers
- All tests pass

**Depends on:** TASK-063, TASK-045

---

## EPIC 24 — AWS Infrastructure

---

### TASK-075 — Create Terraform modules for PostgreSQL, Redis, S3

**Layer:** DevOps  
**Description:**  
Create Terraform infrastructure as code for all AWS resources. Follows the architecture from `design.md §10`.

**Files to create:**
```
infra/terraform/modules/rds/main.tf
infra/terraform/modules/rds/variables.tf
infra/terraform/modules/rds/outputs.tf
infra/terraform/modules/elasticache/main.tf
infra/terraform/modules/elasticache/variables.tf
infra/terraform/modules/s3/main.tf
infra/terraform/modules/s3/variables.tf
infra/terraform/modules/secrets/main.tf
infra/terraform/environments/prod/main.tf
infra/terraform/environments/stg/main.tf
```

**Resources:**

RDS (`modules/rds`): Aurora PostgreSQL 16 Multi-AZ; instance `db.t4g.medium`; encrypted at rest (KMS); automated backups 7 days; deletion protection enabled; no public accessibility; in private VPC subnet group

ElastiCache (`modules/elasticache`): Redis 7; cluster mode enabled; `cache.t4g.medium`; at-rest encryption; in-transit TLS; multi-AZ

S3 (`modules/s3`): Two buckets — `cfchub-{env}-documents` and `cfchub-{env}-medical`; medical bucket: S3 Object Lock enabled (WORM, 5-year retention); server-side encryption (SSE-S3); block all public access; separate IAM policies

Secrets (`modules/secrets`): Secrets Manager secrets for DB connection string, Redis connection string, JWT private key (RSA-4096), data protection keys

**Acceptance Criteria:**
- `terraform plan` produces no errors in `environments/prod`
- Medical S3 bucket has Object Lock enabled with 5-year retention
- No hardcoded credentials or account IDs
- RDS is in private subnet — no public endpoint
- Both S3 buckets block all public access

**Depends on:** TASK-001

---

### TASK-076 — Create ECS Fargate task definitions and ALB configuration

**Layer:** DevOps  
**Description:**  
Create Terraform for ECS Fargate services for `CFCHub.Api` and `CFCHub.Workers`, plus Application Load Balancer.

**Files to create:**
```
infra/terraform/modules/ecs-api/main.tf
infra/terraform/modules/ecs-workers/main.tf
infra/terraform/modules/alb/main.tf
infra/terraform/modules/ecr/main.tf
```

**ECS configuration:**
- `CFCHub.Api`: 2 tasks minimum; CPU 512, memory 1024; health check on `/health/ready` (liveness: `/health`)
- `CFCHub.Workers`: 1 task; CPU 256, memory 512
- ALB: HTTPS only; ACM wildcard cert `*.cfchub.com.br`; HTTP → HTTPS redirect; health check path `/health/ready`
- ECR: private repository per image; image scanning on push; lifecycle policy: keep last 10 images
- OIDC trust policy for GitHub Actions (no stored credentials)

**Acceptance Criteria:**
- `terraform plan` produces no errors
- ALB forwards HTTP to HTTPS (301 redirect)
- ECS uses least-privilege IAM task roles
- Workers task definition deploys only 1 task (not auto-scaled — uses Redis lease for multi-instance safety)

**Depends on:** TASK-075

---

### TASK-077 — Configure CloudWatch alarms and observability

**Layer:** DevOps  
**Description:**  
Create CloudWatch alarms matching `design.md §11`. Configure SNS topics for PagerDuty routing.

**Files to create:**
```
infra/terraform/modules/observability/main.tf
infra/terraform/modules/observability/alarms.tf
```

**Alarms (from `design.md §11`):**
- `OutboxFailureRate` — `outbox_messages WHERE status = 'Failed'` count > 0 in 5min → SNS → PagerDuty
- `ApiErrorRate` — HTTP 5xx > 1% of requests in 5min → SNS → PagerDuty
- `SchedulingConflictSpike` — HTTP 409 on scheduling > 10/min → SNS → email
- `MedicalFileDirectAccess` — S3 server access log: any `GetObject` on medical bucket NOT via pre-signed URL → SNS → PagerDuty (security)
- ALB latency p99 > 2s → SNS → email

**CloudWatch Log Insights queries:** pre-create as named queries for common operational investigations (slow handlers, failed outbox messages, LGPD erasure completions).

**Acceptance Criteria:**
- All 5 alarms from `design.md §11` are created
- SNS topic has PagerDuty endpoint subscribed
- `MedicalFileDirectAccess` alarm is classified as a security event (PagerDuty)

**Depends on:** TASK-075

---

## EPIC 25 — Production Readiness

---

### TASK-078 — JWT key generation and Secrets Manager setup script

**Layer:** DevOps  
**Description:**  
Create a one-time setup script that generates an RSA-4096 key pair for JWT signing and stores it in AWS Secrets Manager. Document the key rotation procedure.

**Files to create:**
```
scripts/setup-secrets.sh
scripts/rotate-jwt-key.sh
docs/key-rotation.md
```

**`setup-secrets.sh`:**
1. Generate RSA-4096 private key: `openssl genrsa -out /tmp/jwt-private.pem 4096`
2. Extract public key: `openssl rsa -pubout -in /tmp/jwt-private.pem -out /tmp/jwt-public.pem`
3. Store private key in Secrets Manager at `$CFCHUB_JWT_PRIVATE_KEY_ARN`
4. Store public key alongside (for validation)
5. Shred key files from disk: `shred -u /tmp/jwt-private.pem`
6. Repeat for data protection keys (one per tenant or a global KMS-wrapped key)

**`rotate-jwt-key.sh`:** rotate key without redeployment — new key stored; API loads at runtime via `ISecretsManagerService`; old tokens remain valid until expiry (1h)

**Acceptance Criteria:**
- Script runs without storing key on disk after upload (shred)
- Private key never appears in any log or source file
- `docs/key-rotation.md` documents the procedure and rollback steps

**Depends on:** TASK-038

---

### TASK-079 — SES email template creation script

**Layer:** DevOps  
**Description:**  
Create AWS CLI script to register all 7 SES email templates (see `design.md §7`). Templates are in pt-BR.

**Files to create:**
```
scripts/create-ses-templates.sh
src/CFCHub.Infrastructure/Email/Templates/cfchub-welcome.json
src/CFCHub.Infrastructure/Email/Templates/cfchub-slot-reminder.json
src/CFCHub.Infrastructure/Email/Templates/cfchub-contract-ready.json
src/CFCHub.Infrastructure/Email/Templates/cfchub-payment-receipt.json
src/CFCHub.Infrastructure/Email/Templates/cfchub-doc-expiry-d30.json
src/CFCHub.Infrastructure/Email/Templates/cfchub-doc-expiry-d7.json
src/CFCHub.Infrastructure/Email/Templates/cfchub-erasure-complete.json
```

**Template rules (from `design.md §7` and `GEMINI.md §6`):**
- `cfchub-erasure-complete`: only `{{reference_number}}` variable — no PII whatsoever
- No template body contains CPF, RG, financial amounts, or medical data
- All templates: `From = "CFCHub <noreply@cfchub.com.br>"`
- Subject lines in pt-BR

**Acceptance Criteria:**
- `aws ses create-template` succeeds for all 7 templates
- `cfchub-erasure-complete` template body contains only `{{reference_number}}` as variable
- No template contains `{{cpf}}`, `{{rg}}`, `{{medical}}`, or `{{amount}}` variables

**Depends on:** TASK-037

---

### TASK-080 — Performance baseline and query analysis

**Layer:** All  
**Description:**  
Run k6 load test against the staging environment. Identify slow queries using `pg_stat_statements`. Add missing indexes if needed. Document baselines.

**Files to create:**
```
tests/load/booking-flow.js
tests/load/availability-check.js
docs/performance-baseline.md
```

**Load test scenarios (k6):**
- Availability check: 200 VUs, 60s — `GET /api/v1/scheduling/slots/available?date={today}&category=B`; target p99 < 200ms
- Booking flow: 50 VUs, 60s — concurrent `POST /api/v1/scheduling/slots`; target p99 < 500ms; assert error rate < 1% (conflict is expected, 5xx is not)
- Student list: 100 VUs, 30s — `GET /api/v1/students?limit=20`; target p99 < 100ms

**Acceptance Criteria:**
- Availability check p99 < 200ms with 200 VUs
- Booking flow p99 < 500ms with 50 VUs
- Zero HTTP 5xx errors under load
- `docs/performance-baseline.md` documents p50/p95/p99 for each scenario
- Any slow query identified (> 50ms) has a corresponding index added via migration

**Depends on:** TASK-025, TASK-044, TASK-045

---

### TASK-081 — OpenAPI/Swagger documentation

**Layer:** `CFCHub.Api`  
**Description:**  
Configure Swagger/OpenAPI for the API. Available in development and staging only. Document all endpoints with request/response schemas.

**Files to create:**
```
src/CFCHub.Api/OpenApi/SwaggerConfiguration.cs
src/CFCHub.Api/OpenApi/SecuritySchemeFilter.cs
```

**Implementation notes:**
- Use `Microsoft.AspNetCore.OpenApi` (built-in .NET 9/10)
- Swagger UI only enabled in `dev` and `stg` environments — never production
- Document JWT Bearer auth scheme
- All endpoint responses documented with 400, 401, 403, 404, 409, 422, 500 schemas
- `ProblemDetails` schema registered as standard error response

**Acceptance Criteria:**
- `GET /swagger` returns Swagger UI in dev/stg
- `GET /swagger` returns 404 in production
- JWT Bearer auth scheme visible in Swagger UI
- All API endpoints appear in Swagger with request/response schemas

**Depends on:** TASK-057, TASK-058, TASK-059

---

### TASK-082 — Final security review checklist

**Layer:** All  
**Description:**  
Perform a structured security review against all mandates in `GEMINI.md §6`, `§7`, `§10`. Create a signed-off checklist document.

**Files to create:**
```
docs/security-review.md
```

**Checklist (verify each item in code + integration tests):**

**LGPD §6:**
- [ ] All `SensitivePersonal` and `SensitiveSpecial` fields use `EncryptedStringConverter`
- [ ] `AuditInterceptor` fires for Student, MedicalExam, Contract, Payment mutations
- [ ] No PII in any Serilog log statement (scan with `SensitiveDataDestructuringPolicy` test)
- [ ] No hard-delete path for `Student` or `Enrollment` without `DataErasureRequest`
- [ ] Every `Enrollment` creation has a `ConsentRecord`
- [ ] `FieldAccessPolicy` enforced in all `GetStudent`-type query handlers
- [ ] Medical data only in `cfchub-{env}-medical` bucket
- [ ] Medical pre-signed URL TTL ≤ 900s
- [ ] No PII in Redis keys or values (scan `RedisKeys` usage)
- [ ] No PII in SES email bodies (scan template files)

**Security §7:**
- [ ] All non-public endpoints require JWT
- [ ] `tenant_id` sourced only from JWT claim
- [ ] All Commands/Queries have FluentValidation validators
- [ ] File magic bytes validated before S3 upload
- [ ] Pre-signed URLs: upload ≤ 300s, download ≤ 3600s, medical ≤ 900s
- [ ] Rate limiting active for all endpoint groups
- [ ] ProblemDetails used for all errors — no stack traces
- [ ] Secrets only from Secrets Manager / env vars — not in source
- [ ] JWT key rotation doesn't require redeployment
- [ ] Security headers on all responses

**Forbidden actions §10:**
- [ ] No `DateTime.UtcNow` or `DateTime.Now` in domain code — uses `ISystemClock`
- [ ] No `Guid.NewGuid()` in domain entities — uses `IIdGenerator`
- [ ] No base `Exception` catch without logging + rethrow
- [ ] No Redis key without TTL
- [ ] No offset pagination (`Skip`/`Take`) in list endpoints

**Acceptance Criteria:**
- Every checkbox verified with a code reference or test name
- Document signed off and committed to `docs/security-review.md`
- No unresolved items

**Depends on:** All previous tasks

---

## Summary

| Epic | Tasks | Layer Focus |
|------|-------|-------------|
| 0 — Scaffolding | TASK-001 to TASK-006 | DevOps, All |
| 1 — Domain Shared Kernel | TASK-007 to TASK-010 | Domain |
| 2 — Domain Identity | TASK-011 to TASK-012 | Domain |
| 3 — Domain Scheduling | TASK-013 to TASK-015 | Domain |
| 4 — Domain Enrollment | TASK-016 to TASK-017 | Domain |
| 5 — Domain Contracts | TASK-018 | Domain |
| 6 — Domain Finance | TASK-019 | Domain |
| 7 — Domain Compliance | TASK-020 | Domain |
| 8 — Infrastructure Persistence | TASK-021 to TASK-026 | Infrastructure |
| 9 — EF Configurations | TASK-027 to TASK-030 | Infrastructure |
| 10 — Repositories | TASK-031 to TASK-032 | Infrastructure |
| 11 — Redis Caching | TASK-033 to TASK-035 | Infrastructure |
| 12 — AWS Integration | TASK-036 to TASK-039 | Infrastructure |
| 13 — Pipeline Behaviors | TASK-040 | Application |
| 14 — Identity Use Cases | TASK-041 to TASK-043 | Application |
| 15 — Scheduling Use Cases | TASK-044 to TASK-046 | Application |
| 16 — Enrollment Use Cases | TASK-047 to TASK-049 | Application |
| 17 — Contracts/Finance/Compliance | TASK-050 to TASK-052 | Application |
| 18 — API Middleware | TASK-053 to TASK-056 | API |
| 19 — API Endpoints | TASK-057 to TASK-061 | API |
| 20 — Background Workers | TASK-062 to TASK-065 | Workers |
| 21 — PDF + Outbox Handlers | TASK-066 to TASK-067 | Workers |
| 22 — Unit Tests | TASK-068 to TASK-071 | Tests |
| 23 — Integration Tests | TASK-072 to TASK-074 | Tests |
| 24 — AWS Infrastructure | TASK-075 to TASK-077 | DevOps |
| 25 — Production Readiness | TASK-078 to TASK-082 | All |

**Total: 82 tasks**

---

## Dependency Graph (critical path)

```
TASK-001 → TASK-007 → TASK-008 → TASK-009 → TASK-013 → TASK-014 → TASK-015
                                     ↓              ↓
                                TASK-016         TASK-021 → TASK-022 → TASK-023
                                     ↓                         ↓
                                TASK-017 → TASK-018    TASK-024 → TASK-025 → TASK-026
                                     ↓
                                TASK-019
                                     ↓
                                TASK-020
                                              ↓
                              TASK-027..030 → TASK-031..032
                                              ↓
                              TASK-033 → TASK-034 → TASK-035
                              TASK-036..039
                                              ↓
                              TASK-040 → TASK-041..052 (Application)
                                              ↓
                              TASK-053..056 (Middleware)
                                              ↓
                              TASK-057..061 (Endpoints)
                                              ↓
                              TASK-062..067 (Workers)
                                              ↓
                              TASK-068..074 (Tests)
                                              ↓
                              TASK-075..082 (Infra + Production)
```

> Start every session by reading `GEMINI.md` in full. When a task references `design.md §N` or `conventions.md §N`, read that specific section before implementing.
