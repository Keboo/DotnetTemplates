using System.Collections.Concurrent;

using ReactApp.Core.Hubs;
using ReactApp.Data;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ReactApp.Core.QA;

public class QuestionService(IDbContextFactory<ApplicationDbContext> contextFactory, IHubContext<RoomHub> hubContext) : IQuestionService
{
    //TODO: This does not horizontally scale
    private static readonly ConcurrentDictionary<string, DateTimeOffset> _lastSubmissionTimes = new();
    private static readonly TimeSpan _rateLimitWindow = TimeSpan.FromSeconds(10);

    public async Task<IEnumerable<Question>> GetQuestionsByRoomIdAsync(Guid roomId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Questions
            .Where(q => q.RoomId == roomId)
            .OrderBy(q => q.CreatedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Question>> GetApprovedQuestionsByRoomIdAsync(Guid roomId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Questions
            .Where(q => q.RoomId == roomId && q.IsApproved)
            .OrderBy(q => q.CreatedDate)
            .ToListAsync();
    }

    public async Task<Question?> GetQuestionByIdAsync(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Questions
            .Include(q => q.Room)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<Question> SubmitQuestionAsync(Guid roomId, string questionText, string? authorName, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        // Verify room exists
        var roomExists = await context.Rooms.AnyAsync(r => r.Id == roomId, cancellationToken);
        if (!roomExists)
        {
            throw new InvalidOperationException("Room not found");
        }

        var question = new Question
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            QuestionText = questionText,
            AuthorName = authorName,
            CreatedDate = DateTimeOffset.UtcNow
        };

        context.Questions.Add(question);
        await context.SaveChangesAsync(cancellationToken);

        await hubContext.SendQuestionSubmittedAsync(question, cancellationToken);
        return question;
    }

    public async Task<Question> UpdateQuestionAsync(Guid questionId, string questionText, string? authorName, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var question = await context.Questions
            .AsTracking()
            .FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken)
            ?? throw new InvalidOperationException("Question not found");

        // Only allow updates to unapproved questions
        if (question.IsApproved)
        {
            throw new InvalidOperationException("Cannot update an approved question");
        }

        question.QuestionText = questionText;
        question.AuthorName = authorName;
        question.LastModifiedDate = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return question;
    }

    public async Task ApproveQuestionAsync(Guid questionId, string userId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var question = await context.Questions
            .Include(x => x.Room)
            .AsTracking()
            .FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken)
            ?? throw new InvalidOperationException("Question not found");

        if (question.Room!.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("Only the room owner can approve questions");
        }

        question.IsApproved = true;
        await context.SaveChangesAsync(cancellationToken);
        
        await hubContext.SendQuestionApprovedAsync(question, cancellationToken);
    }

    public async Task MarkAsAnsweredAsync(Guid questionId, string userId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var question = await context.Questions
            .Include(x => x.Room)
            .AsTracking()
            .FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken)
            ?? throw new InvalidOperationException("Question not found");

        if (question.Room!.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("Only the room owner can mark questions as answered");
        }

        question.IsAnswered = true;
        await context.SaveChangesAsync(cancellationToken);
        
        await hubContext.SendQuestionAnsweredAsync(question, cancellationToken);
    }

    public async Task DeleteQuestionAsync(Guid questionId, string userId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var question = await context.Questions
            .Include(x => x.Room)
            .FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken)
            ?? throw new InvalidOperationException("Question not found");

        
        if (question.Room!.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("Only the room owner can delete questions");
        }

        var roomId = question.RoomId;
        context.Questions.Remove(question);
        await context.SaveChangesAsync(cancellationToken);

        await hubContext.SendQuestionDeletedAsync(roomId, questionId, cancellationToken);
    }

    public Task<bool> CanSubmitQuestionAsync(string clientId)
    {
        if (!_lastSubmissionTimes.TryGetValue(clientId, out var lastSubmission))
        {
            _lastSubmissionTimes[clientId] = DateTimeOffset.UtcNow;
            return Task.FromResult(true);
        }

        var timeSinceLastSubmission = DateTimeOffset.UtcNow - lastSubmission;
        
        if (timeSinceLastSubmission >= _rateLimitWindow)
        {
            _lastSubmissionTimes[clientId] = DateTimeOffset.UtcNow;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
