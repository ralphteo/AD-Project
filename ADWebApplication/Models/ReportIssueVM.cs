using System.ComponentModel.DataAnnotations;

namespace ADWebApplication.Models
{
    public class ReportIssueVM
    {
        [Required(ErrorMessage = "Please select a location")]
        public string PointId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select an issue type")]
        public string IssueType { get; set; } = string.Empty; // Overflow, Damaged, Lock, etc.

        [Required(ErrorMessage = "Please select severity level")]
        public string Severity { get; set; } = string.Empty; // Low, Medium, High

        [Required(ErrorMessage = "Please provide a description")]
        [MinLength(10, ErrorMessage = "Description must be at least 10 characters")]
        public string Description { get; set; } = string.Empty;

        //???
        public IFormFile? PhotoEvidence { get; set; }
    }
}