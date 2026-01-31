namespace ReactApp.UITests.PageObjects;

/// <summary>
/// Page Object Model for My Rooms page (authenticated user's room list)
/// </summary>
public class MyRoomsPage(IPage page) : TestPageBase(page)
{   
    // Locators - MUI TextFields need to target the actual input inside the wrapper
    private ILocator CreateRoomButton => Page.GetByTestId("create-room-button");
    private ILocator RoomNameInput => Page.GetByTestId("room-name-dialog-input").Locator("input");
    private ILocator CreateButton => Page.GetByTestId("create-room-dialog-button");
    private ILocator CancelButton => Page.Locator("button:has-text('Cancel')");

    public Task NavigateAsync(Uri baseUrl) => PerformNavigationAsync(baseUrl, "my-rooms");

    public async Task CreateRoomAsync(string roomName)
    {
        await CreateRoomButton.ClickAsync();
        
        // Wait for dialog to appear
        await Task.Delay(500);
        await RoomNameInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
        
        // Fill in room name
        await RoomNameInput.FillAsync(roomName);
        await Task.Delay(300);
        
        // Click create button
        await CreateButton.ClickAsync();
        
        // Wait for dialog to close and room to be created
        await Task.Delay(2000);
        
        // Wait for navigation back to my-rooms or for room to appear in list
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
    
    public async Task<bool> RoomExistsAsync(string roomName)
    {
        // Wait a moment for the page to update after creation
        await Task.Delay(1000);
        
        // Look for room in the cards by finding the heading with the room name
        // MUI Typography with component="h2" renders as <h2> element
        var roomCard = Page.Locator($"h2:has-text('{roomName}')");
        var count = await roomCard.CountAsync();
        
        return count > 0;
    }
    
    public async Task NavigateToManageRoomAsync(string roomName)
    {
        // Find the manage link for the specific room
        var manageLink = Page.Locator($"a[href*='/room/{roomName}/manage']").First;
        await manageLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
    
    public async Task NavigateToViewRoomAsync(string roomName)
    {
        // Find the view link for the specific room
        var viewLink = Page.Locator($"a[href*='/room/{roomName}']").First;
        await viewLink.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
