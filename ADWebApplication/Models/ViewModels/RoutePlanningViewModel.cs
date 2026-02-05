using ADWebApplication.Models.DTOs;
using ADWebApplication.Services;

namespace ADWebApplication.Models.ViewModels
{
    public class RoutePlanningViewModel
    {
        public List<RoutePlanDto> AllStops { get; set; } = new List<RoutePlanDto>();
        public int HighPriorityCount => AllStops.Count(s => s.IsHighPriority);

        public string TodayDate { get; set; } = string.Empty;
        public int TotalBins => AllStops?.Count ?? 0;
        public string CollectionDate {get; set;}



    }
}
