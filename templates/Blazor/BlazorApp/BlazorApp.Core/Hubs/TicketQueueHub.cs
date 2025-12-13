using Microsoft.AspNetCore.SignalR;

namespace BlazorApp.Core.Hubs;

public class TicketQueueHub : Hub
{
    public async Task JoinQueue(string queueId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"queue-{queueId}");
    }

    public async Task LeaveQueue(string queueId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"queue-{queueId}");
    }

    public async Task JoinAllQueues()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-queues");
    }

    public async Task LeaveAllQueues()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all-queues");
    }
}
