using DevExtreme.AspNet.Data;
using Web.Common;
using Web.Data.Entities.Identity;
using Web.Models.Dtos.Admin;
using Web.Models.FilterModels.Users;

namespace Web.Services.Admin;

public interface IUserService
{
    IQueryable<UserListItemDto> GetListQuery();
    Task<IEnumerable<AppUser>> GetAllAsync();
    Task<AppUser> GetByUserNameAsync(string userName);
    Task<HashSet<string>> GetExistingUserNamesAsync();

    Task<List<string>> GetOfflineUserNames(List<string> userNames);

    Task<UserListItemDto> GetWorkflowUserInfoAsync(string userName);
    Task<Dictionary<string, string>> GetFullNameMapAsync(IEnumerable<string> userNames);
    Task<DataResult<List<UserDetailVM>>> GetUserDetailsAsync(UserFM filter);

    Task<int> GetUserBelongSiteIdAsync(string userName);

    // Shared employee lookup used by request and approval screens.
    Task<IReadOnlyList<EmployeeLookupDto>> GetEmployeeLookupsAsync(string currentUserName, EmployeeLookupFM filter, CancellationToken cancellationToken = default);
}
