using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class PublicUser
{
    [Key]
    [Column("userId")]
    public int Id { get; set; }

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [Column("regionId")]
    public int? RegionId { get; set; }

    [Column("isActive")]
    public bool IsActive { get; set; }

    [Column("passwordHash")]
    public string Password { get; set; } = string.Empty;

    // 1:1
    public RewardWallet RewardWallet { get; set; } = null!;
}
