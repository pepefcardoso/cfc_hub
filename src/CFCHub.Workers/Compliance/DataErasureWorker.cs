using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Outbox;
using CFCHub.Infrastructure.Caching;
using CFCHub.Infrastructure.Persistence;
using CFCHub.Workers.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CFCHub.Workers.Compliance;

public class DataErasureWorker : LeasedBackgroundService
{
    private readonly IHostEnvironment _env;

    public DataErasureWorker(
        IConnectionMultiplexer redis,
        ILogger<DataErasureWorker> logger,
        IServiceScopeFactory serviceScopeFactory,
        IHostEnvironment env)
        : base(redis, logger, serviceScopeFactory)
    {
        _env = env;
    }

    protected override string LeaseKey => RedisKeys.OutboxWorkerLease(_env.EnvironmentName, "data_erasure");
    protected override TimeSpan LeaseTtl => TimeSpan.FromSeconds(60);
    protected override TimeSpan PollingInterval => TimeSpan.FromSeconds(30);

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        using var scope = ServiceScopeFactory.CreateScope();
        var tenantRegistry = scope.ServiceProvider.GetRequiredService<ITenantRegistry>();
        var activeTenants = await tenantRegistry.GetActiveTenantsAsync(ct);

        foreach (var tenant in activeTenants)
        {
            await ProcessTenantDataErasureAsync(tenant, ct);
        }
    }

    private async Task ProcessTenantDataErasureAsync(TenantRecord tenant, CancellationToken ct)
    {
        using var scope = ServiceScopeFactory.CreateScope();
        
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        if (tenantContext is TenantContext concreteContext)
        {
            concreteContext.Resolve(tenant.SchemaName, tenant.Slug, tenant.Id);
        }
        else
        {
            throw new InvalidOperationException("ITenantContext is not a TenantContext");
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataErasureWorker>>();
        var clock = scope.ServiceProvider.GetRequiredService<ISystemClock>();

        // Wrap in transaction to hold the FOR UPDATE SKIP LOCKED locks
        using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

#pragma warning disable EF1002
        var pendingRequestIds = await dbContext.Database
            .SqlQueryRaw<Guid>($"SELECT id FROM {tenant.SchemaName}.data_erasure_requests WHERE status = {(int)DataErasureRequestStatus.Pending} ORDER BY created_at ASC LIMIT 10 FOR UPDATE SKIP LOCKED")
            .ToListAsync(ct);
#pragma warning restore EF1002

        if (!pendingRequestIds.Any())
        {
            await transaction.RollbackAsync(ct);
            return;
        }

        var erasureRequests = await dbContext.DataErasureRequests
            .Where(r => pendingRequestIds.Contains(r.Id.Value))
            .ToListAsync(ct);

        foreach (var request in erasureRequests)
        {
            var student = await dbContext.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId, ct);
            if (student == null)
            {
                request.Block("Student not found");
                await dbContext.SaveChangesAsync(ct);
                continue;
            }

            try
            {
                // 3. student.Anonymize()
                student.Anonymize(clock);

                // 4. Delete S3 objects under medical/{tenantSlug}/{studentId}/* (Not transactional)
                await DeleteS3MedicalFilesAsync(tenant.Slug, student.Id.Value.ToString(), logger, ct);

                // 5. Soft-delete all Enrollment records
                var enrollments = await dbContext.Enrollments
                    .Where(e => e.StudentId == student.Id)
                    .ToListAsync(ct);

                foreach (var enrollment in enrollments)
                {
                    enrollment.SoftDelete(clock);
                }

                // 6 & 7. RETAIN audit_logs (never delete). RETAIN payment_records (fiscal: 5 years).
                var fiveYearsAgo = clock.UtcNow.AddYears(-5);
                var oldPayments = await dbContext.Payments
                    .Where(p => p.StudentId == student.Id && p.CreatedAt < fiveYearsAgo)
                    .ToListAsync(ct);
                
                dbContext.Payments.RemoveRange(oldPayments);

                // 8. Complete erasure request
                request.Complete(clock);

                // 9. Insert outbox message DataErasureCompleteNotified
                var payload = JsonSerializer.Serialize(new { RequestId = request.Id.Value, StudentId = student.Id.Value });
                var outboxMessage = OutboxMessage.Create("DataErasureCompleteNotified", payload, clock.UtcNow);
                dbContext.OutboxMessages.Add(outboxMessage);

                // 10. Commit transaction (SaveChanges will be part of the outer transaction)
                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("Data erasure completed for student {StudentId} in tenant {TenantSlug}", student.Id.Value, tenant.Slug);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process data erasure request {RequestId} for tenant {TenantSlug}", request.Id.Value, tenant.Slug);
                // In case of exception, we throw to rollback this batch and retry.
                // Or we could try to clear ChangeTracker and continue, but since we lock, we just abort this tenant's batch.
                throw;
            }
        }

        await transaction.CommitAsync(ct);
    }

    protected virtual IAmazonSecurityTokenService CreateStsClient() => new AmazonSecurityTokenServiceClient();
    
    protected virtual IAmazonS3 CreateS3Client(AWSCredentials credentials) => new AmazonS3Client(credentials);

    private async Task DeleteS3MedicalFilesAsync(string tenantSlug, string studentId, ILogger logger, CancellationToken ct)
    {
        var roleArn = Environment.GetEnvironmentVariable("CFCHUB_MEDICAL_ERASURE_ROLE_ARN");
        if (string.IsNullOrEmpty(roleArn))
        {
            logger.LogError("CFCHUB_MEDICAL_ERASURE_ROLE_ARN is not set. Cannot assume role to delete S3 files.");
            return;
        }

        var bucketName = Environment.GetEnvironmentVariable("CFCHUB_MEDICAL_BUCKET_NAME") ?? "cfchub-local-medical";

        try
        {
            using var stsClient = CreateStsClient();
            var assumeRoleResponse = await stsClient.AssumeRoleAsync(new AssumeRoleRequest
            {
                RoleArn = roleArn,
                RoleSessionName = $"Erasure-{studentId}"
            }, ct);

            var credentials = new SessionAWSCredentials(
                assumeRoleResponse.Credentials.AccessKeyId,
                assumeRoleResponse.Credentials.SecretAccessKey,
                assumeRoleResponse.Credentials.SessionToken);

            using var s3Client = CreateS3Client(credentials);
            
            var prefix = $"medical/{tenantSlug}/{studentId}/";

            var listRequest = new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = prefix
            };

            ListObjectsV2Response listResponse;
            do
            {
                listResponse = await s3Client.ListObjectsV2Async(listRequest, ct);

                if (listResponse.S3Objects.Any())
                {
                    var deleteRequest = new DeleteObjectsRequest
                    {
                        BucketName = bucketName,
                        Objects = listResponse.S3Objects.Select(o => new KeyVersion { Key = o.Key }).ToList()
                    };

                    await s3Client.DeleteObjectsAsync(deleteRequest, ct);
                    
                    foreach (var s3Obj in listResponse.S3Objects)
                    {
                        logger.LogInformation("Deleted medical file {S3Key} for student {StudentId} in tenant {TenantSlug}", s3Obj.Key, studentId, tenantSlug);
                    }
                }

                listRequest.ContinuationToken = listResponse.NextContinuationToken;
            } while (listResponse.IsTruncated);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete S3 objects for student {StudentId} in tenant {TenantSlug}. S3 deletion errors are ignored.", studentId, tenantSlug);
            // S3 failure should not fail the DB transaction as per requirements:
            // "If S3 deletion fails: log Error but do NOT roll back DB changes; medical file orphan is acceptable and auditable"
        }
    }
}
