using ADWebApplication.Models.DTOs;

namespace ADWebApplication.Services;

public interface IMobileLookupService
{
    Task<List<MobileLookupBinDto>> GetBinsAsync();
    Task<List<MobileLookupCategoryDto>> GetCategoriesAsync();
    Task<List<MobileLookupItemTypeDto>> GetItemTypesAsync(int categoryId);
}
