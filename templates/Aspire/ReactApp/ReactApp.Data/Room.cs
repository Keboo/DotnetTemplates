using System.ComponentModel.DataAnnotations;

namespace ReactApp.Data;

public class Room
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string FriendlyName { get; set; }

    [Required]
    public required string CreatedByUserId { get; set; }

    public ApplicationUser? CreatedBy { get; set; }

    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

    public Guid? CurrentQuestionId { get; set; }

    public Question? CurrentQuestion { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
