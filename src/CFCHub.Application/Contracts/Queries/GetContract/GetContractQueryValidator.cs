using FluentValidation;

namespace CFCHub.Application.Contracts.Queries.GetContract;

public class GetContractQueryValidator : AbstractValidator<GetContractQuery>
{
    public GetContractQueryValidator()
    {
    }
}