using BlazorApp.UITests.PageObjects;

using static BlazorApp.UITests.TestData;

namespace BlazorApp.UITests;

public class AuthTests : UITestBase
{
    [Test]
    public async Task RegisterNewAccount_ShouldSucceed()
    {
        var registerPage = new RegisterPage(Page);
        await registerPage.NavigateAsync();
        await registerPage.RegisterAsync(TestEmail, TestPassword);

        await Assert.That(await registerPage.IsConfirmationMessageVisibleAsync()).IsTrue().Because("Registration confirmation message should be visible");

        var confirmationLink = await registerPage.GetEmailConfirmationLinkAsync();
        await Assert.That(string.IsNullOrEmpty(confirmationLink)).IsFalse().Because("Email confirmation link should be present");
    }

    [Test]
    [DependsOn(nameof(RegisterNewAccount_ShouldSucceed))]
    public async Task LoginWithValidCredentials_ShouldSucceed()
    {
        // First register and confirm email
        var registerPage = new RegisterPage(Page);
        await registerPage.NavigateAsync();
        await registerPage.RegisterAsync(TestEmail, TestPassword);

        var confirmationLink = await registerPage.GetEmailConfirmationLinkAsync();
        await Page.GotoAsync(confirmationLink);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Now login
        var loginPage = new LoginPage(Page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(TestEmail, TestPassword);

        await Assert.That(await loginPage.IsLoggedInAsync()).IsTrue().Because("User should be logged in after successful login");
    }
}


