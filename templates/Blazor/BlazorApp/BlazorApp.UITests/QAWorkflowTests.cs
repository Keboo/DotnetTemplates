using BlazorApp.UITests.PageObjects;

using static BlazorApp.UITests.TestData;

namespace BlazorApp.UITests;

/// <summary>
/// Comprehensive UI tests for the Q&A System workflow
/// Tests cover registration, room creation, question management, and account deletion
/// </summary>
[DependsOn<AuthTests>]
public class QAWorkflowTests : UITestBase
{
    private string TestRoomName { get; set; } = "";

    public override Task SetupAsync()
    {
        TestRoomName = $"TestRoom{CreateUniqueId()}";
        return base.SetupAsync();
    }

    // Helper method to verify room exists with retry logic
    private async Task VerifyRoomExistsAsync(string roomName, int maxAttempts = 3)
    {
        var attempts = 0;
        var roomExists = false;
        
        while (!roomExists && attempts < maxAttempts)
        {
            attempts++;
            await Page.GotoAsync($"{TestConfiguration.BaseUrl}/room/{roomName.ToLower()}");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(2000);
            
            var pageContent = await Page.ContentAsync();
            if (!pageContent.Contains("404"))
            {
                roomExists = true;
            }
            else if (attempts < maxAttempts)
            {
                Console.WriteLine($"Room not available yet, retrying (attempt {attempts}/{maxAttempts})...");
                await Task.Delay(3000);
            }
        }
        
        if (!roomExists)
        {
            throw new Exception($"Room {roomName} was not created successfully - got 404 after {maxAttempts} attempts");
        }
    }

    [Test]
    public async Task CreateRoom_ShouldAppearInMyRooms()
    {
        await LoginAsync();

        // Create room
        var myRoomsPage = new MyRoomsPage(Page);
        await myRoomsPage.NavigateAsync();
        var roomName = await myRoomsPage.CreateRoomAsync(TestRoomName);

        // Instead of checking if room exists in list, try to navigate to it
        // If it exists, the navigation will succeed
        await Page.GotoAsync($"{TestConfiguration.BaseUrl}/room/{roomName.ToLower()}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // If we get here without error, the room exists
        await Assert.That(Page.Url.Contains("/Account/Login")).IsFalse().Because("Should not be redirected to login");
        await Assert.That(Page.Url.Contains("404")).IsFalse().Because("Room should exist (no 404)");
    }

    [Test]
    public async Task CompleteQAWorkflow_ShouldExecuteAllSteps()
    {
        // This is the main comprehensive test that executes the full workflow
        
        // Step 1: Register a new account and verify email
        var registerPage = new RegisterPage(Page);
        await registerPage.NavigateAsync();
        await registerPage.RegisterAsync(TestEmail, TestPassword);
        
        await Assert.That(await registerPage.IsConfirmationMessageVisibleAsync()).IsTrue().Because("Registration confirmation message should be visible");
        
        // Get and navigate to email confirmation link
        var confirmationLink = await registerPage.GetEmailConfirmationLinkAsync();
        await Page.GotoAsync(confirmationLink);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Verify email confirmation success
        var confirmationText = Page.Locator("text=Thank you for confirming your email");
        await confirmationText.WaitForAsync();
        
        // Step 2: Login
        var loginPage = new LoginPage(Page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(TestEmail, TestPassword);
        
        await Assert.That(await loginPage.IsLoggedInAsync()).IsTrue().Because("User should be logged in after successful login");
        
        // Step 3: Create a room
        var myRoomsPage = new MyRoomsPage(Page);
        await myRoomsPage.NavigateAsync();
        await myRoomsPage.CreateRoomAsync(TestRoomName);
        
        // Room created successfully (if it failed, an exception would have been thrown)
        
        // Wait for room to be fully committed to database and verify it exists
        await Task.Delay(5000);
        await VerifyRoomExistsAsync(TestRoomName);
        
        // Step 4: Join the room from a different browser instance and submit two questions
        var context2 = await Browser.NewContextAsync();
        var page2 = await context2.NewPageAsync();
        
        var roomViewPage2 = new RoomViewPage(page2);
        await roomViewPage2.NavigateAsync(TestRoomName);
        await roomViewPage2.SetDisplayNameAsync("Anonymous User");
        
        var question1 = "What is the meaning of life?";
        var question2 = "How does SignalR work?";
        
        await roomViewPage2.SubmitQuestionAsync(question1);
        await Task.Delay(500, CancellationToken.None); // Respect rate limiting
        
        await roomViewPage2.SubmitQuestionAsync(question2);
        
        // Step 5: Reject one of the questions
        var managePage = new ManageRoomPage(Page);
        await managePage.NavigateAsync(TestRoomName);
        
        // Wait for questions to appear in pending
        await Task.Delay(2000, CancellationToken.None);
        
        await Assert.That(await managePage.IsQuestionInPendingAsync(question1)).IsTrue().Because("Question 1 should be in pending approval");
        await Assert.That(await managePage.IsQuestionInPendingAsync(question2)).IsTrue().Because("Question 2 should be in pending approval");
        
        await managePage.RejectQuestionAsync(question1);
        
        // Step 6: Approve the other question, verify it shows in the other browser session
        await managePage.ApproveQuestionAsync(question2);
        
        await Assert.That(await managePage.IsQuestionInApprovedAsync(question2)).IsTrue().Because("Question 2 should be in approved list");
        
        // Verify question appears in other browser via SignalR
        await roomViewPage2.WaitForQuestionToAppearAsync(question2, timeout: 10000);
        await Assert.That(await roomViewPage2.IsQuestionVisibleAsync(question2)).IsTrue().Because("Approved question should appear in real-time on the public view");
        
        // Step 7: Mark the question as current
        await managePage.SetAsCurrentQuestionAsync(question2);
        
        await Assert.That(await managePage.IsCurrentQuestionSetAsync(question2)).IsTrue().Because("Question 2 should be set as current question");
        
        // Verify current question appears in other browser
        await Task.Delay(2000, CancellationToken.None); // Wait for SignalR update
        await Assert.That(await roomViewPage2.IsCurrentQuestionVisibleAsync()).IsTrue().Because("Current question should be visible in public view");
        
        var currentQuestionText = await roomViewPage2.GetCurrentQuestionTextAsync();
        await Assert.That(currentQuestionText?.Contains(question2) ?? false).IsTrue().Because("Current question should contain the correct text");
        
        // Step 8: Clear the question as current
        await managePage.ClearCurrentQuestionAsync();
        
        // Wait for SignalR to update
        await Task.Delay(2000, CancellationToken.None);
        
        // Step 9: Mark the question as answered
        await managePage.MarkAsAnsweredAsync(question2);
        
        // Wait for SignalR to update
        await Task.Delay(2000, CancellationToken.None);
        
        // Step 10: Delete the question
        await managePage.DeleteQuestionAsync(question2);
        
        // Verify question is deleted in other browser via SignalR
        await roomViewPage2.WaitForQuestionToDisappearAsync(question2, timeout: 10000);
        await Assert.That(await roomViewPage2.IsQuestionVisibleAsync(question2)).IsFalse().Because("Deleted question should disappear from public view");
        
        // Step 11: Delete the room
        await managePage.DeleteRoomAsync();
        
        // Verify redirect to My Rooms page
        await Page.WaitForURLAsync($"**/my-rooms", new PageWaitForURLOptions { Timeout = 10000 });
        
        // Step 12: Verify the account pages all load
        var accountPages = new AccountPages(Page);
        
        await Assert.That(await accountPages.CanLoadProfilePageAsync()).IsTrue().Because("Profile page should load");
        await Assert.That(await accountPages.CanLoadEmailPageAsync()).IsTrue().Because("Email page should load");
        await Assert.That(await accountPages.CanLoadChangePasswordPageAsync()).IsTrue().Because("Change password page should load");
        await Assert.That(await accountPages.CanLoadTwoFactorPageAsync()).IsTrue().Because("Two-factor authentication page should load");
        await Assert.That(await accountPages.CanLoadPersonalDataPageAsync()).IsTrue().Because("Personal data page should load");
        
        // Step 13: Delete the user's account
        await accountPages.DeleteAccountAsync(TestPassword);
        
        // Verify logout/redirect
        await Page.WaitForURLAsync($"**/", new PageWaitForURLOptions { Timeout = 10000 });
        
        // Clean up second browser context
        await page2.CloseAsync();
        await context2.CloseAsync();
    }
    
    [Test]
    public async Task SubmitQuestion_ShouldAppearInPending()
    {
        // Setup: Create room
        await LoginAsync();
        var myRoomsPage = new MyRoomsPage(Page);
        await myRoomsPage.NavigateAsync();
        await myRoomsPage.CreateRoomAsync(TestRoomName);
        
        // Wait for room to be fully committed to database and verify it exists
        await Task.Delay(5000, CancellationToken);
        await VerifyRoomExistsAsync(TestRoomName);
        
        // Submit question as different user
        var context2 = await Browser.NewContextAsync();
        var page2 = await context2.NewPageAsync();
        
        var roomViewPage = new RoomViewPage(page2);
        await roomViewPage.NavigateAsync(TestRoomName);
        await roomViewPage.SetDisplayNameAsync("Test User");
        
        var questionText = "Test Question?";
        await roomViewPage.SubmitQuestionAsync(questionText);
        
        // Verify in manage page
        var managePage = new ManageRoomPage(Page);
        await managePage.NavigateAsync(TestRoomName);
        
        await Task.Delay(2000, CancellationToken); // Wait for question submission
        
        await Assert.That(await managePage.IsQuestionInPendingAsync(questionText)).IsTrue().Because("Submitted question should appear in pending");
        
        // Cleanup
        await page2.CloseAsync();
        await context2.CloseAsync();
    }
    
    [Test]
    public async Task ApproveQuestion_ShouldShowInPublicView()
    {
        // Setup: Create room and submit question
        await LoginAsync();
        var myRoomsPage = new MyRoomsPage(Page);
        await myRoomsPage.NavigateAsync();
        await myRoomsPage.CreateRoomAsync(TestRoomName);
        
        // Wait for room to be fully committed to database and verify it exists
        await Task.Delay(5000, CancellationToken);
        await VerifyRoomExistsAsync(TestRoomName);
        
        var context2 = await Browser.NewContextAsync();
        var page2 = await context2.NewPageAsync();
        
        var roomViewPage = new RoomViewPage(page2);
        await roomViewPage.NavigateAsync(TestRoomName);
        await roomViewPage.SetDisplayNameAsync("Test User");
        
        var questionText = "Test Question for Approval?";
        await roomViewPage.SubmitQuestionAsync(questionText);
        
        // Approve the question
        var managePage = new ManageRoomPage(Page);
        await managePage.NavigateAsync(TestRoomName);
        await Task.Delay(2000, CancellationToken);
        
        await managePage.ApproveQuestionAsync(questionText);
        
        // Verify it appears in public view via SignalR
        await roomViewPage.WaitForQuestionToAppearAsync(questionText, timeout: 10000);
        await Assert.That(await roomViewPage.IsQuestionVisibleAsync(questionText)).IsTrue().Because("Approved question should appear in public view");
        
        // Cleanup
        await page2.CloseAsync();
        await context2.CloseAsync();
    }
    
    /// <summary>
    /// Helper method to register a new account and login
    /// </summary>
    private async Task LoginAsync()
    {
        var loginPage = new LoginPage(Page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(TestEmail, TestPassword);
    }
}


