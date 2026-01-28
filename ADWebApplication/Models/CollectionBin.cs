
namespace ADWebApplication.Models;

public class CollectionBin
{
    public int BinId { get; set; }
    public Region? Region { get; set; }
    public String? LocationName { get; set; }
    public String? Address { get; set; }
    public String? LocationType { get; set; }
    public int BinCapacity { get; set; }
    public String? BinStatus { get; set; }
}