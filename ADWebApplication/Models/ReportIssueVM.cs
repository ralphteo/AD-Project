using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ADWebApplication.Models
{
    public class ReportIssueVM
    {
        [Required(ErrorMessage = "Please select a bin")]
        public int BinId { get; set; }
        
        // Auto-populated from selected bin
        public string LocationName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select an issue type")]
        public string IssueType { get; set; } = string.Empty; // Overflow, Damaged, Access Issue, etc.

        [Required(ErrorMessage = "Please select severity level")]
        public string Severity { get; set; } = string.Empty; // Low, Medium, High

        [Required(ErrorMessage = "Please provide a description")]
        [MinLength(10, ErrorMessage = "Description must be at least 10 characters")]
        public string Description { get; set; } = string.Empty;
        
        // For dropdown - list of bins from today's route
        public List<BinOption> AvailableBins { get; set; } = new();

        // Issue log list
        public List<IssueLogItem> Issues { get; set; } = new();
        public int TotalIssues { get; set; }
        public int OpenIssues { get; set; }
        public int InProgressIssues { get; set; }
        public int ResolvedIssues { get; set; }

        // Filters
        public string? Search { get; set; }
        public string? StatusFilter { get; set; }
        public string? PriorityFilter { get; set; }
    }
    
    public class BinOption
    {
        public int BinId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string DisplayText => $"Bin #{BinId} - {LocationName}";
    }

    public class IssueLogItem
    {
        public int StopId { get; set; }
        public int BinId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string IssueType { get; set; } = "Other";
        public string Severity { get; set; } = "Medium";
        public string Status { get; set; } = "Open";
        public string Description { get; set; } = string.Empty;
        public string ReportedBy { get; set; } = string.Empty;
        public DateTime ReportedAt { get; set; } = DateTime.Now;
    }
}
