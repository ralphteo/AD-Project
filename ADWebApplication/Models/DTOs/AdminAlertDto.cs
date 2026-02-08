namespace ADWebApplication.Models.DTOs;

public class AdminAlertDto
{
    public required string Type { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required string LinkText { get; set; }
    public required string LinkUrl { get; set; }
}