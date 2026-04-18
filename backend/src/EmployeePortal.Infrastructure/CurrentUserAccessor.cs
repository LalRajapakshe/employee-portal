using EmployeePortal.Application;
using Microsoft.AspNetCore.Http;

namespace EmployeePortal.Infrastructure;

public sealed class HttpCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetUserName()
        => _httpContextAccessor.HttpContext?.Request.Headers["X-Portal-User"].FirstOrDefault();

    public IReadOnlyList<string> GetRoles()
    {
        var header = _httpContextAccessor.HttpContext?.Request.Headers["X-Portal-Roles"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header))
        {
            return Array.Empty<string>();
        }

        return header
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public string? GetCorrelationId()
        => _httpContextAccessor.HttpContext?.TraceIdentifier;

    public string? GetIpAddress()
        => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? GetUserAgent()
        => _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public string? GetPath()
        => _httpContextAccessor.HttpContext?.Request.Path.Value;
}
