namespace ADWebApplication.Models.DTOs
{
    public class DisposalHistoryDto
    {
        public int LogId { get; set; }
        public DateTime DisposalTimeStamp { get; set; }
        public double EstimatedTotalWeight { get; set; }
        public string? Feedback { get; set; }

        public int? BinId { get; set; }
        public string? BinLocationName { get; set; }

        public int ItemTypeId { get; set; }
        public string? ItemTypeName { get; set; }
        public string? SerialNo { get; set; }

        public string? CategoryName { get; set; }
        public int EarnedPoints { get; set; }   // calculated
        public string? LocationName { get; set; }

    }
}