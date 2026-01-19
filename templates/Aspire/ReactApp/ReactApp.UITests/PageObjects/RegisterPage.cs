using Microsoft.Playwright;

namespace ReactApp.UITests.PageObjects;

/// <summary>
/// Page Object Model for the Register page
/// </summary>
public class RegisterPage
{
    private readonly IPage _page;
    
    // Locators
    private ILocator EmailInput => _page.Locator("input[name='Input.Email']");
    private ILocator PasswordInput => _page.Locator("input[name='Input.Password']");
    private ILocator ConfirmPasswordInput => _page.Locator("input[name='Input.ConfirmPassword']");
    private ILocator RegisterButton => _page.Locator("button[type='submit']:has-text('Register')").First;
    private ILocator ConfirmationMessage => _page.Locator("text=confirm your email");
    private ILocator ConfirmEmailLink => _page.Locator("a[href*='Account/ConfirmEmail']");


    public RegisterPage(IPage page)
    {
        _page = page;
    }
    
    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{TestConfiguration.BaseUrl}/Account/Register");
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
        // Check for various possible confirmation messages
        var confirmLink = await ConfirmEmailLink.IsVisibleAsync();
        var confirmText = await ConfirmationMessage.IsVisibleAsync();
        var registerConfirmation = await _page.Locator("text=RegisterConfirmation").IsVisibleAsync();
        
        return confirmLink || confirmText || registerConfirmation;
    }
    
    /// <summary>
    /// Extract the email confirmation link from the page content
    /// The link is displayed on the register confirmation page
    /// </summary>
    public async Task<string> GetEmailConfirmationLinkAsync()
    {
        // Look for the confirmation link on the page
        var linkLocator = ConfirmEmailLink;
        await linkLocator.WaitForAsync(new LocatorWaitForOptions { Timeout = TestConfiguration.DefaultTimeout });
        var href = await linkLocator.GetAttributeAsync("href");
        
        if (string.IsNullOrEmpty(href))
        {
            throw new Exception("Could not find email confirmation link");
        }
        
        // If it's a relative URL, make it absolute
        if (href.StartsWith("/"))
        {
            return $"{TestConfiguration.BaseUrl}{href}";
        }
        
        return href;
    }

    public async Task ConfirmAccountAsync()
    {
        var confirmationLink = await GetEmailConfirmationLinkAsync();
        await _page.GotoAsync(confirmationLink);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        //TODO: Verify confirmation success message
    }
}
