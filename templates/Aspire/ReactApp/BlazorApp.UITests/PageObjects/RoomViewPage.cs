using Microsoft.Playwright;

namespace BlazorApp.UITests.PageObjects;

/// <summary>
/// Page Object Model for the public Room View page
/// </summary>
public class RoomViewPage
{
    private readonly IPage _page;
    
    //  Locators
    private ILocator DisplayNameInput => _page.Locator("input[placeholder*='name'], input[type='text']").First;
    private ILocator SetNameButton => _page.Locator("button:has-text('Set Name'), button:has-text('OK'), button[type='submit']").First;
    private ILocator QuestionTextInput => _page.Locator("textarea, input[placeholder*='question'], .mud-input-root input, .mud-input-root textarea").First;
    private ILocator SubmitQuestionButton => _page.Locator("button:has-text('Submit'), button:has-text('Ask')").First;
    private ILocator CurrentQuestionCard => _page.Locator("[class*='current-question'], .current-question, h3:has-text('Current Question')").First;
    private ILocator ApprovedQuestionsList => _page.Locator("[class*='approved-questions'], .approved-questions");
    
    public RoomViewPage(IPage page)
    {
        _page = page;
    }
    
    public async Task NavigateAsync(string roomName)
    {
        // Navigate to room using lowercase (Blazor routing and SQL Like are case-insensitive)
        await _page.GotoAsync($"{TestConfiguration.BaseUrl}/room/{roomName.ToLower()}");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Wait for Blazor components to load
        await Task.Delay(2000);
    }
    
    public async Task SetDisplayNameAsync(string displayName)
    {
        // Wait for page to fully load and Blazor to initialize
        await Task.Delay(3000);
        
        // Try to wait for the display name dialog to appear
        try
        {
            await DisplayNameInput.WaitForAsync(new LocatorWaitForOptions 
            { 
                State = WaitForSelectorState.Visible, 
                Timeout = 5000 
            });
            
            // Dialog appeared, fill it in
            await DisplayNameInput.FillAsync(displayName);
            await Task.Delay(500);
            
            // Click the Continue button - it's in a MudPaper, not a MudDialog
            var dialogSetButton = _page.Locator("button:has-text('Continue')").First;
            
            try
            {
                await dialogSetButton.ClickAsync(new LocatorClickOptions { Timeout = 3000 });
            }
            catch
            {
                // If normal click fails, try force
                await dialogSetButton.ClickAsync(new LocatorClickOptions { Force = true });
            }
            
            // Wait for dialog to close and Blazor to re-render
            await Task.Delay(5000);
        }
        catch
        {
            // Dialog didn't appear - name might already be set
            // Wait a bit for form to be ready anyway
            await Task.Delay(2000);
        }
        
        // Check what's actually on the page if submit button doesn't appear
        var submitButtonCount = await SubmitQuestionButton.CountAsync();
        Console.WriteLine($"Submit button count: {submitButtonCount}");
        
        if (submitButtonCount == 0)
        {
            // Check if there's a Blazor error by checking page text
            var bodyText = await _page.Locator("body").TextContentAsync();
            var hasBlazorError = bodyText?.Contains("An unhandled error has occurred") == true;
            Console.WriteLine($"Blazor error visible: {hasBlazorError}");
            
            if (hasBlazorError)
            {
                Console.WriteLine("Blazor error detected, refreshing page...");
                
                // Reload the page to clear the error
                await _page.ReloadAsync();
                await Task.Delay(5000); // Give more time for Blazor to initialize after reload
                
                // Try the dialog flow again
                var dialogCount2 = await DisplayNameInput.CountAsync();
                Console.WriteLine($"Dialog count after reload: {dialogCount2}");
                
                if (dialogCount2 > 0)
                {
                    // Take screenshot to see what's on page
                    await _page.ScreenshotAsync(new PageScreenshotOptions 
                    { 
                        Path = $"after-reload-{DateTimeOffset.Now.Ticks}.png",
                        FullPage = true
                    });
                    
                    var postReloadText = await _page.Locator("body").TextContentAsync();
                    Console.WriteLine($"Page content after reload: {postReloadText?.Substring(0, Math.Min(800, postReloadText.Length))}");
                    
                    await DisplayNameInput.FillAsync(displayName);
                    await Task.Delay(500);
                    
                    var dialogSetButton2 = _page.Locator("button:has-text('Continue')").First;
                    try
                    {
                        await dialogSetButton2.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                    }
                    catch
                    {
                        // Try force click if normal click fails
                        await dialogSetButton2.ClickAsync(new LocatorClickOptions { Force = true, Timeout = 5000 });
                    }
                    await Task.Delay(5000);
                }
                else
                {
                    Console.WriteLine("Dialog didn't reappear after reload - checking if form is now visible");
                }
            }
            else
            {
                // Take screenshot for debugging
                await _page.ScreenshotAsync(new PageScreenshotOptions 
                { 
                    Path = $"no-submit-button-{DateTimeOffset.Now.Ticks}.png",
                    FullPage = true
                });
                
                // Check if there's an error message or empty state
                var pageText = await _page.Locator("body").TextContentAsync();
                var textLength = pageText?.Length ?? 0;
                Console.WriteLine($"Page text when submit button missing: {pageText?.Substring(0, Math.Min(500, textLength))}");
            }
        }
        
        // Verify the question form is ready by waiting for submit button
        await SubmitQuestionButton.WaitForAsync(new LocatorWaitForOptions 
        { 
            State = WaitForSelectorState.Visible, 
            Timeout = 15000 
        });
    }
    
    public async Task SubmitQuestionAsync(string questionText)
    {
        // Check if page loaded with errors
        var has404 = await _page.Locator("text=Room Not Found, text=404").CountAsync() > 0;
        var hasBlazorError = await _page.Locator("#blazor-error-ui").IsVisibleAsync();
        
        if (has404 || hasBlazorError)
        {
            // Take screenshot for debugging
            await _page.ScreenshotAsync(new PageScreenshotOptions 
            { 
                Path = $"submit-question-error-{DateTimeOffset.Now.Ticks}.png" 
            });
            throw new Exception($"Room page loaded with error. Current URL: {_page.Url}. Has404: {has404}, HasBlazorError: {hasBlazorError}");
        }
        
        // Wait for submit button to be available (indicates form is ready)
        await SubmitQuestionButton.WaitForAsync(new LocatorWaitForOptions 
        { 
            State = WaitForSelectorState.Visible, 
            Timeout = 10000 
        });
        
        // Wait for question input to be available - try multiple selectors
        try
        {
            var textareaCount = await _page.Locator("textarea").CountAsync();
            if (textareaCount > 0)
            {
                await _page.Locator("textarea").First.FillAsync(questionText);
            }
            else
            {
                // Try input field instead
                await _page.Locator("input[type='text']").Last.FillAsync(questionText);
            }
        }
        catch (Exception ex)
        {
            // Take a screenshot for debugging
            await _page.ScreenshotAsync(new PageScreenshotOptions 
            { 
                Path = $"submit-question-error-{DateTimeOffset.Now.Ticks}.png" 
            });
            throw new Exception($"Could not find question input. Page URL: {_page.Url}. Error: {ex.Message}");
        }
        
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
        return await CurrentQuestionCard.IsVisibleAsync();
    }
    
    public async Task<string?> GetCurrentQuestionTextAsync()
    {
        if (!await IsCurrentQuestionVisibleAsync())
        {
            return null;
        }
        
        // Get the text content of the current question
        var text = await CurrentQuestionCard.TextContentAsync();
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
