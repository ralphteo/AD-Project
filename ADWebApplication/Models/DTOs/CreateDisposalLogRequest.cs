using System.ComponentModel.DataAnnotations;

namespace ADWebApplication.Models.DTOs
{
    public class CreateDisposalLogRequest
    {
        public int? BinId { get; set; }
        [Required]
        public int ItemTypeId { get; set; }
        public string SerialNo { get; set; } = "";
        [Required]
        public double EstimatedWeightKg { get; set; }
        public string? Feedback { get; set; }
        [Required]
        public int UserId { get; set; }
    }
}