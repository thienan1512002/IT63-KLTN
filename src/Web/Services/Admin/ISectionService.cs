using Web.Data.Entities.Admin;
using Web.Models.Dtos.Admin;

namespace Web.Services.Admin;

public record SectionLookup(int Id, string Name, string EngName, int DepartmentId);

public interface ISectionService
{
    IQueryable<SectionListItemDto> GetListQuery();
    Task<Section> GetByIdAsync(int id);
    Task<Section> CreateAsync(CreateSectionRequestDto request);
    Task UpdateAsync(int id, UpdateSectionRequestDto request);
    Task SetActiveStatusAsync(int id, bool isActive);
    Task<IReadOnlyList<SectionLookup>> GetLookupsAsync();
    Task<IReadOnlyList<SectionLookup>> GetLookupsBySiteIdAsync(int siteId);
    Task<IReadOnlyList<SectionLookupMasterData>> GetSectionLookupMasterData(int siteId);
}
