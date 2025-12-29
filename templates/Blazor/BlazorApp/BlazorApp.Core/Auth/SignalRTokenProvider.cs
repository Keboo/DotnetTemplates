using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace BlazorApp.Core.Auth;

public interface ISignalRTokenProvider
{
    Task<string?> GetAccessTokenAsync();
}

public class SignalRTokenProvider : ISignalRTokenProvider
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SignalRTokenProvider> _logger;
    private const int TokenExpirationMinutes = 60;

    public SignalRTokenProvider(AuthenticationStateProvider authenticationStateProvider, IConfiguration configuration, ILogger<SignalRTokenProvider> logger)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            return GenerateToken(authState.User);
        }
        return null;
    }

    private string GenerateToken(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new InvalidOperationException("User is not authenticated");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Add username if available
        var username = user.Identity?.Name;
        if (!string.IsNullOrEmpty(username))
        {
            claims.Add(new(ClaimTypes.Name, username));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSigningKey()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "BlazorApp",
            audience: "BlazorApp",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(TokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GetSigningKey()
    {
        // Try to get from configuration, otherwise generate a temporary one
        var key = _configuration["SignalR:SigningKey"];
        if (string.IsNullOrEmpty(key))
        {
            // For development, use a fixed key (in production, this should be in configuration)
            key = "BlazorApp-SignalR-Signing-Key-Min-32-Chars-Long!";
        }
        return key;
    }
}
