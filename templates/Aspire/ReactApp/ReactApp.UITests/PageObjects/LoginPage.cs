namespace ReactApp.UITests.PageObjects;

/// <summary>
/// Page Object Model for the Login page
/// </summary>
public class LoginPage
{
    private readonly IPage _page;
    
    // Locators
    private ILocator EmailInput => _page.GetByTestId("email-input");
    private ILocator PasswordInput => _page.GetByTestId("password-input");
    private ILocator LoginButton => _page.GetByTestId("login-button");
    private ILocator LogoutButton => _page.Locator("button:has-text('Logout')");
    
    public LoginPage(IPage page)
    {
        _page = page;
    }
    
    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{TestConfiguration.BaseUrl}/login");
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
        if (url.Contains("/login"))
        {
            return false;
        }
        
        // Look for various indicators that user is logged in
        // Use Count to avoid strict mode violations
        var logoutButtonCount = await LogoutButton.CountAsync();
        var myRoomsLinkCount = await _page.Locator("a[href='/my-rooms']").CountAsync();
        
        return logoutButtonCount > 0 || myRoomsLinkCount > 0;
    }
    
    public async Task LogoutAsync()
    {
        await LogoutButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
