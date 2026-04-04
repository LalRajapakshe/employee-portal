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
    {
        var context = _httpContextAccessor.HttpContext;

        return context?.Request.Headers["X-Portal-User"].FirstOrDefault()
            ?? context?.User?.Identity?.Name;
    }
}
