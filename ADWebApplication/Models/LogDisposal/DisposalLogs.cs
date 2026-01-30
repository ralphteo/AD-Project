using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADWebApplication.Models.LogDisposal
{
    [Table("DisposalLogs")]
    public class DisposalLogs
    {
        [Key]
        [Column("logID")]
        public int LogId { get; set; }

        [Column("binID")]
        public int? BinId { get; set; }

        [Column("userID")]
        public int? UserId { get; set; }

        [Column("estimatedTotalWeight")]
        public double EstimatedTotalWeight { get; set; }

        [Column("disposalTimeStamp")]
        public DateTime DisposalTimeStamp { get; set; } = DateTime.UtcNow;

        [Column("feedback")]
        public string? Feedback { get; set; }

        public DisposalLogItem DisposalLogItem { get; set; } = null!;
        public CollectionBin? CollectionBin { get; set; }

    }
}