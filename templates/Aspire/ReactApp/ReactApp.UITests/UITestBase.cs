using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

using Microsoft.Extensions.Configuration;

using ReactApp.AppHost;
using ReactApp.UITests.PageObjects;

namespace ReactApp.UITests;

/// <summary>
/// Base class for UI tests that configures Playwright browser
/// </summary>
public abstract class UITestBase : IAsyncDisposable
{
    private static TimeSpan AspireDefaultTimeout { get; set; } = TimeSpan.FromMinutes(2);
    private static DistributedApplication _aspireAppHost = null!;

    protected static AxeRunOptions AxeOptions => new()
    {
        RunOnly = new RunOnlyOptions
        {
            Type = "tag",
            // Focus on WCAG 2.x AA compliance (the commonly accepted standard)
            // AAA is excluded as it requires stricter color contrast ratios (7:1)
            // that are difficult to achieve with standard UI frameworks
            Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"]
        },
        ResultTypes = [ResultType.Incomplete, ResultType.Violations]
    };

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

    protected static Uri FrontendBaseUri => _aspireAppHost.GetEndpoint(Resources.Frontend);

    protected static CancellationToken CancellationToken =>
        TestContext.Current?.Execution.CancellationToken ?? CancellationToken.None;

    [Before(TestSession)]
    public static async Task StartAspireHost()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.ReactApp_AppHost>([], (x, i) =>
            {
                i.Configuration!.AddInMemoryCollection(
                [
                    new(Resources.ContainerSuffixKey, "UITests")
                ]);
            });

        // Force the database to run in an in-memory containers
        var sqlServer = appHost.Resources.OfType<SqlServerServerResource>()
            .First(x => x.Name == Resources.SqlServer);
        foreach (var annotation in sqlServer.Annotations
            .ToList())
        {
            if (annotation is ContainerMountAnnotation or ContainerLifetimeAnnotation)
                sqlServer.Annotations.Remove(annotation);
        }

        // Build the aspire host
        var app = _aspireAppHost = await appHost.BuildAsync(CancellationToken)
            .WaitAsync(AspireDefaultTimeout, CancellationToken);

        // Start the aspire host
        await app.StartAsync(CancellationToken)
            .WaitAsync(AspireDefaultTimeout, CancellationToken);

        // Wait for the front end to start
        await app.ResourceNotifications.WaitForResourceHealthyAsync(
            Resources.Frontend, CancellationToken)
            .WaitAsync(AspireDefaultTimeout, CancellationToken);
    }

    [After(TestSession)]
    public static async Task StopAspireHost()
    {
        if (_aspireAppHost != null)
        {
            await _aspireAppHost.DisposeAsync();
            _aspireAppHost = null!;
        }
    }

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
            Headless = PlaywrightConfiguration.Headless,
            SlowMo = PlaywrightConfiguration.SlowMo
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


    protected async Task<AxeResult> AssertNoAccessibilityViolations()
    {
        AxeResult result = await Page.RunAxe(AxeOptions);

        await Assert.That(result.Violations).IsEmpty();
        return result;
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
