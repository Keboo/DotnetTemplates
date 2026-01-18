namespace BlazorApp.UITests;

/// <summary>
/// Configuration settings for UI tests
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// Base URL for the application under test
    /// Can be overridden via environment variable TEST_BASE_URL
    /// </summary>
    public static string BaseUrl => 
        Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "https://blazorapp.dev.localhost:7147";
    
    /// <summary>
    /// Timeout for page navigation and element visibility
    /// </summary>
    public static float DefaultTimeout => 10000;
    
    /// <summary>
    /// Timeout for waiting on real-time SignalR updates
    /// </summary>
    public static float SignalRTimeout => 5000;
    
    /// <summary>
    /// Whether to run browser in headed mode (visible window)
    /// Can be overridden via environment variable HEADED (set to "1" or "true")
    /// </summary>
    public static bool Headless
    {
        get
        {
            var envValue = Environment.GetEnvironmentVariable("HEADLESS");
            return envValue == "1" ||
                   envValue?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
    
    /// <summary>
    /// Slow motion delay in milliseconds (useful for debugging)
    /// Can be overridden via environment variable SLOW_MO
    /// </summary>
    public static float SlowMo
    {
        get
        {
            var envValue = Environment.GetEnvironmentVariable("SLOW_MO");
            return float.TryParse(envValue, out var value) ? value : 0;
        }
    }
}
