using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADWebApplication.Models;

[Table("ewasteitemtype")]
public class EWasteItemType{
        [Key]
        [Column("itemTypeId")]
        public int ItemTypeId { get; set; }

        [Column("categoryId")]
        public int CategoryId { get; set; }

        [Column("itemName")]
        [Required, MaxLength(200)]
        public string ItemName { get; set; } = "";

        [Column("estimatedAvgWeight")]
        public double EstimatedAvgWeight { get; set; }

        [Column("basePoints")]
        public int BasePoints { get; set; }

        public EWasteCategory Category { get; set; } = null!;
        public ICollection<DisposalLogItem> DisposalLogItems { get; set; }
            = new List<DisposalLogItem>();
}
