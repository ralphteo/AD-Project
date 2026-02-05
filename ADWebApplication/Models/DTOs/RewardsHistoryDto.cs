namespace ADWebApplication.Models.DTOs
{
    public class RewardsHistoryDto
    {
        public int TransactionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int Points { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}