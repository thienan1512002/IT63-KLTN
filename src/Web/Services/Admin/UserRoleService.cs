using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Identity;
using Web.Models.Dtos.Admin;

namespace Web.Services.Admin;

public class UserRoleService : IUserRoleService
{
    public const string SuperUserRoleName = "SuperUser";

    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly ApplicationDbContext _db;

    public UserRoleService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    public async Task<UserRolesDetailDto> GetUserRolesDetailAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return null;

        var user = await _userManager.FindByNameAsync(userName);
        if (user is null)
            return null;

        var assignedRoleIds = await _db.Set<AppUserRole>()
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleId)
            .ToHashSetAsync(cancellationToken);

        var allRoles = await _roleManager.Roles
            .AsNoTracking()
            .OrderBy(r => r.CategoryOrder ?? int.MaxValue)
            .ThenBy(r => r.CategoryName)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var superRole = allRoles.FirstOrDefault(r => r.Name == SuperUserRoleName);

        var groups = allRoles
            .GroupBy(r => new { r.CategoryName, r.CategoryOrder })
            .OrderBy(g => g.Key.CategoryOrder ?? int.MaxValue)
            .ThenBy(g => g.Key.CategoryName)
            .Select(g => new RoleAssignmentGroupDto
            {
                CategoryName = g.Key.CategoryName ?? string.Empty,
                CategoryOrder = g.Key.CategoryOrder ?? 0,
                Roles = g.Select(r => new RoleAssignmentItemDto
                {
                    Id = r.Id,
                    Name = r.Name ?? string.Empty,
                    DisplayName = string.IsNullOrWhiteSpace(r.DisplayName) ? r.Name : r.DisplayName,
                    Assigned = assignedRoleIds.Contains(r.Id),
                }).ToList(),
            })
            .ToList();

        return new UserRolesDetailDto
        {
            UserName = user.UserName ?? userName,
            FullName = user.FullName,
            SuperUserRoleId = superRole?.Id,
            Groups = groups,
        };
    }

    public async Task<IdentityResult> UpdateUserRolesAsync(string userName, IReadOnlyList<string> roleIds, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(userName))
            return IdentityResult.Failed(new IdentityError { Description = "User name is required." });

        var user = await _userManager.FindByNameAsync(userName);
        if (user is null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });

        var desiredIds = roleIds?.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToHashSet(StringComparer.Ordinal)
            ?? new HashSet<string>(StringComparer.Ordinal);

        var validIds = await _roleManager.Roles
            .Where(r => desiredIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToHashSetAsync(cancellationToken);

        var currentRoleIds = await _db.Set<AppUserRole>()
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        if (!desiredIds.SetEquals(validIds))
            return IdentityResult.Failed(new IdentityError { Description = "Có vai trò không tồn tại." });
        var toRemove = currentRoleIds.Where(id => !validIds.Contains(id)).ToList();
        var toAdd = validIds.Where(id => !currentRoleIds.Contains(id)).ToList();

        var changed = toRemove.Count > 0 || toAdd.Count > 0;
        var result = await ApplyRoleChangesAsync(user, toRemove, toAdd, cancellationToken);
        if (!result.Succeeded)
            return result;

        // Buộc đăng nhập lại: cookie cũ không còn khớp stamp (LoginSessionNotice trên trang Login).
        if (changed)
            await _userManager.UpdateSecurityStampAsync(user);

        await transaction.CommitAsync(cancellationToken);
        return IdentityResult.Success;
    }

    private async Task<IdentityResult> ApplyRoleChangesAsync(
        AppUser user,
        List<string> toRemove,
        List<string> toAdd,
        CancellationToken cancellationToken)
    {
        if (toRemove.Count > 0)
        {
            var removeNames = await _roleManager.Roles
                .Where(r => toRemove.Contains(r.Id) && r.Name != null)
                .Select(r => r.Name!)
                .ToListAsync(cancellationToken);

            var removeResult = await _userManager.RemoveFromRolesAsync(user, removeNames);
            if (!removeResult.Succeeded)
                return removeResult;
        }

        if (toAdd.Count > 0)
        {
            var addNames = await _roleManager.Roles
                .Where(r => toAdd.Contains(r.Id) && r.Name != null)
                .Select(r => r.Name!)
                .ToListAsync(cancellationToken);

            var addResult = await _userManager.AddToRolesAsync(user, addNames);
            if (!addResult.Succeeded)
                return addResult;
        }

        return IdentityResult.Success;
    }
}
