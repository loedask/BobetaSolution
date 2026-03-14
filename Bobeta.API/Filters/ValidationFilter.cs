using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bobeta.API.Filters;

/// <summary>Runs FluentValidation on action arguments that have a registered validator; returns 400 with errors if validation fails.</summary>
public class ValidationFilter : IAsyncActionFilter
{
    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var value in context.ActionArguments.Values)
        {
            if (value == null) continue;
            var type = value.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(type);
            var validator = context.HttpContext.RequestServices.GetService(validatorType);
            if (validator == null) continue;
            var contextType = typeof(ValidationContext<>).MakeGenericType(type);
            var validationContext = Activator.CreateInstance(contextType, value)!;
            var method = validatorType.GetMethod("ValidateAsync", new[] { contextType, typeof(CancellationToken) })!;
            var task = (Task)method.Invoke(validator, new[] { validationContext, context.HttpContext.RequestAborted })!;
            await task;
            var resultProperty = task.GetType().GetProperty("Result");
            var result = resultProperty?.GetValue(task) as FluentValidation.Results.ValidationResult;
            if (result is { IsValid: false })
            {
                context.Result = new BadRequestObjectResult(result.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
                return;
            }
        }
        await next();
    }
}
