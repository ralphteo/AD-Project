using System.ComponentModel.DataAnnotations;
using JasonRequired = Newtonsoft.Json.JsonPropertyAttribute;

namespace ADWebApplication.Models
{
    public class CollectionConfirmationVM
    {
        [JasonRequired]
        public int StopId { get; set; }
        public string? PointId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? BinId { get; set; }
        public string Zone { get; set; } = string.Empty;

        [Range(0, 100, ErrorMessage = "Fill level must be between 0 and 100")]
        public int BinFillLevel { get; set; }

        public DateTime CollectionTime { get; set; } = DateTime.Now;

        public string BinCondition { get; set; } = "Good";

        // Category Checkboxes
        [JasonRequired]
        public bool CollectedElectronics { get; set; }
        [JasonRequired]
        public bool CollectedBatteries { get; set; }
        [JasonRequired]
        public bool CollectedCables { get; set; }
        [JasonRequired]
        public bool CollectedAccessories { get; set; }

        public string? Remarks { get; set; }

        // Next stop preview (optional)
        public string? NextPointId { get; set; }
        public string? NextLocationName { get; set; }
        public string? NextAddress { get; set; }
        public DateTime? NextPlannedTime { get; set; }
        public int? NextFillLevel { get; set; }
    }
}
