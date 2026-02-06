using Google.OrTools.ConstraintSolver;
using ADWebApplication.Data;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Services
{
    public class RoutePlanningService : IRoutePlanningService
    {
        private readonly In5niteDbContext _db;
        private readonly IBinPredictionService _binPredictionService;

        public RoutePlanningService(In5niteDbContext context, IBinPredictionService binPredictionService)
        {
            _db = context;
            _binPredictionService = binPredictionService;

        }

        public async Task<List<RoutePlanDto>> PlanRouteAsync()
        {

        //this is to build the distance matrix from the lot/lan from database.
        var bins = await _db.CollectionBins
        .Where (bin => bin.BinStatus == "Active")
        .Select(bin => new RoutePlanDto
        {
            BinId = bin.BinId,
            Latitude = bin.Latitude,
            Longitude = bin.Longitude
        })
        .ToListAsync();

        //create a fake depot at index 0
        var depot = new RoutePlanDto
        {
            BinId = 0, 
            Latitude = 1.3521,
            Longitude = 103.8198,
            IsHighPriority = false
        };

        //combine bins with fake depot
        var locations = new List<RoutePlanDto> {depot};
        locations.AddRange(bins);

        //flag high-priority bins here: this will refresh if BinPrediction also refreshes
        var priorities = await _binPredictionService.GetBinPrioritiesAsync();
        foreach (var loc in locations)
            {
                var priorityData = priorities.FirstOrDefault(p => p.BinId == loc.BinId);
                loc.IsHighPriority = priorityData != null && priorityData.DaysTo80 <= 1;
            }

        //filter High risk bins only
        var binsForPlanning = locations
            .Where(l => l.BinId == 0 || l.IsHighPriority)
            .ToList();

        if (binsForPlanning.Count <= 1)
            return new List<RoutePlanDto>();

        //this is the actual matrix.
        long[,] distanceMatrix = CreateDistanceMatrix(binsForPlanning);

        //create index manager (this is where OR-tools comes in)
        int numberOfLocations = binsForPlanning.Count;
        int numberOfCOs = 3;
        int startingNode = 0;

        RoutingIndexManager manager = new RoutingIndexManager(
            numberOfLocations,
            numberOfCOs,
            startingNode
        );

        RoutingModel routing = new RoutingModel(manager);

        //now, make distance callback: a lookup rule given to OR-tools to explore routes
        int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
        {
            var fromNode = manager.IndexToNode(fromIndex);
            var toNode = manager.IndexToNode(toIndex);
            return distanceMatrix[fromNode, toNode];
        });

        //tells OR-tools solver that this distance is to be minimized
        routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

        //create a distance dimension to track travel for each officer
        routing.AddDimension(
            transitCallbackIndex,
            0, //no slack; officers don't wait
            60000, //max distance per officer
            true, //start at 0
            "Distance"
        );

        RoutingDimension distanceDimension = routing.GetMutableDimension("Distance");


        //set high cost to force solver to minimize difference between longest route and shortest route
        distanceDimension.SetGlobalSpanCostCoefficient(100);

        //to constraint the number of bins per officer
        int countCallbackIndex = routing.RegisterUnaryTransitCallback((long fromIndex) =>
        {
            int nodeIndex = manager.IndexToNode(fromIndex);
            return nodeIndex == 0 ? 0 : 1;
        });

        //set hard limit per officer
        int maxBinsPerCO = (int)Math.Ceiling((double)(binsForPlanning.Count-1) / numberOfCOs) + 2;
        routing.AddDimension(
            countCallbackIndex,
            0,
            maxBinsPerCO,
            true, //start at 0
            "BinCount"
        );

        RoutingDimension binCountDimension = routing.GetMutableDimension("BinCount");

        //force the distribution evenness
        int averageBins = (binsForPlanning.Count - 1) / numberOfCOs;
        for (int i = 0; i < numberOfCOs; i++)
            {
                binCountDimension.SetCumulVarSoftLowerBound(routing.End(i), averageBins, 100000);
            }

        //implement hard constraints for high-priority bins
        //long mandatoryPenalty = 10000000;
        long optionalPenalty = 2000;
        
        //loop through all locations; node 0 is starting bin
        // for (int i = 1; i < locations.Count; i++)
        //     {
        //         long index = manager.NodeToIndex(i);
        //         if (locations[i].IsHighPriority)
        //         {
        //             routing.AddDisjunction(new long[] {index}, mandatoryPenalty);
        //         }
        //         else
        //         {
        //             routing.AddDisjunction(new long[] {index}, optionalPenalty);
        //         }
        //     }
        for (int i = 1; i < binsForPlanning.Count; i++)
            {
                long index = manager.NodeToIndex(i);

                if (!binsForPlanning[i].IsHighPriority)
                {
                    routing.AddDisjunction(new long[] { index }, optionalPenalty);
                }
            }

        //search parameters
        RoutingSearchParameters searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();

        //pick cheapest path 
        searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;

        //untangle 7 routes
        searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;

        //give 2-second time limit to find best possible routes
        searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration
        {
            Seconds = 2
        };

        //solve the problem
        Assignment solution = routing.SolveWithParameters(searchParameters);

        if (solution == null)
            return new List<RoutePlanDto>();

        return GetOptimizedRoute(binsForPlanning, routing, manager, solution);
    }

   public async Task<List<SavedRouteStopDto>> GetPlannedRoutesAsync(DateTime date)
    {
        return await _db.RoutePlans
            .Where(r => r.PlannedDate == date)
            .Include(r => r.RouteAssignment)
            .Include(r => r.RouteStops)
                .ThenInclude(rs => rs.CollectionBin)
            .SelectMany(r => r.RouteStops.Select(rs => new SavedRouteStopDto
            {
                RouteKey = r.RouteId,
                BinId = rs.BinId,
                Latitude = rs.CollectionBin!.Latitude!.Value,
                Longitude = rs.CollectionBin!.Longitude!.Value,
                StopNumber = rs.StopSequence,
                AssignedOfficerName = r.RouteAssignment != null ? (r.RouteAssignment!.AssignedTo ?? "") : ""
            }))
            .ToListAsync();
    }


        //helpers to calculate distance matrix
    private long[,] CreateDistanceMatrix(List<RoutePlanDto> locations)
    {
        int count = locations.Count;
        long[,] distanceMatrix = new long[count, count];

        for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    if (i == j)
                    {
                        distanceMatrix[i, j] = 0;
                        continue;
                    }
                    double dist = CalculateDistance(
                        locations[i].Latitude ?? 0.0, 
                        locations[i].Longitude ?? 0.0,
                        locations[j].Latitude ?? 0.0, 
                        locations[j].Longitude ?? 0.0);

                        distanceMatrix[i, j] = (long)(dist * 1000); //scale to meters

                }
            }
            return distanceMatrix;
        }

        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Earth's radius in kilometers
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double angle) => Math.PI * angle / 180.0;
    

    //helper for getting optimized route
    private List<RoutePlanDto> GetOptimizedRoute(
        List<RoutePlanDto> locations,
        RoutingModel routing, 
        RoutingIndexManager manager, 
        Assignment solution)
        {
            var optimizedRoute = new List<RoutePlanDto>();

            //loop through 3 routes (routing.Vehicles() method from OR-tools)
            for (int i = 0; i < routing.Vehicles(); ++i)
            {

                var index = routing.Start(i);
                int stopCount = 1;

                while (!routing.IsEnd(index))
                {
                    var nodeIndex = manager.IndexToNode(index);
                    var originalBin = locations[nodeIndex];

                    //only add to list if it's not depot
                    if (nodeIndex !=0)
                    {
                        var stopEntry = new RoutePlanDto
                        {
                            BinId = originalBin.BinId,
                            Latitude = originalBin.Latitude,
                            Longitude = originalBin.Longitude,
                            IsHighPriority = originalBin.IsHighPriority,
                            AssignedCO = i + 1,
                            StopNumber = stopCount++
                        };
                        optimizedRoute.Add(stopEntry);
                    }

                    index = solution.Value(routing.NextVar(index));
                }
            }
            return optimizedRoute;
        }

    }
}