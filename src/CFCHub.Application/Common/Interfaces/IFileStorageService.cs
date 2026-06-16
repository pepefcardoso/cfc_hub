using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Models;

namespace CFCHub.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<PresignedUploadUrl> GenerateUploadUrlAsync(
        StorageTarget target,
        string tenantSlug,
        string objectKey,
        string contentType,
        CancellationToken ct = default);

    Task UploadAsync(
        StorageTarget target,
        string tenantSlug,
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken ct = default);

    Task<string> GenerateDownloadUrlAsync(
        StorageTarget target,
        string tenantSlug,
        string objectKey,
        TimeSpan? customTtl = null,
        CancellationToken ct = default);
}
