namespace ReactApp.UITests.PageObjects;

public class HomePage(IPage page) : TestPageBase(page)
{
    private ILocator RoomNameInput => Page.Locator("input[type='text'], input[placeholder*='room']").First;

    public Task NavigateAsync(Uri baseUrl) => PerformNavigationAsync(baseUrl, "");

    public async Task AssertIsLoadedAsync()
    {
        await RoomNameInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }
}
