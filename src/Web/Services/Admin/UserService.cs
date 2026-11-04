using Hangfire;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Web.Common;
using Web.Data.DbContexts;
using Web.Data.Entities.Identity;
using Web.Data.Seeding;
using Web.Models.Dtos.Admin;
using Web.Models.FilterModels.Users;

namespace Web.Services.Admin;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ApplicationDbContext db,
        ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EmployeeLookupDto>> GetEmployeeLookupsAsync(string currentUserName, EmployeeLookupFM filter, CancellationToken cancellationToken = default)
    {
        var siteId = await GetUserBelongSiteIdAsync(currentUserName);
        if (siteId == 0) return [];

        var isVi = CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);

        IQueryable<AppUser> query = GetEmployeeLookupUsersQuery(siteId, filter)
        .Include(u => u.Section!)
            .ThenInclude(s => s.Department);

        var users = await query
        .OrderBy(u => u.EmpId)
        .ToListAsync(cancellationToken);

        return users.Select(u => new EmployeeLookupDto
        {
            EmpId = u.EmpId,
            UserName = u.UserName ?? string.Empty,
            FullName = u.FullName,
            DepartmentId = u.Section?.DepartmentId ?? 0,
            DepartmentName = u.Section?.Department?.Name ?? string.Empty,
            SectionId = u.SectionId ?? 0,
            SectionName = u.Section != null ? (isVi ? u.Section.Name : u.Section.EngName) : string.Empty,
            Position = u.Position,
            Email = u.Email ?? string.Empty,
        }).ToList();
    }

    private IQueryable<AppUser> GetEmployeeLookupUsersQuery(int siteId, EmployeeLookupFM filter)
    {
        var query = _db.AppUsers
            .AsNoTracking()
            .Where(u => u.Id != IdentityModelSeed.AdminUserId && u.SiteId == siteId && u.IsActive);

        if (filter.DepartmentId.HasValue)
            query = query.Where(u => u.Section != null && u.Section.DepartmentId == filter.DepartmentId.Value);

        if (filter.SectionId.HasValue)
            query = query.Where(u => u.SectionId == filter.SectionId.Value);

        return query;
    }

    /// <summary>
    /// Gets a queryable list of users with related section, department, and site information.
    /// Use this method to fetch the data source for the datagrid on the User Management page.
    /// </summary>
    public IQueryable<UserListItemDto> GetListQuery()
    {
        var isVi = CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        return _db.AppUsers
            .Where(x => x.Id != IdentityModelSeed.AdminUserId)
            .AsNoTracking()
            .Include(user => user.Section)
                .ThenInclude(section => section.Department)
            .Include(user => user.BelongSite)
            .Select(user => new UserListItemDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                EmpId = user.EmpId,
                SectionId = user.SectionId,
                SectionName = user.Section != null ? (isVi ? user.Section.Name : user.Section.EngName) : string.Empty,
                DepartmentId = user.Section.DepartmentId,
                DepartmentName = user.Section.Department.Name,
                BelongSiteId = user.SiteId,
                BelongSiteName = user.BelongSite.Name,
                Position = user.Position,
                DateOfBirth = user.DateOfBirth,
                IsActive = user.IsActive,
            });
    }

    public async Task<int> GetUserBelongSiteIdAsync(string userName)
    {
        var user = await _db.AppUsers
            .AsNoTracking()
            .Where(u => u.UserName == userName)
            .Select(u => new { u.SiteId })
            .FirstOrDefaultAsync() ?? throw new Exception($"User with username '{userName}' not found.");

        return user.SiteId ?? 0;
    }

    public async Task<IEnumerable<AppUser>> GetAllAsync()
        => await _db.AppUsers
                    .Where(x => x.Id != IdentityModelSeed.AdminUserId)
                    .AsNoTracking().ToListAsync();

    public async Task<AppUser> GetByUserNameAsync(string userName)
        => await _db.AppUsers.FirstOrDefaultAsync(u => u.UserName == userName);

    /// <summary>
    /// Gets a set of existing usernames from the database
    /// </summary>
    /// <returns>A set of existing usernames.</returns>
    public async Task<HashSet<string>> GetExistingUserNamesAsync()
    {
        var userNames = await _db.AppUsers
            .Where(x => x.Id != IdentityModelSeed.AdminUserId)
            .AsNoTracking()
            .Select(u => u.UserName)
            .ToListAsync();

        return userNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }




    /// <summary>
    /// Returns the subset of approvers who are currently offline.
    /// </summary>
    public async Task<List<string>> GetOfflineUserNames(List<string> userNames)
    {
        var offlineUsers = await _db.AppUsers
            .AsNoTracking()
            .Where(u => userNames.Contains(u.UserName) && u.IsOnline == false)
            .Select(u => u.UserName)
            .Distinct()
            .ToListAsync();

        return offlineUsers;
    }

    public async Task<UserListItemDto> GetWorkflowUserInfoAsync(string userName)
    {
        var isVi = CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        return await _db.AppUsers
            .AsNoTracking()
            .Where(u => u.UserName == userName)
            .Select(u => new UserListItemDto
            {
                UserName = u.UserName,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                DepartmentName = u.Section.Department.Name,
                SectionName = u.Section != null ? (isVi ? u.Section.Name : u.Section.EngName) : string.Empty,
                SectionEnName = u.Section.EngName,
                SectionViName = u.Section.Name,
                Position = u.Position,
                SectionId = u.SectionId,
                DepartmentId = u.Section.DepartmentId,
                EmpId = u.EmpId,
                BelongSiteId = u.SiteId,
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<string, string>> GetFullNameMapAsync(IEnumerable<string> userNames)
    {
        var nameSet = userNames.ToHashSet();
        return await _db.AppUsers
            .AsNoTracking()
            .Where(u => nameSet.Contains(u.UserName))
            .ToDictionaryAsync(u => u.UserName, u => u.FullName);
    }

    public async Task<DataResult<List<UserDetailVM>>> GetUserDetailsAsync(UserFM filter)
    {
        try
        {
            var query = _db.AppUsers
                .Where(x => x.Id != IdentityModelSeed.AdminUserId && x.IsActive)
                .AsNoTracking()
                .Include(user => user.Section)
                    .ThenInclude(section => section.Department)
                .Include(user => user.BelongSite)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.UserName))
            {
                var keyword = filter.UserName.Trim();
                query = query.Where(u => EF.Functions.Like(u.UserName ?? string.Empty, $"%{keyword}%")
                    || EF.Functions.Like(u.FullName, $"%{keyword}%")
                    || EF.Functions.Like(u.Email ?? string.Empty, $"%{keyword}%")
                );
            }

            var users = await query.Select(user => new UserDetailVM
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                EmpId = user.EmpId,
                SectionId = user.SectionId,
                SectionName = user.Section.Name,
                DepartmentId = user.Section.DepartmentId,
                DepartmentName = user.Section.Department.Name,
                BelongSiteId = user.SiteId,
                BelongSiteName = user.BelongSite.Name,
                Position = user.Position,
                DateOfBirth = user.DateOfBirth,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Avatar = string.IsNullOrEmpty(user.Avatar) ? null : user.Avatar
            }).ToListAsync();

            return DataResult<List<UserDetailVM>>.Success(users);
        }
        catch (Exception ex)
        {
            return DataResult<List<UserDetailVM>>.Failure($"An error occurred while fetching user details: {ex.Message}");
        }
    }
}
