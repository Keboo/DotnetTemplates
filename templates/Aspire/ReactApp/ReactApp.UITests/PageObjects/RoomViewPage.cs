using Microsoft.Playwright;

namespace ReactApp.UITests.PageObjects;

/// <summary>
/// Page Object Model for the public Room View page
/// </summary>
public class RoomViewPage
{
    private readonly IPage _page;
    
    //  Locators
    private ILocator DisplayNameInput => _page.GetByTestId("author-name-input");
    private ILocator QuestionTextInput => _page.GetByTestId("question-text-input");
    private ILocator SubmitQuestionButton => _page.GetByTestId("submit-question-button");
    private ILocator CurrentQuestionSection => _page.Locator("div:has-text('Current Question:')").First;
    
    public RoomViewPage(IPage page)
    {
        _page = page;
    }
    
    public async Task NavigateAsync(string roomName)
    {
        await _page.GotoAsync($"{TestConfiguration.BaseUrl}/room/{roomName.ToLower()}");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        await Task.Delay(2000);
    }
    
    public async Task SetDisplayNameAsync(string displayName)
    {
        // In React app, the author name field is directly on the room page
        // No dialog needed - just fill in the field
        await Task.Delay(1000);
        
        await DisplayNameInput.WaitForAsync(new LocatorWaitForOptions 
        { 
            State = WaitForSelectorState.Visible, 
            Timeout = 10000 
        });
        
        await DisplayNameInput.FillAsync(displayName);
        await Task.Delay(300);
    }
    
    public async Task SubmitQuestionAsync(string questionText)
    {
        // Wait for submit button to be available (indicates form is ready)
        await SubmitQuestionButton.WaitForAsync(new LocatorWaitForOptions 
        { 
            State = WaitForSelectorState.Visible, 
            Timeout = 10000 
        });
        
        // Fill in the question
        await QuestionTextInput.FillAsync(questionText);
        await Task.Delay(300);
        
        // Submit the question
        await SubmitQuestionButton.ClickAsync();
        
        // Wait for submission to complete
        await Task.Delay(1000);
    }
    
    public async Task<bool> IsQuestionVisibleAsync(string questionText)
    {
        var questionLocator = _page.Locator($"text={questionText}");
        return await questionLocator.IsVisibleAsync();
    }
    
    public async Task<bool> IsCurrentQuestionVisibleAsync()
    {
        return await CurrentQuestionSection.IsVisibleAsync();
    }
    
    public async Task<string?> GetCurrentQuestionTextAsync()
    {
        if (!await IsCurrentQuestionVisibleAsync())
        {
            return null;
        }
        
        // Get the text content of the current question
        var text = await CurrentQuestionSection.TextContentAsync();
        return text?.Trim();
    }
    
    public async Task WaitForQuestionToAppearAsync(string questionText, float timeout = 0)
    {
        if (timeout == 0)
        {
            timeout = TestConfiguration.SignalRTimeout;
        }
        
        var questionLocator = _page.Locator($"text={questionText}");
        await questionLocator.WaitForAsync(new LocatorWaitForOptions 
        { 
            State = WaitForSelectorState.Visible,
            Timeout = timeout
        });
    }
    
    public async Task WaitForQuestionToDisappearAsync(string questionText, float timeout = 0)
    {
        if (timeout == 0)
        {
            timeout = TestConfiguration.SignalRTimeout;
        }
        
        var questionLocator = _page.Locator($"text={questionText}");
        await questionLocator.WaitForAsync(new LocatorWaitForOptions 
        { 
            State = WaitForSelectorState.Hidden,
            Timeout = timeout
        });
    }
}
