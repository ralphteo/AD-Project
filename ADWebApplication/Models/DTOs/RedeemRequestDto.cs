using System.ComponentModel.DataAnnotations;

namespace ADWebApplication.Models.DTOs
{
    public class RedeemRequestDto
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public int RewardId { get; set; }
    }
}
