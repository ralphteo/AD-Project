
namespace ADWebApplication.Models;

public class CollectionDetails
{
    public int CollectionId { get; set; }
    public RouteStop? RouteStop { get; set; }
    public DateTime CollectionDateTimeLocal { get; set; } // Form submitted local time
    public DateTimeOffset CollectionDateTimeOffset { get; set; } // Stored in DB time with offset
    public int BinFillLevel { get; set; } // Percentage
    public int BinFillHeight { get; set; } // Cm height
    public String? CollectionStatus { get; set; }
    public String? IssueLog { get; set; }

    private int CalculateBinFillLevel()
    {
        // Avoid division by zero
        if (RouteStop?.CollectionBin == null || RouteStop.CollectionBin.BinCapacity == 0)
        {
            return 0;
        }

        return (int)((BinFillHeight / (double)RouteStop.CollectionBin.BinCapacity) * 100);
    } 
}