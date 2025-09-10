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
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
    public DbSet<PasswordResetRateLimit> PasswordResetRateLimits { get; set; } = null!;
    public DbSet<EmailSettings> EmailSettings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>();
            entity.Property(e => e.RefreshToken).HasMaxLength(500);
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

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Action).HasConversion<string>().IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EntityId).HasMaxLength(50);
            entity.Property(e => e.EntityName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.OldValues).HasColumnType("nvarchar(max)");
            entity.Property(e => e.NewValues).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.UserId, e.Timestamp });
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });

        // PasswordResetToken configuration
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(256).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TokenHash);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.UserId, e.IsUsed, e.ExpiresAt });
        });

        // PasswordResetRateLimit configuration
        modelBuilder.Entity<PasswordResetRateLimit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Identifier).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Type).HasConversion<string>();
            
            // Indexes for performance
            entity.HasIndex(e => new { e.Identifier, e.Type }).IsUnique();
            entity.HasIndex(e => e.WindowStartAt);
            entity.HasIndex(e => e.BlockedUntil);
        });

        // EmailSettings configuration
        modelBuilder.Entity<EmailSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SmtpHost).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Username).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PasswordEncrypted).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FromEmail).HasMaxLength(200).IsRequired();
            entity.Property(e => e.FromName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.Property(e => e.PasswordResetSubject).HasMaxLength(200);
            entity.Property(e => e.PasswordResetTemplate).HasColumnType("nvarchar(max)");
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

        // Seed users (password for all: Admin123!)
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "admin",
                FirstName = "Διαχειριστής",
                LastName = "Συστήματος",
                Email = "admin@membershub.gr",
                Phone = "6900000001",
                PasswordHash = "AQAAAAIAAYagAAAAEJvhJL5Yk1kqD1FzqC5YwR0N2o5nVfO7qJqZ+3YxD3X1qH7HqGrBqJqZ+3YxD3X1qA==", // This is a placeholder - will be properly hashed
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = 2,
                Username = "owner",
                FirstName = "Αρισοτέλης",
                LastName = "Καμαγάκης",
                Email = "owner@membershub.gr",
                Phone = "6900000002",
                PasswordHash = "AQAAAAIAAYagAAAAEJvhJL5Yk1kqD1FzqC5YwR0N2o5nVfO7qJqZ+3YxD3X1qH7HqGrBqJqZ+3YxD3X1qA==",
                Role = UserRole.Owner,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = 3,
                Username = "treasurer",
                FirstName = "Μαρία",
                LastName = "Οικονόμου",
                Email = "treasurer@membershub.gr",
                Phone = "6900000003",
                PasswordHash = "AQAAAAIAAYagAAAAEJvhJL5Yk1kqD1FzqC5YwR0N2o5nVfO7qJqZ+3YxD3X1qH7HqGrBqJqZ+3YxD3X1qA==",
                Role = UserRole.Treasurer,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed sample members
        modelBuilder.Entity<Member>().HasData(
            new Member
            {
                Id = 1,
                FirstName = "Γιάννης",
                LastName = "Παπαδόπουλος",
                Email = "giannis.papadopoulos@email.com",
                Phone = "6911111111",
                DateOfBirth = new DateTime(1985, 5, 15),
                MembershipTypeId = 1,
                MemberNumber = "A001",
                Status = MemberStatus.Active,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Member
            {
                Id = 2,
                FirstName = "Μαρία",
                LastName = "Καραγιάννη",
                Email = "maria.karagianni@email.com",
                Phone = "6922222222",
                DateOfBirth = new DateTime(1990, 8, 20),
                MembershipTypeId = 1,
                MemberNumber = "A002",
                Status = MemberStatus.Active,
                CreatedAt = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            },
            new Member
            {
                Id = 3,
                FirstName = "Νίκος",
                LastName = "Γεωργίου",
                Email = "nikos.georgiou@email.com",
                Phone = "6933333333",
                DateOfBirth = new DateTime(2005, 3, 10),
                MembershipTypeId = 2,
                MemberNumber = "K001",
                Status = MemberStatus.Active,
                CreatedAt = new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc)
            },
            new Member
            {
                Id = 4,
                FirstName = "Ελένη",
                LastName = "Δημητρίου",
                Email = "eleni.dimitriou@student.uoa.gr",
                Phone = "6944444444",
                DateOfBirth = new DateTime(2002, 11, 25),
                MembershipTypeId = 3,
                MemberNumber = "F001",
                Status = MemberStatus.Active,
                CreatedAt = new DateTime(2024, 1, 4, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 4, 0, 0, 0, DateTimeKind.Utc)
            },
            new Member
            {
                Id = 5,
                FirstName = "Κώστας",
                LastName = "Αντωνίου",
                Phone = "6955555555",
                DateOfBirth = new DateTime(1978, 12, 5),
                MembershipTypeId = 1,
                MemberNumber = "A003",
                Status = MemberStatus.Suspended,
                CreatedAt = new DateTime(2024, 1, 5, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 5, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed sample subscriptions
        modelBuilder.Entity<Subscription>().HasData(
            // January 2025 subscriptions
            new Subscription
            {
                Id = 1,
                MemberId = 1,
                Year = 2025,
                Month = 1,
                Amount = 30,
                DueDate = new DateTime(2025, 1, 31),
                Status = SubscriptionStatus.Paid,
                CreatedAt = new DateTime(2024, 12, 25, 0, 0, 0, DateTimeKind.Utc)
            },
            new Subscription
            {
                Id = 2,
                MemberId = 2,
                Year = 2025,
                Month = 1,
                Amount = 30,
                DueDate = new DateTime(2025, 1, 31),
                Status = SubscriptionStatus.Pending,
                CreatedAt = new DateTime(2024, 12, 25, 0, 0, 0, DateTimeKind.Utc)
            },
            new Subscription
            {
                Id = 3,
                MemberId = 3,
                Year = 2025,
                Month = 1,
                Amount = 20,
                DueDate = new DateTime(2025, 1, 31),
                Status = SubscriptionStatus.Pending,
                CreatedAt = new DateTime(2024, 12, 25, 0, 0, 0, DateTimeKind.Utc)
            },
            new Subscription
            {
                Id = 4,
                MemberId = 4,
                Year = 2025,
                Month = 1,
                Amount = 15,
                DueDate = new DateTime(2025, 1, 31),
                Status = SubscriptionStatus.Paid,
                CreatedAt = new DateTime(2024, 12, 25, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed sample payments
        modelBuilder.Entity<Payment>().HasData(
            new Payment
            {
                Id = 1,
                MemberId = 1,
                SubscriptionId = 1,
                Amount = 30,
                PaymentDate = new DateTime(2025, 1, 5, 14, 30, 0, DateTimeKind.Utc),
                CollectorId = 3,
                PaymentMethod = PaymentMethod.Cash,
                ReceiptNumber = "R2025001",
                Notes = "Πληρωμή Ιανουαρίου 2025",
                IsSynced = true,
                EmailSent = true,
                SmsSent = false,
                CreatedAt = new DateTime(2025, 1, 5, 14, 30, 0, DateTimeKind.Utc)
            },
            new Payment
            {
                Id = 2,
                MemberId = 4,
                SubscriptionId = 4,
                Amount = 15,
                PaymentDate = new DateTime(2025, 1, 8, 10, 15, 0, DateTimeKind.Utc),
                CollectorId = 3,
                PaymentMethod = PaymentMethod.Card,
                ReceiptNumber = "R2025002",
                Notes = "Φοιτητική συνδρομή",
                IsSynced = true,
                EmailSent = true,
                SmsSent = false,
                CreatedAt = new DateTime(2025, 1, 8, 10, 15, 0, DateTimeKind.Utc)
            }
        );
    }
}