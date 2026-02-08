using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ADWebApplication.Models.DTOs
{
    public class RedeemRequestDto
    {
        [Required]
        [JsonRequired]
        public int UserId { get; set; }
        [Required]
        [JsonRequired]
        public int RewardId { get; set; }
    }
}
