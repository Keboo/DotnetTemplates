using System.Net.Http.Headers;

namespace BlazorApp.Services;

public class BearerTokenHandler : DelegatingHandler
{
    private readonly TokenService _tokenService;

    public BearerTokenHandler(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_tokenService.IsTokenValid())
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _tokenService.AccessToken);
        }

        if (request.RequestUri?.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) == true)
        {
            var builder = new UriBuilder(request.RequestUri)
            {
                Host = "localhost"
            };
            request.RequestUri = builder.Uri;
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
