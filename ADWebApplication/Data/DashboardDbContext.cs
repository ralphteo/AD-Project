using ADWebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Data
{
    public class DashboardDbContext : DbContext
    {
        public DashboardDbContext(DbContextOptions<DashboardDbContext> options) : base(options) { }

        public DbSet<PublicUser> Users => Set<PublicUser>();
        public DbSet<DisposalLogs> DisposalLogs => Set<DisposalLogs>();
        public DbSet<DisposalLogItem> DisposalLogItems => Set<DisposalLogItem>();
        public DbSet<EWasteItemType> EWasteItemTypes => Set<EWasteItemType>();
        public DbSet<EWasteCategory> EWasteCategories => Set<EWasteCategory>();
        public DbSet<CollectionBin> CollectionBins => Set<CollectionBin>();
        public DbSet<Region> Regions => Set<Region>();
        public DbSet<FillLevelPrediction> FillLevelPredictions => Set<FillLevelPrediction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PublicUser>()
                .ToTable("publicuser");

            modelBuilder.Entity<EWasteItemType>()
                .HasOne(t => t.Category)
                .WithMany(c => c.EWasteItemTypes)
                .HasForeignKey(t => t.CategoryId);

            modelBuilder.Entity<DisposalLogItem>()
                .HasOne(i => i.ItemType)
                .WithMany(t => t.DisposalLogItems)
                .HasForeignKey(i => i.ItemTypeId);

            modelBuilder.Entity<DisposalLogItem>()
                .HasOne(i => i.DisposalLog)
                .WithOne(l => l.DisposalLogItem)
                .HasForeignKey<DisposalLogItem>(i => i.LogId);

            modelBuilder.Entity<CollectionBin>()
                .HasOne(b => b.Region)
                .WithMany()
                .HasForeignKey(b => b.RegionId);
        }
    }
}
