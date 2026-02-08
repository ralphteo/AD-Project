using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ADWebApplication.Models;

[Table("collectionbin")]
public class CollectionBin
{
    [Key]
    [Column("binId")]
    [Required]
    [JsonRequired]
    public int BinId { get; set; }

    [Column("regionId")]
    public int? RegionId { get; set; }

    [Column("locationName")]
    public string? LocationName { get; set; }

    [Column("locationAddress")]
    public string? LocationAddress { get; set; }

    [Column("binCapacity")]
    [Required]
    [JsonRequired]
    public int BinCapacity { get; set; }

    [Column("binStatus")]
    public string BinStatus { get; set; } = "Active";

    [Column("latitude")]
    public double? Latitude { get; set; }

    [Column("longitude")]
    public double? Longitude { get; set; }

    public Region? Region { get; set; }
}
