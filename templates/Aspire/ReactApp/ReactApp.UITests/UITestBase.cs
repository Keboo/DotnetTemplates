namespace ReactApp.UITests;

/// <summary>
/// Base class for UI tests that configures Playwright browser
/// </summary>
public abstract class UITestBase : IAsyncDisposable
{
    private const string STATE_FILE = ".state.json";

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;

    protected IPage Page { get; private set; } = null!;
    protected IBrowser Browser => _browser!;

    protected static string CreateUniqueId() => Guid.NewGuid().ToString("N")[..12];
    protected string TestPassword { get; } = "Test@Pass123!";
    protected string TestEmail { get; } = $"testuser{CreateUniqueId()}@example.com";

    protected string? StateId { get; set; }

    protected static CancellationToken CancellationToken =>
        TestContext.Current?.Execution.CancellationToken ?? CancellationToken.None;

    [Before(Test)]
    public async Task TestSetup()
    {
        await BeforeTestSetupAsync();

        _playwright = await Playwright.CreateAsync();
        _browser = await CreateBrowserAsync(_playwright);
        _context = await CreateBrowserContextAsync(_browser, StateId is not null ? $"{StateId}_{STATE_FILE}" : null);

        Page = await _context.NewPageAsync();
        await AfterTestSetupAsync();
    }

    protected virtual async Task BeforeTestSetupAsync() { }

    protected virtual async Task AfterTestSetupAsync() { }

    [After(Test)]
    public async Task TearDownAsync()
    {
        await DisposeAsync();
    }

    protected static async Task<IBrowser> CreateBrowserAsync(IPlaywright playwright)
    {
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = TestConfiguration.Headless,
            SlowMo = TestConfiguration.SlowMo
        };

        return await playwright.Chromium.LaunchAsync(launchOptions);
    }

    protected static async Task<IBrowserContext> CreateBrowserContextAsync(IBrowser browser, string? storageStatePath = null)
    {
        return await browser.NewContextAsync(new BrowserNewContextOptions
        {
            StorageStatePath = storageStatePath,
            IgnoreHTTPSErrors = true
        });
    }

    protected static async Task<string> SaveStateAsync(IBrowserContext context, string? prefix = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        prefix ??= CreateUniqueId();

        await context.StorageStateAsync(new BrowserContextStorageStateOptions
        {
            Path = $"{prefix}_{STATE_FILE}"
        });

        return prefix;
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
