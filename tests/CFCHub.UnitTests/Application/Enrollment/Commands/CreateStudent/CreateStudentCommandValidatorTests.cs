using System;
using CFCHub.Application.Enrollment.Commands.CreateStudent;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Shared;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Enrollment.Commands.CreateStudent;

public class CreateStudentCommandValidatorTests
{
    private readonly ISystemClock _clock;
    private readonly CreateStudentCommandValidator _validator;

    public CreateStudentCommandValidatorTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));
        _validator = new CreateStudentCommandValidator(_clock);
    }

    [Fact]
    public void CreateStudent_WithInvalidCpfAlgorithm_ThrowsValidation()
    {
        // Arrange
        var command = new CreateStudentCommand(
            "Test",
            "11111111111", // Invalid CPF algorithm but right length
            "123",
            "test@test.com",
            "+5511999999999",
            new DateOnly(2000, 1, 1),
            new AddressRequest("S", "1", null, "D", "C", "S", "Z"),
            "v1",
            "hash",
            ConsentChannel.Web);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Cpf)
              .WithErrorMessage("Invalid CPF algorithm.");
    }
    
    [Fact]
    public void CreateStudent_WithValidCpf_ShouldNotHaveValidationError()
    {
        // Arrange
        // Using a known valid CPF structure (generated randomly for test). Example: 52998224725
        var command = new CreateStudentCommand(
            "Test",
            "52998224725", 
            "123",
            "test@test.com",
            "+5511999999999",
            new DateOnly(2000, 1, 1),
            new AddressRequest("S", "1", null, "D", "C", "S", "Z"),
            "v1",
            "hash",
            ConsentChannel.Web);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Cpf);
    }
}
