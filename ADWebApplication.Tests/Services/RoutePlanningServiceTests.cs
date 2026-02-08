using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ADWebApplication.Data;
using ADWebApplication.Services;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Models;

namespace ADWebApplication.Tests.Services
{
    public class RoutePlanningServiceTests
{
    private readonly In5niteDbContext _dbContext;
    private readonly Mock<IBinPredictionService> _mockPredictionService;
    private readonly RoutePlanningService _service;

    public RoutePlanningServiceTests()
    {
        // 1. Setup InMemory Database
        var options = new DbContextOptionsBuilder<In5niteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new In5niteDbContext(options);

        // 2. Setup Mock Service
        _mockPredictionService = new Mock<IBinPredictionService>();

        _service = new RoutePlanningService(_dbContext, _mockPredictionService.Object);
    }

    [Fact]
    public async Task PlanRouteAsync_ReturnsEmpty_WhenNoActiveBinsExist()
    {
        // Arrange: Database is empty of active bins
        _mockPredictionService.Setup(s => s.GetBinPrioritiesAsync())
            .ReturnsAsync(new List<BinPriorityDto>());

        // Act
        var result = await _service.PlanRouteAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PlanRouteAsync_ProcessesHighPriorityBinsOnly()
    {
        // Arrange: Seed 2 bins, one high priority (DaysTo80 <= 1), one low
        _dbContext.CollectionBins.AddRange(new List<CollectionBin>
        {
            new CollectionBin { 
                BinId = 1, 
                BinStatus = "Active", 
                Latitude = 1.35, 
                Longitude = 103.82 
            },

            new CollectionBin { 
                BinId = 2, 
                BinStatus = "Active", 
                Latitude = 1.36, 
                Longitude = 103.83 
            }
        });
        await _dbContext.SaveChangesAsync();

        _mockPredictionService.Setup(s => s.GetBinPrioritiesAsync())
            .ReturnsAsync(new List<BinPriorityDto>
            {
                new BinPriorityDto { BinId = 1, DaysTo80 = 0 }, // High Priority
                new BinPriorityDto { BinId = 2, DaysTo80 = 5 }  // Low Priority
            });

        // Act
        var result = await _service.PlanRouteAsync();

        // Assert
        // Result should only include Bin 1 because Bin 2 isn't High Priority 
        // and the code filters for (BinId == 0 || IsHighPriority)
        result.Should().ContainSingle(b => b.BinId == 1);
        result.Should().NotContain(b => b.BinId == 2);
    }

    [Fact]
    public void CalculateDistance_ReturnsCorrectDistanceInKm()
    {
        // Arrange: Singapore coordinates
        double lat1 = 1.290270; double lon1 = 103.851959; // Marina Bay
        double lat2 = 1.352083; double lon2 = 103.819836; // Central Area

        // Act
        var distance = RoutePlanningService.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert: Roughly 7-8km
        distance.Should().BeInRange(7, 9);
    }

    [Fact]
    public async Task PlanRouteAsync_ReturnsEmpty_WhenNoBinsAreHighPriority()
    {
        // Arrange: Seed one bin with low priority
        _dbContext.CollectionBins.Add(new CollectionBin { BinId = 1, BinStatus = "Active", Latitude = 1.3, Longitude = 103.8 });
        await _dbContext.SaveChangesAsync();

        _mockPredictionService.Setup(s => s.GetBinPrioritiesAsync())
            .ReturnsAsync(new List<BinPriorityDto> { new BinPriorityDto { BinId = 1, DaysTo80 = 5 } });

        // Act
        var result = await _service.PlanRouteAsync();

        // Assert: Code returns empty list if count <= 1 (including depot)
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PlanRouteAsync_AssignsRoutesAndStopNumbers_WhenMultipleBinsExist()
    {
        // Arrange: Seed 4 high-priority bins to force route distribution
        for (int i = 1; i <= 4; i++) {
            _dbContext.CollectionBins.Add(new CollectionBin { 
                BinId = i, BinStatus = "Active", Latitude = 1.35 + (i * 0.01), Longitude = 103.82 + (i * 0.01) 
            });
        }
        await _dbContext.SaveChangesAsync();

        _mockPredictionService.Setup(s => s.GetBinPrioritiesAsync())
            .ReturnsAsync(_dbContext.CollectionBins.Select(b => new BinPriorityDto { BinId = b.BinId, DaysTo80 = 0 }).ToList());

        // Act
        var result = await _service.PlanRouteAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.All(b => b.AssignedCO > 0).Should().BeTrue(); // Verify officers (1, 2, or 3) are assigned
        result.All(b => b.StopNumber > 0).Should().BeTrue(); // Verify stop sequence is generated
    }
    }
}