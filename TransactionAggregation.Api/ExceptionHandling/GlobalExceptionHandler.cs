using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TransactionAggregation.Application.Exceptions;

namespace TransactionAggregation.Api.ExceptionHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            TransactionValidationException =>
                StatusCodes.Status400BadRequest,

            TransactionDuplicateException =>
                StatusCodes.Status409Conflict,

            _ =>
                StatusCodes.Status500InternalServerError
        };

        _logger.LogError(
            exception,
            "Unhandled application exception.");

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = exception switch
            {
                TransactionValidationException =>
                    "Transaction validation failed.",

                TransactionDuplicateException duplicate =>
                    duplicate.DuplicateType switch
                    {
                        TransactionDuplicateType.TransactionId =>
                            "Duplicate transaction ID.",

                        TransactionDuplicateType.SourceAndExternalTransactionId =>
                            "Duplicate source and external transaction ID.",

                        _ =>
                            "Duplicate transaction."
                    },

                _ =>
                    "An unexpected error occurred."
            },
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }
}