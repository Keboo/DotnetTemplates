using ReactApp.UITests.PageObjects;

namespace ReactApp.UITests;

public abstract class AuthedUserTestBase : UITestBase
{
    private static string AuthStateId { get; set; } = "";

    protected static string Username { get; private set; } = $"testuser{CreateUniqueId()}@example.com";
    protected static string Password { get; private set; } = "Test@Pass123!";

    [Before(Class)]
    public static async Task LoginUserAsync()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await CreateBrowserAsync(playwright);
        await using var context = await CreateBrowserContextAsync(browser);
        
        IPage? page = null;
        try
        {
            page = await context.NewPageAsync();

            var registerPage = new RegisterPage(page);
            await registerPage.NavigateAsync(FrontendBaseUri);
            await registerPage.RegisterAsync(Username, Password);
            // Note: In React app, registration automatically logs in the user
            // and redirects to /my-rooms, so no separate login step is needed

            AuthStateId = await SaveStateAsync(context);
        }
        finally
        {
            if (page != null)
            {
                await page.CloseAsync();
            }
        }
    }

    protected override async Task BeforeTestSetupAsync()
    {
        await base.BeforeTestSetupAsync();
        StateId = AuthStateId;
    }
}
