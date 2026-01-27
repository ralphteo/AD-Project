using Microsoft.EntityFrameworkCore;

namespace In5nite.Data
{
    public class In5niteDbContext : DbContext
    {
        public In5niteDbContext(DbContextOptions<In5niteDbContext> options)
            : base(options)
        {
        }

        // public DbSet<CollectionBin> CollectionBins { get; set; }
        // public DbSet<DisposalLog> DisposalLogs { get; set; }
    }
}