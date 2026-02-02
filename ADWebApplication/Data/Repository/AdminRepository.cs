using ADWebApplication.Models;
using ADWebApplication.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Data.Repository
{
   public interface IAdminRepository
    {
        Task<List<CollectionBin>> GetAllBinsAsync();
        Task<List<Employee>> GetAllCollectionOfficersAsync();
        Task<List<Region>> GetAllRegionsAsync();
        Task DeleteBinAsync(int binId);
        Task<List<RouteAssignment>> GetAllRouteAssignmentsForCollectionOfficersAsync();
        Task<List<RouteAssignment>> GetRouteAssignmentsForOfficerAsync(string officerUsername, DateTime oneYearAgo);
        Task<Employee> GetEmployeeByUsernameAsync(string username);
        Task<List<Employee>> GetAvailableCollectionOfficersAsync(DateTime from, DateTime to);

    }
   public class AdminRepository(DashboardDbContext dashboardDb, EmpDbContext empDb, In5niteDbContext infDb) : IAdminRepository
    {
        private readonly DashboardDbContext _dashboardDb = dashboardDb;
        private readonly EmpDbContext _empDb = empDb;
        private readonly In5niteDbContext _infDb = infDb;


        // View all collection bins and their current status
        public async Task<List<CollectionBin>> GetAllBinsAsync()
        {
            return await _dashboardDb.CollectionBins
                .AsNoTracking()
                .Include(b => b.Region)
                .OrderBy(b => b.RegionId)
                .ThenBy(b => b.BinId)
                .ToListAsync();
        }

        /// View all Collection Officers (username starts with CO-)
        public async Task<List<Employee>> GetAllCollectionOfficersAsync()
        {
            return await _empDb.Employees
                .AsNoTracking()
                .Include(e => e.Role)
                .Where(e =>
                    e.Username.StartsWith("CO-") &&
                    e.IsActive)
                .OrderBy(e => e.Username)
                .ToListAsync();
        }

        // View all Regions
        public async Task<List<Region>> GetAllRegionsAsync()
        {
            return await _dashboardDb.Regions
                .AsNoTracking()
                .OrderBy(r => r.RegionName)
                .ToListAsync();
        }

        // Delete a collection bin by ID
        public async Task DeleteBinAsync(int binId)
        {
            var bin = await _dashboardDb.CollectionBins.FindAsync(binId);
            if (bin != null)
            {
                _dashboardDb.CollectionBins.Remove(bin);
                await _dashboardDb.SaveChangesAsync();
            }
        }

        // Get all Employees
        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            return await _empDb.Employees
                .AsNoTracking()
                .OrderBy(e => e.Username)
                .ToListAsync();
        }

        // Get all Route Assignments and Route Plans for Collection Officers
        public async Task<List<RouteAssignment>> GetAllRouteAssignmentsForCollectionOfficersAsync()
        {
            // Fetch RouteAssignments for collection officers (username starts with "CO-")
            var routeAssignments = await _infDb.RouteAssignments
                .Where(ra => ra.AssignedTo.StartsWith("CO-")) // Only collection officers (username starts with CO-)
                .OrderBy(ra => ra.AssignedTo) // Optional: Order by AssignedTo (username)
                .Include(ra => ra.RoutePlans) // Load associated RoutePlans
                .ToListAsync();

            // After fetching RouteAssignments, load the Employee based on the AssignedTo username
            foreach (var routeAssignment in routeAssignments)
            {
                var employee = await _empDb.Employees
                    .FirstOrDefaultAsync(e => e.Username == routeAssignment.AssignedTo); // Use AssignedTo as foreign key

                // Manually assign the Employee to the AssignedToEmployee navigation property
                routeAssignment.AssignedToEmployee = employee;
            }

            return routeAssignments;
        }

        // Method to get route assignments from up to one year ago, using collection officer id
        public async Task<List<RouteAssignment>> GetRouteAssignmentsForOfficerAsync(string officerUsername, DateTime oneYearAgo)
        {
            var routeAssignments = await _infDb.RouteAssignments
                .Where(ra => ra.AssignedTo == officerUsername && ra.AssignedDateTime >= oneYearAgo)
                .Include(ra => ra.RoutePlans)
                .OrderByDescending(ra => ra.AssignedDateTime)
                .ToListAsync();

            // Manually load Employee (cross-database)
            foreach (var ra in routeAssignments)
            {
                ra.AssignedToEmployee = await _empDb.Employees
                    .FirstOrDefaultAsync(e => e.Username == ra.AssignedTo);
                ra.AssignedByEmployee = await _empDb.Employees
                    .FirstOrDefaultAsync(a => a.Username == ra.AssignedBy);
            }

            return routeAssignments;
        }

        public async Task<Employee>GetEmployeeByUsernameAsync(String username)
        {
            var employee = await _empDb.Employees
                    .FirstAsync(e => e.Username == username);
            return employee;
        }

        public async Task<List<Employee>> GetAvailableCollectionOfficersAsync(DateTime from, DateTime to)
        {
            // Get usernames that are BUSY in this range
            var busyUsernames = await _infDb.RoutePlans
                .Where(rp => rp.PlannedDate >= from && rp.PlannedDate <= to)
                .Select(rp => rp.RouteAssignment!.AssignedTo)
                .Distinct()
                .ToListAsync();

            // Return officers that are NOT IN the BUSY list
            return await _empDb.Employees
                .AsNoTracking()
                .Include(e => e.Role)
                .Where(e =>
                    e.Username.StartsWith("CO-") &&
                    e.IsActive &&
                    !busyUsernames.Contains(e.Username))
                .OrderBy(e => e.Username)
                .ToListAsync();
        }


    }
}
