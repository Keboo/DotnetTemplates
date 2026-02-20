namespace ReactApp.UITests.PageObjects;

/// <summary>
/// Page Object Model for the Room Management page (room owner dashboard)
/// </summary>
public class ManageRoomPage(IPage page) : TestPageBase(page)
{
    // Locators - MUI Typography with component="h2" renders as <h2> element
    private ILocator PendingSection => Page.Locator("h2:has-text('Pending Questions')").First;
    private ILocator ApprovedSection => Page.Locator("h2:has-text('Approved Questions')").First;
    
    // Current question section
    private ILocator CurrentQuestionSection => Page.Locator("div:has-text('Current Question:')").First;
    private ILocator ClearCurrentButton => Page.Locator("button:has-text('Clear Current')").First;
    
    // Buttons
    private ILocator ApproveButton => Page.GetByTestId("approve-question-button");
    private ILocator ViewPublicRoomButton => Page.Locator("button:has-text('View Public Room')").First;
    
    public async Task NavigateAsync(Uri baseUri, string roomName)
    {
        await PerformNavigationAsync(baseUri, $"room/{roomName}/manage");
    }
    
    public async Task<bool> IsQuestionInPendingAsync(string questionText)
    {
        // Wait for pending section to be visible
        await PendingSection.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = PlaywrightConfiguration.DefaultTimeout });
        await Task.Delay(500);
        
        var questionLocator = Page.Locator($"text={questionText}");
        return await questionLocator.IsVisibleAsync();
    }
    
    public async Task ApproveQuestionAsync(string questionText)
    {
        // Wait for the question to appear via SignalR
        var questionRow = Page.Locator($"tr:has-text('{questionText}')").First;
        await questionRow.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = PlaywrightConfiguration.DefaultTimeout });
        
        // Find the approve button within the row using data-testid
        var approveButton = questionRow.GetByTestId("approve-question-button");
        
        await approveButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = PlaywrightConfiguration.DefaultTimeout });
        await approveButton.ClickAsync();
        await Task.Delay(1000);
    }
    
    public async Task RejectQuestionAsync(string questionText)
    {
        await Task.Delay(500);
        
        // Find the question row and click delete
        var questionRow = Page.Locator($"tr:has-text('{questionText}')").First;
        var deleteButton = questionRow.Locator("button[aria-label*='delete'], button:has(svg)").Last;
        
        await deleteButton.ClickAsync();
        await Task.Delay(1000);
    }
    
    public async Task<bool> IsQuestionInApprovedAsync(string questionText)
    {
        // Wait for approved section to be visible
        await ApprovedSection.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = PlaywrightConfiguration.DefaultTimeout });
        await Task.Delay(1000);
        
        var questionLocator = Page.Locator($"text={questionText}");
        var isVisible = await questionLocator.IsVisibleAsync();
        
        return isVisible;
    }
    
    public async Task SetAsCurrentQuestionAsync(string questionText)
    {
        await Task.Delay(500);
        
        // Find the question row in approved section and click "Set Current"
        var questionRow = Page.Locator($"tr:has-text('{questionText}')").First;
        var setCurrentButton = questionRow.Locator("button:has-text('Set Current')").First;
        
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
        await Task.Delay(500);
        
        // Find the question row in approved section and click "Mark Answered"
        var questionRow = Page.Locator($"tr:has-text('{questionText}')").First;
        var answeredButton = questionRow.Locator("button:has-text('Mark Answered')").First;
        
        await answeredButton.ClickAsync();
        await Task.Delay(1000);
    }
    
    public async Task DeleteQuestionAsync(string questionText)
    {
        await Task.Delay(500);
        
        // Find the question row and click delete
        var questionRow = Page.Locator($"tr:has-text('{questionText}')").First;
        var deleteButton = questionRow.Locator("button[aria-label*='delete'], button:has(svg)").Last;
        
        await deleteButton.ClickAsync();
        await Task.Delay(1000);
    }
    
    public async Task<bool> IsCurrentQuestionSetAsync(string questionText)
    {
        var text = await CurrentQuestionSection.TextContentAsync();
        return text?.Contains(questionText) ?? false;
    }
}
