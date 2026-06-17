using System;
using System.Threading.Tasks;
using CFCHub.Application.Enrollment.Commands.CreateStudent;
using CFCHub.IntegrationTests.Builders;
using CFCHub.IntegrationTests.Common;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CFCHub.IntegrationTests.Lgpd;

[Collection("IntegrationTests")]
public class ConsentCaptureTests : IntegrationTestBase
{
    public ConsentCaptureTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CreateStudent_WithoutConsent_Fails()
    {
        // Arrange
        var command = new StudentIntegrationBuilder()
            .WithoutConsent()
            .BuildCommand();

        // Act
        Func<Task> act = async () => await Sender.Send(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*PolicyVersion*");
    }

    [Fact]
    public async Task CreateStudent_ConsentRecord_IsImmutableInDb()
    {
        // Arrange
        var command = new StudentIntegrationBuilder().BuildCommand();

        // Act
        var result = await Sender.Send(command);

        // Assert: Try to update the consent_records table via raw SQL
        var studentId = result.StudentId;

        // Ensure the record is there
#pragma warning disable EF1002
        var count = await DbContext.Database.ExecuteSqlRawAsync(
            $"SELECT 1 FROM {TenantSchema}.consent_records WHERE student_id = '{studentId}'");
        
        Func<Task> actUpdate = async () => 
        {
            await DbContext.Database.ExecuteSqlRawAsync(
                $"UPDATE {TenantSchema}.consent_records SET policy_version = 'tampered' WHERE student_id = '{studentId}'");
        };
#pragma warning restore EF1002

        await actUpdate.Should().ThrowAsync<Exception>();
    }
}
