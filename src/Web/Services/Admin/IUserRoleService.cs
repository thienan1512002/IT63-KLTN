using Microsoft.AspNetCore.Identity;
using Web.Models.Dtos.Admin;

namespace Web.Services.Admin;

public interface IUserRoleService
{
    Task<UserRolesDetailDto> GetUserRolesDetailAsync(string userName, CancellationToken cancellationToken = default);

    Task<IdentityResult> UpdateUserRolesAsync(string userName, IReadOnlyList<string> roleIds, CancellationToken cancellationToken = default);
}
