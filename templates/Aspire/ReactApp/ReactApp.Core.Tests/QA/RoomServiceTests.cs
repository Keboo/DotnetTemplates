using ReactApp.Core.QA;
using ReactApp.Data;

using Microsoft.EntityFrameworkCore;

using Velopack.Testing;

namespace ReactApp.Core.Tests.QA;

public class RoomServiceTests : ServiceTestsBase
{
    [Test]
    public async Task GetAllRoomsAsync_ReturnsAllRooms()
    {
        var user = await CreateUserAsync();
        var room1 = await CreateRoomAsync(user.Id, "Room 1");
        var room2 = await CreateRoomAsync(user.Id, "Room 2");

        var service = Mocker.CreateInstance<RoomService>();
        var result = await service.GetAllRoomsAsync();

        var rooms = result.ToList();
        await Assert.That(rooms.Count).IsEqualTo(2);
        await Assert.That(rooms.Any(r => r.Id == room1.Id)).IsTrue();
        await Assert.That(rooms.Any(r => r.Id == room2.Id)).IsTrue();
    }

    [Test]
    public async Task GetAllRoomsAsync_OrdersByCreatedDateDescending()
    {
        var user = await CreateUserAsync();
        var oldRoom = await CreateRoomAsync(user.Id, "Old Room");
        await Task.Delay(10);
        var newRoom = await CreateRoomAsync(user.Id, "New Room");

        var service = Mocker.CreateInstance<RoomService>();
        var result = await service.GetAllRoomsAsync();

        var rooms = result.ToList();
        await Assert.That(rooms[0].Id).IsEqualTo(newRoom.Id);
        await Assert.That(rooms[1].Id).IsEqualTo(oldRoom.Id);
    }

    [Test]
    public async Task GetRoomsByUserIdAsync_ReturnsOnlyUserRooms()
    {
        var user1 = await CreateUserAsync("user1");
        var user2 = await CreateUserAsync("user2");
        var user1Room = await CreateRoomAsync(user1.Id, "User 1 Room");
        await CreateRoomAsync(user2.Id, "User 2 Room");
        var service = Mocker.CreateInstance<RoomService>();

        var result = await service.GetRoomsByUserIdAsync(user1.Id);

        var rooms = result.ToList();
        await Assert.That(rooms.Count).IsEqualTo(1);
        await Assert.That(rooms[0].Id).IsEqualTo(user1Room.Id);
    }

    [Test]
    public async Task GetRoomByIdAsync_ReturnsRoom_WhenExists()
    {
        var user = await CreateUserAsync();
        var room = await CreateRoomAsync(user.Id);
        var service = Mocker.CreateInstance<RoomService>();

        var result = await service.GetRoomByIdAsync(room.Id);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(room.Id);
        await Assert.That(result.FriendlyName).IsEqualTo(room.FriendlyName);
        await Assert.That(result.CreatedBy).IsNotNull();
    }

    [Test]
    public async Task GetRoomByIdAsync_ReturnsNull_WhenNotExists()
    {
        var service = Mocker.CreateInstance<RoomService>();

        var result = await service.GetRoomByIdAsync(Guid.NewGuid());

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetRoomByFriendlyNameAsync_ReturnsRoom_WhenExists()
    {
        var user = await CreateUserAsync();
        await CreateRoomAsync(user.Id, "Test Room");
        var service = Mocker.CreateInstance<RoomService>();

        var result = await service.GetRoomByFriendlyNameAsync("Test Room");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.FriendlyName).IsEqualTo("Test Room");
    }

    [Test]
    public async Task GetRoomByFriendlyNameAsync_IsCaseInsensitive()
    {
        var user = await CreateUserAsync();
        await CreateRoomAsync(user.Id, "Test Room");
        var service = Mocker.CreateInstance<RoomService>();

        var result = await service.GetRoomByFriendlyNameAsync("test room");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.FriendlyName).IsEqualTo("Test Room");
    }

    [Test]
    public async Task GetRoomByFriendlyNameAsync_ReturnsNull_WhenNotExists()
    {
        var service = Mocker.CreateInstance<RoomService>();

        var result = await service.GetRoomByFriendlyNameAsync("Nonexistent Room");

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task CreateRoomAsync_CreatesNewRoom()
    {
        var user = await CreateUserAsync();
        var service = Mocker.CreateInstance<RoomService>();

        var result = await service.CreateRoomAsync("New Room", user.Id, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.FriendlyName).IsEqualTo("New Room");
        await Assert.That(result.CreatedByUserId).IsEqualTo(user.Id);

        await Mocker.InDbScopeAsync(async context => 
        {
            var createdRoom = await context.Rooms.SingleAsync(x => x.Id == result.Id);
            await Assert.That(createdRoom).IsNotNull();
            await Assert.That(createdRoom!.FriendlyName).IsEqualTo("New Room");
            await Assert.That(createdRoom.CreatedByUserId).IsEqualTo(user.Id);
        });
    }

    [Test]
    public async Task CreateRoomAsync_ThrowsException_WhenRoomNameExists()
    {
        var user = await CreateUserAsync();
        await CreateRoomAsync(user.Id, "Existing Room");
        var service = Mocker.CreateInstance<RoomService>();

        await Assert.That(async () => await service.CreateRoomAsync(
            "Existing Room",
            user.Id,
            CancellationToken.None))
            .Throws<InvalidOperationException>()
            .WithMessage("A room with the name 'Existing Room' already exists.");
    }

    [Test]
    public async Task CreateRoomAsync_ThrowsException_WhenRoomNameExistsCaseInsensitive()
    {
        var user = await CreateUserAsync();
        await CreateRoomAsync(user.Id, "Existing Room");
        var service = Mocker.CreateInstance<RoomService>();

        await Assert.That(async () => await service.CreateRoomAsync(
            "existing room",
            user.Id,
            CancellationToken.None))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task SetCurrentQuestionAsync_SetsCurrentQuestion()
    {
        var user = await CreateUserAsync();
        var room = await CreateRoomAsync(user.Id);
        var question = await CreateQuestionAsync(room.Id, isApproved: true);
        var service = Mocker.CreateInstance<RoomService>();

        await service.SetCurrentQuestionAsync(room.Id, question.Id, user.Id, CancellationToken.None);

        await Mocker.InDbScopeAsync(async context =>
        {
            var updatedRoom = await context.Rooms.SingleAsync(x => x.Id == room.Id);
            await Assert.That(updatedRoom!.CurrentQuestionId).IsEqualTo(question.Id);
        });
    }

    [Test]
    public async Task SetCurrentQuestionAsync_ClearsCurrentQuestion_WhenQuestionIdIsNull()
    {
        var user = await CreateUserAsync();
        var room = await CreateRoomAsync(user.Id);
        var question = await CreateQuestionAsync(room.Id, isApproved: true);
        
        await Mocker.InDbScopeAsync(async context =>
        {
            var dbRoom = await context.Rooms.SingleAsync(x => x.Id == room.Id);
            dbRoom.CurrentQuestionId = question.Id;
            await context.SaveChangesAsync();
        });

        var service = Mocker.CreateInstance<RoomService>();

        await service.SetCurrentQuestionAsync(room.Id, null, user.Id, CancellationToken.None);

        await Mocker.InDbScopeAsync(async context =>
        {
            var updatedRoom = await context.Rooms.SingleAsync(x => x.Id == room.Id);
            await Assert.That(updatedRoom!.CurrentQuestionId).IsNull();
        });
    }

    [Test]
    public async Task SetCurrentQuestionAsync_ThrowsException_WhenRoomNotFound()
    {
        var service = Mocker.CreateInstance<RoomService>();

        await Assert.That(async () => await service.SetCurrentQuestionAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user",
            CancellationToken.None))
            .Throws<InvalidOperationException>()
            .WithMessage("Room not found");
    }

    [Test]
    public async Task SetCurrentQuestionAsync_ThrowsException_WhenUserNotRoomOwner()
    {
        var owner = await CreateUserAsync("owner");
        var otherUser = await CreateUserAsync("other-user");
        var room = await CreateRoomAsync(owner.Id);
        var question = await CreateQuestionAsync(room.Id, isApproved: true);
        var service = Mocker.CreateInstance<RoomService>();

        await Assert.That(async () => await service.SetCurrentQuestionAsync(
            room.Id,
            question.Id,
            otherUser.Id,
            CancellationToken.None))
            .Throws<UnauthorizedAccessException>()
            .WithMessage("Only the room owner can set the current question");
    }

    [Test]
    public async Task SetCurrentQuestionAsync_ThrowsException_WhenQuestionNotFound()
    {
        var user = await CreateUserAsync();
        var room = await CreateRoomAsync(user.Id);
        var service = Mocker.CreateInstance<RoomService>();

        await Assert.That(async () => await service.SetCurrentQuestionAsync(
            room.Id,
            Guid.NewGuid(),
            user.Id,
            CancellationToken.None))
            .Throws<InvalidOperationException>()
            .WithMessage("Question not found or does not belong to this room");
    }

    [Test]
    public async Task SetCurrentQuestionAsync_ThrowsException_WhenQuestionBelongsToOtherRoom()
    {
        var user = await CreateUserAsync();
        var room1 = await CreateRoomAsync(user.Id, "Room 1");
        var room2 = await CreateRoomAsync(user.Id, "Room 2");
        var question = await CreateQuestionAsync(room2.Id, isApproved: true);
        var service = Mocker.CreateInstance<RoomService>();

        await Assert.That(async () => await service.SetCurrentQuestionAsync(
            room1.Id,
            question.Id,
            user.Id,
            CancellationToken.None))
            .Throws<InvalidOperationException>()
            .WithMessage("Question not found or does not belong to this room");
    }

    [Test]
    public async Task SetCurrentQuestionAsync_ThrowsException_WhenQuestionNotApproved()
    {
        var user = await CreateUserAsync();
        var room = await CreateRoomAsync(user.Id);
        var question = await CreateQuestionAsync(room.Id, isApproved: false);
        var service = Mocker.CreateInstance<RoomService>();

        await Assert.That(async () => await service.SetCurrentQuestionAsync(
            room.Id,
            question.Id,
            user.Id,
            CancellationToken.None))
            .Throws<InvalidOperationException>()
            .WithMessage("Only approved questions can be set as current");
    }

    [Test]
    public async Task DeleteRoomAsync_DeletesRoom()
    {
        var user = await CreateUserAsync();
        var room = await CreateRoomAsync(user.Id);
        var question = await CreateQuestionAsync(room.Id);
        var service = Mocker.CreateInstance<RoomService>();

        await service.DeleteRoomAsync(room.Id, user.Id, CancellationToken.None);

        await Mocker.InDbScopeAsync(async context =>
        {
            var deletedRoom = await context.Rooms.FirstOrDefaultAsync(x => x.Id == room.Id);
            await Assert.That(deletedRoom).IsNull();
            var deletedQuestion = await context.Questions.FirstOrDefaultAsync(x => x.Id == question.Id);
            await Assert.That(deletedQuestion).IsNull();
        });
    }

    [Test]
    public async Task DeleteRoomAsync_ThrowsException_WhenRoomNotFound()
    {
        var service = Mocker.CreateInstance<RoomService>();

        await Assert.That(async () => await service.DeleteRoomAsync(
            Guid.NewGuid(),
            "user",
            CancellationToken.None))
            .Throws<InvalidOperationException>()
            .WithMessage("Room not found");
    }

    [Test]
    public async Task DeleteRoomAsync_ThrowsException_WhenUserNotRoomOwner()
    {
        var owner = await CreateUserAsync("owner");
        var otherUser = await CreateUserAsync("other-user");
        var room = await CreateRoomAsync(owner.Id);
        var service = Mocker.CreateInstance<RoomService>();

        await Assert.That(async () => await service.DeleteRoomAsync(
            room.Id,
            otherUser.Id,
            CancellationToken.None))
            .Throws<UnauthorizedAccessException>()
            .WithMessage("Only the room owner can delete the room");
    }
}
