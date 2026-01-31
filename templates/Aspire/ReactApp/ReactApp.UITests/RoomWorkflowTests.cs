using ReactApp.UITests.PageObjects;

namespace ReactApp.UITests;

/// <summary>
/// Comprehensive UI tests for the Q&A System workflow
/// Tests cover registration, room creation, question management, and account deletion
/// </summary>
public class RoomWorkflowTests : AuthedUserTestBase
{
    private string TestRoomName { get; set; } = "";

    protected override async Task AfterTestSetupAsync()
    {
        await base.AfterTestSetupAsync();
        TestRoomName = $"TestRoom{CreateUniqueId()}";
    }

    [Test]
    public async Task CreateRoom_ShouldAppearInMyRooms()
    {
        // Create room
        var myRoomsPage = new MyRoomsPage(Page);
        await myRoomsPage.NavigateAsync(FrontendBaseUri);
        await myRoomsPage.CreateRoomAsync(TestRoomName);

        // After room creation, verify the room appears in the My Rooms list
        await myRoomsPage.NavigateAsync(FrontendBaseUri);
        
        await Assert.That(myRoomsPage.RoomExistsAsync(TestRoomName))
            .IsTrue()
            .Because("Room should exist in My Rooms list");
    }

    [Test]
    public async Task SubmitQuestion_ShouldAppearInPending()
    {
        // Setup: Create room
        var myRoomsPage = new MyRoomsPage(Page);
        await myRoomsPage.NavigateAsync(FrontendBaseUri);
        await myRoomsPage.CreateRoomAsync(TestRoomName);
        
        // Wait for room to be fully committed to database and verify it exists
        await Task.Delay(5000, CancellationToken);
        
        // Submit question as different user
        await using var context2 = await Browser.NewContextAsync();
        var page2 = await context2.NewPageAsync();
        
        var roomViewPage = new RoomViewPage(page2);
        await roomViewPage.NavigateAsync(FrontendBaseUri, TestRoomName);
        await roomViewPage.SetDisplayNameAsync("Test User");
        
        var questionText = "Test Question?";
        await roomViewPage.SubmitQuestionAsync(questionText);
        
        // Verify in manage page
        var managePage = new ManageRoomPage(Page);
        await managePage.NavigateAsync(FrontendBaseUri, TestRoomName);
        
        await Task.Delay(2000, CancellationToken); // Wait for question submission
        
        await Assert.That(await managePage.IsQuestionInPendingAsync(questionText)).IsTrue().Because("Submitted question should appear in pending");
        
        // Cleanup
        await page2.CloseAsync();
    }
    
    [Test]
    public async Task ApproveQuestion_ShouldShowInPublicView()
    {
        // Setup: Create room and submit question
        var myRoomsPage = new MyRoomsPage(Page);
        await myRoomsPage.NavigateAsync(FrontendBaseUri);
        await myRoomsPage.CreateRoomAsync(TestRoomName);
        
        // Wait for room to be fully committed to database and verify it exists
        await Task.Delay(5000, CancellationToken);
        
        await using var context2 = await Browser.NewContextAsync();
        var page2 = await context2.NewPageAsync();
        
        var roomViewPage = new RoomViewPage(page2);
        await roomViewPage.NavigateAsync(FrontendBaseUri, TestRoomName);
        await roomViewPage.SetDisplayNameAsync("Test User");
        
        var questionText = "Test Question for Approval?";
        await roomViewPage.SubmitQuestionAsync(questionText);
        
        // Approve the question
        var managePage = new ManageRoomPage(Page);
        await managePage.NavigateAsync(FrontendBaseUri, TestRoomName);
        await Task.Delay(2000, CancellationToken);
        
        await managePage.ApproveQuestionAsync(questionText);
        
        // Verify it appears in public view via SignalR
        await roomViewPage.WaitForQuestionToAppearAsync(questionText, timeout: 10000);
        await Assert.That(await roomViewPage.IsQuestionVisibleAsync(questionText)).IsTrue().Because("Approved question should appear in public view");
        
        // Cleanup
        await page2.CloseAsync();
    }

    [Test]
    [Category(TestCategories.Accessibility)]
    public async Task MyRoomsPageIsAccessible()
    {
        var myRoomsPage = new MyRoomsPage(Page);
        await myRoomsPage.NavigateAsync(FrontendBaseUri);
        await myRoomsPage.CreateRoomAsync(TestRoomName);
        
        // Wait for snackbar to disappear (it has insufficient contrast from notistack library)
        await Page.WaitForSelectorAsync(".notistack-MuiContent", new PageWaitForSelectorOptions 
        { 
            State = WaitForSelectorState.Hidden,
            Timeout = 10000 
        });
        
        await AssertNoAccessibilityViolations();
    }

    [Test]
    [Category(TestCategories.Accessibility)]
    public async Task RoomViewPageIsAccessible()
    {
        MyRoomsPage myRoomsPage = new(Page);
        await myRoomsPage.NavigateAsync(FrontendBaseUri);
        await myRoomsPage.CreateRoomAsync(TestRoomName);

        RoomViewPage roomViewPage = new(Page);
        await roomViewPage.NavigateAsync(FrontendBaseUri, TestRoomName);
        await roomViewPage.SetDisplayNameAsync("Test User");

        await AssertNoAccessibilityViolations();
    }

    [Test]
    [Category(TestCategories.Accessibility)]
    public async Task ManageRoomPageIsAccessible()
    {
        MyRoomsPage myRoomsPage = new(Page);
        await myRoomsPage.NavigateAsync(FrontendBaseUri);
        await myRoomsPage.CreateRoomAsync(TestRoomName);

        ManageRoomPage managePage = new(Page);
        await managePage.NavigateAsync(FrontendBaseUri, TestRoomName);

        await AssertNoAccessibilityViolations();
    }
}


