using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Enrollment.Queries.GetStudent;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Identity;
using CFCHub.IntegrationTests.Builders;
using CFCHub.IntegrationTests.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace CFCHub.IntegrationTests.Lgpd;

[Collection("IntegrationTests")]
public class FieldAccessPolicyTests : IntegrationTestBase
{
    public FieldAccessPolicyTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetStudent_AsReceptionist_CpfNotInResponse()
    {
        // Arrange
        var createCommand = new StudentIntegrationBuilder()
            .WithCpf("99988877766")
            .BuildCommand();
        
        var createResult = await Sender.Send(createCommand);
        var studentId = createResult.StudentId;

        var currentUserMock = Substitute.For<ICurrentUserService>();
        currentUserMock.UserId.Returns(Guid.NewGuid());
        currentUserMock.Role.Returns(RoleType.Receptionist);

        var testFactory = _fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<ICurrentUserService>(_ => currentUserMock);
                // The ITenantContext must be maintained
                var tenantContextMock = Substitute.For<ITenantContext>();
                tenantContextMock.TenantId.Returns(TenantId);
                tenantContextMock.SchemaName.Returns(TenantSchema);
                tenantContextMock.TenantSlug.Returns(TenantSchema.Replace("cfc_", ""));
                tenantContextMock.IsResolved.Returns(true);
                services.AddScoped<ITenantContext>(_ => tenantContextMock);
            });
        });

        var client = testFactory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/v1/students/{studentId}");
        response.EnsureSuccessStatusCode();
        var jsonString = await response.Content.ReadAsStringAsync();

        // Assert
        // We know the API wraps in a standard response: { "data": { "cpf": null, "name": "..." } }
        jsonString.Should().Contain("\"cpf\":null");
        jsonString.Should().Contain("\"name\":\"João da Silva\"");
    }
}
