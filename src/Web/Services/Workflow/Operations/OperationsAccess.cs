using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Web.Common;
using Web.Data.DbContexts;

namespace Web.Services.Workflow.Operations;

public record OperationsActor(string UserId, string UserName, int? SiteId, bool IsAdmin, bool HRRead, bool HRWrite, bool Dispatch)
{
    public static async Task<OperationsActor> ResolveAsync(ApplicationDbContext db, ClaimsPrincipal principal, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.UserName == principal.Identity.Name && x.IsActive &&
            (x.LockoutEnd == null || x.LockoutEnd <= DateTimeOffset.UtcNow), ct) ?? throw new UnauthorizedAccessException("Tài khoản không còn hoạt động.");
        bool Role(string role) => principal.IsInRole(role);
        var admin = Role(RoleConstants.SuperUser);
        var write = admin || Role(RoleConstants.HRLeaveManage);
        return new(user.Id, user.UserName, user.SiteId, admin,
            write || Role(RoleConstants.HRLeaveView), write,
            admin || Role(RoleConstants.TransportDispatch));
    }
    public void RequireSite(int siteId)
    {
        if (!IsAdmin && SiteId != siteId) throw new UnauthorizedAccessException("Dữ liệu nằm ngoài phạm vi cơ sở của bạn.");
    }
    public async Task RequireEmployeeAsync(ApplicationDbContext db, string employeeId, bool allowHR, bool allowDrivers, CancellationToken ct)
    {
        if (employeeId == UserId) return;
        var person = await db.Users.Where(x => x.Id == employeeId).Select(x => new { x.SiteId }).SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Không tìm thấy nhân viên.");
        if (IsAdmin || (person.SiteId == SiteId && SiteId.HasValue && (allowHR && HRRead || allowDrivers && Dispatch && await db.DriverProfiles.AnyAsync(x => x.EmployeeId == employeeId, ct)))) return;
        throw new UnauthorizedAccessException("Bạn không có quyền xem thông tin nhân viên này.");
    }
}
