using BlazorApp.Data;

namespace BlazorApp.Core.Ticketing;

public interface ITicketQueueService
{
    Task<IEnumerable<TicketQueue>> GetAllQueuesAsync();
    
    Task<TicketQueue?> GetQueueByIdAsync(Guid id);
    
    Task<TicketQueue> CreateQueueAsync(string friendlyName, string userId, CancellationToken cancellationToken);
    
    Task<int> TakeTicketAsync(Guid queueId);
    
    Task HandleNextAsync(Guid queueId, string userId);
    
    Task ResetQueueAsync(Guid queueId, string userId);
    
    Task DeleteQueueAsync(Guid queueId, string userId, CancellationToken cancellationToken);
}
