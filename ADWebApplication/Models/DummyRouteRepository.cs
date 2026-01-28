using ADWebApplication.Models;

public static class DummyRouteRepository
{
    public static List<RoutePlan> GetRoutePlans()
    {
        var data = SeedData(); // Uses generated data
        return data.OfType<RoutePlan>().ToList(); // Return all RoutePlans
        // To return only RoutePlans realted to the Loggedin Collection Officer
    }

    public static List<RouteStop> GetStopsForRoute(int routePlanId)
    {
        var data = SeedData();
        return data
            .OfType<RouteStop>()
            .Where(rs => rs.RoutePlan?.RoutetId == routePlanId)
            .OrderBy(rs => rs.StopSequence)
            .ToList(); // Return ordered list of stops for the given RoutePlan (includes DI  Bin Location Name).
            // To return only RouteStop with no CollectionDeatils in DB.
    }

    private static List<object> SeedData()
    {
        // ⬅ move your existing CreateData() body here unchanged
        Employee collectionOfficer = new()
        {
            EmployeeId = 1,
            Role = "Collector",
            Name = "John Doe",
        };
        Employee administrator = new()
        {
            EmployeeId = 2,
            Role = "Administrator",
            Name = "Jane Smith",
        };
        
         Region regionOne = new()
        {
            RegionId = 1,
            RegionName = "North Zone",
        };
        CollectionBin binOne = new()
        {
            BinId = 1,
            Region = regionOne,
            LocationName = "Central Park",
            Address = "123 Main St",
            LocationType = "Public",
            BinCapacity = 100,
            BinStatus = "Active",
        };
        CollectionBin binTwo = new()
        {
            BinId = 2,
            Region = regionOne,
            LocationName = "City Mall",
            Address = "456 Market St",
            LocationType = "Commercial",
            BinCapacity = 200,
            BinStatus = "Active",
        };
        CollectionBin binThree = new()
        {
            BinId = 3,
            Region = regionOne,
            LocationName = "Library",
            Address = "789 Book St",
            LocationType = "Public",
            BinCapacity = 150,
            BinStatus = "Active",
        };
        CollectionBin binFour = new()
        {
            BinId = 4,
            Region = regionOne,
            LocationName = "Community Center",
            Address = "101 Center St",
            LocationType = "Public",
            BinCapacity = 120,
            BinStatus = "Active",
        };
        CollectionBin binFive = new()
        {
            BinId = 5,
            Region = regionOne,
            LocationName = "Sports Complex",
            Address = "202 Sports St",
            LocationType = "Recreational",
            BinCapacity = 180,
            BinStatus = "Active",
        };
        CollectionBin binSix = new()
        {
            BinId = 6,
            Region = regionOne,
            LocationName = "Downtown Plaza",
            Address = "303 Plaza St",
            LocationType = "Commercial",
            BinCapacity = 220,
            BinStatus = "Active",
        };

        RouteAssignment routeAssignmentOne = new()
        {
            AssignmentId = 1,
            Employee = administrator,
            AssignedTo = collectionOfficer.EmployeeId,
            AssignedDateTime = DateTimeOffset.Now.AddDays(-1),
        };

        RoutePlan routePlanOne = new()
        {
            RoutetId = 1,
            RouteAssignment = routeAssignmentOne,
            PlannedDateTime = DateTimeOffset.Now,
            RouteStatus = "Planned",
        };
        
        RouteStop routeStopOne = new()
        {
            StopId = 1,
            RoutePlan = routePlanOne,
            CollectionBin = binOne,
            StopSequence = 1,
            PlannedCollectionTime = DateTimeOffset.Now.AddHours(1),
        };
        RouteStop routeStopTwo = new()
        {
            StopId = 2,
            RoutePlan = routePlanOne,
            CollectionBin = binTwo,
            StopSequence = 2,
            PlannedCollectionTime = DateTimeOffset.Now.AddHours(2),
        };
        RouteStop routeStopThree = new()
        {
            StopId = 3,
            RoutePlan = routePlanOne,
            CollectionBin = binThree,
            StopSequence = 3,
            PlannedCollectionTime = DateTimeOffset.Now.AddHours(3),
        };
        RouteAssignment routeAssignmentTwo = new()
        {
            AssignmentId = 2,
            Employee = administrator,
            AssignedTo = collectionOfficer.EmployeeId,
            AssignedDateTime = DateTimeOffset.Now.AddDays(-2),
        };
        RoutePlan routePlanTwo = new()
        {
            RoutetId = 2,
            RouteAssignment = routeAssignmentTwo,
            PlannedDateTime = DateTimeOffset.Now,
            RouteStatus = "Planned",
        };
        RouteStop routeStopFour = new()
        {
            StopId = 1,
            RoutePlan = routePlanTwo,
            CollectionBin = binFour,
            StopSequence = 1,
            PlannedCollectionTime = DateTimeOffset.Now.AddHours(1),
        };
        RouteStop routeStopFive = new()
        {
            StopId = 2,
            RoutePlan = routePlanTwo,
            CollectionBin = binFive,
            StopSequence = 2,
            PlannedCollectionTime = DateTimeOffset.Now.AddHours(2),
        };
        RouteStop routeStopSix = new()
        {
            StopId = 3,
            RoutePlan = routePlanTwo,
            CollectionBin = binSix,
            StopSequence = 3,
            PlannedCollectionTime = DateTimeOffset.Now.AddHours(3),
        };
       
        var data = new List<object>
        {
            collectionOfficer,
            administrator,
            regionOne,
            binOne,
            binTwo,
            binThree,
            binFour,
            binFive,
            binSix,
            routeAssignmentOne,
            routePlanOne,
            routeStopOne,
            routeStopTwo,
            routeStopThree,
            routeAssignmentTwo,
            routePlanTwo,
            routeStopFour,
            routeStopFive,
            routeStopSix
        };

        return data;
    }
}