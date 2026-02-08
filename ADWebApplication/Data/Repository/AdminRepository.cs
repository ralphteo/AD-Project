using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
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
        Task<CollectionBin?> GetBinByIdAsync(int binId);
        Task UpdateBinAsync(CollectionBin bin);
        Task CreateBinAsync(CollectionBin bin);
        Task<List<CollectionOfficerDto>> GetAvailableCollectionOfficersCalendarAsync(DateTime from, DateTime to);
        Task<List<AssignedCollectionOfficerDto>> GetAssignedCollectionOfficersCalendarAsync(DateTime from, DateTime to);
    }


   public class AdminRepository(In5niteDbContext infDb) : IAdminRepository

    {

        private readonly In5niteDbContext _infDb = infDb;


        // View all collection bins and their current status
        public async Task<List<CollectionBin>> GetAllBinsAsync()
        {
            return await _infDb.CollectionBins
                .AsNoTracking()
                .Include(b => b.Region)
                .OrderBy(b => b.RegionId)
                .ThenBy(b => b.BinId)
                .ToListAsync();
        }

        /// View all Collection Officers (username starts with CO-)
        public async Task<List<Employee>> GetAllCollectionOfficersAsync()
        {
            return await _infDb.Employees
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
            return await _infDb.Regions
                .AsNoTracking()
                .OrderBy(r => r.RegionName)
                .ToListAsync();
        }

        // Delete a collection bin by ID
        public async Task DeleteBinAsync(int binId)
        {
            var bin = await _infDb.CollectionBins.FindAsync(binId);
            if (bin != null)
            {
                _infDb.CollectionBins.Remove(bin);
                await _infDb.SaveChangesAsync();
            }
        }

        // Get all Employees
        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            return await _infDb.Employees
                .AsNoTracking()
                .OrderBy(e => e.Username)
                .ToListAsync();
        }

        // Get all Route Assignments and Route Plans for Collection Officers
        public async Task<List<RouteAssignment>> GetAllRouteAssignmentsForCollectionOfficersAsync()
        {
            // Fetch RouteAssignments for collection officers (username starts with "CO-")
            var routeAssignments = await _infDb.RouteAssignments
                .Where(ra => ra.AssignedTo.StartsWith("CO-"))
                .OrderBy(ra => ra.AssignedTo)
                .Include(ra => ra.RoutePlans)
                .ToListAsync();

            // After fetching RouteAssignments, manually load the Employee based on the AssignedTo username
            foreach (var routeAssignment in routeAssignments)
            {
                var employee = await _infDb.Employees
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
                ra.AssignedToEmployee = await _infDb.Employees
                    .FirstOrDefaultAsync(e => e.Username == ra.AssignedTo);
                ra.AssignedByEmployee = await _infDb.Employees
                    .FirstOrDefaultAsync(a => a.Username == ra.AssignedBy);
            }

            return routeAssignments;
        }

        public async Task<Employee> GetEmployeeByUsernameAsync(String username)
        {
            var employee = await _infDb.Employees
                    .FirstAsync(e => e.Username == username);
            return employee;
        }

        public async Task<List<Employee>> GetAvailableCollectionOfficersAsync(DateTime from, DateTime to)
        {
            // Get usernames that are BUSY in this range
            var busyUsernames = await _infDb.RoutePlans
                .Where(rp => rp.PlannedDate.HasValue && rp.PlannedDate.Value >= from && rp.PlannedDate.Value <= to)
                .Select(rp => rp.RouteAssignment!.AssignedTo)
                .Distinct()
                .ToListAsync();

            // Return officers that are NOT IN the BUSY list
            return await _infDb.Employees
                .AsNoTracking()
                .Include(e => e.Role)
                .Where(e =>
                    e.Username.StartsWith("CO-") &&
                    e.IsActive &&
                    !busyUsernames.Contains(e.Username))
                .OrderBy(e => e.Username)
                .ToListAsync();
        }

        public async Task<CollectionBin?> GetBinByIdAsync(int binId)
        {
            return await _infDb.CollectionBins.FindAsync(binId);
        }

        public async Task UpdateBinAsync(CollectionBin bin)
        {
            _infDb.CollectionBins.Update(bin);
            await _infDb.SaveChangesAsync();
        }

        public async Task CreateBinAsync(CollectionBin bin)
        {
            _infDb.CollectionBins.Add(bin);
            await _infDb.SaveChangesAsync();
        }

       public async Task<List<CollectionOfficerDto>> GetAvailableCollectionOfficersCalendarAsync(DateTime from, DateTime to)
        {
            // Normalize date range to DATE ONLY
            var fromDate = from.Date;
            var toDate   = to.Date;

            var busyDebug = await _infDb.RoutePlans
                .Where(rp =>
                    rp.PlannedDate.HasValue &&
                    rp.RouteAssignment != null &&
                    rp.PlannedDate.Value.Date >= fromDate &&
                    rp.PlannedDate.Value.Date <= toDate)
                .Select(rp => new
                {
                    Username = rp.RouteAssignment!.AssignedTo,
                    PlannedDate = rp.PlannedDate.Value
                })
                .ToListAsync();

            // Print results
            foreach (var item in busyDebug)
            {
                Console.WriteLine(
                    $"BUSY → User: '{item.Username}', PlannedDate: {item.PlannedDate:yyyy-MM-dd HH:mm:ss}"
                );
            }

            // Then extract usernames if needed
            var busyUsernames = busyDebug
                .Select(x => x.Username.Trim().ToUpper())
                .Distinct()
                .ToList();

            // Get AVAILABLE officers (not in busy list)
            return await _infDb.Employees
                .AsNoTracking()
                .Include(e => e.Role)
                .Where(e =>
                    e.IsActive &&
                    e.Username.StartsWith("CO-") &&
                    !busyUsernames.Contains(e.Username.Trim().ToUpper()))
                .OrderBy(e => e.Username)
                .Select(e => new CollectionOfficerDto
                {
                    Username = e.Username,
                    FullName = e.FullName
                })
                .ToListAsync();
        }

    public async Task<List<AssignedCollectionOfficerDto>> GetAssignedCollectionOfficersCalendarAsync(DateTime from, DateTime to)
    {
        var fromDate = from.Date;
        var toDate = to.Date;

        // Get all busy routes in range
        var busyDebug = await _infDb.RoutePlans
        .Where(rp =>
            rp.PlannedDate.HasValue &&
            rp.RouteAssignment != null &&
            rp.PlannedDate.Value.Date >= fromDate &&
            rp.PlannedDate.Value.Date <= toDate)
        .Select(rp => new
        {
            Username = rp.RouteAssignment!.AssignedTo.Trim().ToUpper(),
            PlannedDate = rp.PlannedDate.Value.Date,
            RouteId = rp.RouteId
        })
        .ToListAsync();

        // Group busy routes by username as CollectionOfficerPlannedRouteDto
        var grouped = busyDebug
        .GroupBy(x => x.Username)
        .ToDictionary(
            g => g.Key,
            g => g.Select(x => new CollectionOfficerPlannedRouteDto
            {
                RouteId = x.RouteId,
                PlannedDate = x.PlannedDate
            }).ToList()
        );

        // Fetch all employees that are in grouped keys
        var usernames = grouped.Keys.ToList();

        var employees = await _infDb.Employees
            .AsNoTracking()
            .Where(e => e.IsActive && e.Username.StartsWith("CO-") 
                        && usernames.Contains(e.Username.Trim().ToUpper()))
            .OrderBy(e => e.Username)
            .ToListAsync();

        //  Map to AssignedCollectionOfficerDto
        var result = employees.Select(e => new AssignedCollectionOfficerDto
        {
            Username = e.Username,
            FullName = e.FullName,
            PlannedDates = grouped[e.Username.Trim().ToUpper()]
        }).ToList();

        return result;
    }
    
    }
}
