using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ADWebApplication.Models.LogDisposal
{
    [Table("DisposalLogItem")]
    public class DisposalLogItem
    {
        [Key]
        [Column("logItemID")]
        public int LogItemId { get; set; }

        [Column("logId")]
        public int LogId { get; set; }
        
        [Column("itemTypeId")]
        public int ItemTypeId { get; set; }

        [Column("SerialNo")]
        [Required, MaxLength(100)]
        public string SerialNo { get; set; } = "";
        
        public DisposalLogs DisposalLog { get; set; } = null!;
        public EWasteItemType ItemType { get; set; } = null!;

    }
}