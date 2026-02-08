using System.Collections.Generic;
using System.Threading.Tasks;
using ADWebApplication.Controllers;
using ADWebApplication.Models;
using ADWebApplication.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ADWebApplication.Tests.Controllers
{
    public class RewardCatalogueControllerTests
    {
        private class FakeService : IRewardCatalogueService
        {
            public Task<int> AddRewardAsync(RewardCatalogue reward) => Task.FromResult(1);
            public Task<bool> DeleteRewardAsync(int rewardId) => Task.FromResult(true);
            public Task<IEnumerable<RewardCatalogue>> GetAllRewardsAsync() => Task.FromResult<IEnumerable<RewardCatalogue>>(new List<RewardCatalogue>());
            public Task<RewardCatalogue?> GetRewardByIdAsync(int rewardId) => Task.FromResult<RewardCatalogue?>(new RewardCatalogue { RewardId = rewardId });
            public Task<IEnumerable<RewardCatalogue>> GetAvailableRewardsAsync() => Task.FromResult<IEnumerable<RewardCatalogue>>(new List<RewardCatalogue>());
            public Task<IEnumerable<string>> GetAllRewardCategoriesAsync() => Task.FromResult<IEnumerable<string>>(new List<string>());
            public Task<bool> UpdateRewardAsync(RewardCatalogue reward) => Task.FromResult(true);
            public Task<bool> CheckRewardAvailabilityAsync(int rewardId, int quantity = 1) => Task.FromResult(true);
            public Task<IEnumerable<RewardCatalogue>> GetRewardsByCategoryAsync(string category) => Task.FromResult<IEnumerable<RewardCatalogue>>(new List<RewardCatalogue>());
        }

        [Fact]
        public async Task Index_ReturnsView()
        {
            var svc = new FakeService();
            var ctrl = new RewardCatalogueController(svc, NullLogger<RewardCatalogueController>.Instance);
            var res = await ctrl.Index();
            res.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public void Create_Get_ReturnsViewWithModel()
        {
            var svc = new FakeService();
            var ctrl = new RewardCatalogueController(svc, NullLogger<RewardCatalogueController>.Instance);
            var res = ctrl.Create();
            res.Should().BeOfType<ViewResult>();
            var vr = (ViewResult)res;
            vr.Model.Should().BeAssignableTo<RewardCatalogue>();
        }

        [Fact]
        public async Task Edit_Get_InvalidId_RedirectsToIndex()
        {
            var svc = new FakeService();
            var ctrl = new RewardCatalogueController(svc, NullLogger<RewardCatalogueController>.Instance);
            // TempData is null by default in controller unit tests; initialize a simple provider to avoid NRE
            var provider = new SimpleTempDataProvider();
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), provider);
            var res = await ctrl.Edit(0);
            res.Should().BeOfType<RedirectToActionResult>();
        }

        private class SimpleTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData(Microsoft.AspNetCore.Http.HttpContext context) => new Dictionary<string, object>();
            public void SaveTempData(Microsoft.AspNetCore.Http.HttpContext context, IDictionary<string, object> values) { }
        }
    }
}
