using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADWebApplication.Models
{
    [Table("rewardredemption")]
    public class RewardRedemption
    {
        [Key]
        [Column("redemptionId")]
        public int RedemptionId { get; set; }

        [Column("rewardId")]
        public int RewardId { get; set; }

        [Column("walletId")]
        public int WalletId { get; set; }

        [Column("userId")]
        public int UserId { get; set; }

        [Column("pointsUsed")]
        public int PointsUsed { get; set; }

        [Column("redemptionStatus")]
        public string RedemptionStatus { get; set; } = string.Empty;

        [Column("redemptionDateTime")]
        public DateTime RedemptionDateTime { get; set; }

        [Column("fulfilledDatetime")]
        public DateTime? FulfilledDatetime { get; set; }
    }
}
