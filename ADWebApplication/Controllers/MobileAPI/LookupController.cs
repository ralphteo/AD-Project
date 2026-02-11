using ADWebApplication.Models.DTOs;
using ADWebApplication.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace ADWebApplication.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/lookup")]
    [EnableRateLimiting("mobile")]
    public class LookupController : ControllerBase
    {
        private readonly IMobileLookupService _lookupService;

        public LookupController(IMobileLookupService lookupService)
        {
            _lookupService = lookupService;
        }

        [HttpGet("bins")]
        public async Task<ActionResult<List<MobileLookupBinDto>>> GetBins()
        {
            var result = await _lookupService.GetBinsAsync();
            return Ok(result);
        }

        [HttpGet("categories")]
        public async Task<ActionResult<List<MobileLookupCategoryDto>>> GetCategories()
        {
            var result = await _lookupService.GetCategoriesAsync();
            return Ok(result);
        }

        [HttpGet("itemtypes")]
        public async Task<ActionResult<List<MobileLookupItemTypeDto>>> GetItemTypes([FromQuery] int categoryId)
        {
            var result = await _lookupService.GetItemTypesAsync(categoryId);
            return Ok(result);
        }
    }
}
