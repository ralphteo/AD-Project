using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Services;

public class DisposalLogsService : IDisposalLogsService
{
    private readonly In5niteDbContext _context;

    public DisposalLogsService(In5niteDbContext context)
    {
        _context = context;
    }

    public async Task<(int LogId, int EarnedPoints)> CreateAsync(CreateDisposalLogRequest request)
    {
        using var tx = await _context.Database.BeginTransactionAsync();

        var log = new DisposalLogs
        {
            BinId = request.BinId,
            EstimatedTotalWeight = request.EstimatedWeightKg,
            DisposalTimeStamp = DateTime.UtcNow,
            Feedback = request.Feedback,
            UserId = request.UserId
        };

        _context.DisposalLogs.Add(log);
        await _context.SaveChangesAsync();

        var item = new DisposalLogItem
        {
            LogId = log.LogId,
            ItemTypeId = request.ItemTypeId,
            SerialNo = request.SerialNo,
        };

        _context.DisposalLogItems.Add(item);
        await _context.SaveChangesAsync();

        var itemType = await _context.EWasteItemTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ItemTypeId == request.ItemTypeId);

        var earnedPoints = itemType?.BasePoints ?? 0;

        var wallet = await _context.RewardWallet
            .FirstOrDefaultAsync(w => w.UserId == request.UserId);

        if (wallet == null)
        {
            wallet = new RewardWallet
            {
                UserId = request.UserId,
                AvailablePoints = 0
            };
            _context.RewardWallet.Add(wallet);
            await _context.SaveChangesAsync();
        }

        if (earnedPoints > 0)
        {
            wallet.AvailablePoints += earnedPoints;
            _context.PointTransactions.Add(new PointTransaction
            {
                WalletId = wallet.WalletId,
                LogId = log.LogId,
                TransactionType = "DISPOSAL",
                Status = "COMPLETED",
                Points = earnedPoints,
                CreatedDateTime = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return (log.LogId, earnedPoints);
    }

    public async Task<List<DisposalHistoryDto>> GetHistoryAsync(int userId, string range)
    {
        var from = ResolveFromDate(range, DateTime.UtcNow);

        var query = _context.DisposalLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId);

        query = ApplyFromFilter(query, from);

        return await query
            .OrderByDescending(l => l.DisposalTimeStamp)
            .Select(l => new DisposalHistoryDto
            {
                LogId = l.LogId,
                DisposalTimeStamp = l.DisposalTimeStamp,
                EstimatedTotalWeight = l.EstimatedTotalWeight,
                Feedback = l.Feedback,

                BinId = l.BinId,
                BinLocationName = l.CollectionBin != null ? l.CollectionBin.LocationName : null,
                LocationName = l.CollectionBin != null ? l.CollectionBin.LocationName : null,

                ItemTypeId = l.DisposalLogItem.ItemTypeId,
                ItemTypeName = l.DisposalLogItem.ItemType != null ? l.DisposalLogItem.ItemType.ItemName : null,
                SerialNo = l.DisposalLogItem.SerialNo,

                CategoryName = (l.DisposalLogItem.ItemType != null && l.DisposalLogItem.ItemType.Category != null)
                    ? l.DisposalLogItem.ItemType.Category.CategoryName
                    : null,

                EarnedPoints = (l.DisposalLogItem.ItemType != null)
                    ? l.DisposalLogItem.ItemType.BasePoints
                    : 0
            })
            .ToListAsync();
    }

    private static IQueryable<DisposalLogs> ApplyFromFilter(
        IQueryable<DisposalLogs> query,
        DateTime? from)
    {
        if (!from.HasValue)
        {
            return query;
        }

        return query.Where(l => l.DisposalTimeStamp >= from.Value);
    }

    private static DateTime? ResolveFromDate(string range, DateTime now)
    {
        var normalizedRange = NormalizeRange(range);

        if (IsThisMonthRange(normalizedRange))
        {
            return new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        if (IsLastThreeMonthsRange(normalizedRange))
        {
            return now.AddMonths(-3);
        }

        return null;
    }

    private static string NormalizeRange(string range)
    {
        return (range ?? "all").Trim().ToLowerInvariant();
    }

    private static bool IsThisMonthRange(string normalizedRange)
    {
        return normalizedRange == "month" || normalizedRange == "thismonth";
    }

    private static bool IsLastThreeMonthsRange(string normalizedRange)
    {
        return normalizedRange is "last3" or "last 3" or "last_3" or "last3months";
    }
}
