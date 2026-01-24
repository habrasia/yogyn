using Microsoft.EntityFrameworkCore;
using Yogyn.Api.Models;

namespace Yogyn.Api.Data;

public class YogynDbContext : DbContext
{
    public YogynDbContext(DbContextOptions<YogynDbContext> options) : base(options)
    {
    }

    public DbSet<Studio> Studios { get; set; } = null!;
    public DbSet<StudioUser> StudioUsers { get; set; } = null!;
    public DbSet<Session> Sessions { get; set; } = null!;
    public DbSet<Booking> Bookings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Studio configuration
        modelBuilder.Entity<Studio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Timezone).HasMaxLength(50).IsRequired();
        });

        // StudioUser configuration
        modelBuilder.Entity<StudioUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            
            entity.HasOne(e => e.Studio)
                  .WithMany(s => s.StudioUsers)
                  .HasForeignKey(e => e.StudioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Session configuration
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StudioId, e.StartsAt });
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            
            entity.HasOne(e => e.Studio)
                  .WithMany(s => s.Sessions)
                  .HasForeignKey(e => e.StudioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Booking configuration
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => new { e.StudioId, e.SessionId, e.Email })
                  .IsUnique()
                  .HasDatabaseName("IX_Booking_Unique");
            
            entity.HasIndex(e => e.CancelToken);
            
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20);
            
            entity.HasOne(e => e.Studio)
                  .WithMany(s => s.Bookings)
                  .HasForeignKey(e => e.StudioId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Session)
                  .WithMany(s => s.Bookings)
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}