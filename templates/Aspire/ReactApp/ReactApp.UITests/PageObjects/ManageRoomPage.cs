using Microsoft.Playwright;

namespace ReactApp.UITests.PageObjects;

/// <summary>
/// Page Object Model for the Room Management page (room owner dashboard)
/// </summary>
public class ManageRoomPage
{
    private readonly IPage _page;
    
    // Tab locators - MudTabs render with mud-tab class
    private ILocator PendingTab => _page.Locator(".mud-tab:has-text('Pending Approval'), [role='tab']:has-text('Pending Approval')").First;
    private ILocator ApprovedTab => _page.Locator(".mud-tab:has-text('Approved'), [role='tab']:has-text('Approved')").First;
    
    // Current question section
    private ILocator CurrentQuestionSection => _page.Locator("[class*='current-question']").First;
    private ILocator ClearCurrentButton => _page.Locator("button:has-text('Clear')").First;
    
    // Delete room
    private ILocator DeleteRoomButton => _page.Locator("button:has-text('Delete Room')").First;
    private ILocator ConfirmDeleteButton => _page.Locator("button:has-text('Yes'), button:has-text('Confirm')").First;
    
    public ManageRoomPage(IPage page)
    {
        _page = page;
    }
    
    public async Task NavigateAsync(string roomName)
    {
        await _page.GotoAsync($"{TestConfiguration.BaseUrl}/room/{roomName}/manage");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
    
    public async Task<bool> IsQuestionInPendingAsync(string questionText)
    {
        await PendingTab.ClickAsync();
        await Task.Delay(500);
        
        var questionLocator = _page.Locator($"text={questionText}");
        return await questionLocator.IsVisibleAsync();
    }
    
    public async Task ApproveQuestionAsync(string questionText)
    {
        // Make sure we're on pending tab
        try
        {
            await PendingTab.ClickAsync(new LocatorClickOptions { Timeout = 3000 });
        }
        catch
        {
            // If intercepted, force the click
            await PendingTab.ClickAsync(new LocatorClickOptions { Force = true });
        }
        await Task.Delay(1500); // Increased delay to give Blazor time to render
        
        // Dismiss any Snackbars that might be covering the page
        var snackbarCount = await _page.Locator(".mud-snackbar").CountAsync();
        if (snackbarCount > 0)
        {
            Console.WriteLine($"Found {snackbarCount} snackbars, waiting for them to dismiss...");
            // Wait for snackbars to auto-dismiss
            await Task.Delay(3000);
        }
        
        // Wait for any lingering dialog overlays to disappear
        var dialogCount = await _page.Locator(".mud-dialog-container").CountAsync();
        if (dialogCount > 0)
        {
            Console.WriteLine($"Found {dialogCount} dialogs, waiting for them to close...");
            // Wait for dialogs to auto-close
            await Task.Delay(3000);
        }
        
        // Find the question card and click approve
        var questionCard = _page.Locator($"[class*='question-card']:has-text('{questionText}'), div:has-text('{questionText}')").First;
        var approveButton = questionCard.Locator("button:has-text('Approve')").First;
        
        var isVisible = await approveButton.IsVisibleAsync();
        var isEnabled = await approveButton.IsEnabledAsync();
        Console.WriteLine($"Approve button - Visible: {isVisible}, Enabled: {isEnabled}");
        
        if (isVisible && isEnabled)
        {
            await approveButton.ClickAsync();
            Console.WriteLine("Clicked approve button");
            await Task.Delay(2000); // Wait for SignalR and Blazor to process the approval
            
            // Take screenshot to see what happened
            await _page.ScreenshotAsync(new PageScreenshotOptions 
            { 
                Path = $"after-approve-{DateTimeOffset.Now.Ticks}.png",
                FullPage = true
            });
        }
        else
        {
            throw new Exception($"Approve button not clickable - Visible: {isVisible}, Enabled: {isEnabled}");
        }
    }
    
    public async Task RejectQuestionAsync(string questionText)
    {
        // Make sure we're on pending tab
        try
        {
            await PendingTab.ClickAsync(new LocatorClickOptions { Timeout = 3000 });
        }
        catch
        {
            // If intercepted, force the click
            await PendingTab.ClickAsync(new LocatorClickOptions { Force = true });
        }
        await Task.Delay(500);
        
        // Find the question card and click delete/reject
        var questionCard = _page.Locator($"[class*='question-card']:has-text('{questionText}'), div:has-text('{questionText}')").First;
        var deleteButton = questionCard.Locator("button:has-text('Delete'), button:has-text('Reject')").First;
        
        await deleteButton.ClickAsync();
        await Task.Delay(1000);
    }
    
    public async Task<bool> IsQuestionInApprovedAsync(string questionText)
    {
        await ApprovedTab.ClickAsync();
        await Task.Delay(1000); // Give time for tab to load
        
        // Get all text on the page for debugging
        var pageText = await _page.Locator("body").TextContentAsync();
        var hasQuestionText = pageText?.Contains(questionText) ?? false;
        Console.WriteLine($"Checking for question in approved tab. Question text found in page: {hasQuestionText}");
        
        var questionLocator = _page.Locator($"text={questionText}");
        var isVisible = await questionLocator.IsVisibleAsync();
        
        if (!isVisible)
        {
            // Take screenshot to see what's on the approved tab
            await _page.ScreenshotAsync(new PageScreenshotOptions 
            { 
                Path = $"approved-tab-{DateTimeOffset.Now.Ticks}.png",
                FullPage = true
            });
        }
        
        return isVisible;
    }
    
    public async Task SetAsCurrentQuestionAsync(string questionText)
    {
        // Navigate to approved tab
        await ApprovedTab.ClickAsync();
        await Task.Delay(500);
        
        // Find the question card and click "Set as Current"
        var questionCard = _page.Locator($"[class*='question-card']:has-text('{questionText}'), div:has-text('{questionText}')").First;
        var setCurrentButton = questionCard.Locator("button:has-text('Set as Current'), button:has-text('Current')").First;
        
        await setCurrentButton.ClickAsync();
        await Task.Delay(1000);
    }
    
    public async Task ClearCurrentQuestionAsync()
    {
        await ClearCurrentButton.ClickAsync();
        await Task.Delay(1000);
    }
    
    public async Task MarkAsAnsweredAsync(string questionText)
    {
        // Navigate to approved tab
        await ApprovedTab.ClickAsync();
        await Task.Delay(500);
        
        // Find the question card and click "Mark as Answered"
        var questionCard = _page.Locator($"[class*='question-card']:has-text('{questionText}'), div:has-text('{questionText}')").First;
        var answeredButton = questionCard.Locator("button:has-text('Answered'), button:has-text('Mark as Answered')").First;
        
        await answeredButton.ClickAsync();
        await Task.Delay(1000);
    }
    
    public async Task DeleteQuestionAsync(string questionText)
    {
        // Navigate to approved tab
        await ApprovedTab.ClickAsync();
        await Task.Delay(500);
        
        // Find the question card and click delete
        var questionCard = _page.Locator($"[class*='question-card']:has-text('{questionText}'), div:has-text('{questionText}')").First;
        var deleteButton = questionCard.Locator("button:has-text('Delete')").First;
        
        await deleteButton.ClickAsync();
        await Task.Delay(1000);
    }
    
    public async Task DeleteRoomAsync()
    {
        await DeleteRoomButton.ClickAsync();
        
        // Wait for confirmation dialog
        await Task.Delay(500);
        
        await ConfirmDeleteButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
    
    public async Task<bool> IsCurrentQuestionSetAsync(string questionText)
    {
        var text = await CurrentQuestionSection.TextContentAsync();
        return text?.Contains(questionText) ?? false;
    }
}
