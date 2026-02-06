using Microsoft.EntityFrameworkCore;
using ADWebApplication.Models;

namespace ADWebApplication.Data
{
    public class In5niteDbContext : DbContext
    {
        public In5niteDbContext(DbContextOptions<In5niteDbContext> options)
            : base(options) { }

        // Core / Users
        public DbSet<PublicUser> PublicUser { get; set; }
        public DbSet<RewardWallet> RewardWallet { get; set; }

        // Employees & Roles
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Role> Roles { get; set; }

        // Routing & Regions
        public DbSet<RouteAssignment> RouteAssignments { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<RoutePlan> RoutePlans { get; set; }
        public DbSet<RouteStop> RouteStops { get; set; }

        // Collection & ML
        public DbSet<CollectionBin> CollectionBins { get; set; }
        public DbSet<CollectionDetails> CollectionDetails { get; set; }
        public DbSet<FillLevelPrediction> FillLevelPredictions { get; set; }

        // Disposal & Rewards
        public DbSet<EWasteCategory> EWasteCategories { get; set; }
        public DbSet<EWasteItemType> EWasteItemTypes { get; set; }
        public DbSet<DisposalLogs> DisposalLogs { get; set; }
        public DbSet<DisposalLogItem> DisposalLogItems { get; set; }
        public DbSet<RewardCatalogue> RewardCatalogues { get; set; }
        public DbSet<RewardRedemption> RewardRedemptions { get; set; }
        public DbSet<PointTransaction> PointTransactions { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table mapping (singular)
            modelBuilder.Entity<RouteAssignment>().ToTable("routeassignment");
            modelBuilder.Entity<Region>().ToTable("region");
            modelBuilder.Entity<RoutePlan>().ToTable("routeplan");
            modelBuilder.Entity<RouteStop>().ToTable("routestop");
            modelBuilder.Entity<CollectionBin>().ToTable("collectionbin");
            modelBuilder.Entity<CollectionDetails>().ToTable("collectiondetails");
            modelBuilder.Entity<FillLevelPrediction>().ToTable("filllevelprediction");
            modelBuilder.Entity<PublicUser>().ToTable("publicuser");
            modelBuilder.Entity<RewardCatalogue>().ToTable("rewardcatalogue");
            modelBuilder.Entity<Campaign>().ToTable("campaign");

            // Employee & Role
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

            // Routing
            modelBuilder.Entity<RouteStop>()
                .HasOne(rs => rs.CollectionBin)
                .WithMany()
                .HasForeignKey("BinId")
                .IsRequired(false);

            modelBuilder.Entity<RouteStop>()
                .HasOne(rs => rs.RoutePlan)
                .WithMany(rp => rp.RouteStops)
                .HasForeignKey("RouteId")
                .IsRequired(false);

            modelBuilder.Entity<RoutePlan>()
                .HasOne(rp => rp.RouteAssignment)
                .WithMany(ra => ra.RoutePlans)
                .HasForeignKey("AssignmentId")
                .IsRequired();

            // Collection
            modelBuilder.Entity<CollectionBin>()
                .HasOne(cb => cb.Region)
                .WithMany()
                .HasForeignKey("RegionId")
                .IsRequired(false);

            modelBuilder.Entity<CollectionDetails>()
                .HasOne(cd => cd.RouteStop)
                .WithMany(rs => rs.CollectionDetails)
                .HasForeignKey("StopId")
                .IsRequired(false);

            // Disposal
            modelBuilder.Entity<DisposalLogs>()
                .HasOne(l => l.DisposalLogItem)
                .WithOne(i => i.DisposalLog)
                .HasForeignKey<DisposalLogItem>(i => i.LogId);

            modelBuilder.Entity<DisposalLogs>()
                .HasOne(l => l.CollectionBin)
                .WithMany()
                .HasForeignKey(l => l.BinId);

            modelBuilder.Entity<EWasteItemType>()
                .HasOne(t => t.Category)
                .WithMany(c => c.EWasteItemTypes)
                .HasForeignKey(t => t.CategoryId);

            // Rewards & Points
            modelBuilder.Entity<PublicUser>()
                .HasOne(u => u.RewardWallet)
                .WithOne(r => r.User)
                .HasForeignKey<RewardWallet>(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PointTransaction>()
                .HasOne<RewardWallet>()
                .WithMany()
                .HasForeignKey(p => p.WalletId);

            modelBuilder.Entity<PointTransaction>()
                .HasOne(p => p.DisposalLog)
                .WithMany()
                .HasForeignKey(p => p.LogId);

            modelBuilder.Entity<Campaign>()
                .HasKey(c => c.CampaignId);
            
            modelBuilder.Entity<RewardCatalogue>()
                .HasKey(r => r.RewardId);
                
            // Constraints
            modelBuilder.Entity<PublicUser>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}