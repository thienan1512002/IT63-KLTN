using Web.Data.Entities.Admin;
using Web.Models.Dtos.Admin;

namespace Web.Services.Admin;

public record BelongSiteLookup(int Id, string Name);

public interface IBelongSiteService
{
    Task<List<BelongSiteListItemDto>> GetAllAsync();
    Task<BelongSite> GetByIdAsync(int id);
    Task<BelongSite> CreateAsync(CreateBelongSiteRequestDto request);
    Task UpdateAsync(int id, UpdateBelongSiteRequestDto request);
    Task SetActiveStatusAsync(int id, bool isActive);
    Task<IReadOnlyList<BelongSiteLookup>> GetLookupsAsync();
}
