
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADWebApplication.Models;

[Table("region")]
public class Region
{
    [Key]
    [Column("regionId")]
    public int RegionId { get; set; }

    [Column("regionName")]
    public string? RegionName { get; set; }
}
