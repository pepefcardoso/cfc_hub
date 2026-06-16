using System;

namespace CFCHub.Application.Common.Models;

public record PresignedUploadUrl(string Url, string ObjectKey, DateTimeOffset ExpiresAt);
