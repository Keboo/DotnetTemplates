using ReactApp.Data;

namespace ReactApp.Core.QA;

public interface IRoomService
{
    Task<IEnumerable<Room>> GetAllRoomsAsync();
    
    Task<IEnumerable<Room>> GetRoomsByUserIdAsync(string userId);
    
    Task<Room?> GetRoomByIdAsync(Guid id);
    
    Task<Room?> GetRoomByFriendlyNameAsync(string friendlyName);
    
    Task<Room> CreateRoomAsync(string friendlyName, string userId, CancellationToken cancellationToken);
    
    Task SetCurrentQuestionAsync(Guid roomId, Guid? questionId, string userId, CancellationToken cancellationToken);
    
    Task DeleteRoomAsync(Guid roomId, string userId, CancellationToken cancellationToken);
}
