namespace ADWebApplication.Models.DTOs
{
    public class CreateDisposalLogRequest
    {
        public int? BinId { get; set; }
        public int ItemTypeId { get; set; }
        public string SerialNo { get; set; } = "";
        public double EstimatedWeightKg { get; set; }
        public string? Feedback { get; set; }
    }
}