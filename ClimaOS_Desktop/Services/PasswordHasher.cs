using System.Security.Cryptography;
namespace ClimaOS_Desktop.Services;
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    public static string Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Parola nu poate fi goală.", nameof(password));
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashSyncName(), HashSize);
        return $"PBKDF2|{Iterations}|{Convert.ToBase64String(salt)}|{Convert.ToBase64String(hash)}";
    }
    public static bool Verify(string password, string stored)
    {
        if (string.IsNullOrEmpty(stored))
            return false;
        var parts = stored.Split('|');
        if (parts.Length != 4 || parts[0] != "PBKDF2")
            return false;
        if (!int.TryParse(parts[1], out var iter))
            return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iter, HashSyncName(), expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
    private static HashAlgorithmName HashSyncName() => HashAlgorithmName.SHA256;
}
