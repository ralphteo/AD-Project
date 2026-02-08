using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace ADWebApplication.Models.DTOs
{
    public class CreateDisposalLogRequest
    {
        public int? BinId { get; set; }

        [Required]
        [JsonRequired]
        public int ItemTypeId { get; set; }
        public string SerialNo { get; set; } = "";
        
        [Required]
        [JsonRequired]
        public double EstimatedWeightKg { get; set; }
        public string? Feedback { get; set; }
        
        [Required]
        [JsonRequired]
        public int UserId { get; set; }
    }
}