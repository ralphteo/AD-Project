using ADWebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Data;

public class EmpDbContext : DbContext
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Role> Roles => Set<Role>();

    public EmpDbContext(DbContextOptions<EmpDbContext> options) : base(options) { }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Employee>(e =>
    {
        e.HasKey(x => x.Username);

        e.HasOne(x => x.Role)
         .WithMany(r => r.Employees)
         .HasForeignKey(x => x.RoleId)
         .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<Role>()
        .HasIndex(r => r.Name)
        .IsUnique();
}
}