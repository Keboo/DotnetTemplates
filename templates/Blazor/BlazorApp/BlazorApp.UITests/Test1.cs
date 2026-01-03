using Microsoft.Playwright;

namespace BlazorApp.UITests;

/// <summary>
/// Placeholder test class - actual tests are in QAWorkflowTests.cs
/// This file can be used for additional test scenarios
/// </summary>
public class Test1 : UITestBase
{
    [Test]
    public async Task SampleTest_CanNavigateToHomePage()
    {
        await Page.GotoAsync(TestConfiguration.BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Verify page loaded - look for input field or any visible element
        var roomInput = Page.Locator("input[type='text'], input[placeholder*='room']").First;
        await roomInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }
}
