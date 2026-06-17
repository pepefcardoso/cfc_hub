using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CFCHub.Infrastructure.Health;

public class S3HealthCheck : IHealthCheck
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3HealthCheck(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["AWS_S3_DOCUMENTS_BUCKET"] 
            ?? Environment.GetEnvironmentVariable("AWS_S3_DOCUMENTS_BUCKET")
            ?? throw new ArgumentException("AWS_S3_DOCUMENTS_BUCKET is missing");
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verifies if bucket exists and is reachable
            // DoesS3BucketExistV2Async performs a HeadBucket operation under the hood
            var exists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName);
            if (!exists)
            {
                return HealthCheckResult.Unhealthy($"Bucket {_bucketName} does not exist or is not accessible.");
            }
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(exception: ex);
        }
    }
}
