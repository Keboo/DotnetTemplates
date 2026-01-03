using Microsoft.Playwright;

namespace BlazorApp.UITests.PageObjects;

/// <summary>
/// Page Object Model for the Login page
/// </summary>
public class LoginPage
{
    private readonly IPage _page;
    
    // Locators
    private ILocator EmailInput => _page.Locator("input[name='Input.Email']");
    private ILocator PasswordInput => _page.Locator("input[name='Input.Password']");
    private ILocator LoginButton => _page.GetByRole(AriaRole.Button, new() { Name = "Log in", Exact = true });
    private ILocator LogoutButton => _page.Locator("form[action='/Account/Logout'] button");
    
    public LoginPage(IPage page)
    {
        _page = page;
    }
    
    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{TestConfiguration.BaseUrl}/Account/Login");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
    
    public async Task LoginAsync(string email, string password)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        await LoginButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
    
    public async Task<bool> IsLoggedInAsync()
    {
        // Check if we're on a page that requires authentication
        // or if we can find user-specific elements
        var url = _page.Url;
        
        // If we're still on the login page, we're not logged in
        if (url.Contains("/Account/Login"))
        {
            return false;
        }
        
        // Look for various indicators that user is logged in
        // Use Count to avoid strict mode violations
        var logoutFormCount = await _page.Locator("form[action='/Account/Logout']").CountAsync();
        var myRoomsLinkCount = await _page.Locator("a[href='/my-rooms']").CountAsync();
        
        return logoutFormCount > 0 || myRoomsLinkCount > 0;
    }
    
    public async Task LogoutAsync()
    {
        await LogoutButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
