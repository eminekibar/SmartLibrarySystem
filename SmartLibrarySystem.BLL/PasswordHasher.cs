using System;
using System.Security.Cryptography;
using System.Text;

namespace SmartLibrarySystem.BLL
{
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hashBytes = sha.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public static bool Verify(string password, string hash)
        {
            return string.Equals(Hash(password), hash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
