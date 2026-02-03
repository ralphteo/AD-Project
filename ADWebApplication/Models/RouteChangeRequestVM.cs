using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ADWebApplication.Models
{
    public class RouteChangeRequestVM
    {
        [Required(ErrorMessage = "Please select a route")]
        public int? RouteId { get; set; }

        [Required(ErrorMessage = "Please select a request type")]
        public string RequestType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide a reason")]
        public string Reason { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a priority")]
        public string Priority { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide a description")]
        [MinLength(10, ErrorMessage = "Description must be at least 10 characters")]
        public string Description { get; set; } = string.Empty;

        public List<RouteOption> AvailableRoutes { get; set; } = new();
        public List<RouteChangeRequestItem> Requests { get; set; } = new();

        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }

        public string? Search { get; set; }
        public string? StatusFilter { get; set; }
        public string? PriorityFilter { get; set; }
    }

    public class RouteOption
    {
        public int RouteId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string DisplayText => string.IsNullOrWhiteSpace(Region)
            ? RouteName
            : $"{RouteName} - {Region}";
    }

    public class RouteChangeRequestItem
    {
        public string RequestId { get; set; } = string.Empty;
        public int RouteId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public string Status { get; set; } = "Pending";
        public string Description { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.Now;
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNote { get; set; }
    }
}
