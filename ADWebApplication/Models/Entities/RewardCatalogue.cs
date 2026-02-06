using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ADWebApplication.Models;

// [Table("RewardCatalogue")]
public class RewardCatalogue
{
    [Key]
    [Column("rewardId")]
    [Required(ErrorMessage = "Reward ID is required.")]
    [JsonRequired]
    public int RewardId { get; set; }

    [Required(ErrorMessage = "Reward name is required.")]
    [StringLength(200, ErrorMessage = "Reward name cannot exceed 200 characters.")]
    [Column("rewardName")]
    public string RewardName { get; set; }
    = string.Empty;

    [Required(ErrorMessage = "Points required is required.")]
    [Range(10, int.MaxValue, ErrorMessage = "Points must be at least 10.")]
    [Column("points")]
    public int Points { get; set; }

    [Column("description")] 
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "Reward category cannot exceed 100 characters.")]
    [Column("rewardCategory")]
    public string? RewardCategory { get; set; }

    [Required(ErrorMessage = "Stock quantity is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
    [Column("stockQuantity")]
    public int StockQuantity { get; set; }

    [Url(ErrorMessage = "Invalid URL format.")]
    [StringLength(400, ErrorMessage = "Image URL cannot exceed 400 characters.")]
    [Column("imageUrl")]
    public string? ImageUrl { get; set; }

    [Column("Availability")]
    public bool Availability {get; set; } = true;

    [Column("createdDateTime")]
    [Required]
    [JsonRequired]
    public DateTime CreatedDate { get; set; } 

    [Column("updatedDatetime")]
    [Required]
    [JsonRequired]
    public DateTime UpdatedDate { get; set; } 

    [NotMapped]
    public bool CanBeRedeemed => Availability && StockQuantity > 0;

    [NotMapped]
    public string StockStatus
    {
        get
        {
            if (StockQuantity == 0) 
            {
                return "Out of Stock";
            }
            if (StockQuantity < 10) 
            {
                return "Low Stock";
            }
            return "In Stock";
        }
    }
    [NotMapped]
    public string StockBadgeClass
    {
        get
        {
            if (StockQuantity == 0) return "badge bg-danger";
            if (StockQuantity < 10) return "badge bg-warning text-dark";
            return "badge bg-success";
        }
    }
    [NotMapped]
    public string AvailabilityBadgeClass => Availability? "badge bg-success" : "badge bg-secondary";
}

