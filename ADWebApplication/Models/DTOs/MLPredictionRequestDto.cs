namespace ADWebApplication.Models.DTOs;

public class MLPredictionRequestDto
{
    public string container_id { get; set; } = "";
    public double collection_fill_percentage { get; set; }
    public int cycle_duration_days { get; set; }
    public int cycle_start_month { get; set; }
}