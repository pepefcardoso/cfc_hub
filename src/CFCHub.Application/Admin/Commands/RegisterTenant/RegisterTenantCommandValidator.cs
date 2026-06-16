using FluentValidation;

namespace CFCHub.Application.Admin.Commands.RegisterTenant;

public class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .Matches(@"^[a-z0-9][a-z0-9_]{2,62}[a-z0-9]$")
            .WithMessage("Invalid slug format.");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("ContactEmail is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(150);

        RuleFor(x => x.Cnpj)
            .NotEmpty().WithMessage("Cnpj is required.")
            .MaximumLength(14); // Usually CNPJ is 14 digits
    }
}
