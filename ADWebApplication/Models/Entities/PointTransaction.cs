using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADWebApplication.Models
{
    [Table("pointtransaction")]
    public class PointTransaction
    {
        [Key]
        [Column("transactionId")]
        public int TransactionId { get; set; }

        [Column("walletId")]
        public int WalletId { get; set; }

        [Column("logId")]
        public int? LogId { get; set; }

        [Column("transactionDate")]
        public DateTime TransactionDate {get; set;}

        [Column("transactionType")]
        public string TransactionType { get; set; } = string.Empty; 

        [Column("status")]
        public string Status { get; set; } = string.Empty;

        [Column("points")]
        public int Points { get; set; }

        [Column("createdDateTime")]
        public DateTime CreatedDateTime { get; set; }

        [ForeignKey(nameof(LogId))]
        public DisposalLogs? DisposalLog { get; set; }
    }
}
