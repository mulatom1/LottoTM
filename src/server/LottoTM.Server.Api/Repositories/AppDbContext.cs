using LottoTM.Server.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LottoTM.Server.Api.Repositories;

/// <summary>
/// Entity Framework Core database context for LottoTM application.
/// Manages database connection and entity configurations for:
/// - Users (authentication and authorization)
/// - Tickets and TicketNumbers (user lottery tickets)
/// - Draws and DrawNumbers (lottery draw results)
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets for entity collections

    /// <summary>
    /// Users table - stores user accounts with authentication data
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Tickets table - stores metadata for user lottery tickets
    /// </summary>
    public DbSet<Ticket> Tickets { get; set; } = null!;

    /// <summary>
    /// TicketNumbers table - stores individual numbers for each ticket (normalized)
    /// </summary>
    public DbSet<TicketNumber> TicketNumbers { get; set; } = null!;

    /// <summary>
    /// Draws table - stores lottery draw metadata (global registry)
    /// </summary>
    public DbSet<Draw> Draws { get; set; } = null!;

    /// <summary>
    /// DrawNumbers table - stores individual drawn numbers for each draw (normalized)
    /// </summary>
    public DbSet<DrawNumber> DrawNumbers { get; set; } = null!;

    /// <summary>
    /// Configures entity mappings, relationships, constraints, and indexes.
    /// Called by EF Core when the model is being built.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureTicket(modelBuilder);
        ConfigureTicketNumber(modelBuilder);
        ConfigureDraw(modelBuilder);
        ConfigureDrawNumber(modelBuilder);
    }

    /// <summary>
    /// Configures the User entity mapping
    /// </summary>
    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "LottoTM");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.IsAdmin)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.HasMany(e => e.Tickets)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.CreatedDraws)
                .WithOne(e => e.CreatedByUser)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the Ticket entity mapping
    /// </summary>
    private static void ConfigureTicket(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets", "LottoTM");

            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.GroupName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.User)
                .WithMany(e => e.Tickets)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Numbers)
                .WithOne(e => e.Ticket)
                .HasForeignKey(e => e.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the TicketNumber entity mapping
    /// </summary>
    private static void ConfigureTicketNumber(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TicketNumber>(entity =>
        {
            entity.ToTable("TicketNumbers", "LottoTM", t =>
            {
                // Check constraint for Number range (1-49)
                t.HasCheckConstraint(
                    "CHK_TicketNumbers_Number",
                    "[Number] >= 1 AND [Number] <= 49");

                // Check constraint for Position range (1-6)
                t.HasCheckConstraint(
                    "CHK_TicketNumbers_Position",
                    "[Position] >= 1 AND [Position] <= 6");
            });

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.TicketId)
                .IsRequired();

            entity.Property(e => e.Number)
                .IsRequired();

            entity.Property(e => e.Position)
                .IsRequired();

            entity.HasIndex(e => new { e.TicketId, e.Position })
                .IsUnique();

            entity.HasIndex(e => e.TicketId);

            entity.HasIndex(e => e.Number);

            entity.HasOne(e => e.Ticket)
                .WithMany(e => e.Numbers)
                .HasForeignKey(e => e.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the Draw entity mapping
    /// </summary>
    private static void ConfigureDraw(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Draw>(entity =>
        {
            entity.ToTable("Draws", "LottoTM");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.DrawSystemId)
                .IsRequired()
                .HasColumnType("int");

            entity.Property(e => e.DrawDate)
                .IsRequired()
                .HasColumnType("date");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.CreatedByUserId)
                .IsRequired(false);

            entity.Property(e => e.LottoType)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.DrawDate);

            entity.HasIndex(e => e.DrawSystemId);

            entity.HasIndex(e => e.CreatedByUserId);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany(e => e.CreatedDraws)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Numbers)
                .WithOne(e => e.Draw)
                .HasForeignKey(e => e.DrawId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.TicketPrice)
               .IsRequired(false)
               .HasPrecision(18, 2)
               .HasColumnType("numeric(18, 2)");

            entity.Property(e => e.WinPoolCount1)
               .IsRequired(false)
               .HasColumnType("int");

            entity.Property(e => e.WinPoolAmount1)
               .IsRequired(false)
               .HasPrecision(18, 2)
               .HasColumnType("numeric(18, 2)");

            entity.Property(e => e.WinPoolCount2)
               .IsRequired(false)
               .HasColumnType("int");

            entity.Property(e => e.WinPoolAmount2)
               .IsRequired(false)
               .HasPrecision(18, 2)
               .HasColumnType("numeric(18, 2)");

            entity.Property(e => e.WinPoolCount3)
               .IsRequired(false)
               .HasColumnType("int");

            entity.Property(e => e.WinPoolAmount3)
               .IsRequired(false)
               .HasPrecision(18, 2)
               .HasColumnType("numeric(18, 2)");

            entity.Property(e => e.WinPoolCount4)
               .IsRequired(false)
               .HasColumnType("int");

            entity.Property(e => e.WinPoolAmount4)
               .IsRequired(false)
               .HasPrecision(18, 2)
               .HasColumnType("numeric(18, 2)");
        });
    }

    /// <summary>
    /// Configures the DrawNumber entity mapping
    /// </summary>
    private static void ConfigureDrawNumber(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DrawNumber>(entity =>
        {
            entity.ToTable("DrawNumbers", "LottoTM", t =>
            {
                // Check constraint for Number range (1-49)
                t.HasCheckConstraint(
                    "CHK_DrawNumbers_Number",
                    "[Number] >= 1 AND [Number] <= 49");

                // Check constraint for Position range (1-6)
                t.HasCheckConstraint(
                    "CHK_DrawNumbers_Position",
                    "[Position] >= 1 AND [Position] <= 6");
            });

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.DrawId)
                .IsRequired();

            entity.Property(e => e.Number)
                .IsRequired();

            entity.Property(e => e.Position)
                .IsRequired();

            entity.HasIndex(e => new { e.DrawId, e.Position })
                .IsUnique();

            entity.HasIndex(e => e.DrawId);

            entity.HasIndex(e => e.Number);

            entity.HasOne(e => e.Draw)
                .WithMany(e => e.Numbers)
                .HasForeignKey(e => e.DrawId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
