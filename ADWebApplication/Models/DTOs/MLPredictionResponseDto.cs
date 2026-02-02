namespace ADWebApplication.Models.DTOs;

public class MLPredictionResponseDto
{
    public double predicted_next_avg_daily_growth { get; set; }
    public int estimated_days_to_threshold { get; set; }
}