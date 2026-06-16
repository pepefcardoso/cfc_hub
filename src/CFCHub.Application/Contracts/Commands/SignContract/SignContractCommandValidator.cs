using FluentValidation;

namespace CFCHub.Application.Contracts.Commands.SignContract;

public class SignContractCommandValidator : AbstractValidator<SignContractCommand>
{
    public SignContractCommandValidator()
    {
        RuleFor(x => x.ContractId).NotEmpty();
        RuleFor(x => x.SignatureHash).NotEmpty().MaximumLength(256);
    }
}
