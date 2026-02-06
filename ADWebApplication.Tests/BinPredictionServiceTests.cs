using Xunit;
using Moq;
using Moq.Protected;
using ADWebApplication.Services;
using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Models.ViewModels.BinPredictions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ADWebApplication.Tests
{
    public class BinPredictionServiceTests
    {
        private In5niteDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<In5niteDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new In5niteDbContext(options);
        }

        private Mock<HttpMessageHandler> CreateMockHttpMessageHandler(HttpStatusCode statusCode, object responseContent)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            var responseJson = JsonSerializer.Serialize(responseContent);

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
                });

            return mockHandler;
        }

        private HttpClient CreateMockHttpClient(Mock<HttpMessageHandler> mockHandler)
        {
            return new HttpClient(mockHandler.Object)
            {
                BaseAddress = new Uri("http://localhost:5000")
            };
        }

        #region BuildBinPredictionsPageAsync Tests

        [Fact]
        public async Task BuildBinPredictionsPageAsync_ReturnsCorrectViewModel_WithValidData()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, new MLPredictionResponseDto());
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            var region = new Region { RegionId = 1, RegionName = "North" };
            var bin = new CollectionBin { BinId = 1, BinStatus = "Active", RegionId = 1, Region = region };

            dbContext.Regions.Add(region);
            dbContext.CollectionBins.Add(bin);
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 1,
                BinId = 1,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-5),
                BinFillLevel = 20
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 1,
                BinId = 1,
                PredictedAvgDailyGrowth = 10.0,
                PredictedDate = DateTime.UtcNow.AddDays(-4),
                ModelVersion = "v1"
            });
            await dbContext.SaveChangesAsync();

            // Act
            var result = await service.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "All", "All");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalBins);
            Assert.Single(result.Rows);
            Assert.Equal(1, result.Rows[0].BinId);
        }

        [Fact]
        public async Task BuildBinPredictionsPageAsync_FiltersHighRiskBins_Correctly()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, new MLPredictionResponseDto());
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            var region = new Region { RegionId = 1, RegionName = "North" };

            // High risk bin (days to threshold <= 1)
            var bin1 = new CollectionBin { BinId = 1, BinStatus = "Active", RegionId = 1, Region = region };
            dbContext.CollectionBins.Add(bin1);
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 1,
                BinId = 1,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-7),
                BinFillLevel = 20
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 1,
                BinId = 1,
                PredictedAvgDailyGrowth = 11.0, // Will reach 77% in 7 days, about 0.27 days to 80%
                PredictedDate = DateTime.UtcNow.AddDays(-6),
                ModelVersion = "v1"
            });

            // Low risk bin
            var bin2 = new CollectionBin { BinId = 2, BinStatus = "Active", RegionId = 1, Region = region };
            dbContext.CollectionBins.Add(bin2);
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 2,
                BinId = 2,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-2),
                BinFillLevel = 20
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 2,
                BinId = 2,
                PredictedAvgDailyGrowth = 5.0, // Only 10% filled after 2 days
                PredictedDate = DateTime.UtcNow.AddDays(-1),
                ModelVersion = "v1"
            });

            await dbContext.SaveChangesAsync();

            // Act
            var result = await service.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "High", "All");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Rows);
            Assert.Equal("High", result.Rows[0].RiskLevel);
        }

        [Fact]
        public async Task BuildBinPredictionsPageAsync_FiltersBy3DayTimeframe_Correctly()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, new MLPredictionResponseDto());
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            var region = new Region { RegionId = 1, RegionName = "North" };

            // Bin reaching threshold in 2 days
            var bin1 = new CollectionBin { BinId = 1, BinStatus = "Active", RegionId = 1, Region = region };
            dbContext.CollectionBins.Add(bin1);
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 1,
                BinId = 1,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-5),
                BinFillLevel = 20
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 1,
                BinId = 1,
                PredictedAvgDailyGrowth = 11.0,
                PredictedDate = DateTime.UtcNow.AddDays(-4),
                ModelVersion = "v1"
            });

            // Bin reaching threshold in 10 days
            var bin2 = new CollectionBin { BinId = 2, BinStatus = "Active", RegionId = 1, Region = region };
            dbContext.CollectionBins.Add(bin2);
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 2,
                BinId = 2,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-2),
                BinFillLevel = 20
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 2,
                BinId = 2,
                PredictedAvgDailyGrowth = 5.0,
                PredictedDate = DateTime.UtcNow.AddDays(-1),
                ModelVersion = "v1"
            });

            await dbContext.SaveChangesAsync();

            // Act
            var result = await service.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "All", "3");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Rows.All(r => r.EstimatedDaysToThreshold <= 3));
        }

        [Fact]
        public async Task BuildBinPredictionsPageAsync_SortsBy_EstimatedFill_Descending()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, new MLPredictionResponseDto());
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            var region = new Region { RegionId = 1, RegionName = "North" };

            // Bin with high fill
            var bin1 = new CollectionBin { BinId = 1, BinStatus = "Active", RegionId = 1, Region = region };
            dbContext.CollectionBins.Add(bin1);
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 1,
                BinId = 1,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-5),
                BinFillLevel = 20
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 1,
                BinId = 1,
                PredictedAvgDailyGrowth = 12.0,
                PredictedDate = DateTime.UtcNow.AddDays(-4),
                ModelVersion = "v1"
            });

            // Bin with low fill
            var bin2 = new CollectionBin { BinId = 2, BinStatus = "Active", RegionId = 1, Region = region };
            dbContext.CollectionBins.Add(bin2);
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 2,
                BinId = 2,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-2),
                BinFillLevel = 20
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 2,
                BinId = 2,
                PredictedAvgDailyGrowth = 5.0,
                PredictedDate = DateTime.UtcNow.AddDays(-1),
                ModelVersion = "v1"
            });

            await dbContext.SaveChangesAsync();

            // Act
            var result = await service.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "All", "All");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Rows.Count);
            Assert.True(result.Rows[0].EstimatedFillToday >= result.Rows[1].EstimatedFillToday);
        }

        [Fact]
        public async Task BuildBinPredictionsPageAsync_PaginatesResults_Correctly()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, new MLPredictionResponseDto());
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            var region = new Region { RegionId = 1, RegionName = "North" };

            // Create 15 bins (pageSize is 10)
            for (int i = 1; i <= 15; i++)
            {
                var bin = new CollectionBin { BinId = i, BinStatus = "Active", RegionId = 1, Region = region };
                dbContext.CollectionBins.Add(bin);
                dbContext.CollectionDetails.Add(new CollectionDetails
                {
                    CollectionId = i,
                    BinId = i,
                    CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-5),
                    BinFillLevel = 20
                });
                dbContext.FillLevelPredictions.Add(new FillLevelPrediction
                {
                    PredictionId = i,
                    BinId = i,
                    PredictedAvgDailyGrowth = 10.0,
                    PredictedDate = DateTime.UtcNow.AddDays(-4),
                    ModelVersion = "v1"
                });
            }
            await dbContext.SaveChangesAsync();

            // Act - Get page 1
            var page1Result = await service.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "All", "All");
            var page2Result = await service.BuildBinPredictionsPageAsync(2, "EstimatedFill", "desc", "All", "All");

            // Assert
            Assert.Equal(10, page1Result.Rows.Count);
            Assert.Equal(5, page2Result.Rows.Count);
            Assert.Equal(1, page1Result.CurrentPage);
            Assert.Equal(2, page2Result.CurrentPage);
            Assert.Equal(2, page1Result.TotalPages);
        }

        [Fact]
        public async Task BuildBinPredictionsPageAsync_DetectsNewCycle_WhenPredictionIsOlderThanCollection()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, new MLPredictionResponseDto());
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            var region = new Region { RegionId = 1, RegionName = "North" };
            var bin = new CollectionBin { BinId = 1, BinStatus = "Active", RegionId = 1, Region = region };

            dbContext.CollectionBins.Add(bin);
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 1,
                BinId = 1,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-1), // Recent collection
                BinFillLevel = 25
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 1,
                BinId = 1,
                PredictedAvgDailyGrowth = 10.0,
                PredictedDate = DateTime.UtcNow.AddDays(-5), // Old prediction (before collection)
                ModelVersion = "v1"
            });
            await dbContext.SaveChangesAsync();

            // Act
            var result = await service.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "All", "All");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.NewCycleDetectedCount);
            Assert.True(result.Rows[0].NeedsPredictionRefresh);
            Assert.Equal("Collection done", result.Rows[0].PlanningStatus);
        }

        [Fact]
        public async Task BuildBinPredictionsPageAsync_CalculatesHighRiskUnscheduledCount_Correctly()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, new MLPredictionResponseDto());
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            var region = new Region { RegionId = 1, RegionName = "North" };

            // High risk unscheduled bin
            var bin1 = new CollectionBin { BinId = 1, BinStatus = "Active", RegionId = 1, Region = region };
            dbContext.CollectionBins.Add(bin1);
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 1,
                BinId = 1,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-7),
                BinFillLevel = 20
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 1,
                BinId = 1,
                PredictedAvgDailyGrowth = 11.0, // High risk
                PredictedDate = DateTime.UtcNow.AddDays(-6),
                ModelVersion = "v1"
            });

            await dbContext.SaveChangesAsync();

            // Act
            var result = await service.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "All", "All");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.HighRiskUnscheduledCount > 0);
        }

        [Fact]
        public async Task BuildBinPredictionsPageAsync_IgnoresInactiveBins()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, new MLPredictionResponseDto());
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            var region = new Region { RegionId = 1, RegionName = "North" };

            // Active bin
            var activeBin = new CollectionBin { BinId = 1, BinStatus = "Active", RegionId = 1, Region = region };
            dbContext.CollectionBins.Add(activeBin);

            // Inactive bin
            var inactiveBin = new CollectionBin { BinId = 2, BinStatus = "Inactive", RegionId = 1, Region = region };
            dbContext.CollectionBins.Add(inactiveBin);

            dbContext.CollectionDetails.AddRange(
                new CollectionDetails
                {
                    CollectionId = 1,
                    BinId = 1,
                    CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-5),
                    BinFillLevel = 20
                },
                new CollectionDetails
                {
                    CollectionId = 2,
                    BinId = 2,
                    CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-5),
                    BinFillLevel = 20
                }
            );
            dbContext.FillLevelPredictions.AddRange(
                new FillLevelPrediction
                {
                    PredictionId = 1,
                    BinId = 1,
                    PredictedAvgDailyGrowth = 10.0,
                    PredictedDate = DateTime.UtcNow.AddDays(-4),
                    ModelVersion = "v1"
                },
                new FillLevelPrediction
                {
                    PredictionId = 2,
                    BinId = 2,
                    PredictedAvgDailyGrowth = 10.0,
                    PredictedDate = DateTime.UtcNow.AddDays(-4),
                    ModelVersion = "v1"
                }
            );
            await dbContext.SaveChangesAsync();

            // Act
            var result = await service.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "All", "All");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalBins); // Only active bin counted
            Assert.Single(result.Rows);
            Assert.Equal(1, result.Rows[0].BinId);
        }

        [Fact]
        public async Task BuildBinPredictionsPageAsync_CalculatesAvgDailyFillGrowth_Correctly()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, new MLPredictionResponseDto());
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            var region = new Region { RegionId = 1, RegionName = "North" };

            // Bin with growth rate 10
            var bin1 = new CollectionBin { BinId = 1, BinStatus = "Active", RegionId = 1, Region = region };
            dbContext.CollectionBins.Add(bin1);
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 1,
                BinId = 1,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-5),
                BinFillLevel = 20
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 1,
                BinId = 1,
                PredictedAvgDailyGrowth = 10.0,
                PredictedDate = DateTime.UtcNow.AddDays(-4),
                ModelVersion = "v1"
            });

            // Bin with growth rate 20
            var bin2 = new CollectionBin { BinId = 2, BinStatus = "Active", RegionId = 1, Region = region };
            dbContext.CollectionBins.Add(bin2);
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 2,
                BinId = 2,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-5),
                BinFillLevel = 20
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 2,
                BinId = 2,
                PredictedAvgDailyGrowth = 20.0,
                PredictedDate = DateTime.UtcNow.AddDays(-4),
                ModelVersion = "v1"
            });

            await dbContext.SaveChangesAsync();

            // Act
            var result = await service.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "All", "All");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(15.0, result.AvgDailyFillGrowthOverall); // Average of 10 and 20
        }

        #endregion

        #region RefreshPredictionsForNewCyclesAsync Tests

        [Fact]
        public async Task RefreshPredictionsForNewCyclesAsync_RefreshesPredictions_WhenNewCycleDetected()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mlResponse = new MLPredictionResponseDto { predicted_next_avg_daily_growth = 12.5 };
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, mlResponse);
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            // Add two collection records (old and new)
            dbContext.CollectionDetails.AddRange(
                new CollectionDetails
                {
                    CollectionId = 1,
                    BinId = 1,
                    CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-10),
                    BinFillLevel = 80
                },
                new CollectionDetails
                {
                    CollectionId = 2,
                    BinId = 1,
                    CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-1), // Recent collection
                    BinFillLevel = 25
                }
            );

            // Old prediction (before recent collection)
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 1,
                BinId = 1,
                PredictedAvgDailyGrowth = 8.0,
                PredictedDate = DateTime.UtcNow.AddDays(-5),
                ModelVersion = "v1"
            });

            await dbContext.SaveChangesAsync();

            // Act
            var refreshedCount = await service.RefreshPredictionsForNewCyclesAsync();

            // Assert
            Assert.Equal(1, refreshedCount);
            var predictions = await dbContext.FillLevelPredictions.Where(p => p.BinId == 1).ToListAsync();
            Assert.Equal(2, predictions.Count); // Old + new prediction
            var newPrediction = predictions.OrderByDescending(p => p.PredictedDate).First();
            Assert.Equal(12.5, newPrediction.PredictedAvgDailyGrowth);
        }

        [Fact]
        public async Task RefreshPredictionsForNewCyclesAsync_DoesNotRefresh_WhenPredictionIsUpToDate()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mlResponse = new MLPredictionResponseDto { predicted_next_avg_daily_growth = 12.5 };
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, mlResponse);
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            var collectionTime = DateTimeOffset.UtcNow.AddDays(-5);
            dbContext.CollectionDetails.AddRange(
                new CollectionDetails
                {
                    CollectionId = 1,
                    BinId = 1,
                    CurrentCollectionDateTime = collectionTime.AddDays(-10),
                    BinFillLevel = 80
                },
                new CollectionDetails
                {
                    CollectionId = 2,
                    BinId = 1,
                    CurrentCollectionDateTime = collectionTime,
                    BinFillLevel = 25
                }
            );

            // Recent prediction (after collection)
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 1,
                BinId = 1,
                PredictedAvgDailyGrowth = 8.0,
                PredictedDate = collectionTime.AddDays(1).UtcDateTime, // After collection
                ModelVersion = "v1"
            });

            await dbContext.SaveChangesAsync();

            // Act
            var refreshedCount = await service.RefreshPredictionsForNewCyclesAsync();

            // Assert
            Assert.Equal(0, refreshedCount);
            var predictions = await dbContext.FillLevelPredictions.Where(p => p.BinId == 1).ToListAsync();
            Assert.Single(predictions); // Only original prediction
        }

        [Fact]
        public async Task RefreshPredictionsForNewCyclesAsync_SkipsBins_WithLessThanTwoCollections()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mlResponse = new MLPredictionResponseDto { predicted_next_avg_daily_growth = 12.5 };
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, mlResponse);
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            // Only one collection record
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 1,
                BinId = 1,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-1),
                BinFillLevel = 25
            });

            await dbContext.SaveChangesAsync();

            // Act
            var refreshedCount = await service.RefreshPredictionsForNewCyclesAsync();

            // Assert
            Assert.Equal(0, refreshedCount);
        }

        [Fact]
        public async Task RefreshPredictionsForNewCyclesAsync_CalculatesCycleDuration_Correctly()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mlResponse = new MLPredictionResponseDto { predicted_next_avg_daily_growth = 12.5 };
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, mlResponse);
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            var olderCollectionDate = DateTimeOffset.UtcNow.AddDays(-15);
            var recentCollectionDate = DateTimeOffset.UtcNow.AddDays(-1);

            dbContext.CollectionDetails.AddRange(
                new CollectionDetails
                {
                    CollectionId = 1,
                    BinId = 1,
                    CurrentCollectionDateTime = olderCollectionDate,
                    BinFillLevel = 80
                },
                new CollectionDetails
                {
                    CollectionId = 2,
                    BinId = 1,
                    CurrentCollectionDateTime = recentCollectionDate,
                    BinFillLevel = 25
                }
            );

            await dbContext.SaveChangesAsync();

            // Act
            var refreshedCount = await service.RefreshPredictionsForNewCyclesAsync();

            // Assert
            Assert.Equal(1, refreshedCount);
            
            // Verify the ML API was called with correct cycle duration (should be ~14 days)
            mockHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri!.ToString().Contains("/predict")),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        #endregion

        #region GetBinPrioritiesAsync Tests

        [Fact]
        public async Task GetBinPrioritiesAsync_ReturnsHighPriorityBins_Correctly()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, new MLPredictionResponseDto());
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            // High priority bin (will reach 80% in less than 1 day)
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 1,
                BinId = 1,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-7),
                BinFillLevel = 20
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 1,
                BinId = 1,
                PredictedAvgDailyGrowth = 11.0, // 77% filled, less than 1 day to 80%
                PredictedDate = DateTime.UtcNow.AddDays(-6),
                ModelVersion = "v1"
            });

            // Low priority bin
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 2,
                BinId = 2,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-2),
                BinFillLevel = 20
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 2,
                BinId = 2,
                PredictedAvgDailyGrowth = 5.0, // Only 10% filled, many days to 80%
                PredictedDate = DateTime.UtcNow.AddDays(-1),
                ModelVersion = "v1"
            });

            await dbContext.SaveChangesAsync();

            // Act
            var result = await service.GetBinPrioritiesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            
            var highPriorityBin = result.FirstOrDefault(b => b.BinId == 1);
            Assert.NotNull(highPriorityBin);
            Assert.True(highPriorityBin!.IsHighPriority);
            
            var lowPriorityBin = result.FirstOrDefault(b => b.BinId == 2);
            Assert.NotNull(lowPriorityBin);
            Assert.False(lowPriorityBin!.IsHighPriority);
        }

        [Fact]
        public async Task GetBinPrioritiesAsync_SkipsBins_WithNoPrediction()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, new MLPredictionResponseDto());
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            // Bin with collection but no prediction
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 1,
                BinId = 1,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-5),
                BinFillLevel = 20
            });

            await dbContext.SaveChangesAsync();

            // Act
            var result = await service.GetBinPrioritiesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetBinPrioritiesAsync_SkipsBins_WithZeroOrNegativeGrowthRate()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, new MLPredictionResponseDto());
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            // Bin with zero growth rate
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 1,
                BinId = 1,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-5),
                BinFillLevel = 20
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 1,
                BinId = 1,
                PredictedAvgDailyGrowth = 0.0,
                PredictedDate = DateTime.UtcNow.AddDays(-4),
                ModelVersion = "v1"
            });

            // Bin with negative growth rate
            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 2,
                BinId = 2,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-5),
                BinFillLevel = 20
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 2,
                BinId = 2,
                PredictedAvgDailyGrowth = -5.0,
                PredictedDate = DateTime.UtcNow.AddDays(-4),
                ModelVersion = "v1"
            });

            await dbContext.SaveChangesAsync();

            // Act
            var result = await service.GetBinPrioritiesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetBinPrioritiesAsync_CalculatesDaysTo80_Correctly()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, new MLPredictionResponseDto());
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 1,
                BinId = 1,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-5),
                BinFillLevel = 20
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 1,
                BinId = 1,
                PredictedAvgDailyGrowth = 10.0, // 50% filled after 5 days, 3 more days to 80%
                PredictedDate = DateTime.UtcNow.AddDays(-4),
                ModelVersion = "v1"
            });

            await dbContext.SaveChangesAsync();

            // Act
            var result = await service.GetBinPrioritiesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].BinId);
            Assert.Equal(3, result[0].DaysTo80); // (80 - 50) / 10 = 3
        }

        [Fact]
        public async Task GetBinPrioritiesAsync_Returns0DaysTo80_WhenAlreadyAboveThreshold()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, new MLPredictionResponseDto());
            var httpClient = CreateMockHttpClient(mockHandler);
            var service = new BinPredictionService(httpClient, dbContext);

            dbContext.CollectionDetails.Add(new CollectionDetails
            {
                CollectionId = 1,
                BinId = 1,
                CurrentCollectionDateTime = DateTimeOffset.UtcNow.AddDays(-10),
                BinFillLevel = 20
            });
            dbContext.FillLevelPredictions.Add(new FillLevelPrediction
            {
                PredictionId = 1,
                BinId = 1,
                PredictedAvgDailyGrowth = 10.0, // 100% filled after 10 days
                PredictedDate = DateTime.UtcNow.AddDays(-9),
                ModelVersion = "v1"
            });

            await dbContext.SaveChangesAsync();

            // Act
            var result = await service.GetBinPrioritiesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(0, result[0].DaysTo80);
            Assert.True(result[0].IsHighPriority);
        }

        #endregion
    }
}


