using System.Security.Cryptography;

namespace HelpDeskAPI.Services
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        bool NeedsRehash(string hashedPassword);
    }

    public class PasswordService : IPasswordService
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        /// <summary>
        /// Genera un hash seguro de la contraseña usando PBKDF2
        /// </summary>
        public string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
            );

            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verifica si la contraseña coincide con el hash almacenado
        /// </summary>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            try
            {
                if (IsBcryptHash(hashedPassword))
                    return BCrypt.Net.BCrypt.Verify(password, hashedPassword);

                string[] parts = hashedPassword.Split('.');
                if (parts.Length != 3)
                    return false;

                int iterations = int.Parse(parts[0]);
                byte[] salt = Convert.FromBase64String(parts[1]);
                byte[] storedHash = Convert.FromBase64String(parts[2]);

                byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    storedHash.Length
                );

                return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
            }
            catch
            {
                return false;
            }
        }

        public bool NeedsRehash(string hashedPassword)
        {
            return IsBcryptHash(hashedPassword);
        }

        private static bool IsBcryptHash(string hashedPassword)
        {
            return hashedPassword.StartsWith("$2a$") ||
                   hashedPassword.StartsWith("$2b$") ||
                   hashedPassword.StartsWith("$2y$");
        }
    }
}
