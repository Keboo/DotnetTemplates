using Microsoft.Playwright;

namespace ReactApp.UITests.PageObjects;

/// <summary>
/// Page Object Model for the Register page
/// </summary>
public class RegisterPage
{
    private readonly IPage _page;
    
    // Locators
    private ILocator EmailInput => _page.GetByTestId("email-input");
    private ILocator PasswordInput => _page.GetByTestId("password-input");
    private ILocator ConfirmPasswordInput => _page.GetByTestId("confirm-password-input");
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
    }
    
    public async Task<bool> IsConfirmationMessageVisibleAsync()
    {
        // React app redirects to /my-rooms on successful registration
        // Check if we're on my-rooms page or if success snackbar was shown
        var url = _page.Url;
        if (url.Contains("/my-rooms"))
        {
            return true;
        }
        
        // Check for success message (snackbar)
        var successCount = await SuccessMessage.CountAsync();
        return successCount > 0;
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
