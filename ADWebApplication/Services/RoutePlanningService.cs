using Google.OrTools.ConstraintSolver;
using ADWebApplication.Models.DTOs;

namespace ADWebApplication.Services
{
    public class RoutePlanningService
    {
        public async Task<RoutePlanningViewModel> GetRoutePlanningDetailsAsync(DateTime date)
        {
            // 1. Define your raw mock data (The "Stops" we want to visit)
            var rawStops = GetInitialMockStops();

            // 2. Generate the Distance Matrix using coordinates
            long[,] distanceMatrix = CreateDistanceMatrix(rawStops);

            // 3. Solve using OR-Tools
            var optimalIndices = SolveRoute(distanceMatrix, rawStops);

            // 4. Reorder the stops based on the solution
            var optimizedStops = new List<RouteStopDto>();
            for (int i = 0; i < optimalIndices.Count; i++)
            {
                var stop = rawStops[optimalIndices[i]];
                stop.SequenceOrder = i + 1; // Update order based on optimization
                optimizedStops.Add(stop);
            }

            return new RoutePlanningViewModel
            {
                SelectedDate = date,
                SelectedVehicleId = "All",
                TotalStops = optimizedStops.Count,
                EstimatedTotalDistance = 24.8, // Hardcoded for now
                Stops = optimizedStops
            };
        }

        private List<RouteStopDto> GetInitialMockStops()
        {
            return new List<RouteStopDto>
            {
                new RouteStopDto { LocationName = "Central Station (Depot)", Latitude = 1.290270, Longitude = 103.851959, BinType = "General", Status = "Completed" },
                new RouteStopDto { LocationName = "North Plaza", Latitude = 1.352083, Longitude = 103.819836, BinType = "Recycling", Status = "Pending" },
                new RouteStopDto { LocationName = "East Industrial Park", Latitude = 1.3236, Longitude = 103.9216, BinType = "Organic", Status = "Pending", IsHighRisk = true },
                new RouteStopDto { LocationName = "West Mall", Latitude = 1.3331, Longitude = 103.7423, BinType = "General", Status = "Pending" },
                new RouteStopDto { LocationName = "South Terminal", Latitude = 1.2655, Longitude = 103.8239, BinType = "Recycling", Status = "Pending" }
            };
        }

        private long[,] CreateDistanceMatrix(List<RouteStopDto> stops)
        {
            int count = stops.Count;
            long[,] matrix = new long[count, count];

            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    // Haversine or simple Euclidean distance calculation
                    matrix[i, j] = CalculateDistance(stops[i], stops[j]);
                }
            }
            return matrix;
        }

        private long CalculateDistance(RouteStopDto s1, RouteStopDto s2)
        {
            // Simple integer-based distance for OR-Tools mock
            double dLat = s2.Latitude - s1.Latitude;
            double dLon = s2.Longitude - s1.Longitude;
            return (long)(Math.Sqrt(dLat * dLat + dLon * dLon) * 100000); 
        }

        private List<int> SolveRoute(long[,] matrix, List<RouteStopDto> stops)
        {
            int size = matrix.GetLength(0);
            RoutingIndexManager manager = new RoutingIndexManager(size, 1, 0);
            RoutingModel routing = new RoutingModel(manager);

            int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) => {
                return matrix[manager.IndexToNode(fromIndex), manager.IndexToNode(toIndex)];
            });

            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            //ADD CONSTRAINT FOR HIGH RISK
            for (int i = 1; i < size; i++)
            {
                if (stops[i].IsHighRisk)
                {
                    long penalty = 100000;
                    routing.AddDisjunction(new long[] {manager.NodeToIndex(i)}, penalty);
                }
            }
            
            RoutingSearchParameters searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;

            Assignment solution = routing.SolveWithParameters(searchParameters);
            
            var result = new List<int>();
            var index = routing.Start(0);
            while (!routing.IsEnd(index)) {
                result.Add(manager.IndexToNode(index));
                index = solution.Value(routing.NextVar(index));
            }
            return result;
        }
    }
}