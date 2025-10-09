using Maliev.CustomerService.Api.Models;
using System.Net;
using System.Text.Json;

namespace Maliev.CustomerService.Api.Middleware;

/// <summary>
/// Global exception handling middleware for structured error responses
/// </summary>
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
            _logger.LogError(ex, "Unhandled exception occurred. TraceId: {TraceId}, Path: {Path}, Method: {Method}",
                context.TraceIdentifier,
                context.Request.Path,
                context.Request.Method);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var errorResponse = new ErrorResponse
        {
            Code = GetErrorCode(exception),
            Message = GetErrorMessage(exception),
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        var statusCode = GetStatusCode(exception);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(errorResponse, options);

        return context.Response.WriteAsync(json);
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            InvalidOperationException => (int)HttpStatusCode.Conflict,
            TimeoutException => (int)HttpStatusCode.RequestTimeout,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    private static string GetErrorCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => "VALIDATION_ERROR",
            UnauthorizedAccessException => "UNAUTHORIZED",
            KeyNotFoundException => "NOT_FOUND",
            InvalidOperationException => "CONFLICT",
            TimeoutException => "TIMEOUT",
            _ => "INTERNAL_SERVER_ERROR"
        };
    }

    private static string GetErrorMessage(Exception exception)
    {
        // In production, avoid exposing internal exception details
        // Return generic messages for 500 errors
        return exception switch
        {
            ArgumentException => exception.Message,
            UnauthorizedAccessException => "You are not authorized to perform this action.",
            KeyNotFoundException => "The requested resource was not found.",
            InvalidOperationException => exception.Message,
            TimeoutException => "The request timed out. Please try again.",
            _ => "An unexpected error occurred. Please contact support if the problem persists."
        };
    }
}

/// <summary>
/// Extension method for registering the exception handling middleware
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
