using ReactApp.Core.Hubs;
using ReactApp.Data;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ReactApp.Core.QA;

public class RoomService(IDbContextFactory<ApplicationDbContext> contextFactory, IHubContext<RoomHub> hubContext) : IRoomService
{
    public async Task<IEnumerable<Room>> GetAllRoomsAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Rooms
            .Include(r => r.CurrentQuestion)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Room>> GetRoomsByUserIdAsync(string userId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Rooms
            .Include(r => r.CurrentQuestion)
            .Where(r => r.CreatedByUserId == userId)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();
    }

    public async Task<Room?> GetRoomByIdAsync(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Rooms
            .Include(r => r.CurrentQuestion)
            .Include(r => r.CreatedBy)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Room?> GetRoomByFriendlyNameAsync(string friendlyName)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Rooms
            .Include(r => r.CurrentQuestion)
            .Include(r => r.CreatedBy)
            .FirstOrDefaultAsync(x => EF.Functions.Like(x.FriendlyName, friendlyName));
    }

    public async Task<Room> CreateRoomAsync(string friendlyName, string userId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        // Check if room with same name already exists (case-insensitive)
        var exists = await context.Rooms
            .AnyAsync(r => EF.Functions.Like(r.FriendlyName, friendlyName), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"A room with the name '{friendlyName}' already exists.");
        }

        var room = new Room
        {
            Id = Guid.NewGuid(),
            FriendlyName = friendlyName,
            CreatedByUserId = userId,
            CreatedDate = DateTimeOffset.UtcNow
        };

        context.Rooms.Add(room);
        await context.SaveChangesAsync(cancellationToken);
        
        // Broadcast AFTER database save completes
        await hubContext.Clients.All.SendAsync("RoomCreated", (RoomDto?)room, cancellationToken);
        return room;
    }

    public async Task SetCurrentQuestionAsync(Guid roomId, Guid? questionId, string userId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var room = await context.Rooms
            .AsTracking()
            .Include(r => r.CurrentQuestion)
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken)
            ?? throw new InvalidOperationException("Room not found");

        if (room.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("Only the room owner can set the current question");
        }

        // If questionId is provided, verify it belongs to this room and is approved
        if (questionId.HasValue)
        {
            var question = await context.Questions
                .FirstOrDefaultAsync(q => q.Id == questionId.Value && q.RoomId == roomId, cancellationToken)
                ?? throw new InvalidOperationException("Question not found or does not belong to this room");

            if (!question.IsApproved)
            {
                throw new InvalidOperationException("Only approved questions can be set as current");
            }
            room.CurrentQuestion = question;
        }
        else
        {
            room.CurrentQuestion = null;
        }

        room.CurrentQuestionId = questionId;
        await context.SaveChangesAsync(cancellationToken);

        await hubContext.SendCurrentQuestionChangedAsync(room.Id, room.CurrentQuestion, cancellationToken);
    }

    public async Task DeleteRoomAsync(Guid roomId, string userId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var room = await context.Rooms
            .Include(r => r.Questions)
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken)
            ?? throw new InvalidOperationException("Room not found");

        if (room.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("Only the room owner can delete the room");
        }

        // Manually delete all questions first (due to Restrict delete behavior)
        context.Questions.RemoveRange(room.Questions);
        
        context.Rooms.Remove(room);
        await context.SaveChangesAsync(cancellationToken);
        
        // Broadcast AFTER database save completes
        await hubContext.SendRoomDeletedAsync(roomId, cancellationToken);
    }
}
