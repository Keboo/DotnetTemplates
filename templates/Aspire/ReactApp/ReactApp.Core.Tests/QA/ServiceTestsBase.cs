using ReactApp.Core.Hubs;
using ReactApp.Data;

using Velopack.Testing;

namespace ReactApp.Core.Tests.QA;

public abstract class ServiceTestsBase
{
    protected AutoMocker Mocker { get; } = new();

    [Before(Test)]
    public async Task Setup()
    {
        Mocker.WithDbContext<ApplicationDbContext>();
        Mocker.WithSignalR<RoomHub>();
    }

    [After(Test)]
    public async Task TearDown()
    {
        if (Mocker.AsDisposable() is { } disposable)
        {
            disposable.Dispose();
        }
    }

    protected async Task<ApplicationUser> CreateUserAsync(string userId = "test-user")
    {
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = $"{userId}@example.com",
            Email = $"{userId}@example.com"
        };
        
        await Mocker.InDbScopeAsync(async context =>
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        });
        return user;
    }

    protected async Task<Room> CreateRoomAsync(string userId, string friendlyName = "Test Room")
    {
        var room = new Room
        {
            Id = Guid.NewGuid(),
            FriendlyName = friendlyName,
            CreatedByUserId = userId,
            CreatedDate = DateTimeOffset.UtcNow
        };

        await Mocker.InDbScopeAsync(async context =>
        {
            context.Rooms.Add(room);
            await context.SaveChangesAsync();
        });

        return room;
    }

    protected async Task<Question> CreateQuestionAsync(
        Guid roomId,
        string questionText = "Test Question",
        string? authorName = "Test Author",
        bool isApproved = false,
        bool isAnswered = false)
    {
        var question = new Question
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            QuestionText = questionText,
            AuthorName = authorName,
            IsApproved = isApproved,
            IsAnswered = isAnswered,
            CreatedDate = DateTimeOffset.UtcNow
        };

        await Mocker.InDbScopeAsync(async context =>
        {
            context.Questions.Add(question);
            await context.SaveChangesAsync();
        });

        return question;
    }
}
