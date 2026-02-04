namespace ADWebApplication.Models.DTOs
{
    public class RewardCatalogueDto
    {
        public int RewardId { get; set; }
        public string RewardName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Points { get; set; }
        public string RewardCategory { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool Availability { get; set; }
    }
}
