using ADWebApplication.Models;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace ADWebApplication.Tests.ViewModels
{
    public class CollectionConfirmationVMTests
    {
        [Fact]
        public void CollectionConfirmationVM_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var viewModel = new CollectionConfirmationVM();

            // Assert
            Assert.Equal(0, viewModel.StopId);
            Assert.Null(viewModel.PointId);
            Assert.Equal(string.Empty, viewModel.LocationName);
            Assert.Equal(string.Empty, viewModel.Address);
            Assert.Null(viewModel.BinId);
            Assert.Equal(string.Empty, viewModel.Zone);
            Assert.Equal(0, viewModel.BinFillLevel);
            Assert.Equal("Good", viewModel.BinCondition);
            Assert.False(viewModel.CollectedElectronics);
            Assert.False(viewModel.CollectedBatteries);
            Assert.False(viewModel.CollectedCables);
            Assert.False(viewModel.CollectedAccessories);
            Assert.Null(viewModel.Remarks);
            Assert.Null(viewModel.NextPointId);
            Assert.Null(viewModel.NextLocationName);
            Assert.Null(viewModel.NextAddress);
            Assert.Null(viewModel.NextPlannedTime);
            Assert.Null(viewModel.NextFillLevel);
        }

        [Fact]
        public void CollectionConfirmationVM_AllProperties_CanBeSet()
        {
            // Arrange
            var viewModel = new CollectionConfirmationVM();
            var collectionTime = new DateTime(2026, 2, 7, 10, 30, 0);
            var nextPlannedTime = new DateTime(2026, 2, 7, 11, 0, 0);

            // Act
            viewModel.StopId = 123;
            viewModel.PointId = "P001";
            viewModel.LocationName = "Central Mall";
            viewModel.Address = "123 Main Street";
            viewModel.BinId = "BIN-001";
            viewModel.Zone = "Zone A";
            viewModel.BinFillLevel = 75;
            viewModel.CollectionTime = collectionTime;
            viewModel.BinCondition = "Damaged";
            viewModel.CollectedElectronics = true;
            viewModel.CollectedBatteries = true;
            viewModel.CollectedCables = true;
            viewModel.CollectedAccessories = true;
            viewModel.Remarks = "Heavy load collected";
            viewModel.NextPointId = "P002";
            viewModel.NextLocationName = "West Plaza";
            viewModel.NextAddress = "456 West Ave";
            viewModel.NextPlannedTime = nextPlannedTime;
            viewModel.NextFillLevel = 60;

            // Assert
            Assert.Equal(123, viewModel.StopId);
            Assert.Equal("P001", viewModel.PointId);
            Assert.Equal("Central Mall", viewModel.LocationName);
            Assert.Equal("123 Main Street", viewModel.Address);
            Assert.Equal("BIN-001", viewModel.BinId);
            Assert.Equal("Zone A", viewModel.Zone);
            Assert.Equal(75, viewModel.BinFillLevel);
            Assert.Equal(collectionTime, viewModel.CollectionTime);
            Assert.Equal("Damaged", viewModel.BinCondition);
            Assert.True(viewModel.CollectedElectronics);
            Assert.True(viewModel.CollectedBatteries);
            Assert.True(viewModel.CollectedCables);
            Assert.True(viewModel.CollectedAccessories);
            Assert.Equal("Heavy load collected", viewModel.Remarks);
            Assert.Equal("P002", viewModel.NextPointId);
            Assert.Equal("West Plaza", viewModel.NextLocationName);
            Assert.Equal("456 West Ave", viewModel.NextAddress);
            Assert.Equal(nextPlannedTime, viewModel.NextPlannedTime);
            Assert.Equal(60, viewModel.NextFillLevel);
        }

        [Fact]
        public void BinFillLevel_ValidatesRange_WithinBounds()
        {
            // Arrange
            var viewModel = new CollectionConfirmationVM
            {
                BinFillLevel = 50
            };
            var context = new ValidationContext(viewModel);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(viewModel, context, results, true);

            // Assert - No error for BinFillLevel
            Assert.DoesNotContain(results, r => r.MemberNames.Contains("BinFillLevel"));
        }

        [Fact]
        public void BinFillLevel_ValidatesRange_TooHigh()
        {
            // Arrange
            var viewModel = new CollectionConfirmationVM
            {
                BinFillLevel = 101
            };
            var context = new ValidationContext(viewModel) { MemberName = "BinFillLevel" };
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateProperty(viewModel.BinFillLevel, context, results);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "Fill level must be between 0 and 100");
        }

        [Fact]
        public void BinFillLevel_ValidatesRange_Negative()
        {
            // Arrange
            var viewModel = new CollectionConfirmationVM
            {
                BinFillLevel = -1
            };
            var context = new ValidationContext(viewModel) { MemberName = "BinFillLevel" };
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateProperty(viewModel.BinFillLevel, context, results);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "Fill level must be between 0 and 100");
        }

        [Fact]
        public void BinFillLevel_AcceptsZero()
        {
            // Arrange
            var viewModel = new CollectionConfirmationVM
            {
                BinFillLevel = 0
            };
            var context = new ValidationContext(viewModel) { MemberName = "BinFillLevel" };
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateProperty(viewModel.BinFillLevel, context, results);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void BinFillLevel_Accepts100()
        {
            // Arrange
            var viewModel = new CollectionConfirmationVM
            {
                BinFillLevel = 100
            };
            var context = new ValidationContext(viewModel) { MemberName = "BinFillLevel" };
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateProperty(viewModel.BinFillLevel, context, results);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void CollectionTime_DefaultsToNow()
        {
            // Arrange
            var before = DateTime.Now.AddSeconds(-1);
            
            // Act
            var viewModel = new CollectionConfirmationVM();
            var after = DateTime.Now.AddSeconds(1);

            // Assert
            Assert.InRange(viewModel.CollectionTime, before, after);
        }

        [Fact]
        public void CategoryCheckboxes_CanBeToggled()
        {
            // Arrange
            var viewModel = new CollectionConfirmationVM();

            // Act & Assert - Initially false
            Assert.False(viewModel.CollectedElectronics);
            Assert.False(viewModel.CollectedBatteries);
            Assert.False(viewModel.CollectedCables);
            Assert.False(viewModel.CollectedAccessories);

            // Toggle to true
            viewModel.CollectedElectronics = true;
            viewModel.CollectedBatteries = true;
            viewModel.CollectedCables = true;
            viewModel.CollectedAccessories = true;

            Assert.True(viewModel.CollectedElectronics);
            Assert.True(viewModel.CollectedBatteries);
            Assert.True(viewModel.CollectedCables);
            Assert.True(viewModel.CollectedAccessories);
        }

        [Fact]
        public void NextStopProperties_CanAllBeNull()
        {
            // Arrange & Act
            var viewModel = new CollectionConfirmationVM
            {
                NextPointId = null,
                NextLocationName = null,
                NextAddress = null,
                NextPlannedTime = null,
                NextFillLevel = null
            };

            // Assert
            Assert.Null(viewModel.NextPointId);
            Assert.Null(viewModel.NextLocationName);
            Assert.Null(viewModel.NextAddress);
            Assert.Null(viewModel.NextPlannedTime);
            Assert.Null(viewModel.NextFillLevel);
        }

        [Fact]
        public void OptionalStrings_CanBeNull()
        {
            // Arrange & Act
            var viewModel = new CollectionConfirmationVM
            {
                PointId = null,
                BinId = null,
                Remarks = null
            };

            // Assert
            Assert.Null(viewModel.PointId);
            Assert.Null(viewModel.BinId);
            Assert.Null(viewModel.Remarks);
        }

        [Fact]
        public void BinCondition_CanBeSetToDifferentValues()
        {
            // Arrange
            var viewModel = new CollectionConfirmationVM();

            // Act & Assert - Default
            Assert.Equal("Good", viewModel.BinCondition);

            // Set to different conditions
            viewModel.BinCondition = "Damaged";
            Assert.Equal("Damaged", viewModel.BinCondition);

            viewModel.BinCondition = "Needs Repair";
            Assert.Equal("Needs Repair", viewModel.BinCondition);

            viewModel.BinCondition = "Excellent";
            Assert.Equal("Excellent", viewModel.BinCondition);
        }

        [Fact]
        public void Zone_CanBeSetToEmptyString()
        {
            // Arrange & Act
            var viewModel = new CollectionConfirmationVM
            {
                Zone = ""
            };

            // Assert
            Assert.Equal(string.Empty, viewModel.Zone);
        }

        [Fact]
        public void LocationName_CanBeSetToEmptyString()
        {
            // Arrange & Act
            var viewModel = new CollectionConfirmationVM
            {
                LocationName = ""
            };

            // Assert
            Assert.Equal(string.Empty, viewModel.LocationName);
        }

        [Fact]
        public void Address_CanBeSetToEmptyString()
        {
            // Arrange & Act
            var viewModel = new CollectionConfirmationVM
            {
                Address = ""
            };

            // Assert
            Assert.Equal(string.Empty, viewModel.Address);
        }

        [Fact]
        public void StopId_CanBeSetToPositiveValue()
        {
            // Arrange & Act
            var viewModel = new CollectionConfirmationVM
            {
                StopId = 999
            };

            // Assert
            Assert.Equal(999, viewModel.StopId);
        }

        [Fact]
        public void Remarks_CanContainLongText()
        {
            // Arrange
            var longRemarks = new string('A', 1000);
            var viewModel = new CollectionConfirmationVM
            {
                Remarks = longRemarks
            };

            // Act & Assert
            Assert.Equal(longRemarks, viewModel.Remarks);
            Assert.Equal(1000, viewModel.Remarks.Length);
        }

        [Fact]
        public void NextFillLevel_CanBeSetToValidRange()
        {
            // Arrange & Act
            var viewModel = new CollectionConfirmationVM
            {
                NextFillLevel = 85
            };

            // Assert
            Assert.Equal(85, viewModel.NextFillLevel);
        }

        [Fact]
        public void AllCategoryCheckboxes_CanBeSetIndependently()
        {
            // Arrange
            var viewModel = new CollectionConfirmationVM();

            // Act - Set only Electronics
            viewModel.CollectedElectronics = true;

            // Assert
            Assert.True(viewModel.CollectedElectronics);
            Assert.False(viewModel.CollectedBatteries);
            Assert.False(viewModel.CollectedCables);
            Assert.False(viewModel.CollectedAccessories);

            // Act - Add Cables
            viewModel.CollectedCables = true;

            // Assert
            Assert.True(viewModel.CollectedElectronics);
            Assert.False(viewModel.CollectedBatteries);
            Assert.True(viewModel.CollectedCables);
            Assert.False(viewModel.CollectedAccessories);
        }

        [Fact]
        public void CollectionTime_CanBeSetToSpecificDateTime()
        {
            // Arrange
            var specificTime = new DateTime(2026, 12, 25, 14, 30, 0);
            var viewModel = new CollectionConfirmationVM();

            // Act
            viewModel.CollectionTime = specificTime;

            // Assert
            Assert.Equal(specificTime, viewModel.CollectionTime);
        }

        [Fact]
        public void NextPlannedTime_CanBeSetToFutureDate()
        {
            // Arrange
            var futureTime = new DateTime(2027, 1, 1, 9, 0, 0);
            var viewModel = new CollectionConfirmationVM();

            // Act
            viewModel.NextPlannedTime = futureTime;

            // Assert
            Assert.Equal(futureTime, viewModel.NextPlannedTime);
        }

        [Fact]
        public void PointId_CanBeSetToAlphanumericValue()
        {
            // Arrange & Act
            var viewModel = new CollectionConfirmationVM
            {
                PointId = "ABC-123-XYZ"
            };

            // Assert
            Assert.Equal("ABC-123-XYZ", viewModel.PointId);
        }

        [Fact]
        public void BinId_CanBeSetToAlphanumericValue()
        {
            // Arrange & Act
            var viewModel = new CollectionConfirmationVM
            {
                BinId = "BIN-2026-001"
            };

            // Assert
            Assert.Equal("BIN-2026-001", viewModel.BinId);
        }
    }
}
