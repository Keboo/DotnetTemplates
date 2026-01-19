using ReactApp.UITests.PageObjects;

namespace ReactApp.UITests;

public class AuthTests : UITestBase
{
    [Test]
    public async Task CanRegiserAndLoginWithNewAccount()
    {
        var registerPage = new RegisterPage(Page);
        await registerPage.NavigateAsync();
        await registerPage.RegisterAsync(TestEmail, TestPassword);

        await Assert.That(await registerPage.IsConfirmationMessageVisibleAsync()).IsTrue().Because("Registration confirmation message should be visible");

        var confirmationLink = await registerPage.GetEmailConfirmationLinkAsync();
        await Assert.That(string.IsNullOrEmpty(confirmationLink)).IsFalse().Because("Email confirmation link should be present");

        await registerPage.ConfirmAccountAsync();
        
        var loginPage = new LoginPage(Page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(TestEmail, TestPassword);

        await Assert.That(await loginPage.IsLoggedInAsync()).IsTrue().Because("User should be logged in after successful login");
    }
}


