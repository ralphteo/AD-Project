namespace ADWebApplication.Models.DTOs
{
    public class RedeemResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RemainingPoints { get; set; }
        public int RedemptionId { get; set; }
    }
}
