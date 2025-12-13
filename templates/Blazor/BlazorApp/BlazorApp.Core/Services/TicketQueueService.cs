using BlazorApp.Core.Hubs;
using BlazorApp.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;

namespace BlazorApp.Core.Services;

public class TicketQueueService(IDbContextFactory<ApplicationDbContext> contextFactory, IHubContext<TicketQueueHub> hubContext) : ITicketQueueService
{
    private static readonly ResiliencePipeline RetryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<DbUpdateConcurrencyException>(),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(50),
            BackoffType = DelayBackoffType.Linear
        })
        .Build();

    private async Task<TResult> ExecuteQueueOperationAsync<TResult>(
        Guid queueId,
        Func<TicketQueue, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        return await RetryPipeline.ExecuteAsync(async ct =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct);

            var queue = await context.TicketQueues
                .FirstOrDefaultAsync(x => x.Id == queueId, ct) ?? throw new InvalidOperationException("Queue not found");

            var result = await operation(queue, ct);

            await context.SaveChangesAsync(ct);

            await hubContext.Clients.Group($"queue-{queueId}").SendAsync("QueueUpdated", queue, ct);
            await hubContext.Clients.Group("all-queues").SendAsync("QueueUpdated", queue, ct);

            return result;
        }, cancellationToken);
    }

    private async Task ExecuteQueueOperationAsync(
        Guid queueId,
        Func<TicketQueue, CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteQueueOperationAsync<object?>(queueId, async (queue, ct) =>
        {
            await operation(queue, ct);
            return null;
        }, cancellationToken);
    }

    public async Task<IEnumerable<TicketQueue>> GetAllQueuesAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.TicketQueues
            .OrderByDescending(q => q.CreatedDate)
            .ToListAsync();
    }

    public async Task<TicketQueue?> GetQueueByIdAsync(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.TicketQueues.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<TicketQueue> CreateQueueAsync(string friendlyName, string userId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

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
        return await ExecuteQueueOperationAsync(queueId, (queue, ct) =>
        {
            var ticketNumber = queue.NextNumber;
            queue.NextNumber++;
            return Task.FromResult(ticketNumber);
        });
    }

    public async Task HandleNextAsync(Guid queueId, string userId)
    {
        await ExecuteQueueOperationAsync(queueId, (queue, ct) =>
        {
            queue.CurrentNumber++;
            return Task.CompletedTask;
        });
    }

    public async Task ResetQueueAsync(Guid queueId, string userId)
    {
        await ExecuteQueueOperationAsync(queueId, (queue, ct) =>
        {
            if (queue.CreatedByUserId != userId)
            {
                throw new UnauthorizedAccessException("Only the creator can reset this queue");
            }

            queue.CurrentNumber = 0;
            queue.NextNumber = 1;
            return Task.CompletedTask;
        });
    }

    public async Task DeleteQueueAsync(Guid queueId, string userId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var queue = await context.TicketQueues.FirstOrDefaultAsync(x => x.Id == queueId)
            ?? throw new InvalidOperationException("Queue not found");

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
