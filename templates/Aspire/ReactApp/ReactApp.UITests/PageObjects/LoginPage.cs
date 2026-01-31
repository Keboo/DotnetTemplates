namespace ReactApp.UITests.PageObjects;

/// <summary>
/// Page Object Model for the Login page
/// </summary>
public class LoginPage
{
    private readonly IPage _page;
    
    // Locators - MUI TextFields need to target the actual input inside the wrapper
    private ILocator EmailInput => _page.GetByTestId("email-input").Locator("input");
    private ILocator PasswordInput => _page.GetByTestId("password-input").Locator("input");
    private ILocator LoginButton => _page.GetByTestId("login-button");
    private ILocator LogoutButton => _page.Locator("button:has-text('Logout')");
    
    public LoginPage(IPage page)
    {
        _page = page;
    }
    
    public async Task NavigateAsync(Uri baseUri)
    {
        await _page.GotoAsync($"{baseUri}login");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
    
    public async Task LoginAsync(string email, string password)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        await LoginButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Wait for redirect to my-rooms after successful login
        await _page.WaitForURLAsync("**/my-rooms", new PageWaitForURLOptions { Timeout = 10000 });
    }
    
    public async Task<bool> IsLoggedInAsync()
    {
        // Check if we're on a page that requires authentication
        // or if we can find user-specific elements
        var url = _page.Url;
        
        // If we're on my-rooms page, we're logged in
        if (url.Contains("/my-rooms"))
        {
            return true;
        }
        
        // If we're still on the login page, we're not logged in
        if (url.Contains("/login"))
        {
            return false;
        }
        
        // Look for various indicators that user is logged in
        // Use Count to avoid strict mode violations
        var logoutButtonCount = await LogoutButton.CountAsync();
        var myRoomsButtonCount = await _page.Locator("button:has-text('My Rooms')").CountAsync();
        
        return logoutButtonCount > 0 || myRoomsButtonCount > 0;
    }
    
    public async Task LogoutAsync()
    {
        await LogoutButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
