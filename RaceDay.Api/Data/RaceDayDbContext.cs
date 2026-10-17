using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// Code-First context for the RaceDay database. All the column types, keys and
/// constraints are configured here in one place so the generated schema can be
/// compared against docs/raceday_schema.sql from Part 1 side by side.
/// </summary>
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Enrolment> Enrolments => Set<Enrolment>();

    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(role =>
        {
            role.ToTable("Roles");
            role.HasKey(r => r.RoleId);
            role.Property(r => r.RoleId).HasColumnName("RoleID");
            role.Property(r => r.RoleName).HasMaxLength(20).IsUnicode(false).IsRequired();
            role.HasIndex(r => r.RoleName).IsUnique();

            // The two roles are fixed by the brief, so they ship with the
            // migration rather than being inserted by hand after deployment.
            role.HasData(
                new Role { RoleId = 1, RoleName = RoleNames.Organiser },
                new Role { RoleId = 2, RoleName = RoleNames.Participant });
        });

        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("Users");
            user.HasKey(u => u.UserId);
            user.Property(u => u.UserId).HasColumnName("UserID");
            user.Property(u => u.RoleId).HasColumnName("RoleID");
            user.Property(u => u.FullName).HasMaxLength(100).IsUnicode(false).IsRequired();
            user.Property(u => u.Email).HasMaxLength(150).IsUnicode(false).IsRequired();
            user.Property(u => u.PasswordHash).HasMaxLength(255).IsUnicode(false).IsRequired();
            user.Property(u => u.ProfilePictureUrl).HasMaxLength(255).IsUnicode(false);
            user.Property(u => u.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            user.HasIndex(u => u.Email).IsUnique();

            user.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Event>(raceEvent =>
        {
            raceEvent.ToTable("Events", table => table.HasCheckConstraint(
                "CK_Events_EventType", "EventType IN ('Run', 'Walk', 'Cycle')"));
            raceEvent.HasKey(e => e.EventId);
            raceEvent.Property(e => e.EventId).HasColumnName("EventID");
            raceEvent.Property(e => e.OrganiserId).HasColumnName("OrganiserID");
            raceEvent.Property(e => e.EventName).HasMaxLength(150).IsUnicode(false).IsRequired();
            raceEvent.Property(e => e.Description).HasMaxLength(1000).IsUnicode(false);
            raceEvent.Property(e => e.EventDate).HasColumnType("datetime");
            raceEvent.Property(e => e.Location).HasMaxLength(200).IsUnicode(false).IsRequired();
            raceEvent.Property(e => e.DistanceKm).HasColumnType("decimal(6, 2)");
            raceEvent.Property(e => e.EventType).HasMaxLength(20).IsUnicode(false).IsRequired();
            raceEvent.Property(e => e.BannerImageUrl).HasMaxLength(255).IsUnicode(false);
            raceEvent.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");

            raceEvent.HasOne(e => e.Organiser)
                .WithMany(u => u.OrganisedEvents)
                .HasForeignKey(e => e.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(category =>
        {
            category.ToTable("Categories");
            category.HasKey(c => c.CategoryId);
            category.Property(c => c.CategoryId).HasColumnName("CategoryID");
            category.Property(c => c.EventId).HasColumnName("EventID");
            category.Property(c => c.CategoryName).HasMaxLength(100).IsUnicode(false).IsRequired();
            category.Property(c => c.DistanceKm).HasColumnType("decimal(6, 2)");

            category.HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Enrolment>(enrolment =>
        {
            enrolment.ToTable("Enrolments", table => table.HasCheckConstraint(
                "CK_Enrolments_Status", "Status IN ('Confirmed', 'Pending', 'Cancelled')"));
            enrolment.HasKey(e => e.EnrolmentId);
            enrolment.Property(e => e.EnrolmentId).HasColumnName("EnrolmentID");
            enrolment.Property(e => e.ParticipantId).HasColumnName("ParticipantID");
            enrolment.Property(e => e.EventId).HasColumnName("EventID");
            enrolment.Property(e => e.CategoryId).HasColumnName("CategoryID");
            enrolment.Property(e => e.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            enrolment.Property(e => e.Status).HasMaxLength(20).IsUnicode(false)
                .HasDefaultValue(EnrolmentStatuses.Confirmed).IsRequired();

            enrolment.HasIndex(e => new { e.ParticipantId, e.EventId })
                .IsUnique()
                .HasDatabaseName("UQ_Enrolments_Participant_Event");

            enrolment.HasOne(e => e.Participant)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(e => e.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);

            enrolment.HasOne(e => e.Event)
                .WithMany(ev => ev.Enrolments)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            enrolment.HasOne(e => e.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(result =>
        {
            result.ToTable("Results");
            result.HasKey(r => r.ResultId);
            result.Property(r => r.ResultId).HasColumnName("ResultID");
            result.Property(r => r.EnrolmentId).HasColumnName("EnrolmentID");
            result.Property(r => r.FinishTime).HasColumnType("time");
            result.Property(r => r.CapturedAt).HasColumnType("datetime");
            result.HasIndex(r => r.EnrolmentId).IsUnique();

            result.HasOne(r => r.Enrolment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
