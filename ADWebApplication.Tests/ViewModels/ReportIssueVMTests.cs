using ADWebApplication.Models;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace ADWebApplication.Tests.ViewModels
{
    public class ReportIssueVMTests
    {
        [Fact]
        public void ReportIssueVM_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var viewModel = new ReportIssueVM();

            // Assert
            Assert.Equal(0, viewModel.BinId);
            Assert.Equal(string.Empty, viewModel.LocationName);
            Assert.Equal(string.Empty, viewModel.Region);
            Assert.Equal(string.Empty, viewModel.IssueType);
            Assert.Equal(string.Empty, viewModel.Severity);
            Assert.Equal(string.Empty, viewModel.Description);
            Assert.Empty(viewModel.AvailableBins);
            Assert.Empty(viewModel.Issues);
            Assert.Equal(0, viewModel.TotalIssues);
            Assert.Equal(0, viewModel.OpenIssues);
            Assert.Equal(0, viewModel.InProgressIssues);
            Assert.Equal(0, viewModel.ResolvedIssues);
            Assert.Null(viewModel.Search);
            Assert.Null(viewModel.StatusFilter);
            Assert.Null(viewModel.PriorityFilter);
        }

        [Fact]
        public void ReportIssueVM_AllProperties_CanBeSet()
        {
            // Arrange
            var viewModel = new ReportIssueVM();

            // Act
            viewModel.BinId = 123;
            viewModel.LocationName = "Central Mall";
            viewModel.Region = "Zone A";
            viewModel.IssueType = "Overflow";
            viewModel.Severity = "High";
            viewModel.Description = "Bin is overflowing with waste";
            viewModel.TotalIssues = 10;
            viewModel.OpenIssues = 5;
            viewModel.InProgressIssues = 3;
            viewModel.ResolvedIssues = 2;
            viewModel.Search = "overflow";
            viewModel.StatusFilter = "Open";
            viewModel.PriorityFilter = "High";

            // Assert
            Assert.Equal(123, viewModel.BinId);
            Assert.Equal("Central Mall", viewModel.LocationName);
            Assert.Equal("Zone A", viewModel.Region);
            Assert.Equal("Overflow", viewModel.IssueType);
            Assert.Equal("High", viewModel.Severity);
            Assert.Equal("Bin is overflowing with waste", viewModel.Description);
            Assert.Equal(10, viewModel.TotalIssues);
            Assert.Equal(5, viewModel.OpenIssues);
            Assert.Equal(3, viewModel.InProgressIssues);
            Assert.Equal(2, viewModel.ResolvedIssues);
            Assert.Equal("overflow", viewModel.Search);
            Assert.Equal("Open", viewModel.StatusFilter);
            Assert.Equal("High", viewModel.PriorityFilter);
        }

        [Fact]
        public void BinId_AcceptsZero()
        {
            // Arrange & Act
            var viewModel = new ReportIssueVM
            {
                BinId = 0
            };

            // Assert - BinId is an int, so 0 is a valid value even with [Required]
            Assert.Equal(0, viewModel.BinId);
        }

        [Fact]
        public void IssueType_Required_FailsValidation_WhenEmpty()
        {
            // Arrange
            var viewModel = new ReportIssueVM
            {
                BinId = 1,
                IssueType = "",
                Severity = "High",
                Description = "Test description that is long enough"
            };
            var context = new ValidationContext(viewModel);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(viewModel, context, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("IssueType") && r.ErrorMessage == "Please select an issue type");
        }

        [Fact]
        public void Severity_Required_FailsValidation_WhenEmpty()
        {
            // Arrange
            var viewModel = new ReportIssueVM
            {
                BinId = 1,
                IssueType = "Overflow",
                Severity = "",
                Description = "Test description that is long enough"
            };
            var context = new ValidationContext(viewModel);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(viewModel, context, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("Severity") && r.ErrorMessage == "Please select severity level");
        }

        [Fact]
        public void Description_Required_FailsValidation_WhenEmpty()
        {
            // Arrange
            var viewModel = new ReportIssueVM
            {
                BinId = 1,
                IssueType = "Overflow",
                Severity = "High",
                Description = ""
            };
            var context = new ValidationContext(viewModel);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(viewModel, context, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("Description"));
        }

        [Fact]
        public void Description_MinLength_FailsValidation_WhenTooShort()
        {
            // Arrange
            var viewModel = new ReportIssueVM
            {
                BinId = 1,
                IssueType = "Overflow",
                Severity = "High",
                Description = "Short"
            };
            var context = new ValidationContext(viewModel);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(viewModel, context, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("Description") && r.ErrorMessage == "Description must be at least 10 characters");
        }

        [Fact]
        public void ReportIssueVM_PassesValidation_WithValidData()
        {
            // Arrange
            var viewModel = new ReportIssueVM
            {
                BinId = 123,
                IssueType = "Overflow",
                Severity = "High",
                Description = "This is a valid description with more than 10 characters"
            };
            var context = new ValidationContext(viewModel);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(viewModel, context, results, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Fact]
        public void AvailableBins_CanBePopulated()
        {
            // Arrange
            var viewModel = new ReportIssueVM
            {
                AvailableBins = new List<BinOption>
                {
                    new BinOption { BinId = 1, LocationName = "Location 1", Region = "Zone A" },
                    new BinOption { BinId = 2, LocationName = "Location 2", Region = "Zone B" }
                }
            };

            // Act & Assert
            Assert.Equal(2, viewModel.AvailableBins.Count);
            Assert.Equal(1, viewModel.AvailableBins[0].BinId);
            Assert.Equal("Location 1", viewModel.AvailableBins[0].LocationName);
        }

        [Fact]
        public void Issues_CanBePopulated()
        {
            // Arrange
            var viewModel = new ReportIssueVM
            {
                Issues = new List<IssueLogItem>
                {
                    new IssueLogItem { BinId = 1, IssueType = "Overflow" },
                    new IssueLogItem { BinId = 2, IssueType = "Damaged" }
                }
            };

            // Act & Assert
            Assert.Equal(2, viewModel.Issues.Count);
            Assert.Equal("Overflow", viewModel.Issues[0].IssueType);
            Assert.Equal("Damaged", viewModel.Issues[1].IssueType);
        }
    }

    public class BinOptionTests
    {
        [Fact]
        public void BinOption_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var binOption = new BinOption();

            // Assert
            Assert.Equal(0, binOption.BinId);
            Assert.Equal(string.Empty, binOption.LocationName);
            Assert.Equal(string.Empty, binOption.Region);
        }

        [Fact]
        public void DisplayText_FormatsCorrectly()
        {
            // Arrange
            var binOption = new BinOption
            {
                BinId = 123,
                LocationName = "Central Mall",
                Region = "Zone A"
            };

            // Act
            var displayText = binOption.DisplayText;

            // Assert
            Assert.Equal("Bin #123 - Central Mall", displayText);
        }

        [Fact]
        public void DisplayText_FormatsCorrectly_WithEmptyLocationName()
        {
            // Arrange
            var binOption = new BinOption
            {
                BinId = 456,
                LocationName = "",
                Region = "Zone B"
            };

            // Act
            var displayText = binOption.DisplayText;

            // Assert
            Assert.Equal("Bin #456 - ", displayText);
        }
    }

    public class IssueLogItemTests
    {
        [Fact]
        public void IssueLogItem_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var issueLogItem = new IssueLogItem();

            // Assert
            Assert.Equal(0, issueLogItem.StopId);
            Assert.Equal(0, issueLogItem.BinId);
            Assert.Equal(string.Empty, issueLogItem.LocationName);
            Assert.Equal(string.Empty, issueLogItem.Address);
            Assert.Equal("Other", issueLogItem.IssueType);
            Assert.Equal("Medium", issueLogItem.Severity);
            Assert.Equal("Open", issueLogItem.Status);
            Assert.Equal(string.Empty, issueLogItem.Description);
            Assert.Equal(string.Empty, issueLogItem.ReportedBy);
        }

        [Fact]
        public void IssueLogItem_AllProperties_CanBeSet()
        {
            // Arrange
            var reportedAt = new DateTime(2026, 2, 7, 10, 30, 0);
            var issueLogItem = new IssueLogItem();

            // Act
            issueLogItem.StopId = 101;
            issueLogItem.BinId = 202;
            issueLogItem.LocationName = "Central Mall";
            issueLogItem.Address = "123 Main St";
            issueLogItem.IssueType = "Overflow";
            issueLogItem.Severity = "High";
            issueLogItem.Status = "Resolved";
            issueLogItem.Description = "Bin overflowing";
            issueLogItem.ReportedBy = "collector1";
            issueLogItem.ReportedAt = reportedAt;

            // Assert
            Assert.Equal(101, issueLogItem.StopId);
            Assert.Equal(202, issueLogItem.BinId);
            Assert.Equal("Central Mall", issueLogItem.LocationName);
            Assert.Equal("123 Main St", issueLogItem.Address);
            Assert.Equal("Overflow", issueLogItem.IssueType);
            Assert.Equal("High", issueLogItem.Severity);
            Assert.Equal("Resolved", issueLogItem.Status);
            Assert.Equal("Bin overflowing", issueLogItem.Description);
            Assert.Equal("collector1", issueLogItem.ReportedBy);
            Assert.Equal(reportedAt, issueLogItem.ReportedAt);
        }

        [Fact]
        public void ReportedAt_DefaultsToNow()
        {
            // Arrange
            var before = DateTime.Now.AddSeconds(-1);

            // Act
            var issueLogItem = new IssueLogItem();
            var after = DateTime.Now.AddSeconds(1);

            // Assert
            Assert.InRange(issueLogItem.ReportedAt, before, after);
        }
    }
}
