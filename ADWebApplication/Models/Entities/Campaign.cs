using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ADWebApplication.Models;

public class Campaign
{
    [Key]
    [Column("campaignId")]
    [Required(ErrorMessage = "Campaign ID is required.")]
    [JsonRequired]
    public int CampaignId { get; set; }

    [Required(ErrorMessage = "Campaign name is required.")]
    [StringLength(100, ErrorMessage = "Campaign name cannot exceed 100 characters.")]
    [Column("campaignName")]
    public string CampaignName { get; set; }
    = string.Empty;

    [Required(ErrorMessage = "Start date is required.")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [JsonRequired]
    [Column("startDate")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [JsonRequired]
    [Column("endDate")]
    public DateTime EndDate { get; set; }

    [Required(ErrorMessage = "Incentive type is required.")]
    [Column("incentiveType")]
    public String IncentiveType { get; set; } = "None"; // e.g. "Multiplier", "Bonus", "None"

    [Required(ErrorMessage = "Incentive value is required.")]
    [Column("incentiveValue")]
    public decimal IncentiveValue { get; set; } = 0; // e.g. 1.5 for 1.5x points

    [Column("description")] 
    public string? Description { get; set; } = string.Empty;
    
    [Column("status")]
    public string? Status { get; set; } = "INACTIVE"; // e.g. "Planned", "Active", "Completed, Inactive"

    [NotMapped]
    public bool IsActive
    {
        get
        {
            var now = DateTime.UtcNow;
            return now >= StartDate && now <= EndDate && Status == "ACTIVE";
        }
    }
    [NotMapped]
    public bool IsExpired
    {
        get
        {
            var now = DateTime.UtcNow;
            return now > EndDate || Status == "INACTIVE";
        }
    }
    [NotMapped]
    public int CampaignDurationDays
    {
        get
        {
            return (EndDate - StartDate).Days;
        }
    }
    [NotMapped]
    public string StatusBadgeClass
    {
        get
        {
            return Status switch
            {
                "ACTIVE" => "bg-success",
                "SCHEDULED" => "bg-info",
                "INACTIVE" => "bg-warning",
                "EXPIRED" => "bg-secondary",
                _ => "bg-secondary"
            };
        }
    }
    public string IncentiveTypeDisplay
    {
        get
        {
            return IncentiveType switch
            {
                "Multiplier" => $"{IncentiveValue}x Points",
                "Bonus" => $"Bonus {IncentiveValue} Points",
                _ => "No Incentive"
            };
        }
    }
}