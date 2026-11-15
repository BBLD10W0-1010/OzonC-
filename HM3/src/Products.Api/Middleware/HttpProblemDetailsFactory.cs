using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Products.Api.Application;

namespace Products.Api.Middleware;

internal static class HttpProblemDetailsFactory
{
    public static ProblemDetails Create(Exception exception) => exception switch
    {
        ValidationException validation => CreateValidationProblem(validation),
        ProductNotFoundException => new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Товар не найден.",
            Detail = exception.Message
        },
        BadHttpRequestException => new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Некорректный запрос."
        },
        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Внутренняя ошибка сервера."
        }
    };

    private static ValidationProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Ошибка валидации запроса."
        };
    }
}
