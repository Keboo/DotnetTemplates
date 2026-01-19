using System.ComponentModel.DataAnnotations;

namespace ReactApp.Data;

public class Question
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public required Guid RoomId { get; set; }

    public Room? Room { get; set; }

    [Required]
    [MaxLength(2000)]
    public required string QuestionText { get; set; }

    [MaxLength(100)]
    public string? AuthorName { get; set; }

    public bool IsAnswered { get; set; } = false;

    public bool IsApproved { get; set; } = false;

    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastModifiedDate { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}