using System.Security.Cryptography;

namespace AudioGuideOwnerPortal.Helpers;

public static class PasswordHashHelper
{
    public static (string Hash, string Salt) HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var derived = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 100_000, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(derived), Convert.ToBase64String(saltBytes));
    }

    public static bool VerifyPassword(string password, string hash, string salt)
    {
        if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(salt))
        {
            return false;
        }

        var saltBytes = Convert.FromBase64String(salt);
        var derived = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(hash),
            derived);
    }
}
