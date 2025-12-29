namespace BlazorApp.Core.Auth;

public interface ISignalRTokenProvider
{
    Task<string?> GetAccessTokenAsync();
}
