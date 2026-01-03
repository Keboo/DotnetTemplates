namespace BlazorApp.UITests;

/// <summary>
/// Base class for UI tests that configures Playwright browser
/// </summary>
public class UITestBase : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    
    protected IPage Page { get; private set; } = null!;
    protected IBrowser Browser => _browser!;
    
    protected static CancellationToken CancellationToken => 
        TestContext.Current?.Execution.CancellationToken ?? CancellationToken.None;

    [Before(Test)]
    public virtual async Task SetupAsync()
    {
        _playwright = await Playwright.CreateAsync();
        
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = !TestConfiguration.Headed,
            SlowMo = TestConfiguration.SlowMo
        };
        
        _browser = await _playwright.Chromium.LaunchAsync(launchOptions);
        
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });
        
        Page = await _context.NewPageAsync();
    }
    
    [After(Test)]
    public async Task TearDownAsync()
    {
        await DisposeAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        if (Page != null)
            await Page.CloseAsync();
        
        if (_context != null)
            await _context.CloseAsync();
        
        if (_browser != null)
            await _browser.CloseAsync();
        
        _playwright?.Dispose();
        
        GC.SuppressFinalize(this);
    }
}
