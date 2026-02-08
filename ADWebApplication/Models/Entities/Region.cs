
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ADWebApplication.Models;

[Table("region")]
public class Region
{
    [Key]
    [Column("regionId")]
    [Required]
    [JsonRequired]
    public int RegionId { get; set; }

    [Column("regionName")]
    public string? RegionName { get; set; }
}
