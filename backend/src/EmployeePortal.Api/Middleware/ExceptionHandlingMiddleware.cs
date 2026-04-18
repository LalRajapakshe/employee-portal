using System.Text.Json;
using EmployeePortal.Application;

namespace EmployeePortal.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IErrorLogService errorLogService, ICurrentUserAccessor currentUserAccessor)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var correlationId = context.TraceIdentifier;
            context.Response.Headers[RequestCorrelationMiddleware.HeaderName] = correlationId;

            await errorLogService.WriteAsync(
                new ErrorLogEntry(
                    ErrorCode: "UNHANDLED_EXCEPTION",
                    ErrorMessage: exception.Message,
                    StackTrace: exception.StackTrace,
                    SourceLayer: "Core Backend Layer",
                    CorrelationId: correlationId,
                    UserName: currentUserAccessor.GetUserName(),
                    RequestPath: currentUserAccessor.GetPath(),
                    IpAddress: currentUserAccessor.GetIpAddress(),
                    UserAgent: currentUserAccessor.GetUserAgent(),
                    StatusCode: StatusCodes.Status500InternalServerError),
                context.RequestAborted);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                success = false,
                message = "An unexpected error occurred.",
                correlationId
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
