using FluentValidation;

namespace CFCHub.Application.Identity.Queries.GetStaffUsers;

public class GetStaffUsersQueryValidator : AbstractValidator<GetStaffUsersQuery>
{
    public GetStaffUsersQueryValidator()
    {
    }
}