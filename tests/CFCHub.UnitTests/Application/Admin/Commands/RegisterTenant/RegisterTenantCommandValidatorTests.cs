using CFCHub.Application.Admin.Commands.RegisterTenant;
using FluentValidation.TestHelper;
using Xunit;

namespace CFCHub.UnitTests.Application.Admin.Commands.RegisterTenant;

public class RegisterTenantCommandValidatorTests
{
    private readonly RegisterTenantCommandValidator _validator;

    public RegisterTenantCommandValidatorTests()
    {
        _validator = new RegisterTenantCommandValidator();
    }

    [Theory]
    [InlineData("a_c_d")]
    [InlineData("1bc2")]
    [InlineData("ab12")]
    [InlineData("valid_slug_123")]
    public void Validate_WithValidSlug_ShouldNotHaveError(string slug)
    {
        var command = new RegisterTenantCommand("Test CFC", slug, "test@test.com", "12345678901234");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Slug);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")] // Too short
    [InlineData("_abc")] // Cannot start with underscore
    [InlineData("abc_")] // Cannot end with underscore
    [InlineData("A_bc")] // Only lowercase
    [InlineData("ab@c")] // Invalid character
    public void Validate_WithInvalidSlug_ShouldHaveError(string slug)
    {
        var command = new RegisterTenantCommand("Test CFC", slug, "test@test.com", "12345678901234");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }
}
