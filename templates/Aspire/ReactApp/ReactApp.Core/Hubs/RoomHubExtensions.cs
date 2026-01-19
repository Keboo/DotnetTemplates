using Microsoft.AspNetCore.SignalR;

using static ReactApp.Core.Hubs.RoomHub;

namespace ReactApp.Core.Hubs;

public static class RoomHubExtensions
{
    extension(IHubContext<RoomHub> hubContext)
    {
        public async Task SendQuestionAnsweredAsync(QuestionDto question, CancellationToken cancellationToken = default)
        {
            string group = GetRoomGroupName(question.RoomId);
            await hubContext.Clients.Group(group).SendAsync(QuestionAnsweredEvent, question, cancellationToken);
        }

        public async Task SendQuestionDeletedAsync(Guid roomId, Guid questionId, CancellationToken cancellationToken = default)
        {
            string group = GetRoomGroupName(roomId);
            await hubContext.Clients.Group(group).SendAsync(QuestionDeletedEvent, questionId, cancellationToken);
        }

        public async Task SendQuestionApprovedAsync(QuestionDto question, CancellationToken cancellationToken = default)
        {
            string group = GetRoomGroupName(question.RoomId);
            await hubContext.Clients.Group(group).SendAsync(QuestionApprovedEvent, question, cancellationToken);
        }

        public async Task SendQuestionSubmittedAsync(QuestionDto question, CancellationToken cancellationToken = default)
        {
            string group = GetOwnerRoomGroupName(question.RoomId);
            await hubContext.Clients.Group(group).SendAsync(QuestionSubmittedEvent, question, cancellationToken);
        }

        public async Task SendCurrentQuestionChangedAsync(Guid roomId, QuestionDto? question, CancellationToken cancellationToken = default)
        {
            string group = GetRoomGroupName(roomId);
            await hubContext.Clients.Group(group).SendAsync(CurrentQuestionChangedEvent, question, cancellationToken);
        }

        public async Task SendRoomDeletedAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            string group = GetRoomGroupName(roomId);
            await hubContext.Clients.Group(group).SendAsync(RoomDeletedEvent, roomId, cancellationToken);
        }
    }
}
