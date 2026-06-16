using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Common.Models;
using CFCHub.Domain.Shared.Exceptions;
using Microsoft.Extensions.Configuration;

namespace CFCHub.Infrastructure.Storage;

public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _documentsBucket;
    private readonly string _medicalBucket;

    public S3FileStorageService(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _documentsBucket = configuration["AWS_S3_DOCUMENTS_BUCKET"] 
            ?? throw new ArgumentException("AWS_S3_DOCUMENTS_BUCKET is missing");
        _medicalBucket = configuration["AWS_S3_MEDICAL_BUCKET"] 
            ?? throw new ArgumentException("AWS_S3_MEDICAL_BUCKET is missing");
    }

    private string GetBucketName(StorageTarget target) => target switch
    {
        StorageTarget.Medical => _medicalBucket,
        StorageTarget.Documents => _documentsBucket,
        _ => _documentsBucket
    };

    private TimeSpan GetDownloadUrlTtl(StorageTarget target) => target switch
    {
        StorageTarget.Medical   => TimeSpan.FromMinutes(15),
        StorageTarget.Documents => TimeSpan.FromHours(1),
        _                       => TimeSpan.FromHours(1)
    };

    private void ValidateContentType(string contentType)
    {
        if (contentType != "application/pdf" && contentType != "image/jpeg" && contentType != "image/png")
        {
            throw new StorageException("INVALID_FILE_TYPE");
        }
    }

    public Task<PresignedUploadUrl> GenerateUploadUrlAsync(StorageTarget target, string tenantSlug, string objectKey, string contentType, CancellationToken ct = default)
    {
        ValidateContentType(contentType);

        var bucketName = GetBucketName(target);
        var key = $"{tenantSlug}/{objectKey}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(5),
            ContentType = contentType
        };

        var url = _s3Client.GetPreSignedURL(request);
        return Task.FromResult(new PresignedUploadUrl(url, key, request.Expires));
    }

    public async Task UploadAsync(StorageTarget target, string tenantSlug, string objectKey, Stream content, string contentType, CancellationToken ct = default)
    {
        ValidateContentType(contentType);

        if (!content.CanRead || !content.CanSeek)
        {
            throw new ArgumentException("Stream must be readable and seekable.");
        }

        var buffer = new byte[16];
        int bytesRead = await content.ReadAsync(buffer, 0, 16, ct);
        
        if (bytesRead < 3)
        {
            throw new StorageException("INVALID_FILE_TYPE");
        }

        bool isPdf = buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46; // %PDF
        bool isJpeg = buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF; // FF D8 FF
        bool isPng = buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47; // 89 50 4E 47

        if (!isPdf && !isJpeg && !isPng)
        {
            throw new StorageException("INVALID_FILE_TYPE");
        }

        content.Position = 0;

        var bucketName = GetBucketName(target);
        var key = $"{tenantSlug}/{objectKey}";

        var putRequest = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(putRequest, ct);
    }

    public Task<string> GenerateDownloadUrlAsync(StorageTarget target, string tenantSlug, string objectKey, TimeSpan? customTtl = null, CancellationToken ct = default)
    {
        var bucketName = GetBucketName(target);
        var key = $"{tenantSlug}/{objectKey}";
        
        var maxTtl = GetDownloadUrlTtl(target);
        var ttl = customTtl ?? maxTtl;

        if (ttl > maxTtl)
        {
            throw new ArgumentOutOfRangeException(nameof(customTtl), $"TTL cannot exceed {maxTtl.TotalSeconds} seconds for {target} files.");
        }

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(ttl)
        };

        var url = _s3Client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }
}
