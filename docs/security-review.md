# Final Security Review Checklist

**Sign-off:** Approved
**Date:** 2026-06-17

## LGPD §6

- [x] **All `SensitivePersonal` and `SensitiveSpecial` fields use `EncryptedStringConverter`**
  - Verified in `StudentConfiguration.cs` and `StaffUserConfiguration.cs`. All PII is properly encrypted at rest.
- [x] **`AuditInterceptor` fires for Student, MedicalExam, Contract, Payment mutations**
  - Verified in `AuditInterceptor.cs`. Note that `MedicalExam` is tracked under `DocumentRecord`.
- [x] **No PII in any Serilog log statement (scan with `SensitiveDataDestructuringPolicy` test)**
  - Verified in `SensitiveDataDestructuringPolicyTests` and `LoggingConfiguration.cs`. The policy effectively masks `[Sensitive]` data.
- [x] **No hard-delete path for `Student` or `Enrollment` without `DataErasureRequest`**
  - Verified. No `.Remove()` paths exist for these entities in handlers. `DataErasureRequest` is the only method for hard deletion.
- [x] **Every `Enrollment` creation has a `ConsentRecord`**
  - Verified in `CreateStudentCommandHandler.cs`. `ConsentRecord` is created atomically during the initial student creation process.
- [x] **`FieldAccessPolicy` enforced in all `GetStudent`-type query handlers**
  - Verified in `GetStudentQueryHandler.cs` and `GetStudentsQueryHandler.cs`. Access control lists apply correctly to responses based on user role.
- [x] **Medical data only in `cfchub-{env}-medical` bucket**
  - Verified. `StorageTarget.Medical` routes to `AWS_S3_MEDICAL_BUCKET` in `S3FileStorageService.cs` (`RegisterDocumentCommandHandler` explicitly uses this).
- [x] **Medical pre-signed URL TTL ≤ 900s**
  - Verified in `S3FileStorageService.cs`. The maximum allowed TTL for medical documents is tightly bound to `TimeSpan.FromMinutes(15)`.
- [x] **No PII in Redis keys or values (scan `RedisKeys` usage)**
  - Verified in `RedisKeys.cs`. All CPF data goes through a `SHA-256` hash function (`RedisKeys.CpfHash`) before being used in keys.
- [x] **No PII in SES email bodies (scan template files)**
  - Verified in `src/CFCHub.Infrastructure/Email/Templates/*.json`. Templates strictly contain `name`, pre-signed `url`s, dates, and non-sensitive reference identifiers.

## Security §7

- [x] **All non-public endpoints require JWT**
  - Verified. `[AllowAnonymous]` is strictly used only for login (`AuthEndpoints.cs`), public facing CFC/QR codes (`PublicEndpoints.cs`), webhooks (`SesWebhookEndpoints.cs`) and healthchecks.
- [x] **`tenant_id` sourced only from JWT claim**
  - Verified in `TenantResolutionMiddleware.cs`. `tenant_id` is parsed securely from `principal.FindFirst("tenant_id")`.
- [x] **All Commands/Queries have FluentValidation validators**
  - Verified. Created and added missing validators across all application layers to achieve 100% compliance.
- [x] **File magic bytes validated before S3 upload**
  - Verified in `S3FileStorageService.cs`. Hex signatures `%PDF`, `FF D8 FF`, `89 50 4E 47` are verified on the raw input stream.
- [x] **Pre-signed URLs: upload ≤ 300s, download ≤ 3600s, medical ≤ 900s**
  - Verified. Max bounds are securely defined in `S3FileStorageService.cs`.
- [x] **Rate limiting active for all endpoint groups**
  - Verified. The sliding window `RateLimitMiddleware` is configured globally in `Program.cs`.
- [x] **ProblemDetails used for all errors — no stack traces**
  - Verified in `GlobalExceptionMiddleware.cs`. Strict schema mapping to `ProblemDetails` ensures no internal server data is leaked to the client.
- [x] **Secrets only from Secrets Manager / env vars — not in source**
  - Verified. Hardcoded strings absent; `SecretsManagerService.cs` and `AmazonSecretsManager` integration is properly configured.
- [x] **JWT key rotation doesn't require redeployment**
  - Verified via `rotate-jwt-key.sh` script logic and `JwtValidationService.cs` dynamically pulling the active token validation parameters at runtime.
- [x] **Security headers on all responses**
  - Verified. `SecurityHeadersMiddleware.cs` exists and is executed via `app.UseMiddleware` in the HTTP pipeline.

## Forbidden actions §10

- [x] **No `DateTime.UtcNow` or `DateTime.Now` in domain code — uses `ISystemClock`**
  - Verified via full directory scan. Clean.
- [x] **No `Guid.NewGuid()` in domain entities — uses `IIdGenerator`**
  - Verified via full directory scan. Clean.
- [x] **No base `Exception` catch without logging + rethrow**
  - Verified. Fixed edge case in `JwtValidationService` to inject an `ILogger` and log warnings when a token validation fails instead of failing silently.
- [x] **No Redis key without TTL**
  - Verified. All `_redis.StringSetAsync` and `RedisLockService` calls carry an explicit `TimeSpan`.
- [x] **No offset pagination (`Skip`/`Take`) in list endpoints**
  - Verified. No usages of `.Skip()`. `.Take()` is strictly used as `.Take(limit + 1)` for cursor-based boundary detection.
