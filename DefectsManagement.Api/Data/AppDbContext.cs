using DefectsManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DefectsManagement.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // === Основные таблицы ===
    public DbSet<User> Users { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<Defect> Defects { get; set; }
    public DbSet<DefectTag> DefectTags { get; set; }

    // === Добавь это ===
    public DbSet<DefectComment> DefectComments { get; set; }
    public DbSet<DefectAttachment> DefectAttachments { get; set; }
    public DbSet<DefectHistory> DefectHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- User ---
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);

        // --- Project ---
        modelBuilder.Entity<Project>()
            .HasKey(p => p.Id);

        // Один проект имеет многих участников
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Members)
            .WithOne(m => m.Project)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- ProjectMember ---
        modelBuilder.Entity<ProjectMember>()
            .HasKey(pm => new { pm.ProjectId, pm.UserId });

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.User)
            .WithMany(u => u.ProjectMemberships)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Defect ---
        modelBuilder.Entity<Defect>()
            .HasKey(d => d.Id);

        modelBuilder.Entity<Defect>()
            .Property(d => d.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<Defect>()
            .HasOne(d => d.Project)
            .WithMany(p => p.Defects)
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Defect>()
            .HasOne(d => d.CreatedBy)
            .WithMany()
            .HasForeignKey(d => d.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Defect>()
            .HasOne(d => d.AssignedTo)
            .WithMany()
            .HasForeignKey(d => d.AssignedId)
            .OnDelete(DeleteBehavior.SetNull);

        // Типы/конвертеры для Defect
        modelBuilder.Entity<Defect>()
            .Property(d => d.DueDate)
            .HasColumnType("date"); // DateOnly → date (PostgreSQL)

        modelBuilder.Entity<Defect>()
            .Property(d => d.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Defect>()
            .Property(d => d.Priority)
            .HasConversion<string>();

        // Soft-delete (если в модели Defect есть bool IsDeleted)
        modelBuilder.Entity<Defect>()
            .HasQueryFilter(d => !d.IsDeleted);

        // --- DefectTag ---
        modelBuilder.Entity<DefectTag>()
            .HasKey(dt => new { dt.DefectId, dt.Tag });

        modelBuilder.Entity<DefectTag>()
            .HasOne(dt => dt.Defect)
            .WithMany(d => d.Tags)
            .HasForeignKey(dt => dt.DefectId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- DefectComment ---
        modelBuilder.Entity<DefectComment>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<DefectComment>()
            .HasOne(c => c.Defect)
            .WithMany(d => d.Comments)
            .HasForeignKey(c => c.DefectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DefectComment>()
            .HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DefectComment>()
            .HasIndex(c => new { c.DefectId, c.CreatedAt });

        // --- DefectAttachment ---
        modelBuilder.Entity<DefectAttachment>()
            .HasKey(a => a.Id);

        modelBuilder.Entity<DefectAttachment>()
            .HasOne(a => a.Defect)
            .WithMany(d => d.Attachments)
            .HasForeignKey(a => a.DefectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DefectAttachment>()
            .HasOne(a => a.UploadedBy)
            .WithMany()
            .HasForeignKey(a => a.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DefectAttachment>()
            .HasIndex(a => new { a.DefectId, a.UploadedAt });

        // --- DefectHistory ---
        modelBuilder.Entity<DefectHistory>()
            .HasKey(h => h.Id);

        modelBuilder.Entity<DefectHistory>()
            .HasOne(h => h.Defect)
            .WithMany(d => d.History)
            .HasForeignKey(h => h.DefectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DefectHistory>()
            .HasOne(h => h.Actor)
            .WithMany()
            .HasForeignKey(h => h.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        // jsonb для payload (если поле строка/JSON-тип)
        modelBuilder.Entity<DefectHistory>()
            .Property(h => h.Payload)
            .HasColumnType("jsonb");

        modelBuilder.Entity<DefectHistory>()
            .HasIndex(h => new { h.DefectId, h.OccurredAt });

        // --- Индексы на Defect ---
        modelBuilder.Entity<Defect>().HasIndex(d => d.ProjectId);
        modelBuilder.Entity<Defect>().HasIndex(d => d.Status);
        modelBuilder.Entity<Defect>().HasIndex(d => d.Priority);
        modelBuilder.Entity<Defect>().HasIndex(d => d.DueDate);
        modelBuilder.Entity<Defect>().HasIndex(d => new { d.ProjectId, d.Status, d.Priority });
    }
}
