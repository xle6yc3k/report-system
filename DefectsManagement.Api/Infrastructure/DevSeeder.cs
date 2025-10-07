// Infrastructure/DevSeeder.cs
using DefectsManagement.Api.Data;
using DefectsManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DefectsManagement.Api.Infrastructure;

public static class DevSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // если уже есть хотя бы один пользователь и проект — выходим
        var hasUsers = await db.Users.AnyAsync();
        var hasProjects = await db.Projects.AnyAsync();
        if (hasUsers && hasProjects) return;

        // --- пользователи (upsert-подход) ---
        var manager = await db.Users.FirstOrDefaultAsync(u => u.Username == "manager")
                      ?? new User
                      {
                          Id = Guid.NewGuid(),
                          Name = "Alice Manager",
                          Username = "manager",
                          PasswordHash = BCrypt.Net.BCrypt.HashPassword("manager"),
                          Role = "Manager"
                      };

        var engineer = await db.Users.FirstOrDefaultAsync(u => u.Username == "engineer")
                       ?? new User
                       {
                           Id = Guid.NewGuid(),
                           Name = "Evan Engineer",
                           Username = "engineer",
                           PasswordHash = BCrypt.Net.BCrypt.HashPassword("engineer"),
                           Role = "Engineer"
                       };

        if (db.Entry(manager).State == EntityState.Detached) db.Users.Add(manager);
        if (db.Entry(engineer).State == EntityState.Detached) db.Users.Add(engineer);

        // --- проект (с уникальным Key) ---
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Key == "PRJ")
                      ?? new Project
                      {
                          Id = Guid.NewGuid(),
                          Key = "PRJ",
                          Name = "Demo Project"
                      };

        if (db.Entry(project).State == EntityState.Detached) db.Projects.Add(project);

        await db.SaveChangesAsync();

        // --- роли в проекте (не дублируем) ---
        if (!await db.ProjectMembers.AnyAsync(pm => pm.ProjectId == project.Id && pm.UserId == manager.Id))
        {
            db.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = manager.Id,
                Role = ProjectRole.Manager
            });
        }

        if (!await db.ProjectMembers.AnyAsync(pm => pm.ProjectId == project.Id && pm.UserId == engineer.Id))
        {
            db.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = engineer.Id,
                Role = ProjectRole.Engineer
            });
        }

        await db.SaveChangesAsync();
    }
}
