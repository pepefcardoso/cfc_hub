using CFCHub.Domain.Shared.Exceptions;
using FluentValidation;

namespace CFCHub.Application.Common.Pagination;

public abstract class PaginatedQueryValidator<TQuery, TResult> : AbstractValidator<TQuery>
    where TQuery : PaginatedQuery<TResult>
{
    protected PaginatedQueryValidator()
    {
        RuleFor(x => x.Limit)
            .LessThanOrEqualTo(100)
            .WithState(_ => throw new CFCHub.Domain.Shared.Exceptions.ValidationException("Page limit cannot exceed 100.", "VALIDATION_ERROR"));
    }
}
