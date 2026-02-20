using System.Collections.Concurrent;
using System.Text;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ReactApp.AppHost;

namespace ReactApp.UITests;

/// <summary>
/// Base class for UI tests that configures Playwright browser
/// </summary>
public abstract class UITestBase : IAsyncDisposable
{
    private static TimeSpan AspireDefaultTimeout { get; set; } = TimeSpan.FromMinutes(2);
    private static DistributedApplication? _aspireAppHost = null;
    private static Uri? _externalFrontendUrl = null;

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
    private CancellationTokenSource? _logCts;
    private ConcurrentQueue<string> _logLines = new();

    protected IPage Page { get; private set; } = null!;
    protected IBrowser Browser => _browser!;

    protected static string CreateUniqueId() => Guid.NewGuid().ToString("N")[..12];
    protected string TestPassword { get; } = "Test@Pass123!";
    protected string TestEmail { get; } = $"testuser{CreateUniqueId()}@example.com";

    protected string? StateId { get; set; }

    protected static Uri FrontendBaseUri
    {
        get
        {
            if (_externalFrontendUrl is not null)
                return _externalFrontendUrl;
            
            if (_aspireAppHost is null)
                throw new InvalidOperationException("Neither external frontend URL nor Aspire host is available");
            
            return _aspireAppHost.GetEndpoint(Resources.Frontend);
        }
    }

    protected static CancellationToken CancellationToken =>
        TestContext.Current?.Execution.CancellationToken ?? CancellationToken.None;

    [Before(TestSession)]
    public static async Task StartAspireHost()
    {
        // Check if an external frontend URL is provided
        var externalUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
        if (!string.IsNullOrWhiteSpace(externalUrl))
        {
            _externalFrontendUrl = new Uri(externalUrl);
            Console.WriteLine($"Using external frontend at: {externalUrl}");
            Console.WriteLine("Skipping Aspire host creation - using externally running instance");
            return;
        }

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

        StartCollectingLogs();

        _playwright = await Playwright.CreateAsync();
        _browser = await CreateBrowserAsync(_playwright);
        _context = await CreateBrowserContextAsync(_browser, StateId is not null ? $"{StateId}_{STATE_FILE}" : null);

        Page = await _context.NewPageAsync();
        await AfterTestSetupAsync();
    }

    protected virtual async Task BeforeTestSetupAsync() { }

    protected virtual async Task AfterTestSetupAsync() { }

    [After(Test)]
    public async Task TearDownAsync(TestContext testContext)
    {
        StopCollectingLogs();
        await CaptureScreenshotOnFailureAsync(testContext);
        await CaptureLogsOnFailureAsync(testContext);
        await DisposeAsync();
    }

    private async Task CaptureScreenshotOnFailureAsync(TestContext testContext)
    {
        try
        {
            if (testContext.Execution.Result?.State is not TestState.Failed || Page is null || Page.IsClosed)
                return;

            var screenshotDir = PlaywrightConfiguration.ScreenshotDirectory;
            Directory.CreateDirectory(screenshotDir);

            var testName = testContext.Metadata.TestName;
            var className = testContext.Metadata.TestDetails.Class.ClassType.FullName;
            var sanitized = string.Join("_", $"{className}.{testName}".Split(Path.GetInvalidFileNameChars()));
            var screenshotPath = Path.Combine(screenshotDir, $"{sanitized}_{CreateUniqueId()}.png");

            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = screenshotPath,
                FullPage = true
            });

            Console.WriteLine($"Screenshot saved to: {screenshotPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to capture screenshot: {ex.Message}");
        }
    }

    private void StartCollectingLogs()
    {
        // Skip log collection if using external AppHost
        if (_aspireAppHost is null)
            return;

        _logLines = new ConcurrentQueue<string>();
        _logCts = new CancellationTokenSource();

        var loggerService = _aspireAppHost.Services.GetRequiredService<ResourceLoggerService>();
        var appModel = _aspireAppHost.Services.GetRequiredService<DistributedApplicationModel>();

        foreach (var resource in appModel.Resources)
        {
            var resourceName = resource.Name;
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var batch in loggerService.WatchAsync(resourceName)
                        .WithCancellation(_logCts.Token))
                    {
                        foreach (var line in batch)
                        {
                            var prefix = line.IsErrorMessage ? "ERR" : "OUT";
                            _logLines.Enqueue($"[{resourceName}] [{prefix}] {line.Content}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when test ends
                }
            });
        }
    }

    private void StopCollectingLogs()
    {
        try
        {
            _logCts?.Cancel();
            _logCts?.Dispose();
            _logCts = null;
        }
        catch { }
    }

    private Task CaptureLogsOnFailureAsync(TestContext testContext)
    {
        try
        {
            if (testContext.Execution.Result?.State is not TestState.Failed || _logLines.IsEmpty)
                return Task.CompletedTask;

            var logsDir = PlaywrightConfiguration.LogsDirectory;
            Directory.CreateDirectory(logsDir);

            var testName = testContext.Metadata.TestName;
            var className = testContext.Metadata.TestDetails.Class.ClassType.FullName;
            var sanitized = string.Join("_", $"{className}.{testName}".Split(Path.GetInvalidFileNameChars()));
            var logPath = Path.Combine(logsDir, $"{sanitized}_{CreateUniqueId()}.log");

            var sb = new StringBuilder();
            while (_logLines.TryDequeue(out var line))
            {
                sb.AppendLine(line);
            }

            File.WriteAllText(logPath, sb.ToString());
            Console.WriteLine($"Aspire logs saved to: {logPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to capture Aspire logs: {ex.Message}");
        }

        return Task.CompletedTask;
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
