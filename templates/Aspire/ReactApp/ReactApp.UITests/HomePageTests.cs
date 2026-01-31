using ReactApp.UITests.PageObjects;

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
        HomePage homePage = new(Page);
        await homePage.NavigateAsync(FrontendBaseUri);

        await homePage.AssertIsLoadedAsync();
    }

    [Test]
    [Category(TestCategories.Accessibility)]
    public async Task HomePageIsAccessible()
    {
        HomePage homePage = new(Page);
        await homePage.NavigateAsync(FrontendBaseUri);

        await AssertNoAccessibilityViolations();
    }
}
