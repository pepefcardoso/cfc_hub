using System;
using System.Linq;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Common.Security;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Shared;
using CFCHub.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.PostgreSql;
using Xunit;

namespace CFCHub.IntegrationTests.Infrastructure;

public class StudentConfigurationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private AppDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;
    private ISystemClock _clock = null!;
    private IDataProtectionService _dataProtectionService = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        _tenantContext = Substitute.For<ITenantContext>();
        _tenantContext.SchemaName.Returns("cfc_test");
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _tenantContext.TenantSlug.Returns("test");
        _tenantContext.IsResolved.Returns(true);

        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        _dataProtectionService = Substitute.For<IDataProtectionService>();
        _dataProtectionService.Encrypt(Arg.Any<string>(), Arg.Any<string>()).Returns(x => $"[ENCRYPTED]{x[0]}");
        _dataProtectionService.Decrypt(Arg.Any<string>(), Arg.Any<string>()).Returns(x => ((string)x[0]).Replace("[ENCRYPTED]", ""));

        var services = new ServiceCollection();
        services.AddSingleton(_tenantContext);
        services.AddSingleton(_clock);
        services.AddSingleton(_dataProtectionService);
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_dbContainer.GetConnectionString()));

        var serviceProvider = services.BuildServiceProvider();
        _dbContext = serviceProvider.GetRequiredService<AppDbContext>();

#pragma warning disable EF1002
        await _dbContext.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{_tenantContext.SchemaName}\";");
#pragma warning restore EF1002
        
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task SaveStudent_StoresCpfEncrypted()
    {
        // Arrange
        var idGenerator = Substitute.For<IIdGenerator>();
        var studentId = new StudentId(Guid.NewGuid());
        idGenerator.NewId<StudentId>().Returns(studentId);
        
        var student = Student.Create(
            studentId,
            "John Doe",
            "12345678901",
            "RG123",
            "john@example.com",
            "555-1234",
            new DateOnly(1990, 1, 1),
            new Address("Street", "123", "Apt 4", "Downtown", "City", "ST", "12345-678"),
            _clock,
            idGenerator);

        _dbContext.Students.Add(student);
        await _dbContext.SaveChangesAsync();

        // Act - Read raw from DB to verify encryption
        await using var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT \"Cpf\" FROM \"{_tenantContext.SchemaName}\".\"students\" WHERE \"Id\" = '{studentId.Value}';";
        var rawCpf = (string)(await command.ExecuteScalarAsync() ?? string.Empty);

        // Assert
        rawCpf.Should().Be("[ENCRYPTED]12345678901");
        rawCpf.Should().NotBe("12345678901");

        // EF Core read decrypts automatically
        _dbContext.ChangeTracker.Clear();
        var savedStudent = await _dbContext.Students.FirstAsync(s => s.Id == studentId);
        savedStudent.Cpf.Should().Be("12345678901");
    }
}
