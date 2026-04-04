using EmployeePortal.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeePortal.Infrastructure;

public static class Dependencies
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.Configure<PayrollReadOptions>(configuration.GetSection(PayrollReadOptions.SectionName));
        services.Configure<PortalAuthOptions>(configuration.GetSection(PortalAuthOptions.SectionName));

        services.AddScoped<ICurrentUserAccessor, HttpCurrentUserAccessor>();
        services.AddScoped<IEmployeeReadRepository, PayrollEmployeeReadRepository>();
        services.AddScoped<IAuthUserRepository, PortalAuthRepository>();
        services.AddScoped<IPasswordVerifier, PasswordVerifier>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IEmployeeProfileService, EmployeeProfileService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IErrorLogService, ErrorLogService>();

        return services;
    }
}
