using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Enrollment.Queries.GetStudents;
using CFCHub.Application.Common.Models;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CFCHub.IntegrationTests;

public class ProgramTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private RSA _rsa = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();

        _rsa = RSA.Create(2048);
        var publicKeyPem = _rsa.ExportSubjectPublicKeyInfoPem();

        Environment.SetEnvironmentVariable("CFCHUB_DB_CONNECTION_STRING", _dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("CFCHUB_REDIS_CONNECTION_STRING", _redisContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("CFCHUB_JWT_PUBLIC_KEY_ARN", "arn:aws:secretsmanager:us-east-1:000000000000:secret:jwt-public-key");
        Environment.SetEnvironmentVariable("arn:aws:secretsmanager:us-east-1:000000000000:secret:jwt-public-key", publicKeyPem);

        var senderMock = Substitute.For<ISender>();
        senderMock.Send(Arg.Any<GetStudentsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedList<StudentDto>(new List<StudentDto>(), 0, 1, 10));

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(senderMock);
            });
        });
        
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
        await _dbContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
        _rsa?.Dispose();
    }

    [Fact]
    public async Task Program_WithValidJwt_Returns200()
    {
        // Arrange
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new RsaSecurityKey(_rsa);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim("tenant_id", Guid.NewGuid().ToString()),
                new Claim("role", "Receptionist")
            }),
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtString = tokenHandler.WriteToken(token);
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtString);
        
        // Act
        var response = await _client.GetAsync("/api/v1/students");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
