using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Common.Security;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Shared;
using CFCHub.Infrastructure.Persistence;
using CFCHub.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.PostgreSql;
using Xunit;

namespace CFCHub.IntegrationTests.Enrollment;

public class StudentRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private AppDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;
    private ISystemClock _clock = null!;
    private IDataProtectionService _dataProtectionService = null!;
    private IIdGenerator _idGenerator = null!;
    private IStudentRepository _sut = null!;

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
        _dataProtectionService.Encrypt(Arg.Any<string>(), Arg.Any<string>()).Returns(x => x[0].ToString() + "_enc");
        _dataProtectionService.Decrypt(Arg.Any<string>(), Arg.Any<string>()).Returns(x => x[0]?.ToString()?.Replace("_enc", ""));

        _idGenerator = Substitute.For<IIdGenerator>();
        _idGenerator.NewId<StudentId>().Returns(new StudentId(Guid.NewGuid()));

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

        _sut = new StudentRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task GetByCpfAsync_FindsStudentByCpfHash()
    {
        // Arrange
        var cpf = "12345678901";
        var student = Student.Create(
            new StudentId(Guid.NewGuid()),
            "John Doe",
            cpf,
            "1234567",
            "john@example.com",
            "11999999999",
            new DateOnly(1990, 1, 1),
            new Address("Street", "123", "", "Dist", "City", "ST", "12345678"),
            _clock,
            _idGenerator);

        await _sut.AddAsync(student, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _sut.GetByCpfAsync(cpf, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Cpf.Should().Be(cpf);
        
        // Let's verify we actually populated cpf_hash
        var connection = _dbContext.Database.GetDbConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT cpf_hash FROM {_tenantContext.SchemaName}.students WHERE id = '{student.Id.Value}'";
        await connection.OpenAsync();
        var hashInDb = await command.ExecuteScalarAsync();
        hashInDb.Should().NotBeNull();
        hashInDb?.ToString().Should().NotBeNullOrEmpty();
    }
}
