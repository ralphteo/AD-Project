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
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid filter parameters.");
            }
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
    }
}