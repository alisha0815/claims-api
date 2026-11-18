namespace ClaimsApi.Auth;

public static class UsernameNormalizer
{
    public static string Normalize(string username)
    {
        return username.Trim().ToLowerInvariant();
    }
}
