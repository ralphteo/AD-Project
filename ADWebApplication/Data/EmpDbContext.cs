using System.ComponentModel.DataAnnotations;
using ADWebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Data
{
    public class EmpDbContext : DbContext
    {
      public DbSet<EmpAccount> EmpAccounts => Set<EmpAccount>();
      public DbSet<Role> Roles => Set<Role>();
      public DbSet<EmpRole> EmpRoles => Set<EmpRole>();

        public EmpDbContext(DbContextOptions<EmpDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            // unique username
            mb.Entity<EmpAccount>()
              .HasIndex(u => u.Username)
              .IsUnique();

            // unique role name
            mb.Entity<Role>()
              .HasIndex(r => r.Name)
              .IsUnique();

            // many-to-many via join table
            mb.Entity<EmpRole>()
              .HasKey(ur => new { ur.EmpAccountId, ur.RoleId });

            mb.Entity<EmpRole>()
              .HasOne(ur => ur.EmpAccount)
              .WithMany(u => u.EmpRoles)
              .HasForeignKey(ur => ur.EmpAccountId);

            mb.Entity<EmpRole>()
              .HasOne(ur => ur.Role)
              .WithMany()
              .HasForeignKey(ur => ur.RoleId);

            base.OnModelCreating(mb);
        }
    }
}