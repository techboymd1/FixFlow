using System.Security.Cryptography;

namespace FixFlow.Services;

public class PasswordService
{
    private const int SaltLength = 16;
    private const int KeyLength = 32;
    private const int Iterations = 120_000;

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeyLength);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        var pieces = storedHash.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length != 3 || !int.TryParse(pieces[0], out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(pieces[1]);
            var expectedHash = Convert.FromBase64String(pieces[2]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
