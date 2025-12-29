using Microsoft.AspNetCore.SignalR;

namespace BlazorApp.Core.Hubs;

public class RoomHub : Hub
{
    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room-{roomId}");
    }

    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room-{roomId}");
    }

    public async Task JoinRoomAsOwner(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room-{roomId}-owner");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room-{roomId}");
    }

    public async Task LeaveRoomAsOwner(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room-{roomId}-owner");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room-{roomId}");
    }
}

