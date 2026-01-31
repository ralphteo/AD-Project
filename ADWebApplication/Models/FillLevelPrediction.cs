using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADWebApplication.Models;

[Table("filllevelprediction")]
public class FillLevelPrediction
{
    [Key]
    [Column("predictionId")]
    public int PredictionId { get; set; }

    [Column("binId")]
    public int BinId { get; set; }

    [Column("predictedFillPercentage")]
    public decimal PredictedFillPercentage { get; set; }

    [Column("predictedStatus")]
    public string? PredictedStatus { get; set; }

    [Column("predictedDate")]
    public DateTime PredictedDate { get; set; }

    [Column("modelVersion")]
    public string? ModelVersion { get; set; }

    [Column("confidenceScore")]
    public decimal? ConfidenceScore { get; set; }
}
