using CFCHub.Domain.Shared.Exceptions;
using FluentValidation;
using MediatR;

namespace CFCHub.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(new ValidationContext<TRequest>(request), cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .Select(f => new ValidationFailureDetail(f.PropertyName, f.ErrorMessage, f.ErrorCode ?? "VALIDATION_ERROR"))
            .ToList();

        if (failures.Count != 0)
        {
            throw new CFCHub.Domain.Shared.Exceptions.ValidationException(failures);
        }

        return await next();
    }
}
