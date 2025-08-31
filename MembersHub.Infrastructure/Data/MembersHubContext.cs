using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using MembersHub.Core.Entities;

namespace MembersHub.Infrastructure.Data;

public class MembersHubContext : DbContext
{
    public MembersHubContext(DbContextOptions<MembersHubContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Member> Members { get; set; } = null!;
    public DbSet<MembershipType> MembershipTypes { get; set; } = null!;
    public DbSet<Subscription> Subscriptions { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<Expense> Expenses { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>();
        });

        // Member configuration
        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20).IsRequired();
            entity.Property(e => e.MemberNumber).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.MemberNumber).IsUnique();
            entity.Property(e => e.Status).HasConversion<string>();
            
            entity.HasOne(e => e.MembershipType)
                .WithMany(mt => mt.Members)
                .HasForeignKey(e => e.MembershipTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MembershipType configuration
        modelBuilder.Entity<MembershipType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MonthlyFee).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // Subscription configuration
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Notes).HasMaxLength(500);
            
            entity.HasOne(e => e.Member)
                .WithMany(m => m.Subscriptions)
                .HasForeignKey(e => e.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => new { e.MemberId, e.Year, e.Month }).IsUnique();
        });

        // Payment configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.PaymentMethod).HasConversion<string>();
            entity.Property(e => e.ReceiptNumber).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.ReceiptNumber).IsUnique();
            entity.Property(e => e.Notes).HasMaxLength(500);
            
            entity.HasOne(e => e.Member)
                .WithMany(m => m.Payments)
                .HasForeignKey(e => e.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Subscription)
                .WithMany(s => s.Payments)
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.Collector)
                .WithMany(u => u.Payments)
                .HasForeignKey(e => e.CollectorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Expense configuration
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ReceiptImagePath).HasMaxLength(500);
            
            entity.HasOne(e => e.Collector)
                .WithMany(u => u.Expenses)
                .HasForeignKey(e => e.CollectorId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Approver)
                .WithMany()
                .HasForeignKey(e => e.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Seed initial data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed membership types
        modelBuilder.Entity<MembershipType>().HasData(
            new MembershipType { Id = 1, Name = "Ενήλικες", MonthlyFee = 30, Description = "Μέλη άνω των 18 ετών", IsActive = true },
            new MembershipType { Id = 2, Name = "Παιδιά", MonthlyFee = 20, Description = "Μέλη κάτω των 18 ετών", IsActive = true },
            new MembershipType { Id = 3, Name = "Φοιτητές", MonthlyFee = 15, Description = "Φοιτητές με φοιτητική ταυτότητα", IsActive = true }
        );

        // Seed admin user (password: Admin123!)
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@membershub.gr",
                PasswordHash = "AQAAAAIAAYagAAAAEJvhJL5Yk1kqD1FzqC5YwR0N2o5nVfO7qJqZ+3YxD3X1qH7HqGrBqJqZ+3YxD3X1qA==", // This is a placeholder - will be properly hashed
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = new System.DateTime(2024, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
            }
        );
    }
}