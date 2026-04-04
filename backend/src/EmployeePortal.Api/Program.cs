using EmployeePortal.Api.Middleware;
using EmployeePortal.Application;
using EmployeePortal.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.MapGet("/health", () => Results.Ok(new { success = true, status = "ok", layer = "core-backend" }));

app.MapPost("/api/auth/login", async (
    LoginRequestDto request,
    IAuthenticationService authenticationService,
    CancellationToken cancellationToken) =>
{
    var result = await authenticationService.LoginAsync(request, cancellationToken);

    return result.Success && result.User is not null
        ? Results.Ok(new { success = true, data = result.User })
        : Results.BadRequest(new { success = false, message = result.Message ?? "Login failed." });
});

app.MapPost("/api/auth/logout", () => Results.Ok(new { success = true, message = "Logout acknowledged by core backend." }));

app.MapGet("/api/auth/me", async (
    IAuthenticationService authenticationService,
    CancellationToken cancellationToken) =>
{
    var currentUser = await authenticationService.GetCurrentUserAsync(cancellationToken);
    return currentUser is null
        ? Results.Unauthorized()
        : Results.Ok(new { success = true, data = currentUser });
});

app.MapGet("/api/employees/me", async (
    IEmployeeProfileService employeeProfileService,
    CancellationToken cancellationToken) =>
{
    var profile = await employeeProfileService.GetCurrentEmployeeProfileAsync(cancellationToken);
    return profile is null
        ? Results.NotFound(new { success = false, message = "Employee profile not found." })
        : Results.Ok(new { success = true, data = profile });
});

app.Run();
