using System.Runtime.CompilerServices;

using BlazorApp.Data;

namespace BlazorApp.Core.Hubs;

public class QuestionDto()
{
    internal QuestionDto(Question question) : this()
    {
        RoomId = question.RoomId;
        QuestionText = question.QuestionText;
        AuthorName = question.AuthorName;
        IsAnswered = question.IsAnswered;
        IsApproved = question.IsApproved;
        CreatedDate = question.CreatedDate;
        LastModifiedDate = question.LastModifiedDate;
    }

    public Guid RoomId { get; set; }
    public string QuestionText { get; set; } = "";
    public string? AuthorName { get; set; }
    public bool IsAnswered { get; set; }
    public bool IsApproved { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset? LastModifiedDate { get; set; }

    public static explicit operator QuestionDto?(Question? question) => question is null ? null : new(question);
}
