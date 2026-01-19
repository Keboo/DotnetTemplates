using Microsoft.Playwright;

namespace ReactApp.UITests.PageObjects;

/// <summary>
/// Page Object Model for Account management pages
/// </summary>
public class AccountPages
{
    private readonly IPage _page;
    
    public AccountPages(IPage page)
    {
        _page = page;
    }
    
    public async Task<bool> CanLoadProfilePageAsync()
    {
        try
        {
            await _page.GotoAsync($"{TestConfiguration.BaseUrl}/Account/Manage");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Check if we can see profile content
            var profileHeading = _page.Locator("h1, h2, h3").First;
            return await profileHeading.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<bool> CanLoadEmailPageAsync()
    {
        try
        {
            await _page.GotoAsync($"{TestConfiguration.BaseUrl}/Account/Manage/Email");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            var emailInput = _page.Locator("input[type='email']").First;
            return await emailInput.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<bool> CanLoadChangePasswordPageAsync()
    {
        try
        {
            await _page.GotoAsync($"{TestConfiguration.BaseUrl}/Account/Manage/ChangePassword");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            var passwordInput = _page.Locator("input[type='password']").First;
            return await passwordInput.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<bool> CanLoadTwoFactorPageAsync()
    {
        try
        {
            await _page.GotoAsync($"{TestConfiguration.BaseUrl}/Account/Manage/TwoFactorAuthentication");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Just check page loads
            var heading = _page.Locator("h1, h2, h3").First;
            return await heading.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<bool> CanLoadPersonalDataPageAsync()
    {
        try
        {
            await _page.GotoAsync($"{TestConfiguration.BaseUrl}/Account/Manage/PersonalData");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            var deleteButton = _page.Locator("button:has-text('Delete')").First;
            return await deleteButton.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }
    
    public async Task DeleteAccountAsync(string password)
    {
        await _page.GotoAsync($"{TestConfiguration.BaseUrl}/Account/Manage/DeletePersonalData");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Enter password and confirm deletion
        var passwordInput = _page.Locator("input[type='password']");
        await passwordInput.FillAsync(password);
        
        var deleteButton = _page.Locator("button[type='submit']:has-text('Delete')");
        await deleteButton.ClickAsync();
        
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
