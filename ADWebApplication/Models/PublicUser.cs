using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ADWebApplication.Models;

public class PublicUser
{
    [Key]
    [Column("userId")]
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    [Column("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [Column("Address")]
    public string? Address { get; set; }

    [Column("ReferralCode")]
    public string? ReferralCode { get; set; }

    [Column("role")]
    public UserRole Role { get; set; } = UserRole.USER;

    [Column("passwordHash")]
    public string Password { get; set; } = string.Empty;

    // 1:1
    public RewardWallet RewardWallet { get; set; } = null!;
}
