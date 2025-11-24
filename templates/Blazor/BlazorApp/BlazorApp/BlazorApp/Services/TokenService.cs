namespace BlazorApp.Services;

public class TokenService
{
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? TokenExpiry { get; private set; }

    public void SetTokens(string accessToken, string refreshToken, int expiresIn)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        TokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);
    }

    public void ClearTokens()
    {
        AccessToken = null;
        RefreshToken = null;
        TokenExpiry = null;
    }

    public bool IsTokenValid()
    {
        return !string.IsNullOrEmpty(AccessToken) &&
               TokenExpiry.HasValue &&
               TokenExpiry.Value > DateTime.UtcNow.AddMinutes(1);
    }
}
