using BlazorApp.Core.QA;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BlazorApp.Core.Hubs;

public class RoomHub(IRoomService roomService) : Hub
{
    public const string QuestionAnsweredEvent = "QuestionAnswered";
    public const string QuestionDeletedEvent = "QuestionDeleted";
    public const string QuestionApprovedEvent = "QuestionApproved";
    public const string QuestionSubmittedEvent = "QuestionSubmitted";
    public const string CurrentQuestionChangedEvent = "CurrentQuestionChanged";
    public const string RoomDeletedEvent = "RoomDeleted";

    public static string GetRoomGroupName(Guid roomId) => GetRoomGroupName(roomId.ToString());
    public static string GetRoomGroupName(string roomId) => $"room-{roomId}";

    public static string GetOwnerRoomGroupName(Guid roomId) => GetOwnerRoomGroupName(roomId.ToString());
    public static string GetOwnerRoomGroupName(string roomId) => $"room-{roomId}-owner";

    /// <summary>
    /// Allows any user (authenticated or not) to join a room as a regular participant.
    /// </summary>
    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetRoomGroupName(roomId));
    }

    /// <summary>
    /// Allows any user to leave a room.
    /// </summary>
    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetRoomGroupName(roomId));
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
        await Groups.AddToGroupAsync(Context.ConnectionId, GetOwnerRoomGroupName(roomId));
        await Groups.AddToGroupAsync(Context.ConnectionId, GetRoomGroupName(roomId));
    }

    /// <summary>
    /// Allows users to leave the owner group.
    /// </summary>
    public async Task LeaveRoomAsOwner(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetOwnerRoomGroupName(roomId));
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetRoomGroupName(roomId));
    }
}

