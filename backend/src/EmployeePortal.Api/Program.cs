using EmployeePortal.Api.Middleware;
using EmployeePortal.Application;
using EmployeePortal.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
// Disabled for local HTTP-only development. Re-enable after HTTPS is configured.
// app.UseHttpsRedirection();

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

app.MapPost("/api/auth/logout", () =>
    Results.Ok(new { success = true, message = "Logout acknowledged by core backend." }));

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

app.MapGet("/api/salary-advance", async (
    ISalaryAdvanceService salaryAdvanceService,
    CancellationToken cancellationToken) =>
{
    var policy = await salaryAdvanceService.GetPolicyAsync(cancellationToken);
    var items = await salaryAdvanceService.ListMyRequestsAsync(cancellationToken);

    return Results.Ok(new
    {
        success = true,
        data = new
        {
            policy,
            items
        }
    });
});

app.MapPost("/api/salary-advance", async (
    SalaryAdvanceCreateRequestDto request,
    ISalaryAdvanceService salaryAdvanceService,
    CancellationToken cancellationToken) =>
{
    var draft = await salaryAdvanceService.CreateDraftAsync(request, cancellationToken);
    return Results.Ok(new { success = true, data = draft });
});

app.MapGet("/api/salary-advance/{requestId:guid}", async (
    Guid requestId,
    ISalaryAdvanceService salaryAdvanceService,
    CancellationToken cancellationToken) =>
{
    var request = await salaryAdvanceService.GetRequestAsync(requestId, cancellationToken);
    return request is null
        ? Results.NotFound(new { success = false, message = "Salary advance request not found." })
        : Results.Ok(new { success = true, data = request });
});

app.MapPut("/api/salary-advance/{requestId:guid}", async (
    Guid requestId,
    SalaryAdvanceUpdateDraftRequestDto request,
    ISalaryAdvanceService salaryAdvanceService,
    CancellationToken cancellationToken) =>
{
    var updated = await salaryAdvanceService.UpdateDraftAsync(requestId, request, cancellationToken);
    return updated is null
        ? Results.NotFound(new { success = false, message = "Salary advance request not found." })
        : Results.Ok(new { success = true, data = updated });
});

app.MapPost("/api/salary-advance/{requestId:guid}/submit", async (
    Guid requestId,
    ISalaryAdvanceService salaryAdvanceService,
    CancellationToken cancellationToken) =>
{
    var submitted = await salaryAdvanceService.SubmitAsync(requestId, cancellationToken);
    return submitted is null
        ? Results.NotFound(new { success = false, message = "Salary advance request not found." })
        : Results.Ok(new { success = true, data = submitted });
});

app.MapPost("/api/salary-advance/{requestId:guid}/actions", async (
    Guid requestId,
    SalaryAdvanceApprovalActionRequestDto request,
    ISalaryAdvanceService salaryAdvanceService,
    CancellationToken cancellationToken) =>
{
    var updated = await salaryAdvanceService.ApplyApprovalActionAsync(requestId, request, cancellationToken);
    return updated is null
        ? Results.NotFound(new { success = false, message = "Salary advance request not found." })
        : Results.Ok(new { success = true, data = updated });
});

app.MapGet("/api/salary-advance/{requestId:guid}/print", async (
    Guid requestId,
    ISalaryAdvanceService salaryAdvanceService,
    CancellationToken cancellationToken) =>
{
    var printData = await salaryAdvanceService.GetPrintDataAsync(requestId, cancellationToken);
    return printData is null
        ? Results.NotFound(new { success = false, message = "Salary advance request not found." })
        : Results.Ok(new { success = true, data = printData });
});

app.MapGet("/api/approvals/inbox", async (
    ISalaryAdvanceService salaryAdvanceService,
    CancellationToken cancellationToken) =>
{
    var items = await salaryAdvanceService.GetApprovalInboxAsync(cancellationToken);
    return Results.Ok(new { success = true, data = items });
});

app.MapGet("/api/notifications", async (
    ISalaryAdvanceService salaryAdvanceService,
    CancellationToken cancellationToken) =>
{
    var items = await salaryAdvanceService.GetNotificationsAsync(cancellationToken);
    return Results.Ok(new { success = true, data = items });
});

app.Run();