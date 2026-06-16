using System;
using System.Security.Cryptography;
using CFCHub.Application.Common.Security;
using CFCHub.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Infrastructure.Security;

public class DataProtectionServiceTests
{
    private readonly ISecretsManagerService _secretsManagerServiceMock;
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configurationMock;
    private readonly DataProtectionService _sut;
    private const string TenantId = "tenant-123";
    private const string Plaintext = "sensitive data";
    
    public DataProtectionServiceTests()
    {
        _secretsManagerServiceMock = Substitute.For<ISecretsManagerService>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _configurationMock = Substitute.For<IConfiguration>();
        
        _configurationMock["CFCHUB_DATA_PROTECTION_KEY_PREFIX"].Returns("prefix/");
        
        _sut = new DataProtectionService(_secretsManagerServiceMock, _memoryCache, _configurationMock);
    }

    [Fact]
    public void Encrypt_ThenDecrypt_RoundTrips()
    {
        // Arrange
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        var keyMaterial = Convert.ToBase64String(key);
        
        _secretsManagerServiceMock.GetSecret($"prefix/{TenantId}").Returns(keyMaterial);

        // Act
        var ciphertext = _sut.Encrypt(Plaintext, TenantId);
        var decrypted = _sut.Decrypt(ciphertext, TenantId);

        // Assert
        decrypted.Should().Be(Plaintext);
        ciphertext.Should().NotBe(Plaintext);
    }

    [Fact]
    public void Decrypt_WithWrongKey_ThrowsSecurityException()
    {
        // Arrange
        var key1 = new byte[32];
        var key2 = new byte[32];
        RandomNumberGenerator.Fill(key1);
        RandomNumberGenerator.Fill(key2);
        
        _secretsManagerServiceMock.GetSecret($"prefix/{TenantId}").Returns(Convert.ToBase64String(key1));
        var ciphertext = _sut.Encrypt(Plaintext, TenantId);

        // Clear cache and change secret to key2
        var newCache = new MemoryCache(new MemoryCacheOptions());
        var sut = new DataProtectionService(_secretsManagerServiceMock, newCache, _configurationMock);
        _secretsManagerServiceMock.GetSecret($"prefix/{TenantId}").Returns(Convert.ToBase64String(key2));

        // Act
        Action act = () => sut.Decrypt(ciphertext, TenantId);

        // Assert
        act.Should().Throw<System.Security.SecurityException>()
           .WithMessage("Decryption failed. Invalid key.");
    }

    [Fact]
    public void Decrypt_WithPreviousKey_RotatesSuccessfully()
    {
        // Arrange
        var oldKey = new byte[32];
        var newKey = new byte[32];
        RandomNumberGenerator.Fill(oldKey);
        RandomNumberGenerator.Fill(newKey);
        
        // Encrypt with old key
        _secretsManagerServiceMock.GetSecret($"prefix/{TenantId}").Returns(Convert.ToBase64String(oldKey));
        var ciphertext = _sut.Encrypt(Plaintext, TenantId);

        // Clear cache to simulate rotation
        var newCache = new MemoryCache(new MemoryCacheOptions());
        var sut = new DataProtectionService(_secretsManagerServiceMock, newCache, _configurationMock);
        
        // Secret manager now returns new key for current, old key for /prev
        _secretsManagerServiceMock.GetSecret($"prefix/{TenantId}").Returns(Convert.ToBase64String(newKey));
        _secretsManagerServiceMock.GetSecret($"prefix/{TenantId}/prev").Returns(Convert.ToBase64String(oldKey));

        // Act
        var decrypted = sut.Decrypt(ciphertext, TenantId);

        // Assert
        decrypted.Should().Be(Plaintext);
    }
}
