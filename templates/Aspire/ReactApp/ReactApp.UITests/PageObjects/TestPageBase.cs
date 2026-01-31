using System.Runtime.CompilerServices;

namespace ReactApp.UITests.PageObjects;

public abstract class TestPageBase(IPage page)
{
    public IPage Page { get; } = page;

    protected async Task PerformNavigationAsync(Uri baseUri, string relativeUrl)
    {
        Uri uri = new(baseUri, relativeUrl);
        await Page.GotoAsync(uri.AbsoluteUri);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}