using System.Security.Cryptography;
using System.Text;

namespace TORSEPAN.Domain.Security;

public static class PasswordCredential
{
    private const string Prefix = "$torsepan$pbkdf2-sha256$";
    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 600_000, HashAlgorithmName.SHA256, 32);
        return Prefix + "600000$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
    }
    public static bool Verify(string stored, string password)
    {
        if (string.IsNullOrEmpty(stored) || string.IsNullOrEmpty(password) || password.Length > 1024) return false;
        // Existing accounts continue to work until their password is changed.
        if (!stored.StartsWith(Prefix, StringComparison.Ordinal))
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(stored), Encoding.UTF8.GetBytes(password));
        try
        {
            var parts = stored[Prefix.Length..].Split('$');
            if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations) || iterations is < 600_000 or > 1_200_000) return false;
            var salt = Convert.FromBase64String(parts[1]); var expected = Convert.FromBase64String(parts[2]);
            return salt.Length == 16 && expected.Length == 32 && CryptographicOperations.FixedTimeEquals(expected,
                Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32));
        }
        catch (FormatException) { return false; }
    }
}
