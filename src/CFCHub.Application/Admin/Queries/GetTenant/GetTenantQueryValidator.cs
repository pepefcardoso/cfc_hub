using FluentValidation;

namespace CFCHub.Application.Admin.Queries.GetTenant;

public class GetTenantQueryValidator : AbstractValidator<GetTenantQuery>
{
    public GetTenantQueryValidator()
    {
    }
}