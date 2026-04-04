using System.Security.Cryptography;
using System.Text;
using EmployeePortal.Application;

namespace EmployeePortal.Infrastructure;

public sealed class PasswordVerifier : IPasswordVerifier
{
    public bool Verify(string providedPassword, string storedPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(storedPasswordHash))
        {
            return false;
        }

        if (storedPasswordHash.StartsWith("{plain}", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(
                providedPassword,
                storedPasswordHash[7..],
                StringComparison.Ordinal);
        }

        if (storedPasswordHash.StartsWith("{sha256}", StringComparison.OrdinalIgnoreCase))
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(providedPassword)));
            return string.Equals(hash, storedPasswordHash[8..], StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(providedPassword, storedPasswordHash, StringComparison.Ordinal);
    }
}
