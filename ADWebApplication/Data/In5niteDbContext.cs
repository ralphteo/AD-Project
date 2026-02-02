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
        public DbSet<RouteAssignment> RouteAssignments { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<RoutePlan> RoutePlans { get; set; }
        public DbSet<RouteStop> RouteStops { get; set; }

        public DbSet<CollectionBin> CollectionBins { get; set; }
        public DbSet<CollectionDetails> CollectionDetails { get; set; }
        public DbSet<FillLevelPrediction> FillLevelPredictions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map to singular table names to match database
            modelBuilder.Entity<RouteAssignment>().ToTable("routeassignment");
            modelBuilder.Entity<Region>().ToTable("region");
            modelBuilder.Entity<RoutePlan>().ToTable("routeplan");
            modelBuilder.Entity<RouteStop>().ToTable("routestop");
            modelBuilder.Entity<CollectionBin>().ToTable("collectionbin");
            modelBuilder.Entity<CollectionDetails>().ToTable("collectiondetails");
            modelBuilder.Entity<CollectionDetails>().ToTable("collectiondetails");
            modelBuilder.Entity<FillLevelPrediction>().ToTable("filllevelprediction");

            // Configure foreign key relationships to match database column names
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
