using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace ClaimsApi.Auth;

public static class RefreshTokenHasher
{
    private const int TokenBytes = 32;

    public static string Generate()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(TokenBytes);

        return Base64Url.EncodeToString(randomBytes);
    }

    public static string Hash(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }
}
