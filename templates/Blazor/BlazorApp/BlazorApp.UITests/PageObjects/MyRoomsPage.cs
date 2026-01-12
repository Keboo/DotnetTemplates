using Microsoft.Playwright;

namespace BlazorApp.UITests.PageObjects;

/// <summary>
/// Page Object Model for My Rooms page (authenticated user's room list)
/// </summary>
public class MyRoomsPage
{
    private readonly IPage _page;
    
    // Locators
    private ILocator CreateRoomButton => _page.Locator("button:has-text('Create'), button:has-text('New Room'), button:has-text('Add Room')").First;
    private ILocator RoomNameInput => _page.Locator("input[placeholder*='room'], input[placeholder*='name'], input[name='friendlyName'], input[type='text']").First;
    private ILocator CreateButton => _page.Locator("button:has-text('Create'), button:has-text('Save'), button[type='submit']").First;
    private ILocator CancelButton => _page.Locator("button:has-text('Cancel')");
    
    public MyRoomsPage(IPage page)
    {
        _page = page;
    }
    
    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{TestConfiguration.BaseUrl}/my-rooms");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Wait for page to fully render (Blazor components)
        await Task.Delay(2000);
    }
    
    public async Task CreateRoomAsync(string roomName)
    {
        await CreateRoomButton.ClickAsync();
        
        // Wait for modal to appear and overlay to finish animating
        await Task.Delay(1500);
        await RoomNameInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
        
        // Clear any existing value and fill
        await RoomNameInput.ClearAsync();
        
        // Use PressSequentiallyAsync to simulate typing, which triggers Blazor's input events
        await RoomNameInput.PressSequentiallyAsync(roomName, new LocatorPressSequentiallyOptions { Delay = 50 });
        await Task.Delay(500);
        
        // Check if create button is enabled
        var isButtonDisabled = await CreateButton.IsDisabledAsync();
        Console.WriteLine($"Create button disabled: {isButtonDisabled}");
        
        if (isButtonDisabled)
        {
            // Take screenshot to debug why button is disabled
            await _page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = $"create-button-disabled-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.png",
                FullPage = true
            });
            throw new Exception("Create button is disabled - cannot create room");
        }
        
        // Get the current URL before clicking create
        var urlBeforeCreate = _page.Url;
        
        // Wait for dialog to be fully ready - the overlay will be present but shouldn't block clicks on dialog contents
        await Task.Delay(1000);
        
        // Click the button scoped within the dialog to ensure we're not clicking the overlay
        var dialogCreateButton = _page.Locator(".mud-dialog").Locator("button:has-text('Create'), button:has-text('Save'), button[type='submit']").First;
        await dialogCreateButton.ClickAsync();
        Console.WriteLine("Clicked create button in dialog");
        
        // Wait for Blazor to process and navigate
        await Task.Delay(4000);
        
        // Check what happened after clicking create
        var currentUrl = _page.Url;
        Console.WriteLine($"URL before create: {urlBeforeCreate}");
        Console.WriteLine($"URL after create: {currentUrl}");
        
        // Check for ACTUAL validation errors (not empty state messages)
        // Look specifically for MudBlazor error classes within input fields or the modal dialog
        var errorLocator = _page.Locator(".mud-dialog .mud-input-error, .mud-dialog .mud-error-text, .mud-dialog .validation-message");
        var errorCount = await errorLocator.CountAsync();
        if (errorCount > 0)
        {
            var errors = new List<string>();
            for (int i = 0; i < errorCount; i++)
            {
                var errorText = await errorLocator.Nth(i).TextContentAsync();
                if (!string.IsNullOrWhiteSpace(errorText))
                {
                    errors.Add(errorText);
                    Console.WriteLine($"Validation error {i + 1}: {errorText}");
                }
            }
            
            if (errors.Count > 0)
            {
                throw new Exception($"Room creation failed with {errors.Count} validation error(s): {string.Join(", ", errors)}");
            }
        }
        
        // If we navigated to the manage page, that's success!
        if (currentUrl.Contains("/manage"))
        {
            Console.WriteLine($"Successfully created room - navigated to manage page");
            await _page.GotoAsync($"{TestConfiguration.BaseUrl}/my-rooms");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(2000);
            return;
        }
        
        // If we're still on my-rooms, something might have failed silently
        if (currentUrl.Contains("/my-rooms"))
        {
            // Check if there's a snackbar or toast message
            var snackbarCount = await _page.Locator(".mud-snackbar, .mud-toast, [class*='snack']").CountAsync();
            if (snackbarCount > 0)
            {
                var snackbarText = await _page.Locator(".mud-snackbar, .mud-toast, [class*='snack']").First.TextContentAsync();
                Console.WriteLine($"Snackbar message: {snackbarText}");
            }
            
            // Wait a bit longer to see if delayed navigation happens
            await Task.Delay(2000);
            currentUrl = _page.Url;
            
            if (currentUrl.Contains("/manage"))
            {
                Console.WriteLine($"Delayed navigation to manage page detected");
                await _page.GotoAsync($"{TestConfiguration.BaseUrl}/my-rooms");
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(2000);
                return;
            }
            
            // Still on my-rooms - operation may have failed
            Console.WriteLine("WARNING: Still on my-rooms page after attempting room creation");
        }
        
        return;
    }
    
    public async Task<bool> RoomExistsAsync(string roomName)
    {
        // Wait a moment for the page to update after creation
        await Task.Delay(1500);
        
        // Room names are case-insensitive, so search for both original and lowercase
        var lowerRoomName = roomName.ToLower();
        
        // Try multiple ways to find the room
        var byText = await _page.Locator($"text={roomName}").CountAsync();
        var byTextLower = await _page.Locator($"text={lowerRoomName}").CountAsync();
        var byLink = await _page.Locator($"a[href*='{lowerRoomName}']").CountAsync();
        var byHeading = await _page.Locator($"h1, h2, h3, h4, h5, h6").Locator($"text={roomName}").CountAsync();
        var byHeadingLower = await _page.Locator($"h1, h2, h3, h4, h5, h6").Locator($"text={lowerRoomName}").CountAsync();
        
        // Also try getting the page content and searching it
        var pageContent = await _page.ContentAsync();
        var containsRoom = pageContent.Contains(roomName, StringComparison.OrdinalIgnoreCase);
        
        return byText > 0 || byTextLower > 0 || byLink > 0 || byHeading > 0 || byHeadingLower > 0 || containsRoom;
    }
    
    public async Task NavigateToManageRoomAsync(string roomName)
    {
        // Find the manage link for the specific room
        var manageLink = _page.Locator($"a[href*='/room/{roomName}/manage']").First;
        await manageLink.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
    
    public async Task NavigateToViewRoomAsync(string roomName)
    {
        // Find the view link for the specific room
        var viewLink = _page.Locator($"a[href*='/room/{roomName}']").First;
        await viewLink.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
