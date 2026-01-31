using Microsoft.Playwright;

namespace ReactApp.UITests.PageObjects;

/// <summary>
/// Page Object Model for the Register page
/// </summary>
public class RegisterPage
{
    private readonly IPage _page;
    
    // Locators - MUI TextFields need to target the actual input inside the wrapper
    private ILocator EmailInput => _page.GetByTestId("email-input").Locator("input");
    private ILocator PasswordInput => _page.GetByTestId("password-input").Locator("input");
    private ILocator ConfirmPasswordInput => _page.GetByTestId("confirm-password-input").Locator("input");
    private ILocator RegisterButton => _page.GetByTestId("register-button");
    private ILocator SuccessMessage => _page.Locator("text=Registration successful");


    public RegisterPage(IPage page)
    {
        _page = page;
    }
    
    public async Task NavigateAsync(Uri baseUri)
    {
        await _page.GotoAsync($"{baseUri.AbsoluteUri}register");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
    
    public async Task RegisterAsync(string email, string password)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        await ConfirmPasswordInput.FillAsync(password);
        await RegisterButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Wait for navigation to my-rooms after successful registration
        await _page.WaitForURLAsync("**/my-rooms", new PageWaitForURLOptions { Timeout = 10000 });
    }
    
    public async Task<bool> IsConfirmationMessageVisibleAsync()
    {
        // React app redirects to /my-rooms on successful registration
        // User is now logged in and on my-rooms page
        var url = _page.Url;
        return url.Contains("/my-rooms");
    }
    
    /// <summary>
    /// For the React app, email confirmation is not required for login.
    /// This method is kept for API compatibility but is a no-op.
    /// </summary>
    public async Task<string> GetEmailConfirmationLinkAsync()
    {
        // React app auto-confirms email, no link needed
        await Task.CompletedTask;
        return string.Empty;
    }

    public async Task ConfirmAccountAsync()
    {
        // React app auto-confirms email, no manual confirmation needed
        await Task.CompletedTask;
    }
}
