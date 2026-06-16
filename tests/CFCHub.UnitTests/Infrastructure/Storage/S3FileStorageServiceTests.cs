using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Infrastructure.Storage;

public class S3FileStorageServiceTests
{
    private readonly IAmazonS3 _s3ClientMock;
    private readonly IConfiguration _configurationMock;
    private readonly S3FileStorageService _sut;

    public S3FileStorageServiceTests()
    {
        _s3ClientMock = Substitute.For<IAmazonS3>();
        _configurationMock = Substitute.For<IConfiguration>();

        _configurationMock["AWS_S3_DOCUMENTS_BUCKET"].Returns("test-docs");
        _configurationMock["AWS_S3_MEDICAL_BUCKET"].Returns("test-medical");

        _sut = new S3FileStorageService(_s3ClientMock, _configurationMock);
    }

    [Fact]
    public async Task GenerateDownloadUrl_MedicalTarget_TTLIs900s()
    {
        // Arrange
        _s3ClientMock.GetPreSignedURL(Arg.Any<GetPreSignedUrlRequest>()).Returns("https://s3.test/medical-file");

        // Act
        var url = await _sut.GenerateDownloadUrlAsync(
            StorageTarget.Medical,
            "tenant-a",
            "file123.pdf");

        // Assert
        url.Should().NotBeNullOrEmpty();
        _s3ClientMock.Received(1).GetPreSignedURL(Arg.Is<GetPreSignedUrlRequest>(req =>
            req.BucketName == "test-medical" &&
            req.Key == "tenant-a/file123.pdf" &&
            req.Verb == HttpVerb.GET &&
            req.Expires > DateTime.UtcNow.AddMinutes(14) &&
            req.Expires < DateTime.UtcNow.AddMinutes(16)
        ));
    }

    [Fact]
    public async Task GenerateDownloadUrl_MedicalTarget_ThrowsIfCustomTTLexceeds900s()
    {
        // Act
        Func<Task> act = async () => await _sut.GenerateDownloadUrlAsync(
            StorageTarget.Medical,
            "tenant-a",
            "file123.pdf",
            customTtl: TimeSpan.FromMinutes(20));

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*TTL cannot exceed 900 seconds*");
    }

    [Fact]
    public async Task UploadAsync_ValidPdf_CallsS3()
    {
        // Arrange
        var content = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        
        // Act
        await _sut.UploadAsync(StorageTarget.Documents, "tenant", "doc.pdf", content, "application/pdf");

        // Assert
        await _s3ClientMock.Received(1).PutObjectAsync(Arg.Is<PutObjectRequest>(req =>
            req.BucketName == "test-docs" &&
            req.Key == "tenant/doc.pdf" &&
            req.ContentType == "application/pdf"
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_InvalidMagicBytes_ThrowsStorageException()
    {
        // Arrange
        var content = new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        
        // Act
        Func<Task> act = async () => await _sut.UploadAsync(StorageTarget.Documents, "tenant", "doc.pdf", content, "application/pdf");

        // Assert
        await act.Should().ThrowAsync<StorageException>()
            .WithMessage("*INVALID_FILE_TYPE*");
    }

    [Fact]
    public async Task GenerateUploadUrlAsync_InvalidContentType_ThrowsStorageException()
    {
        // Act
        Func<Task> act = async () => await _sut.GenerateUploadUrlAsync(StorageTarget.Documents, "tenant", "doc.txt", "text/plain");

        // Assert
        await act.Should().ThrowAsync<StorageException>()
            .WithMessage("*INVALID_FILE_TYPE*");
    }
}
