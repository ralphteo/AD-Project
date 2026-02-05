namespace ADWebApplication.Models.DTOs
{
    public class RewardRedemptionItemDto
    {
        public int RedemptionId { get; set; }
        public int RewardId { get; set; }
        public string RewardName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int PointsUsed { get; set; }
        public string RedemptionStatus { get; set; } = string.Empty;
        public DateTime RedemptionDateTime { get; set; }
    }
}
