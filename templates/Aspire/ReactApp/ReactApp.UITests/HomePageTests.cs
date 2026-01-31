namespace ReactApp.UITests;

/// <summary>
/// Placeholder test class - actual tests are in QAWorkflowTests.cs
/// This file can be used for additional test scenarios
/// </summary>
public class HomePageTests : UITestBase
{
    [Test]
    public async Task CanNavigateToHomePage()
    {
        await Page.GotoAsync(FrontendBaseUri.AbsoluteUri);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Verify page loaded - look for input field or any visible element
        var roomInput = Page.Locator("input[type='text'], input[placeholder*='room']").First;
        await roomInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }
}
