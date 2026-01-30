using Microsoft.EntityFrameworkCore;
using ADWebApplication.Models;
using ADWebApplication.Models.LogDisposal;

namespace ADWebApplication.Data
{
    public class LogDisposalDbContext : DbContext
    {
        public LogDisposalDbContext(DbContextOptions<LogDisposalDbContext> options)
            : base(options) { }

        public DbSet<EWasteCategory> EWasteCategories => Set<EWasteCategory>();
        public DbSet<EWasteItemType> EWasteItemTypes => Set<EWasteItemType>();
        public DbSet<CollectionBin> CollectionBins => Set<CollectionBin>();
        public DbSet<DisposalLogs> DisposalLogs => Set<DisposalLogs>();
        public DbSet<DisposalLogItem> DisposalLogItems => Set<DisposalLogItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
        modelBuilder.Entity<DisposalLogs>()
            .HasOne(l => l.DisposalLogItem)
            .WithOne(i => i.DisposalLog)
            .HasForeignKey<DisposalLogItem>(i => i.LogId);

        modelBuilder.Entity<DisposalLogItem>()
            .HasOne(i => i.ItemType)
            .WithMany(t => t.DisposalLogItems)
            .HasForeignKey(i => i.ItemTypeId);

        modelBuilder.Entity<DisposalLogs>()
            .HasOne(l => l.CollectionBin)
            .WithMany()
            .HasForeignKey(l => l.BinId);
        }
    }
}