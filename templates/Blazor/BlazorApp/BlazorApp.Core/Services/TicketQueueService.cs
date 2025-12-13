using BlazorApp.Core.Hubs;
using BlazorApp.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp.Core.Services;

public class TicketQueueService(ApplicationDbContext context, IHubContext<TicketQueueHub> hubContext) : ITicketQueueService
{
    public async Task<IEnumerable<TicketQueue>> GetAllQueuesAsync()
    {
        return await context.TicketQueues
            .OrderByDescending(q => q.CreatedDate)
            .ToListAsync();
    }

    public async Task<TicketQueue?> GetQueueByIdAsync(Guid id)
    {
        return await context.TicketQueues.FindAsync(id);
    }

    public async Task<TicketQueue> CreateQueueAsync(string friendlyName, string userId)
    {
        var queue = new TicketQueue
        {
            Id = Guid.NewGuid(),
            FriendlyName = friendlyName,
            CurrentNumber = 0,
            NextNumber = 1,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };

        context.TicketQueues.Add(queue);
        await context.SaveChangesAsync();

        await hubContext.Clients.Group("all-queues").SendAsync("QueueCreated", queue);

        return queue;
    }

    public async Task<int> TakeTicketAsync(Guid queueId)
    {
        var queue = await context.TicketQueues.FindAsync(queueId);
        
        if (queue == null)
        {
            throw new InvalidOperationException("Queue not found");
        }

        var ticketNumber = queue.NextNumber;
        queue.NextNumber++;
        
        await context.SaveChangesAsync();

        await hubContext.Clients.Group($"queue-{queueId}").SendAsync("QueueUpdated", queue);
        await hubContext.Clients.Group("all-queues").SendAsync("QueueUpdated", queue);

        return ticketNumber;
    }

    public async Task HandleNextAsync(Guid queueId, string userId)
    {
        var queue = await context.TicketQueues.FindAsync(queueId) ?? throw new InvalidOperationException("Queue not found");

        // Allow any authenticated user to handle next
        queue.CurrentNumber++;
        
        await context.SaveChangesAsync();

        await hubContext.Clients.Group($"queue-{queueId}").SendAsync("QueueUpdated", queue);
        await hubContext.Clients.Group("all-queues").SendAsync("QueueUpdated", queue);
    }

    public async Task ResetQueueAsync(Guid queueId, string userId)
    {
        var queue = await context.TicketQueues.FindAsync(queueId);
        
        if (queue == null)
        {
            throw new InvalidOperationException("Queue not found");
        }

        if (queue.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("Only the creator can reset this queue");
        }

        queue.CurrentNumber = 0;
        queue.NextNumber = 1;
        
        await context.SaveChangesAsync();

        await hubContext.Clients.Group($"queue-{queueId}").SendAsync("QueueUpdated", queue);
        await hubContext.Clients.Group("all-queues").SendAsync("QueueUpdated", queue);
    }

    public async Task DeleteQueueAsync(Guid queueId, string userId)
    {
        var queue = await context.TicketQueues.FindAsync(queueId);
        
        if (queue == null)
        {
            throw new InvalidOperationException("Queue not found");
        }

        if (queue.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("Only the creator can delete this queue");
        }

        context.TicketQueues.Remove(queue);
        await context.SaveChangesAsync();

        await hubContext.Clients.Group($"queue-{queueId}").SendAsync("QueueDeleted", queueId);
        await hubContext.Clients.Group("all-queues").SendAsync("QueueDeleted", queueId);
    }
}
