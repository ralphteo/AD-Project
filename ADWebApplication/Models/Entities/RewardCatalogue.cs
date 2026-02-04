using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADWebApplication.Models
{
    [Table("rewardcatalogue")]
    public class RewardCatalogue
    {
        [Key]
        [Column("rewardId")]
        public int RewardId { get; set; }

        [Column("rewardName")]
        public string RewardName { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("points")]
        public int Points { get; set; }

        [Column("rewardCategory")]
        public string RewardCategory { get; set; } = string.Empty;

        [Column("stockQuantity")]
        public int StockQuantity { get; set; }

        [Column("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        [Column("Availability")]
        public bool Availability { get; set; }

        [Column("createdDateTime")]
        public DateTime? CreatedDateTime { get; set; }

        [Column("updatedDatetime")]
        public DateTime? UpdatedDatetime { get; set; }
    }
}
