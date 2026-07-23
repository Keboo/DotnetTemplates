namespace AspireApp.UITests;

public class SmokeTests
{
    [Test]
    public async Task TemplateBuildsAndRunsTests()
    {
        await Assert.That(Environment.CurrentDirectory).IsNotEmpty();
    }
}

