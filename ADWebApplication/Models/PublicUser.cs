using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class PublicUser
{
    [Key]
    [Column("userId")]
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    [Column("passwordHash")]
    public string Password { get; set; } = string.Empty;

    // 1:1
    public RewardWallet RewardWallet { get; set; } = null!;
}