using ReactApp.UITests.PageObjects;

namespace ReactApp.UITests;

public class AuthTests : UITestBase
{
    [Test]
    public async Task CanRegiserAndLoginWithNewAccount()
    {
        // Register a new user - this automatically logs them in
        var registerPage = new RegisterPage(Page);
        await registerPage.NavigateAsync(FrontendBaseUri);
        await registerPage.RegisterAsync(TestEmail, TestPassword);

        await Assert.That(await registerPage.IsConfirmationMessageVisibleAsync()).IsTrue().Because("User should be redirected to my-rooms after registration");

        // Verify user is logged in by checking for logout button or my-rooms access
        var loginPage = new LoginPage(Page);
        await Assert.That(await loginPage.IsLoggedInAsync()).IsTrue().Because("User should be logged in after successful registration");

        // Log out and log back in to verify login flow works
        await loginPage.LogoutAsync();
        
        await loginPage.NavigateAsync(FrontendBaseUri);
        await loginPage.LoginAsync(TestEmail, TestPassword);

        await Assert.That(await loginPage.IsLoggedInAsync()).IsTrue().Because("User should be logged in after successful login");
    }

    [Test]
    [Category(TestCategories.Accessibility)]
    public async Task LoginPageIsAccessible()
    {
        var loginPage = new LoginPage(Page);
        await loginPage.NavigateAsync(FrontendBaseUri);
        await AssertNoAccessibilityViolations();
    }

    [Test]
    [Category(TestCategories.Accessibility)]
    public async Task RegisterPageIsAccessible()
    {
        RegisterPage registerPage = new(Page);
        await registerPage.NavigateAsync(FrontendBaseUri);
        await AssertNoAccessibilityViolations();
    }
}


