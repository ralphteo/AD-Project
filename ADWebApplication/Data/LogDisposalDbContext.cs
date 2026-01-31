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

        protected override void OnModelCreating(ModelBuilder modelBuilder){
    modelBuilder.Entity<DisposalLogs>()
        .HasOne(l => l.DisposalLogItem)
        .WithOne(i => i.DisposalLog)
        .HasForeignKey<DisposalLogItem>(i => i.LogId);

    modelBuilder.Entity<EWasteItemType>()
        .HasOne(t => t.Category)
        .WithMany()
        .HasForeignKey(t => t.CategoryId);

    modelBuilder.Entity<DisposalLogs>()
        .HasOne(l => l.CollectionBin)
        .WithMany()
        .HasForeignKey(l => l.BinId);

        }
    }
}
