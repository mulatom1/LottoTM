using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;

namespace LottoTM.Server.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, _logger);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger<ExceptionHandlingMiddleware> logger)
    {
        string errorSourceClass = "N/A";
        try
        {
            var stackTrace = new StackTrace(exception, true);
            var frame = stackTrace.GetFrame(0);
            if (frame != null) errorSourceClass = frame.GetMethod()?.DeclaringType?.FullName ?? "N/A";
        }
        catch
        {
            // Ignoruj błędy podczas analizy StackTrace (może być niedostępny)
        }

        logger.LogError(
           exception,
           "An unhandled exception has occurred in {ErrorSource}: {Message}",
           errorSourceClass,
           exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var problemDetails = new ProblemDetails
        {            
            Status = StatusCodes.Status400BadRequest,
            Title = "Server Error",
            Detail = $"{exception.Message} {exception.InnerException?.Message} {exception.InnerException?.InnerException?.Message}".Trim(),
            Instance = errorSourceClass,
        };

        return context.Response.WriteAsJsonAsync(problemDetails);
    }
}