using Web.Data.Entities.Admin;
using Web.Models.Dtos.Admin;

namespace Web.Services.Admin;

public record DepartmentLookup(int Id, string Name, int BelongSiteId);

public interface IDepartmentService
{
    IQueryable<DepartmentListItemDto> GetListQuery();
    Task<Department> GetByIdAsync(int id);
    Task<Department> CreateAsync(CreateDepartmentRequestDto request);
    Task UpdateAsync(int id, UpdateDepartmentRequestDto request);
    Task SetActiveStatusAsync(int id, bool isActive);
    Task<HashSet<string>> GetExistingNamesAsync(int belongSiteId);
    Task<IReadOnlyList<DepartmentLookup>> GetLookupsAsync();
}
