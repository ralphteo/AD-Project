using Microsoft.EntityFrameworkCore;
using ADWebApplication.Models;

namespace ADWebApplication.Data
{
    public class In5niteDbContext : DbContext
    {
        public In5niteDbContext(DbContextOptions<In5niteDbContext> options)
            : base(options)
        {
        }

        public DbSet<PublicUser> PublicUser { get; set; }
        public DbSet<RewardWallet> RewardWallet { get; set; }
        public DbSet<Region> Regions { get; set; }

        // public DbSet<CollectionBin> CollectionBins { get; set; }
        // public DbSet<DisposalLog> DisposalLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //one user to one wallet
            modelBuilder.Entity<PublicUser>()
                .HasOne(u => u.RewardWallet)
                .WithOne(r => r.User)
                .HasForeignKey<RewardWallet>(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PublicUser>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}
