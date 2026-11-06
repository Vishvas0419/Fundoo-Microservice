
using System.Security.Cryptography;
using System.Text;
using Fundoo.User.Application.Interfaces;

namespace Fundoo.User.Infrastructure.Security;

public class PasswordResetTokenService : IPasswordResetTokenService
{
    public string GenerateToken()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string HashToken(string token)
    {
        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);

        byte[] hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }
}
