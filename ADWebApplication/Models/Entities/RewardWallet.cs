using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ADWebApplication.Models
{
    [Table("rewardwallet")]
    public class RewardWallet
    {
        [Key]
        [Column("walletId")]
        public int WalletId { get; set; }

        [Column("userId")]
        public int UserId { get; set; }

        [Column("availablePoints")]
        public int AvailablePoints { get; set; }

        public PublicUser? User { get; set; }
    }
}
