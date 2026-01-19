using ReactApp.Data;

namespace ReactApp.Core.QA;

public interface IQuestionService
{
    Task<IEnumerable<Question>> GetQuestionsByRoomIdAsync(Guid roomId);
    
    Task<IEnumerable<Question>> GetApprovedQuestionsByRoomIdAsync(Guid roomId);
    
    Task<Question?> GetQuestionByIdAsync(Guid id);
    
    Task<Question> SubmitQuestionAsync(Guid roomId, string questionText, string? authorName, CancellationToken cancellationToken);
    
    Task<Question> UpdateQuestionAsync(Guid questionId, string questionText, string? authorName, CancellationToken cancellationToken);
    
    Task ApproveQuestionAsync(Guid questionId, string userId, CancellationToken cancellationToken);
    
    Task MarkAsAnsweredAsync(Guid questionId, string userId, CancellationToken cancellationToken);
    
    Task DeleteQuestionAsync(Guid questionId, string userId, CancellationToken cancellationToken);
    
    Task<bool> CanSubmitQuestionAsync(string clientId);
}
