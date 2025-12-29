using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Question> Questions => Set<Question>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Room entity
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasIndex(e => e.FriendlyName)
                .IsUnique();

            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CurrentQuestion)
                .WithMany()
                .HasForeignKey(e => e.CurrentQuestionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Question entity
        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasOne(e => e.Room)
                .WithMany(r => r.Questions)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.RoomId);
            entity.HasIndex(e => new { e.RoomId, e.IsApproved, e.IsAnswered });
        });
    }
}
