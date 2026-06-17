using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Email;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared;
using CFCHub.Infrastructure.Persistence;
using CFCHub.Workers.Outbox.Handlers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Testcontainers.PostgreSql;
using Xunit;

namespace CFCHub.IntegrationTests.Workers.Outbox.Handlers;

public class DocumentExpiryAlertHandlerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private AppDbContext _dbContext = null!;
    private IEmailService _emailService = null!;
    private DocumentExpiryAlertHandler _handler = null!;
    private IServiceProvider _serviceProvider = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var services = new ServiceCollection();
        
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var tenantContext = new TenantContext();
        tenantContext.Resolve("cfc_test", "test", Guid.NewGuid());
        
        services.AddSingleton(clock);
        services.AddSingleton<ITenantContext>(tenantContext);

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_dbContainer.GetConnectionString()));

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();

        await _dbContext.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS \"cfc_test\";");
        await _dbContext.Database.EnsureCreatedAsync();

        _emailService = Substitute.For<IEmailService>();
        _handler = new DocumentExpiryAlertHandler(_dbContext, _emailService, NullLogger<DocumentExpiryAlertHandler>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldDeserializePayloadAndSendAlertToStaff()
    {
        // Arrange
        var idGen = Substitute.For<IIdGenerator>();
        idGen.NewId<StaffUserId>().Returns(new StaffUserId(Guid.NewGuid()));

        var staff1 = StaffUser.Create(idGen.NewId<StaffUserId>(), "Admin User", "admin@cfc.com", "hash", RoleType.Admin, Substitute.For<ISystemClock>());
        var staff2 = StaffUser.Create(idGen.NewId<StaffUserId>(), "Receptionist", "rec@cfc.com", "hash", RoleType.Receptionist, Substitute.For<ISystemClock>());
        
        _dbContext.StaffUsers.Add(staff1);
        _dbContext.StaffUsers.Add(staff2);
        await _dbContext.SaveChangesAsync();

        var json = """
            {
                "TenantId": "tenant1",
                "StudentId": "00000000-0000-0000-0000-000000000001",
                "StudentName": "John Doe",
                "DocumentType": "MedicalExam",
                "AlertTier": "D30"
            }
            """;
        var payload = JsonSerializer.Deserialize<DocumentExpiryAlertRequested>(json);

        // Act
        await _handler.HandleAsync(payload!, CancellationToken.None);

        // Assert
        payload.Should().NotBeNull();
        payload!.AlertTier.Should().Be("D30");

        await _emailService.Received(2).SendAsync(
            Arg.Is<EmailMessage>(m => 
                m.TemplateId == "cfchub-doc-expiry-d30" &&
                (m.ToAddress == "admin@cfc.com" || m.ToAddress == "rec@cfc.com") &&
                m.TemplateData["student_name"] == "John Doe" &&
                m.TemplateData["document_type"] == "MedicalExam" &&
                !m.TemplateData.ContainsKey("cpf")
            ),
            CancellationToken.None);
    }
}
