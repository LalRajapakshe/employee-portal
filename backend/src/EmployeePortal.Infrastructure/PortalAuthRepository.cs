using System.Data;
using EmployeePortal.Application;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace EmployeePortal.Infrastructure;

public sealed class PortalAuthRepository : IAuthUserRepository
{
    private readonly PortalAuthOptions _options;

    public PortalAuthRepository(IOptions<PortalAuthOptions> options)
    {
        _options = options.Value;
    }

    public async Task<AuthenticatedPortalUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            return FindDemoUser(userName);
        }

        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var user = await LoadUserAsync(connection, userName, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roles = await LoadRolesAsync(connection, user.PortalUserId, cancellationToken);
        var permissions = await LoadPermissionsAsync(connection, user.PortalUserId, cancellationToken);

        return user with { Roles = roles, Permissions = permissions };
    }

    public async Task RecordLastLoginAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            return;
        }

        const string sql = @"
UPDATE portal.PortalUsers
SET LastLoginAt = SYSUTCDATETIME(),
    UpdatedAt = SYSUTCDATETIME()
WHERE UserName = @UserName;";

        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };
        command.Parameters.AddWithValue("@UserName", userName);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private AuthenticatedPortalUser? FindDemoUser(string userName)
    {
        var match = _options.DemoUsers.FirstOrDefault(x => string.Equals(x.UserName, userName, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return null;
        }

        return new AuthenticatedPortalUser(
            PortalUserId: 0,
            UserName: match.UserName,
            PasswordHash: $"{{plain}}{match.Password}",
            EmployeeCode: match.EmployeeCode,
            DisplayName: string.IsNullOrWhiteSpace(match.DisplayName) ? match.UserName : match.DisplayName,
            Email: match.Email,
            IsActive: match.IsActive,
            Roles: match.Roles,
            Permissions: match.Permissions);
    }

    private static async Task<AuthenticatedPortalUser?> LoadUserAsync(SqlConnection connection, string userName, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT TOP (1)
    pu.PortalUserId,
    pu.UserName,
    pu.PasswordHash,
    pu.EmployeeCode,
    COALESCE(NULLIF(epp.PortalDisplayName, ''), pu.UserName) AS DisplayName,
    COALESCE(NULLIF(epp.NotificationEmail, ''), pu.Email) AS Email,
    pu.IsActive
FROM portal.PortalUsers pu
LEFT JOIN portal.EmployeePortalProfile epp
    ON epp.EmployeeCode = pu.EmployeeCode
WHERE pu.UserName = @UserName;";

        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };
        command.Parameters.AddWithValue("@UserName", userName);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var passwordHash = reader["PasswordHash"]?.ToString() ?? string.Empty;

        return new AuthenticatedPortalUser(
            PortalUserId: reader.GetInt32(reader.GetOrdinal("PortalUserId")),
            UserName: reader["UserName"]?.ToString() ?? string.Empty,
            PasswordHash: passwordHash,
            EmployeeCode: reader["EmployeeCode"]?.ToString() ?? string.Empty,
            DisplayName: reader["DisplayName"]?.ToString() ?? string.Empty,
            Email: reader["Email"]?.ToString(),
            IsActive: reader["IsActive"] is bool isActive && isActive,
            Roles: [],
            Permissions: []);
    }

    private static async Task<IReadOnlyList<string>> LoadRolesAsync(SqlConnection connection, int portalUserId, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT r.RoleCode
FROM portal.UserRoleMappings urm
INNER JOIN portal.Roles r ON r.RoleId = urm.RoleId
WHERE urm.PortalUserId = @PortalUserId
  AND urm.IsActive = 1
  AND r.IsActive = 1
ORDER BY r.RoleCode;";

        var roles = new List<string>();
        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };
        command.Parameters.AddWithValue("@PortalUserId", portalUserId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var roleCode = reader["RoleCode"]?.ToString();
            if (!string.IsNullOrWhiteSpace(roleCode))
            {
                roles.Add(roleCode);
            }
        }

        return roles;
    }

    private static async Task<IReadOnlyList<string>> LoadPermissionsAsync(SqlConnection connection, int portalUserId, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT DISTINCT p.PermissionCode
FROM portal.UserRoleMappings urm
INNER JOIN portal.RolePermissionMappings rpm ON rpm.RoleId = urm.RoleId
INNER JOIN portal.Permissions p ON p.PermissionId = rpm.PermissionId
WHERE urm.PortalUserId = @PortalUserId
  AND urm.IsActive = 1
  AND p.IsActive = 1
ORDER BY p.PermissionCode;";

        var permissions = new List<string>();
        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };
        command.Parameters.AddWithValue("@PortalUserId", portalUserId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var permissionCode = reader["PermissionCode"]?.ToString();
            if (!string.IsNullOrWhiteSpace(permissionCode))
            {
                permissions.Add(permissionCode);
            }
        }

        return permissions;
    }
}
