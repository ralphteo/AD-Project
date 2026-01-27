using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
public class RewardWallet
{
    [Key]
    [Column("walletId")]
    public int Id { get; set; }

    [Column("availablePoints")]
    public int Points { get; set; }

    public int UserId { get; set; }

    public PublicUser User { get; set; } = null!;
}