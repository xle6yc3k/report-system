using DefectsManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DefectsManagement.Api.Data;

public class AppDbContext : DbContext
{
    // Конструктор для передачи опций контекста
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // DbSet представляет собой коллекцию сущностей в базе данных
    public DbSet<User> Users { get; set; }
    public DbSet<Defect> Defects { get; set; }
}