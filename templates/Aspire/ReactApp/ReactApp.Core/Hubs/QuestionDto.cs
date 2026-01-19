using System.Diagnostics.CodeAnalysis;

using ReactApp.Data;

namespace ReactApp.Core.Hubs;

public class QuestionDto()
{
    internal QuestionDto(Question question) : this()
    {
        Id = question.Id;
        RoomId = question.RoomId;
        QuestionText = question.QuestionText;
        AuthorName = question.AuthorName;
        IsAnswered = question.IsAnswered;
        IsApproved = question.IsApproved;
        CreatedDate = question.CreatedDate;
        LastModifiedDate = question.LastModifiedDate;
    }

    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string QuestionText { get; set; } = "";
    public string? AuthorName { get; set; }
    public bool IsAnswered { get; set; }
    public bool IsApproved { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset? LastModifiedDate { get; set; }

    [return:NotNullIfNotNull(nameof(question))]
    public static implicit operator QuestionDto?(Question? question) => question is null ? null : new(question);
}
