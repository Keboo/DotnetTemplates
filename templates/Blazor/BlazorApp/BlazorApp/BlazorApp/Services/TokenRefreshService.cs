using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace BlazorApp.Services;

public class TokenRefreshService : IDisposable
{
    private readonly TokenService _tokenService;
    private readonly TokenAuthenticationStateProvider _authStateProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private Timer? _refreshTimer;

    public TokenRefreshService(
        TokenService tokenService,
        TokenAuthenticationStateProvider authStateProvider,
        IHttpClientFactory httpClientFactory)
    {
        _tokenService = tokenService;
        _authStateProvider = authStateProvider;
        _httpClientFactory = httpClientFactory;
    }

    public void StartRefreshTimer()
    {
        if (_tokenService.TokenExpiry.HasValue)
        {
            var refreshTime = _tokenService.TokenExpiry.Value
                .AddMinutes(-2)
                .Subtract(DateTime.UtcNow);

            if (refreshTime.TotalMilliseconds > 0)
            {
                _refreshTimer = new Timer(
                    async _ => await RefreshToken(),
                    null,
                    refreshTime,
                    Timeout.InfiniteTimeSpan
                );
            }
        }
    }

    private async Task RefreshToken()
    {
        var httpClient = _httpClientFactory.CreateClient("Backend");

        var response = await httpClient.PostAsJsonAsync("/refresh", new
        {
            refreshToken = _tokenService.RefreshToken
        });

        if (response.IsSuccessStatusCode)
        {
            var tokenResponse = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();

            if (tokenResponse != null)
            {
                _authStateProvider.NotifyUserAuthentication(
                    tokenResponse.AccessToken,
                    tokenResponse.RefreshToken,
                    tokenResponse.ExpiresIn
                );

                StartRefreshTimer();
            }
        }
    }

    public void Dispose()
    {
        _refreshTimer?.Dispose();
    }
}

public class AccessTokenResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = "";

    [JsonPropertyName("tokenType")]
    public string TokenType { get; set; } = "Bearer";
}
