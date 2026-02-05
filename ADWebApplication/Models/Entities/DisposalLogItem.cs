using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ADWebApplication.Models
{
    [Table("DisposalLogItem")]
    public class DisposalLogItem
    {
        [Key]
        [Column("logItemId")]
        public int LogItemId { get; set; }

        [Column("logId")]
        public int LogId { get; set; }

        [Column("itemTypeId")]
        public int ItemTypeId { get; set; }

        [Column("SerialNo")]
        public string SerialNo { get; set; } = "";

        [ForeignKey(nameof(LogId))]
        public DisposalLogs? DisposalLog { get; set; }

        [ForeignKey(nameof(ItemTypeId))]
        public EWasteItemType? ItemType { get; set; }
    }
}
