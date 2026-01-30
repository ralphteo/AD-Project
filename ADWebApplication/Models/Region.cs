using System.ComponentModel.DataAnnotations;

namespace ADWebApplication.Models;

public class Region
{
    [Key]
    public int RegionId { get; set; }
    public String? RegionName { get; set; }
}