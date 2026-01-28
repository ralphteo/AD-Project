using System.Data;
using ADWebApplication.Models.DTOs;
using Microsoft.Data.SqlClient;
using Dapper;

namespace ADWebApplication.Data.Repository
{
    public interface IDashboardRepository
    {
       Task <DashboardKPIs> GetAdminDashboardAsync(DateTime? forMonth = null);
       Task<List<CollectionTrend>> GetCollectionTrendsAsync(int monthsBack = 6);
       Task<List<CategoryBreakdown>> GetCategoryBreakdownAsync();
       Task<List<AvgPerformance>> GetAvgPerformanceMetricsAsync();
    }
    public class DashboardRepository : IDashboardRepository
    {
        private readonly String _connectionString;

        public DashboardRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SQLDatabase");
        }

        public async Task<DashboardKPIs> GetAdminDashboardAsync(DateTime? forMonth = null)
        {
            var targetMonth = forMonth ?? DateTime.UtcNow;
            var previousMonth = targetMonth.AddMonths(-1);
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                -- Total Active Users
                DECLARE @TotalUsers INT = (SELECT COUNT(*) FROM Users WHERE IsActive = 1);
                DECLARE @PrevUsers INT = (
                    SELECT COUNT(*) FROM Users 
                    WHERE IsActive = 1 
                    AND RegistrationDate < @PrevMonthStart
                );

                -- Collections This Month
                DECLARE @CurrentCollections INT = (
                    SELECT COUNT(*) FROM Collections 
                    WHERE YEAR(RequestDate) = YEAR(@TargetMonth) 
                    AND MONTH(RequestDate) = MONTH(@TargetMonth)
                );
                DECLARE @PrevCollections INT = (
                    SELECT COUNT(*) FROM Collections 
                    WHERE YEAR(RequestDate) = YEAR(@PrevMonth) 
                    AND MONTH(RequestDate) = MONTH(@PrevMonth)
                );

                -- Total Weight This Month
                DECLARE @CurrentWeight DECIMAL(10,2) = (
                    SELECT ISNULL(SUM(TotalWeight), 0) FROM Collections 
                    WHERE YEAR(RequestDate) = YEAR(@TargetMonth) 
                    AND MONTH(RequestDate) = MONTH(@TargetMonth)
                    AND Status = 'Completed'
                );
                DECLARE @PrevWeight DECIMAL(10,2) = (
                    SELECT ISNULL(SUM(TotalWeight), 0) FROM Collections 
                    WHERE YEAR(RequestDate) = YEAR(@PrevMonth) 
                    AND MONTH(RequestDate) = MONTH(@PrevMonth)
                    AND Status = 'Completed'
                );

                -- Average Bin Fill Rate (Current Month)
                DECLARE @AvgFillRate DECIMAL(5,2) = (
                    SELECT AVG(FillPercentage) 
                    FROM BinFillLevels 
                    WHERE YEAR(RecordedDateTime) = YEAR(@TargetMonth)
                    AND MONTH(RecordedDateTime) = MONTH(@TargetMonth)
                );
                DECLARE @PrevFillRate DECIMAL(5,2) = (
                    SELECT AVG(FillPercentage) 
                    FROM BinFillLevels 
                    WHERE YEAR(RecordedDateTime) = YEAR(@PrevMonth)
                    AND MONTH(RecordedDateTime) = MONTH(@PrevMonth)
                );

                -- Calculate Growth Percentages
                SELECT 
                    @TotalUsers AS TotalUsers,
                    CASE WHEN @PrevUsers > 0 
                        THEN ((@TotalUsers - @PrevUsers) * 100.0 / @PrevUsers) 
                        ELSE 0 END AS UserGrowthPercent,
                    
                    @CurrentCollections AS TotalCollections,
                    CASE WHEN @PrevCollections > 0 
                        THEN ((@CurrentCollections - @PrevCollections) * 100.0 / @PrevCollections) 
                        ELSE 0 END AS CollectionGrowthPercent,
                    
                    @CurrentWeight AS TotalWeightRecycled,
                    CASE WHEN @PrevWeight > 0 
                        THEN ((@CurrentWeight - @PrevWeight) * 100.0 / @PrevWeight) 
                        ELSE 0 END AS WeightGrowthPercent,
                    
                    ISNULL(@AvgFillRate, 0) AS AverageBinFillRate,
                    CASE WHEN @PrevFillRate > 0 
                        THEN ((@AvgFillRate - @PrevFillRate) * 100.0 / @PrevFillRate) 
                        ELSE 0 END AS FillRateChange;
            ";

            var parameters = new 
            { 
                TargetMonth = targetMonth,
                PrevMonth = previousMonth,
                PrevMonthStart = new DateTime(previousMonth.Year, previousMonth.Month, 1)
            };

            return await connection.QuerySingleAsync<DashboardKPIs>(sql, parameters);
        }

        public async Task<List<CollectionTrend>> GetCollectionTrendsAsync(int monthsBack = 6)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT 
                    FORMAT(RequestDate, 'yyyy-MM') AS Month,
                    COUNT(*) AS Collections,
                    ISNULL(SUM(TotalWeight), 0) AS Weight
                FROM Collections
                WHERE RequestDate >= DATEADD(MONTH, -@MonthsBack, GETDATE())
                AND Status = 'Completed'
                GROUP BY YEAR(RequestDate), MONTH(RequestDate), FORMAT(RequestDate, 'MMM')
                ORDER BY YEAR(RequestDate), MONTH(RequestDate);
                ";

            var results = await connection.QueryAsync<CollectionTrend>(sql, new { MonthsBack = monthsBack });
            return results.ToList();
        }

        public async Task<List<CategoryBreakdown>> GetCategoryBreakdownAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                WITH CategoryCounts AS (
                    SELECT 
                        Category,
                        COUNT(*) AS ItemCount
                    FROM Items i
                    JOIN Collections c ON i.CollectionID = c.CollectionID
                    WHERE c.Status = 'Completed'
                    AND c.RequestDate >= DATEADD(MONTH, -1, GETDATE())
                    GROUP BY Category
                ),
                TotalItems AS (
                    SELECT SUM(ItemCount) AS Total FROM CategoryCounts
                )
                SELECT 
                    cc.Category AS Category,
                    CAST((cc.ItemCount * 100.0 / t.Total) AS INT) AS Value,
                    CASE cc.Category
                        WHEN 'Computers' THEN '#3b82f6'
                        WHEN 'Mobile Devices' THEN '#10b981'
                        WHEN 'Home Appliances' THEN '#f59e0b'
                        WHEN 'Accessories' THEN '#8b5cf6'
                        ELSE '#6b7280'
                    END AS Color
                FROM CategoryCounts cc
                CROSS JOIN TotalItems t
                ORDER BY cc.ItemCount DESC;
            ";

            var results = await connection.QueryAsync<CategoryBreakdown>(sql);
            return results.ToList();
        }
        

        public async Task<List<AvgPerformance>> GetAvgPerformanceMetricsAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            
            var sql = @"
                WITH AreaStats AS (
                    SELECT 
                        Area,
                        COUNT(*) AS Collections,
                        COUNT(DISTINCT UserID) AS UniqueUsers
                    FROM Collections
                    WHERE RequestDate >= DATEADD(MONTH, -1, GETDATE())
                    AND Status = 'Completed'
                    GROUP BY Area
                ),
                AreaPopulation AS (
                    SELECT 
                        Area,
                        COUNT(*) AS TotalUsers
                    FROM Users
                    WHERE IsActive = 1
                    GROUP BY Area
                )
                SELECT 
                    a.Area,
                    a.Collections,
                    CAST((a.UniqueUsers * 100.0 / NULLIF(p.TotalUsers, 0)) AS DECIMAL(5,2)) AS Participation
                FROM AreaStats a
                LEFT JOIN AreaPopulation p ON a.Area = p.Area
                ORDER BY a.Collections DESC;
            ";
            var results = await connection.QueryAsync<AvgPerformance>(sql);
            return results.ToList();
        }
    }
}