using System.ComponentModel.DataAnnotations;

namespace ADWebApplication.Models
{
    public class CollectionConfirmationVM
    {
        public string PointId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string BinId { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;

        [Required]
        [Range(0.1, 1000, ErrorMessage = "Please enter a valid weight (0.1 - 1000 kg)")]
        public double CollectedWeightKg { get; set; }

        public DateTime CollectionTime { get; set; } = DateTime.Now;

        public string BinCondition { get; set; } = "Good";

        // Category Checkboxes
        public bool CollectedElectronics { get; set; }
        public bool CollectedBatteries { get; set; }
        public bool CollectedCables { get; set; }
        public bool CollectedAccessories { get; set; }

        public string? Remarks { get; set; }
    }
}