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
    public class CampaignControllerTests
    {
        private class FakeService : ICampaignService
        {
            public Task<IEnumerable<Campaign>> GetAllCampaignsAsync() => Task.FromResult<IEnumerable<Campaign>>(new List<Campaign>{ new Campaign { CampaignId = 1, CampaignName = "C1" } });
            public Task<Campaign?> GetCampaignByIdAsync(int campaignId) => Task.FromResult<Campaign?>(new Campaign { CampaignId = campaignId });
            public Task<int> AddCampaignAsync(Campaign campaign) => Task.FromResult(1);
            public Task<bool> UpdateCampaignAsync(Campaign campaign) => Task.FromResult(true);
            public Task<bool> DeleteCampaignAsync(int campaignId) => Task.FromResult(true);
            public Task<IEnumerable<Campaign>> GetActiveCampaignsAsync() => Task.FromResult<IEnumerable<Campaign>>(new List<Campaign>());
            public Task<Campaign?> GetCurrentCampaignAsync() => Task.FromResult<Campaign?>(null);
            public Task<bool> ActivateCampaignAsync(int campaignId) => Task.FromResult(true);
            public Task<bool> DeactivateCampaignAsync(int campaignId) => Task.FromResult(true);
            public Task<decimal> CalculateTotalIncentivesAsync(int campaignId) => Task.FromResult(0m);
        }

        [Fact]
        public async Task Index_ReturnsViewWithCampaigns()
        {
            var svc = new FakeService();
            var ctrl = new CampaignController(svc) { };
            var res = await ctrl.Index();
            res.Should().BeOfType<ViewResult>();
            var vr = (ViewResult)res;
            vr.Model.Should().BeAssignableTo<IEnumerable<Campaign>>();
        }

        [Fact]
        public void Create_Get_ReturnsViewWithDefaultModel()
        {
            var svc = new FakeService();
            var ctrl = new CampaignController(svc);
            var res = ctrl.Create();
            res.Should().BeOfType<ViewResult>();
            var vr = (ViewResult)res;
            vr.Model.Should().BeAssignableTo<Campaign>();
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsView()
        {
            var svc = new FakeService();
            var ctrl = new CampaignController(svc);
            ctrl.ModelState.AddModelError("x", "err");
            var campaign = new Campaign();
            var res = await ctrl.Create(campaign);
            res.Should().BeOfType<ViewResult>();
        }
    }
}
