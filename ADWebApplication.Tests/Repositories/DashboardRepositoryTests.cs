using System;
using System.Linq;
using ADWebApplication.Data;
using ADWebApplication.Data.Repository;
using ADWebApplication.Models;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;

namespace ADWebApplication.Tests.Repositories
{
    public class DashboardRepositoryTests
    {
        private In5niteDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<In5niteDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new In5niteDbContext(options);
        }

        [Fact]
        public async Task GetBinCountsAsync_ShouldReturnCorrectCounts()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.CollectionBins.AddRange(
                new CollectionBin { BinId = 1, BinStatus = "Active", RegionId = 1 },
                new CollectionBin { BinId = 2, BinStatus = "Active", RegionId = 1 },
                new CollectionBin { BinId = 3, BinStatus = "Inactive", RegionId = 1 }
            );
            await context.SaveChangesAsync();

            var repository = new DashboardRepository(context);

            // Act
            var result = await repository.GetBinCountsAsync();

            // Assert
            result.TotalBins.Should().Be(3);
            result.ActiveBins.Should().Be(2);
        }

        [Fact]
        public async Task GetBinCountsAsync_WithNoBins_ShouldReturnZero()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new DashboardRepository(context);

            // Act
            var result = await repository.GetBinCountsAsync();

            // Assert
            result.TotalBins.Should().Be(0);
            result.ActiveBins.Should().Be(0);
        }

        [Fact]
        public async Task GetCollectionTrendsAsync_ShouldAggregateByMonth()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var now = DateTime.UtcNow;
            var log1 = new DisposalLogs { LogId = 100, DisposalTimeStamp = now.AddDays(-10), EstimatedTotalWeight = 2.5 };
            var log2 = new DisposalLogs { LogId = 101, DisposalTimeStamp = now.AddMonths(-1).AddDays(-1), EstimatedTotalWeight = 1.0 };
            context.DisposalLogs.AddRange(log1, log2);
            await context.SaveChangesAsync();

            var repository = new DashboardRepository(context);

            // Act
            var trends = await repository.GetCollectionTrendsAsync(monthsBack: 3);

            // Assert
            // Ensure the aggregate contains the two inserted logs across months
            trends.Should().NotBeNull();
            trends.Select(t => t.Collections).Sum().Should().Be(2);
            foreach (var t in trends)
            {
                t.Month.Should().MatchRegex(@"^\d{4}-\d{2}$");
            }
        }

        [Fact]
        public async Task GetHighRiskUnscheduledCountAsync_ReturnsDistinctCriticalBins()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.FillLevelPredictions.AddRange(
                new FillLevelPrediction { PredictionId = 1, BinId = 10, PredictedStatus = "critical" , PredictedDate = DateTime.UtcNow},
                new FillLevelPrediction { PredictionId = 2, BinId = 10, PredictedStatus = "Critical" , PredictedDate = DateTime.UtcNow},
                new FillLevelPrediction { PredictionId = 3, BinId = 11, PredictedStatus = "critical" , PredictedDate = DateTime.UtcNow},
                new FillLevelPrediction { PredictionId = 4, BinId = 12, PredictedStatus = "normal" , PredictedDate = DateTime.UtcNow}
            );
            await context.SaveChangesAsync();

            var repository = new DashboardRepository(context);

            // Act
            var count = await repository.GetHighRiskUnscheduledCountAsync();

            // Assert
            count.Should().Be(2); // bins 10 and 11
        }

        [Fact]
        public async Task GetCategoryBreakdownAsync_ComputesPercentagesAndColors()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var cat1 = new EWasteCategory { CategoryId = 1, CategoryName = "Phones" };
            var cat2 = new EWasteCategory { CategoryId = 2, CategoryName = "Laptops" };
            context.EWasteCategories.AddRange(cat1, cat2);

            var type1 = new EWasteItemType { ItemTypeId = 1, CategoryId = 1, ItemName = "Phone A" };
            var type2 = new EWasteItemType { ItemTypeId = 2, CategoryId = 2, ItemName = "Laptop A" };
            context.EWasteItemTypes.AddRange(type1, type2);

            var log1 = new DisposalLogs { LogId = 200, DisposalTimeStamp = DateTime.UtcNow, EstimatedTotalWeight = 1.2 };
            var log2 = new DisposalLogs { LogId = 201, DisposalTimeStamp = DateTime.UtcNow, EstimatedTotalWeight = 2.0 };
            context.DisposalLogs.AddRange(log1, log2);

            context.DisposalLogItems.AddRange(
                new DisposalLogItem { LogItemId = 1, LogId = 200, ItemTypeId = 1 },
                new DisposalLogItem { LogItemId = 2, LogId = 201, ItemTypeId = 1 },
                new DisposalLogItem { LogItemId = 3, LogId = 201, ItemTypeId = 2 }
            );

            await context.SaveChangesAsync();

            var repository = new DashboardRepository(context);

            // Act
            var breakdown = await repository.GetCategoryBreakdownAsync();

            // Assert
            breakdown.Should().NotBeNull();
            Assert.True(breakdown.Count >= 2);
            var phones = breakdown.FirstOrDefault(b => b.Category == "Phones");
            var laptops = breakdown.FirstOrDefault(b => b.Category == "Laptops");
            phones.Should().NotBeNull();
            laptops.Should().NotBeNull();
            // Ensure phones has a percentage at least as large as laptops and colors are present
            phones.Value.Should().BeGreaterThanOrEqualTo(laptops.Value);
            phones.Color.Should().NotBeNullOrEmpty();
            laptops.Color.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetAvgPerformanceMetricsAsync_ComputesParticipationAndCollections()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var region = new Region { RegionId = 1, RegionName = "North" };
            context.Regions.Add(region);

            var bin = new CollectionBin { BinId = 500, RegionId = 1 };
            context.CollectionBins.Add(bin);

            var user1 = new PublicUser { Id = 1000, IsActive = true, RegionId = 1 };
            var user2 = new PublicUser { Id = 1001, IsActive = true, RegionId = 1 };
            context.PublicUser.AddRange(user1, user2);

            context.DisposalLogs.AddRange(
                new DisposalLogs { LogId = 300, BinId = 500, UserId = 1000, DisposalTimeStamp = DateTime.UtcNow },
                new DisposalLogs { LogId = 301, BinId = 500, UserId = 1001, DisposalTimeStamp = DateTime.UtcNow },
                new DisposalLogs { LogId = 302, BinId = 500, UserId = 1000, DisposalTimeStamp = DateTime.UtcNow }
            );

            await context.SaveChangesAsync();

            var repository = new DashboardRepository(context);

            // Act
            var metrics = await repository.GetAvgPerformanceMetricsAsync();

            // Assert
            var area = metrics.FirstOrDefault(m => m.Area == "North");
            area.Should().NotBeNull();
            area.Collections.Should().Be(3);
            area.Participation.Should().Be(100m);
        }
    }
}