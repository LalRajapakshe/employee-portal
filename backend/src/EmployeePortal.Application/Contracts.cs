namespace EmployeePortal.Application;

public sealed record EmployeeProfileDto(
    string EmployeeCode,
    string FullName,
    string? Department,
    string? Designation,
    DateOnly? JoinDate,
    string? EmploymentStatus,
    bool IsPermanent,
    string? OfficialEmail
);
