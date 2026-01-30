using System.ComponentModel.DataAnnotations;

namespace ADWebApplication.Models;

public class CollectionBin
{
    [Key]
    public int BinId { get; set; }
    public int? RegionId { get; set; }  // Foreign key
    public Region? Region { get; set; }
    public String? LocationName { get; set; }
    public int BinCapacity { get; set; }
    public String? BinStatus { get; set; }
}