using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ReactApp.Data;

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

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            // SQLite does not have proper support for DateTimeOffset via Entity Framework Core, see the limitations
            // here: https://docs.microsoft.com/ef/core/providers/sqlite/limitations#query-limitations
            // To work around this, when the Sqlite database provider is used, all model properties of type DateTimeOffset
            // use the DateTimeOffsetToBinaryConverter
            // Based on: https://github.com/aspnet/EntityFrameworkCore/issues/10784#issuecomment-415769754
            // This only supports millisecond precision, but should be sufficient for most use cases.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType
                    .GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTimeOffset) || p.PropertyType == typeof(DateTimeOffset?));
                foreach (var property in properties)
                {
                    var builder = modelBuilder
                        .Entity(entityType.Name)
                        .Property(property.Name)
                        .HasConversion<DateTimeOffsetToBinaryConverter>();
                }
            }

            modelBuilder.Entity<IdentityPasskeyData>().HasNoKey();
        }
    }
}
