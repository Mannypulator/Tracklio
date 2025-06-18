using FluentValidation;
using MediatR;
using Tracklio.Shared.Domain.Dto;

namespace Tracklio.Shared.Behaviours;

public class ModelValidationBehaviour<TRequest, TResponse>
    (IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        // Use async validation for better performance
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            // Check if TResponse is IResult
            if (typeof(TResponse) == typeof(IResult))
            {
                // Create validation problem for IResult endpoints
                var validationProblemsDictionary = failures
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => ToCamelCase(g.Key),
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                var errorData = new
                {
                    type = "ValidationError",
                    errors = validationProblemsDictionary
                };

                var response = new GenericResponse<object>
                {
                    StatusCode = 400,
                    Message = "Validation failed",
                    Data = errorData
                };

                var result = Results.BadRequest(response);
                return (TResponse)(object)result;
            }
            else
            {
                // For non-IResult handlers, throw ValidationException
                // Let GlobalExceptionHandler format it properly
                throw new ValidationException(failures);
            }
        }

        return await next();
    }

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
            return str;

        return char.ToLowerInvariant(str[0]) + str[1..];
    }
}
