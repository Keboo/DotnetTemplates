using System.ComponentModel.DataAnnotations;

namespace BlazorApp.Data;

public class TicketQueue
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string FriendlyName { get; set; }

    public int CurrentNumber { get; set; }

    public int NextNumber { get; set; }

    [Required]
    public required string CreatedByUserId { get; set; }

    public ApplicationUser? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
