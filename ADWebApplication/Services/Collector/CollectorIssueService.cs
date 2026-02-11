using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Services.Collector
{
    public class CollectorIssueService : ICollectorIssueService
    {
        private readonly In5niteDbContext _db;

        // SonarQube: prevent potential Regex DoS by enforcing a match timeout
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(200);

        public CollectorIssueService(In5niteDbContext db)
        {
            _db = db;
        }

        public async Task<ReportIssueVM> GetReportIssueViewModelAsync(string username, string? search, string? status, string? priority)
        {
            var today = DateTime.Today;
            var todaysBins = await _db.RouteAssignments
                .Where(ra => ra.AssignedTo == username.Trim())
                .SelectMany(ra => ra.RoutePlans)
                .Where(rp => rp.PlannedDate.HasValue && rp.PlannedDate.Value.Date == today)
                .SelectMany(rp => rp.RouteStops)
                .Where(rs => rs.CollectionBin != null)
                .Select(rs => new BinOption
                {
                    BinId = rs.CollectionBin!.BinId,
                    LocationName = rs.CollectionBin!.LocationName ?? "",
                    Region = rs.CollectionBin!.Region != null ? (rs.CollectionBin!.Region!.RegionName ?? "") : ""
                })
                .Distinct()
                .ToListAsync();

            var issueStops = await _db.RouteStops
                .Include(rs => rs.CollectionBin)
                    .ThenInclude(cb => cb!.Region)
                .Include(rs => rs.CollectionDetails)
                .Include(rs => rs.RoutePlan)
                    .ThenInclude(rp => rp!.RouteAssignment)
                .Where(rs => rs.RoutePlan != null
                          && rs.RoutePlan.RouteAssignment != null
                          && rs.RoutePlan.RouteAssignment.AssignedTo == username.Trim())
                .OrderByDescending(rs => rs.RoutePlan!.PlannedDate!)
                .ToListAsync();

            var issues = issueStops
                .Select(rs => new
                {
                    Stop = rs,
                    IssueLog = !string.IsNullOrWhiteSpace(rs.IssueLog)
                        ? rs.IssueLog
                        : rs.CollectionDetails
                            .OrderByDescending(cd => cd.CurrentCollectionDateTime)
                            .Select(cd => cd.IssueLog)
                            .FirstOrDefault()
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.IssueLog))
                .Select(x => MapIssueLog(x.Stop, x.IssueLog!, username))
                .ToList();

            var filteredIssues = issues.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                filteredIssues = filteredIssues.Where(i =>
                    i.IssueType.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    i.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    i.LocationName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    i.Address.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    i.BinId.ToString().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                filteredIssues = filteredIssues.Where(i => string.Equals(i.Status, status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                filteredIssues = filteredIssues.Where(i => string.Equals(i.Severity, priority, StringComparison.OrdinalIgnoreCase));
            }

            return new ReportIssueVM
            {
                AvailableBins = todaysBins,
                Issues = filteredIssues.ToList(),
                TotalIssues = issues.Count,
                OpenIssues = issues.Count(i => string.Equals(i.Status, CollectorConstants.StatusOpen, StringComparison.OrdinalIgnoreCase)),
                InProgressIssues = issues.Count(i => string.Equals(i.Status, CollectorConstants.StatusInProgress, StringComparison.OrdinalIgnoreCase)),
                ResolvedIssues = issues.Count(i => string.Equals(i.Status, CollectorConstants.StatusResolved, StringComparison.OrdinalIgnoreCase)),
                Search = search,
                StatusFilter = status,
                PriorityFilter = priority
            };
        }

        public async Task<bool> SubmitIssueAsync(ReportIssueVM model, string username)
        {
            var today = DateTime.Today;
            var stop = await _db.RouteStops
                .Include(rs => rs.RoutePlan)
                    .ThenInclude(rp => rp!.RouteAssignment)
                .Where(rs => rs.BinId == model.BinId
                          && rs.RoutePlan != null
                          && rs.RoutePlan.RouteAssignment != null
                          && rs.RoutePlan.RouteAssignment.AssignedTo == username.Trim()
                          && rs.RoutePlan.PlannedDate.HasValue
                          && rs.RoutePlan.PlannedDate.Value.Date == today)
                .FirstOrDefaultAsync();

            if (stop == null) return false;

            var newIssue = $"type: {model.IssueType}; severity: {model.Severity}; status: {CollectorConstants.StatusOpen}; description: {model.Description}";

            if (string.IsNullOrWhiteSpace(stop.IssueLog))
                stop.IssueLog = newIssue;
            else
                stop.IssueLog = $"{stop.IssueLog}{Environment.NewLine}{newIssue}";

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<string> StartIssueWorkAsync(int stopId, string username)
        {
            var stop = await _db.RouteStops
                .Include(rs => rs.CollectionDetails)
                .Include(rs => rs.RoutePlan)
                    .ThenInclude(rp => rp!.RouteAssignment)
                .Where(rs => rs.StopId == stopId
                          && rs.RoutePlan != null
                          && rs.RoutePlan.RouteAssignment != null
                          && rs.RoutePlan.RouteAssignment.AssignedTo == username.Trim())
                .FirstOrDefaultAsync();

            if (stop == null) return "Issue not found for this route.";

            var latestDetail = stop.CollectionDetails
                .Where(cd => !string.IsNullOrWhiteSpace(cd.IssueLog))
                .OrderByDescending(cd => cd.CurrentCollectionDateTime)
                .FirstOrDefault();

            var currentLog = latestDetail?.IssueLog ?? stop.IssueLog ?? string.Empty;
            var currentStatus = ExtractValue(currentLog, "status") ?? InferStatus(currentLog);

            if (string.Equals(currentStatus, CollectorConstants.StatusResolved, StringComparison.OrdinalIgnoreCase)) return "Issue is already resolved.";

            var newStatus = string.Equals(currentStatus, CollectorConstants.StatusInProgress, StringComparison.OrdinalIgnoreCase) ? CollectorConstants.StatusResolved : CollectorConstants.StatusInProgress;

            if (latestDetail != null)
            {
                latestDetail.IssueLog = UpdateIssueLogStatus(currentLog, newStatus);
            }
            else
            {
                stop.IssueLog = UpdateIssueLogStatus(currentLog, newStatus);
            }

            await _db.SaveChangesAsync();
            return newStatus;
        }

        #region Helper Methods

        private static IssueLogItem MapIssueLog(RouteStop stop, string issueLog, string reportedBy)
        {
            var issueType = ExtractValue(issueLog, "type") ?? "Other";
            var severity = ExtractValue(issueLog, "severity") ?? InferSeverity(issueLog);
            var status = ExtractValue(issueLog, "status") ?? InferStatus(issueLog);
            var description = ExtractValue(issueLog, "description") ?? issueLog.Trim();

            var latestCollection = stop.CollectionDetails
                .OrderByDescending(cd => cd.CurrentCollectionDateTime)
                .FirstOrDefault();

            return new IssueLogItem
            {
                StopId = stop.StopId,
                BinId = stop.BinId ?? 0,
                LocationName = stop.CollectionBin?.LocationName ?? "Unknown location",
                Address = stop.CollectionBin?.LocationAddress ?? "",
                IssueType = issueType,
                Severity = severity,
                Status = status,
                Description = description,
                ReportedBy = reportedBy,
                ReportedAt = latestCollection?.CurrentCollectionDateTime?.DateTime ?? stop.PlannedCollectionTime.DateTime
            };
        }

        // SonarQube: add timeout + escape the key to avoid ReDoS and pattern injection
        private static string? ExtractValue(string input, string key)
        {
            var safeKey = System.Text.RegularExpressions.Regex.Escape(key);

            var match = System.Text.RegularExpressions.Regex.Match(
                input ?? string.Empty,
                $@"{safeKey}\s*[:=]\s*([^;|\n]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase,
                RegexTimeout);

            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        // SonarQube: add timeout to Regex constructor
        private static string UpdateIssueLogStatus(string? input, string status)
        {
            var baseText = string.IsNullOrWhiteSpace(input) ? "" : input;

            var regex = new System.Text.RegularExpressions.Regex(
                @"status\s*[:=]\s*([^;|\n]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase,
                RegexTimeout);

            if (regex.IsMatch(baseText))
            {
                return regex.Replace(baseText, $"status: {status}");
            }

            if (string.IsNullOrWhiteSpace(baseText))
            {
                return $"status: {status}";
            }

            return $"{baseText}; status: {status}";
        }

        private static string InferSeverity(string input)
        {
            if (input.Contains("high", StringComparison.OrdinalIgnoreCase)) return "High";
            if (input.Contains("low", StringComparison.OrdinalIgnoreCase)) return "Low";
            return "Medium";
        }

        private static string InferStatus(string input)
        {
            if (input.Contains("resolved", StringComparison.OrdinalIgnoreCase) || input.Contains("closed", StringComparison.OrdinalIgnoreCase)) return CollectorConstants.StatusResolved;
            if (input.Contains("progress", StringComparison.OrdinalIgnoreCase)) return CollectorConstants.StatusInProgress;
            return CollectorConstants.StatusOpen;
        }

        #endregion
    }
}
