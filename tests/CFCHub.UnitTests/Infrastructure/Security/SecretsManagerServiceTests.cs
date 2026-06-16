using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using CFCHub.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Infrastructure.Security;

public class SecretsManagerServiceTests
{
    private readonly IAmazonSecretsManager _secretsManagerMock;
    private readonly ILogger<SecretsManagerService> _loggerMock;
    private readonly SecretsManagerService _sut;

    public SecretsManagerServiceTests()
    {
        _secretsManagerMock = Substitute.For<IAmazonSecretsManager>();
        _loggerMock = Substitute.For<ILogger<SecretsManagerService>>();
        
        _sut = new SecretsManagerService(_secretsManagerMock, _loggerMock);
    }

    [Fact]
    public async Task GetSecretAsync_CallsOnceWithinCacheTtl()
    {
        // Arrange
        var arn = "arn:aws:secretsmanager:us-east-1:123456789012:secret:my-secret";
        var secretValue = "super-secret-value";
        
        _secretsManagerMock.GetSecretValueAsync(Arg.Is<GetSecretValueRequest>(r => r.SecretId == arn), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GetSecretValueResponse { SecretString = secretValue }));

        // Act
        var result1 = await _sut.GetSecretAsync(arn);
        var result2 = await _sut.GetSecretAsync(arn);
        var result3 = await _sut.GetSecretAsync(arn);

        // Assert
        result1.Should().Be(secretValue);
        result2.Should().Be(secretValue);
        result3.Should().Be(secretValue);

        await _secretsManagerMock.Received(1).GetSecretValueAsync(
            Arg.Is<GetSecretValueRequest>(r => r.SecretId == arn),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretAsync_LocalDev_FallsBackToEnvVar()
    {
        // Arrange
        var arn = "arn:aws:secretsmanager:us-east-1:000000000000:secret:local-secret";
        var envValue = "local-env-secret-value";
        
        // Temporarily set the environment variable
        Environment.SetEnvironmentVariable(arn, envValue);

        try
        {
            // Act
            var result = await _sut.GetSecretAsync(arn);

            // Assert
            result.Should().Be(envValue);
            
            // Ensure AWS SDK was not called
            await _secretsManagerMock.DidNotReceive().GetSecretValueAsync(
                Arg.Any<GetSecretValueRequest>(),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            // Clean up
            Environment.SetEnvironmentVariable(arn, null);
        }
    }
}
