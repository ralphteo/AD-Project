using ADWebApplication.Models;
using ADWebApplication.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Controllers
{
    public class RewardRedemptionController : Controller
    {
        private readonly In5niteDbContext _context;

        public RewardRedemptionController(In5niteDbContext context)
        {
            _context = context;
        }

        // GET: RewardRedemption
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, string? status, int? userId, int? rewardId)
        {
            var query = _context.RewardRedemptions
                .Include(rr => rr.RewardCatalogue)
                .Include(rr => rr.User)
                .AsQueryable();

            //Date Range Filter
            if (startDate.HasValue)
            {
                query = query.Where(rr => rr.RedemptionDateTime >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(rr => rr.RedemptionDateTime <= endDate.Value);
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(rr => rr.RedemptionStatus == status);
            }
            if (userId.HasValue)
            {
                query = query.Where(rr => rr.UserId == userId.Value);
            }
            if (rewardId.HasValue)
            {
                query = query.Where(rr => rr.RewardId == rewardId.Value);
            }

            var rewardRedemptions = await query
            .OrderByDescending(rr => rr.RedemptionDateTime)
            .ToListAsync();

            // Pass filter values to ViewBag for retaining filter state in the view
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Status = status;
            ViewBag.UserId = userId;
            ViewBag.RewardId = rewardId;

            //get distinct statuses for filter dropdown
            ViewBag.Statuses = await _context.RewardRedemptions
                .Select(rr => rr.RedemptionStatus)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();
            return View(rewardRedemptions);
        }

        //Export to CSV
        public async Task<IActionResult> ExportToCsv(DateTime? startDate, DateTime? endDate, string? status, int? userId, int? rewardId)
        {
            var query = _context.RewardRedemptions
                .Include(rr => rr.RewardCatalogue)
                .Include(rr => rr.User)
                .AsQueryable();

            // Apply filters (same as in Index)
            if (startDate.HasValue)
            {
                query = query.Where(rr => rr.RedemptionDateTime >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(rr => rr.RedemptionDateTime <= endDate.Value);
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(rr => rr.RedemptionStatus == status);
            }
            if (userId.HasValue && userId.Value > 0)
            {
                query = query.Where(rr => rr.UserId == userId.Value);
            }
            if (rewardId.HasValue && rewardId.Value > 0)
            {
                query = query.Where(rr => rr.RewardId == rewardId.Value);
            }

            var rewardRedemptions = await query
                .OrderByDescending(rr => rr.RedemptionDateTime)
                .ToListAsync();

            var csvLines = new System.Text.StringBuilder();
            csvLines.AppendLine("RedemptionId,RewardId,WalletId,UserId,PointsUsed,RedemptionStatus,RedemptionDateTime,FulfilledDatetime");

            foreach (var item in rewardRedemptions)
            {
                csvLines.AppendLine($"{item.RedemptionId},{item.RewardId},{item.WalletId},{item.UserId},{item.PointsUsed},{item.RedemptionStatus},{item.RedemptionDateTime:yyyy-MM-dd HH:mm:ss},{(item.FulfilledDatetime.HasValue ? item.FulfilledDatetime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "")}");
            }

            var csvContent = csvLines.ToString();
            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", "RewardRedemptions.csv");
        }
    }
}