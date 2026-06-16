using System;
using System.Linq;
using CFCHub.Domain.Shared;
using FluentValidation;

namespace CFCHub.Application.Enrollment.Commands.CreateStudent;

public class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
{
    public CreateStudentCommandValidator(ISystemClock clock)
    {
        RuleFor(x => x.Name).NotEmpty();

        RuleFor(x => x.Cpf)
            .NotEmpty()
            .Must(BeAValidCpf)
            .WithMessage("Invalid CPF algorithm.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Phone)
            .NotEmpty()
            .Matches(@"^\+55\d{10,11}$")
            .WithMessage("Phone must be +55 followed by 10 or 11 digits.");

        RuleFor(x => x.BirthDate)
            .NotEmpty()
            .Must(date => 
            {
                var today = DateOnly.FromDateTime(clock.UtcNow.DateTime);
                var age = today.Year - date.Year;
                if (date > today.AddYears(-age)) age--;
                return age >= 16 && age <= 100;
            })
            .WithMessage("BirthDate must be between 16 and 100 years ago.");

        RuleFor(x => x.PolicyVersion).NotEmpty();
        RuleFor(x => x.PolicyContentHash).NotEmpty();
        RuleFor(x => x.ConsentChannel).IsInEnum();
        
        RuleFor(x => x.HomeAddress).NotNull();
    }

    private bool BeAValidCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        cpf = new string(cpf.Where(char.IsDigit).ToArray());

        if (cpf.Length != 11)
            return false;

        if (cpf.All(c => c == cpf[0]))
            return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (cpf[i] - '0') * (10 - i);

        var remainder = sum % 11;
        var firstDigit = remainder < 2 ? 0 : 11 - remainder;

        if (cpf[9] - '0' != firstDigit)
            return false;

        sum = 0;
        for (var i = 0; i < 10; i++)
            sum += (cpf[i] - '0') * (11 - i);

        remainder = sum % 11;
        var secondDigit = remainder < 2 ? 0 : 11 - remainder;

        return cpf[10] - '0' == secondDigit;
    }
}
