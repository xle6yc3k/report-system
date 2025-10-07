using DefectsManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DefectsManagement.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // Таблицы
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<Defect> Defects => Set<Defect>();
    public DbSet<DefectTag> DefectTags => Set<DefectTag>();
    public DbSet<DefectComment> DefectComments => Set<DefectComment>();
    public DbSet<DefectAttachment> DefectAttachments => Set<DefectAttachment>();
    public DbSet<DefectHistory> DefectHistories => Set<DefectHistory>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);

        // ===== User =====
        model.Entity<User>()
            .HasKey(u => u.Id);

        // навигация пользователя к участию в проектах (если есть)
        model.Entity<User>()
            .HasMany(u => u.ProjectMemberships)
            .WithOne(pm => pm.User)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== Project =====
        model.Entity<Project>()
            .HasKey(p => p.Id);

        model.Entity<Project>()
            .HasMany(p => p.Members)
            .WithOne(pm => pm.Project)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        model.Entity<Project>()
            .HasMany(p => p.Defects)
            .WithOne(d => d.Project)
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== ProjectMember =====
        model.Entity<ProjectMember>()
            .HasKey(pm => new { pm.ProjectId, pm.UserId });

        model.Entity<ProjectMember>()
            .Property(pm => pm.Role)
            .HasConversion<string>();

        // уникальность пары (ProjectId, UserId)
        model.Entity<ProjectMember>()
            .HasIndex(pm => new { pm.ProjectId, pm.UserId })
            .IsUnique();

        // ===== Defect =====
        model.Entity<Defect>()
            .HasKey(d => d.Id);

        model.Entity<Defect>()
            .Property(d => d.RowVersion)
            .IsRowVersion();

        model.Entity<Defect>()
            .Property(d => d.Status)
            .HasConversion<string>();

        model.Entity<Defect>()
            .Property(d => d.Priority)
            .HasConversion<string>();

        // DateOnly -> date (PostgreSQL)
        model.Entity<Defect>()
            .Property(d => d.DueDate)
            .HasColumnType("date");

        // связи автора/исполнителя
        model.Entity<Defect>()
            .HasOne(d => d.CreatedBy)
            .WithMany()
            .HasForeignKey(d => d.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<Defect>()
            .HasOne(d => d.AssignedTo)
            .WithMany()
            .HasForeignKey(d => d.AssignedId)
            .OnDelete(DeleteBehavior.SetNull);

        // soft-delete на дефектах
        model.Entity<Defect>()
            .HasQueryFilter(d => !d.IsDeleted);

        // индексы для фильтров
        model.Entity<Defect>().HasIndex(d => d.ProjectId);
        model.Entity<Defect>().HasIndex(d => d.Status);
        model.Entity<Defect>().HasIndex(d => d.Priority);
        model.Entity<Defect>().HasIndex(d => d.DueDate);
        model.Entity<Defect>().HasIndex(d => new { d.ProjectId, d.Status, d.Priority });

        // ===== DefectTag (join-таблица тегов) =====
        model.Entity<DefectTag>()
            .HasKey(dt => new { dt.DefectId, dt.Tag });

        model.Entity<DefectTag>()
            .HasOne(dt => dt.Defect)
            .WithMany(d => d.Tags)
            .HasForeignKey(dt => dt.DefectId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== DefectComment =====
        model.Entity<DefectComment>()
            .HasKey(c => c.Id);

        model.Entity<DefectComment>()
            .HasOne(c => c.Defect)
            .WithMany(d => d.Comments)
            .HasForeignKey(c => c.DefectId)
            .OnDelete(DeleteBehavior.Cascade);

        model.Entity<DefectComment>()
            .HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<DefectComment>()
            .HasIndex(c => new { c.DefectId, c.CreatedAt });

        // ===== DefectAttachment =====
        model.Entity<DefectAttachment>()
            .HasKey(a => a.Id);

        model.Entity<DefectAttachment>()
            .HasOne(a => a.Defect)
            .WithMany(d => d.Attachments)
            .HasForeignKey(a => a.DefectId)
            .OnDelete(DeleteBehavior.Cascade);

        model.Entity<DefectAttachment>()
            .HasOne(a => a.UploadedBy)
            .WithMany()
            .HasForeignKey(a => a.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<DefectAttachment>()
            .HasIndex(a => new { a.DefectId, a.UploadedAt });

        model.Entity<DefectAttachment>().Property(a => a.FileName).HasMaxLength(255);
        model.Entity<DefectAttachment>().Property(a => a.ContentType).HasMaxLength(128);
        model.Entity<DefectAttachment>().Property(a => a.StorageKey).HasMaxLength(512);

        // ===== DefectHistory =====
        model.Entity<DefectHistory>()
            .HasKey(h => h.Id);

        model.Entity<DefectHistory>()
            .HasOne(h => h.Defect)
            .WithMany(d => d.History)
            .HasForeignKey(h => h.DefectId)
            .OnDelete(DeleteBehavior.Cascade);

        model.Entity<DefectHistory>()
            .HasOne(h => h.Actor)
            .WithMany()
            .HasForeignKey(h => h.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        // jsonb для payload (если свойство string/JsonDocument — тип в БД jsonb)
        model.Entity<DefectHistory>()
            .Property(h => h.Payload)
            .HasColumnType("jsonb");

        model.Entity<DefectHistory>()
            .HasIndex(h => new { h.DefectId, h.OccurredAt });

        // ===== Matching query filters для дочерних, чтобы убрать варнинги 10622 =====
        model.Entity<DefectComment>().HasQueryFilter(x => !x.Defect.IsDeleted);
        model.Entity<DefectAttachment>().HasQueryFilter(x => !x.Defect.IsDeleted);
        model.Entity<DefectHistory>().HasQueryFilter(x => !x.Defect.IsDeleted);
        model.Entity<DefectTag>().HasQueryFilter(x => !x.Defect.IsDeleted);
    }
}
