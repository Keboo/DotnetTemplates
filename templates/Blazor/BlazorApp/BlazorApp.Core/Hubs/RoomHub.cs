using BlazorApp.Core.QA;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BlazorApp.Core.Hubs;

public class RoomHub(IRoomService roomService, ILogger<RoomHub> logger) : Hub
{
    /// <summary>
    /// Allows any user (authenticated or not) to join a room as a regular participant.
    /// </summary>
    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room-{roomId}");
    }

    /// <summary>
    /// Allows any user to leave a room.
    /// </summary>
    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room-{roomId}");
    }

    /// <summary>
    /// Allows only authenticated users who own the room to join as owner.
    /// Verifies room ownership before adding to owner group.
    /// Requires JWT Bearer authentication via access token.
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task JoinRoomAsOwner(string roomId)
    {
        // Get the authenticated user's ID
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new HubException("User is not authenticated.");
        }

        // Validate room ID format
        if (!Guid.TryParse(roomId, out var roomGuid))
        {
            throw new HubException("Invalid room ID.");
        }

        // Verify the room exists
        var room = await roomService.GetRoomByIdAsync(roomGuid);
        if (room == null)
        {
            throw new HubException("Room not found.");
        }

        // Verify the user is the owner of the room
        if (room.CreatedByUserId != userId)
        {
            throw new HubException("You are not the owner of this room.");
        }

        // Add to both owner and regular room groups
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room-{roomId}-owner");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room-{roomId}");
    }

    /// <summary>
    /// Allows users to leave the owner group.
    /// </summary>
    public async Task LeaveRoomAsOwner(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room-{roomId}-owner");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room-{roomId}");
    }
}

