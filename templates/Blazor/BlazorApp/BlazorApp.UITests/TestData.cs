namespace BlazorApp.UITests;

public static class TestData
{
    public static string UniqueId { get; } = CreateUniqueId();
    public static string TestPassword { get; } = "Test@Pass123!";
    public static string TestEmail => $"testuser{UniqueId}@example.com";

    public static string CreateUniqueId() => Guid.NewGuid().ToString("N")[..12];
}


