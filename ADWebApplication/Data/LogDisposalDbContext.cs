using Microsoft.EntityFrameworkCore;
using ADWebApplication.Models;

namespace ADWebApplication.Data
{
    public class LogDisposalDbContext : DbContext
    {
        public LogDisposalDbContext(DbContextOptions<LogDisposalDbContext> options)
            : base(options) { }

        public DbSet<CollectionBin> CollectionBins => Set<CollectionBin>();
        public DbSet<FillLevelPrediction> FillLevelPredictions => Set<FillLevelPrediction>();
        public DbSet<EWasteCategory> EWasteCategories => Set<EWasteCategory>();
        public DbSet<EWasteItemType> EWasteItemTypes => Set<EWasteItemType>();
        public DbSet<DisposalLogs> DisposalLogs => Set<DisposalLogs>();
        public DbSet<DisposalLogItem> DisposalLogItems => Set<DisposalLogItem>();
        public DbSet<RewardWallet> RewardWallets => Set<RewardWallet>();
        public DbSet<RewardCatalogue> RewardCatalogues => Set<RewardCatalogue>();
        public DbSet<RewardRedemption> RewardRedemptions => Set<RewardRedemption>();
        public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DisposalLogs>()
                .HasOne(l => l.DisposalLogItem)
                .WithOne(i => i.DisposalLog)
                .HasForeignKey<DisposalLogItem>(i => i.LogId);

            modelBuilder.Entity<EWasteItemType>()
                .HasOne(t => t.Category)
                .WithMany(c => c.EWasteItemTypes)
                .HasForeignKey(t => t.CategoryId);

            modelBuilder.Entity<DisposalLogs>()
                .HasOne(l => l.CollectionBin)
                .WithMany()
                .HasForeignKey(l => l.BinId);

            modelBuilder.Entity<PointTransaction>()
                .HasOne<RewardWallet>()
                .WithMany()
                .HasForeignKey(p => p.WalletId);

            modelBuilder.Entity<PointTransaction>()
                .HasOne(p => p.DisposalLog)
                .WithMany()
                .HasForeignKey(p => p.LogId);
        }
    }
}
