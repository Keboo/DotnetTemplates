using ReactApp.Core.QA;

using Microsoft.EntityFrameworkCore;

using Velopack.Testing;

namespace ReactApp.Core.Tests.QA;

public sealed class QuestionServiceTests : ServiceTestsBase
{
    [Test]
    public async Task GetQuestionsByRoomIdAsync_ReturnsAllQuestionsInRoom()
    {
        var user = await CreateUserAsync();
        var room = await CreateRoomAsync(user.Id);
        var question1 = await CreateQuestionAsync(room.Id, "Question 1");
        var question2 = await CreateQuestionAsync(room.Id, "Question 2");
        var otherRoom = await CreateRoomAsync(user.Id, "Other Room");
        await CreateQuestionAsync(otherRoom.Id, "Other Question");

        var service = Mocker.CreateInstance<QuestionService>();
        var result = await service.GetQuestionsByRoomIdAsync(room.Id);

        var questions = result.ToList();
        await Assert.That(questions.Count).IsEqualTo(2);
        await Assert.That(questions.Any(q => q.Id == question1.Id)).IsTrue();
        await Assert.That(questions.Any(q => q.Id == question2.Id)).IsTrue();
    }

    [Test]
    public async Task GetApprovedQuestionsByRoomIdAsync_ReturnsOnlyApprovedQuestions()
    {
        var user = await CreateUserAsync();
        var room = await CreateRoomAsync(user.Id);
        var approvedQuestion = await CreateQuestionAsync(room.Id, "Approved", isApproved: true);
        await CreateQuestionAsync(room.Id, "Not Approved", isApproved: false);

        var service = Mocker.CreateInstance<QuestionService>();
        var result = await service.GetApprovedQuestionsByRoomIdAsync(room.Id);

        var questions = result.ToList();
        await Assert.That(questions.Count).IsEqualTo(1);
        await Assert.That(questions[0].Id).IsEqualTo(approvedQuestion.Id);
        await Assert.That(questions[0].IsApproved).IsTrue();
    }

    [Test]
    public async Task GetQuestionByIdAsync_ReturnsQuestion_WhenExists()
    {
        var user = await CreateUserAsync();
        var room = await CreateRoomAsync(user.Id);
        var question = await CreateQuestionAsync(room.Id, "Test Question");

        var service = Mocker.CreateInstance<QuestionService>();
        var result = await service.GetQuestionByIdAsync(question.Id);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(question.Id);
        await Assert.That(result.QuestionText).IsEqualTo("Test Question");
        await Assert.That(result.Room).IsNotNull();
    }

    [Test]
    public async Task GetQuestionByIdAsync_ReturnsNull_WhenNotExists()
    {
        var service = Mocker.CreateInstance<QuestionService>();
        var result = await service.GetQuestionByIdAsync(Guid.NewGuid());

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task SubmitQuestionAsync_CreatesNewQuestion()
    {
        var user = await CreateUserAsync();
        var room = await CreateRoomAsync(user.Id);
        var service = Mocker.CreateInstance<QuestionService>();

        var result = await service.SubmitQuestionAsync(
            room.Id,
            "New Question",
            "John Doe",
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.QuestionText).IsEqualTo("New Question");
        await Assert.That(result.AuthorName).IsEqualTo("John Doe");
        await Assert.That(result.RoomId).IsEqualTo(room.Id);
        await Assert.That(result.IsApproved).IsFalse();
        await Assert.That(result.IsAnswered).IsFalse();

        await Mocker.InDbScopeAsync(async context =>
        {
            var savedQuestion = await context.Questions.SingleAsync(x => x.Id == result.Id);
            await Assert.That(savedQuestion).IsNotNull();
        });
    }

    [Test]
    public async Task SubmitQuestionAsync_ThrowsException_WhenRoomNotFound()
    {
        var service = Mocker.CreateInstance<QuestionService>();

        await Assert.That(async () => await service.SubmitQuestionAsync(
            Guid.NewGuid(),
            "Question",
            "Author",
            CancellationToken.None))
            .Throws<InvalidOperationException>()
            .WithMessage("Room not found");
    }

    [Test]
    public async Task UpdateQuestionAsync_UpdatesQuestion()
    {
        var user = await CreateUserAsync();
        var room = await CreateRoomAsync(user.Id);
        var question = await CreateQuestionAsync(room.Id, "Original", "Original Author");

        var service = Mocker.CreateInstance<QuestionService>();

        var result = await service.UpdateQuestionAsync(
            question.Id,
            "Updated Question",
            "Updated Author",
            CancellationToken.None);

        await Assert.That(result.QuestionText).IsEqualTo("Updated Question");
        await Assert.That(result.AuthorName).IsEqualTo("Updated Author");
        await Assert.That(result.LastModifiedDate).IsNotNull();

        await Mocker.InDbScopeAsync(async context =>
        {
            var savedQuestion = await context.Questions.SingleAsync(x => x.Id == question.Id);
            await Assert.That(savedQuestion.QuestionText).IsEqualTo("Updated Question");
            await Assert.That(savedQuestion.AuthorName).IsEqualTo("Updated Author");
            await Assert.That(savedQuestion.LastModifiedDate).IsNotNull();
        });
    }

    [Test]
    public async Task UpdateQuestionAsync_ThrowsException_WhenQuestionNotFound()
    {
        var service = Mocker.CreateInstance<QuestionService>();

        await Assert.That(async () => await service.UpdateQuestionAsync(
            Guid.NewGuid(),
            "Updated",
            "Author",
            CancellationToken.None))
            .Throws<InvalidOperationException>()
            .WithMessage("Question not found");
    }

    [Test]
    public async Task UpdateQuestionAsync_ThrowsException_WhenQuestionIsApproved()
    {
        var user = await CreateUserAsync();
        var room = await CreateRoomAsync(user.Id);
        var question = await CreateQuestionAsync(room.Id, isApproved: true);
        var service = Mocker.CreateInstance<QuestionService>();

        await Assert.That(async () => await service.UpdateQuestionAsync(
            question.Id,
            "Updated",
            "Author",
            CancellationToken.None))
            .Throws<InvalidOperationException>()
            .WithMessage("Cannot update an approved question");
    }

    [Test]
    public async Task ApproveQuestionAsync_ApprovesQuestion()
    {
        var user = await CreateUserAsync();
        var room = await CreateRoomAsync(user.Id);
        var question = await CreateQuestionAsync(room.Id);
        var service = Mocker.CreateInstance<QuestionService>();

        await service.ApproveQuestionAsync(question.Id, user.Id, CancellationToken.None);

        await Mocker.InDbScopeAsync(async context =>
        {
            var approvedQuestion = await context.Questions.FindAsync(question.Id);
            await Assert.That(approvedQuestion!.IsApproved).IsTrue();
        });
    }

    [Test]
    public async Task ApproveQuestionAsync_ThrowsException_WhenQuestionNotFound()
    {
        var service = Mocker.CreateInstance<QuestionService>();

        await Assert.That(async () => await service.ApproveQuestionAsync(
            Guid.NewGuid(),
            "user",
            CancellationToken.None))
            .Throws<InvalidOperationException>()
            .WithMessage("Question not found");
    }

    [Test]
    public async Task ApproveQuestionAsync_ThrowsException_WhenUserNotRoomOwner()
    {
        var owner = await CreateUserAsync("owner");
        var otherUser = await CreateUserAsync("other-user");
        var room = await CreateRoomAsync(owner.Id);
        var question = await CreateQuestionAsync(room.Id);
        var service = Mocker.CreateInstance<QuestionService>();

        await Assert.That(async () => await service.ApproveQuestionAsync(
            question.Id,
            otherUser.Id,
            CancellationToken.None))
            .Throws<UnauthorizedAccessException>()
            .WithMessage("Only the room owner can approve questions");
    }

    [Test]
    public async Task MarkAsAnsweredAsync_MarksQuestionAsAnswered()
    {
        var user = await CreateUserAsync();
        var room = await CreateRoomAsync(user.Id);
        var question = await CreateQuestionAsync(room.Id);
        var service = Mocker.CreateInstance<QuestionService>();

        await service.MarkAsAnsweredAsync(question.Id, user.Id, CancellationToken.None);

        await Mocker.InDbScopeAsync(async context =>
        {
            var answeredQuestion = await context.Questions.FindAsync(question.Id);
            await Assert.That(answeredQuestion!.IsAnswered).IsTrue();
        });
    }

    [Test]
    public async Task MarkAsAnsweredAsync_ThrowsException_WhenQuestionNotFound()
    {
        var service = Mocker.CreateInstance<QuestionService>();

        await Assert.That(async () => await service.MarkAsAnsweredAsync(
            Guid.NewGuid(),
            "user",
            CancellationToken.None))
            .Throws<InvalidOperationException>()
            .WithMessage("Question not found");
    }

    [Test]
    public async Task MarkAsAnsweredAsync_ThrowsException_WhenUserNotRoomOwner()
    {
        var owner = await CreateUserAsync("owner");
        var otherUser = await CreateUserAsync("other-user");
        var room = await CreateRoomAsync(owner.Id);
        var question = await CreateQuestionAsync(room.Id);
        var service = Mocker.CreateInstance<QuestionService>();

        await Assert.That(async () => await service.MarkAsAnsweredAsync(
            question.Id,
            otherUser.Id,
            CancellationToken.None))
            .Throws<UnauthorizedAccessException>()
            .WithMessage("Only the room owner can mark questions as answered");
    }

    [Test]
    public async Task DeleteQuestionAsync_DeletesQuestion()
    {
        var user = await CreateUserAsync();
        var room = await CreateRoomAsync(user.Id);
        var question = await CreateQuestionAsync(room.Id);

        var service = Mocker.CreateInstance<QuestionService>();

        await service.DeleteQuestionAsync(question.Id, user.Id, CancellationToken.None);

        await Mocker.InDbScopeAsync(async context =>
        {
            var deletedQuestion = await context.Questions.FirstOrDefaultAsync(x => x.Id == question.Id);
            await Assert.That(deletedQuestion).IsNull();
        });
    }

    [Test]
    public async Task DeleteQuestionAsync_ThrowsException_WhenQuestionNotFound()
    {
        var service = Mocker.CreateInstance<QuestionService>();

        await Assert.That(async () => await service.DeleteQuestionAsync(
            Guid.NewGuid(),
            "user",
            CancellationToken.None))
            .Throws<InvalidOperationException>()
            .WithMessage("Question not found");
    }

    [Test]
    public async Task DeleteQuestionAsync_ThrowsException_WhenUserNotRoomOwner()
    {
        var owner = await CreateUserAsync("owner");
        var otherUser = await CreateUserAsync("other-user");
        var room = await CreateRoomAsync(owner.Id);
        var question = await CreateQuestionAsync(room.Id);
        var service = Mocker.CreateInstance<QuestionService>();

        await Assert.That(async () => await service.DeleteQuestionAsync(
            question.Id,
            otherUser.Id,
            CancellationToken.None))
            .Throws<UnauthorizedAccessException>()
            .WithMessage("Only the room owner can delete questions");
    }

    [Test]
    public async Task CanSubmitQuestionAsync_AllowsFirstSubmission()
    {
        var service = Mocker.CreateInstance<QuestionService>();

        var result = await service.CanSubmitQuestionAsync("client-123");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CanSubmitQuestionAsync_BlocksRapidSubmissions()
    {
        var clientId = "client-123";
        var service = Mocker.CreateInstance<QuestionService>();

        var firstResult = await service.CanSubmitQuestionAsync(clientId);
        await Assert.That(firstResult).IsTrue();

        var secondResult = await service.CanSubmitQuestionAsync(clientId);
        await Assert.That(secondResult).IsFalse();
    }

    [Test]
    public async Task CanSubmitQuestionAsync_AllowsSubmissionAfterRateLimitWindow()
    {
        var clientId = "client-123";
        var service = Mocker.CreateInstance<QuestionService>();

        await service.CanSubmitQuestionAsync(clientId);
        
        await Task.Delay(TimeSpan.FromSeconds(11));
        
        var result = await service.CanSubmitQuestionAsync(clientId);
        await Assert.That(result).IsTrue();
    }
}
