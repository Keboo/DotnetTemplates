using BlazorApp.Data;

namespace BlazorApp.Core.Services;

public interface ITicketQueueService
{
    Task<IEnumerable<TicketQueue>> GetAllQueuesAsync();
    
    Task<TicketQueue?> GetQueueByIdAsync(Guid id);
    
    Task<TicketQueue> CreateQueueAsync(string friendlyName, string userId);
    
    Task<int> TakeTicketAsync(Guid queueId);
    
    Task HandleNextAsync(Guid queueId, string userId);
    
    Task ResetQueueAsync(Guid queueId, string userId);
    
    Task DeleteQueueAsync(Guid queueId, string userId);
}
