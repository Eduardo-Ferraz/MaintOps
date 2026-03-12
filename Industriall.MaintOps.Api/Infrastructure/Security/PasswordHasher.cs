using System.Security.Cryptography;

namespace Industriall.MaintOps.Api.Infrastructure.Security;

/// <summary>
/// PBKDF2-based password hasher using SHA-256.
/// Output format: "{hexHash}-{hexSalt}"
/// </summary>
public sealed class PasswordHasher
{
    private const int SaltSize   = 16;   // 128-bit salt
    private const int HashSize   = 32;   // 256-bit hash
    private const int Iterations = 350_000;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);

        return $"{Convert.ToHexString(hash)}-{Convert.ToHexString(salt)}";
    }

    public bool Verify(string password, string hashedPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedPassword);

        var parts = hashedPassword.Split('-', 2);
        if (parts.Length != 2) return false;

        try
        {
            var storedHash = Convert.FromHexString(parts[0]);
            var salt       = Convert.FromHexString(parts[1]);
            var newHash    = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);

            return CryptographicOperations.FixedTimeEquals(storedHash, newHash);
        }
        catch
        {
            return false;
        }
    }
}
