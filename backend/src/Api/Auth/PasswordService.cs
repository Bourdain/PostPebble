using System.Security.Cryptography;

namespace Api.Auth;

public sealed class PasswordService
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int Iterations = 100_000;

    public (string Hash, string Salt) HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    public bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(storedSalt))
        {
            return false;
        }

        var saltBytes = Convert.FromBase64String(storedSalt);
        var expectedHashBytes = Convert.FromBase64String(storedHash);
        var actualHashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
        return CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes);
    }
}
