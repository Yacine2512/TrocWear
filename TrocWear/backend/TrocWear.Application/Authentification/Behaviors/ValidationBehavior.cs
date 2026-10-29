using FluentValidation;
using MediatR;
using TrocWear.BuildingBlocks.SharedExceptions;

namespace AuthService.Application.Behaviors;

/// <summary>Validate requests before invoking their business handler.</summary>
public sealed class ValidationBehavior<TRequest,TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest,TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            if (!result.IsValid)
                throw new DomainException(string.Join(" ", result.Errors.Select(e => e.ErrorMessage)), 400);
        }
        return await next();
    }
}
