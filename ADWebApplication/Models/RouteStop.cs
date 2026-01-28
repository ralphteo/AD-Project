
using System.Collections.ObjectModel;

namespace ADWebApplication.Models;

public class RouteStop
{
    public int StopId { get; set; }
    public RoutePlan? RoutePlan { get; set; }
    public CollectionBin? CollectionBin { get; set; }
    public int StopSequence { get; set; }
    public DateTimeOffset PlannedCollectionTime  { get; set; }
}