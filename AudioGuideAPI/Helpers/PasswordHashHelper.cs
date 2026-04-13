using System.Security.Cryptography;

namespace AudioGuideAPI.Helpers;

public static class PasswordHashHelper
{
    public static (string Hash, string Salt) HashPassword(string rawPassword)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            rawPassword,
            saltBytes,
            10_000,
            HashAlgorithmName.SHA256,
            32);

        return (Convert.ToHexString(hashBytes), Convert.ToHexString(saltBytes));
    }

    public static bool VerifyPassword(string rawPassword, string hash, string salt)
    {
        if (string.IsNullOrWhiteSpace(rawPassword) || string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(salt))
        {
            return false;
        }

        var saltBytes = Convert.FromHexString(salt);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            rawPassword,
            saltBytes,
            10_000,
            HashAlgorithmName.SHA256,
            32);

        return string.Equals(Convert.ToHexString(hashBytes), hash, StringComparison.OrdinalIgnoreCase);
    }
}
