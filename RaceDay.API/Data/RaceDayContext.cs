using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public class RaceDayContext : DbContext
{
    public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("User");
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.FirstName).HasMaxLength(50);
            e.Property(u => u.LastName).HasMaxLength(50);
            e.Property(u => u.Email).HasMaxLength(100);
            e.Property(u => u.PasswordHash).HasMaxLength(255);
            e.Property(u => u.Role).HasMaxLength(20);
            e.ToTable(t => t.HasCheckConstraint("CK_User_Role", "Role IN ('Organiser', 'Participant')"));
        });

        b.Entity<Event>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Location).HasMaxLength(150);
            e.Property(x => x.DistanceKm).HasPrecision(6, 2);
            e.Property(x => x.EventType).HasMaxLength(10);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Event_Distance", "DistanceKm > 0");
                t.HasCheckConstraint("CK_Event_Capacity", "Capacity > 0");
                t.HasCheckConstraint("CK_Event_Type", "EventType IN ('Run', 'Walk', 'Cycle')");
            });
            e.HasOne(x => x.Organiser).WithMany(u => u.Events)
                .HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Category>(e =>
        {
            e.HasIndex(c => c.Name).IsUnique();
            e.Property(c => c.Name).HasMaxLength(100);
            e.Property(c => c.Description).HasMaxLength(300);
        });

        b.Entity<EventCategory>(e =>
        {
            e.HasKey(x => new { x.EventId, x.CategoryId });
            e.HasOne(x => x.Event).WithMany(ev => ev.EventCategories)
                .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.EventCategories)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Enrolment>(e =>
        {
            e.HasIndex(x => new { x.EventId, x.UserId }).IsUnique();
            e.Property(x => x.Status).HasMaxLength(20);
            e.ToTable(t => t.HasCheckConstraint("CK_Enrolment_Status",
                "Status IN ('Pending', 'Confirmed', 'Cancelled', 'Completed')"));
            e.HasOne(x => x.Event).WithMany(ev => ev.Enrolments)
                .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User).WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            // Composite FK: a participant can only pick a category linked to that event
            e.HasOne(x => x.EventCategory).WithMany()
                .HasForeignKey(x => new { x.EventId, x.CategoryId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Result>(e =>
        {
            e.HasIndex(x => x.EnrolmentId).IsUnique();
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result)
                .HasForeignKey<Result>(x => x.EnrolmentId);
            e.ToTable(t => t.HasCheckConstraint("CK_Result_Position", "Position IS NULL OR Position > 0"));
        });
    }
}