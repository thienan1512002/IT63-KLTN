using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow;
using Web.Services.Workflow.Core;

namespace Web.Services.Workflow;

/// <summary>Read-only projection using the same resolver and retained revisions as submission.</summary>
public class RequestApprovalPreviewService(ApplicationDbContext db, IApproverResolver resolver)
{
    public async Task<RequestApprovalPreviewDto> GetAsync(int requestTypeId, int? requestId, string userName, CancellationToken ct = default)
    {
        ApprovalRequest request = null;
        if (requestId.HasValue)
        {
            request = await db.ApprovalRequests.AsNoTracking().Include(x => x.ApprovalDetails).ThenInclude(x => x.ApproverInfos)
                .SingleOrDefaultAsync(x => x.Id == requestId, ct)
                ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");
            if (request.FaxCode != userName || request.Status is not (RequestStatus.Draft or RequestStatus.Returned))
                throw new UnauthorizedAccessException("Bạn chỉ được xem trước luồng của yêu cầu đang soạn của mình.");
            if (request.Status == RequestStatus.Returned && request.RequestTypeId != requestTypeId)
                throw new InvalidOperationException("Không được đổi loại yêu cầu đã gửi.");
        }

        var result = new RequestApprovalPreviewDto();
        // Returned requests already have assigned approvers: do not resolve them again from today's directory.
        if (request?.Status == RequestStatus.Returned)
        {
            result.IsPinned = true; result.IsSnapshot = true; result.Revision = request.WorkflowRevision;
            result.Message = "Yêu cầu được gửi lại theo các tầng và người duyệt đã gán trước đó.";
            result.Steps = request.ApprovalDetails.OrderBy(x => x.StepOrder).Select(x => new RequestApprovalPreviewStepDto {
                StepOrder = x.StepOrder, Name = x.StepTitle, Assignment = "Người duyệt đã được gán",
                Approvers = x.ApproverInfos.Select(Person).ToList(),
                Warning = x.ApproverInfos.Count == 0 ? "Tầng này chưa có người duyệt. Vui lòng liên hệ quản trị viên." : string.Empty
            }).ToList();
            return result;
        }

        var type = await db.RequestTypes.AsNoTracking().Include(x => x.ApprovalCase)
            .SingleOrDefaultAsync(x => x.Id == requestTypeId && x.IsActive, ct)
            ?? throw new InvalidOperationException("Loại yêu cầu không tồn tại hoặc đã bị khóa.");
        result.IsPinned = request?.RequestTypeId == requestTypeId;
        var caseId = result.IsPinned ? request.ApprovalCaseId : type.ApprovalCaseId;
        var workflow = await db.ApprovalCases.AsNoTracking().SingleOrDefaultAsync(x => x.CaseId == caseId, ct);
        if (workflow == null) { result.Message = "Loại yêu cầu chưa được cấu hình luồng phê duyệt."; return result; }
        // A null revision on a legacy draft follows the same active-step selection as the workflow builder.
        var revision = result.IsPinned ? request.WorkflowRevision : workflow.Revision;
        result.Revision = revision;
        result.WorkflowName = await db.ApprovalWorkflowVersions.AsNoTracking()
            .Where(x => x.ApprovalCaseId == caseId && x.Revision == revision).Select(x => x.Name).SingleOrDefaultAsync(ct) ?? workflow.CaseName;
        if ((!result.IsPinned || !revision.HasValue) && !workflow.IsActive)
        {
            result.Message = "Luồng này đang tạm dừng nhận yêu cầu mới. Vui lòng liên hệ quản trị viên.";
            return result;
        }
        var steps = await db.ApprovalSteps.AsNoTracking().Include(x => x.ApprovalRole)
            .Where(x => x.ApprovalCaseId == caseId && (revision.HasValue ? x.Revision == revision.Value : !x.IsRemoved))
            .OrderBy(x => x.StepOrder).ToListAsync(ct);
        if (steps.Count == 0) { result.Message = "Luồng chưa có tầng phê duyệt. Vui lòng liên hệ quản trị viên."; return result; }
        result.Message = result.IsPinned
            ? "Bản nháp giữ phiên bản luồng đã lưu. Người duyệt dự kiến được xác định theo thông tin đơn vị hiện tại khi gửi."
            : "Người duyệt dự kiến theo cấu hình hiện tại. Luồng được gắn với yêu cầu khi lưu nháp; người duyệt được xác định khi gửi.";
        var user = await db.Users.AsNoTracking().Include(x => x.Section).ThenInclude(x => x.Department)
            .SingleOrDefaultAsync(x => x.UserName == userName && x.IsActive &&
                (x.LockoutEnd == null || x.LockoutEnd <= DateTimeOffset.UtcNow), ct);
        var validUnit = user?.Section?.IsActive == true && user.Section.Department?.IsActive == true;
        Dictionary<string, List<ApproverInfo>> map = [];
        if (validUnit)
        {
            try { map = await resolver.BuildStepApproverMapAsync(steps, userName, user.Section.DepartmentId, user.Section.Id); }
            catch (InvalidDataException)
            {
                // Keep valid layers visible even if one layer has a missing/locked user or removed unit.
                foreach (var step in steps)
                {
                    ct.ThrowIfCancellationRequested();
                    try {
                        var resolved = await resolver.BuildStepApproverMapAsync([step], userName, user.Section.DepartmentId, user.Section.Id);
                        map[step.Id] = resolved.GetValueOrDefault(step.Id, []);
                    } catch (InvalidDataException) { map[step.Id] = []; }
                }
            }
        }
        result.Steps = steps.Select(step => {
            var people = map.GetValueOrDefault(step.Id, []);
            return new RequestApprovalPreviewStepDto {
                StepOrder = step.StepOrder, Name = step.ApprovalRole.Title, Assignment = Assignment(step.ApprovalRole),
                Approvers = people.Select(Person).ToList(),
                Warning = !validUnit ? "Tài khoản chưa có đơn vị đang hoạt động. Vui lòng cập nhật thông tin nhân sự."
                    : people.Count == 0 ? "Chưa xác định được người duyệt hợp lệ. Vui lòng liên hệ quản trị viên trước khi gửi."
                    : people.All(x => x.UserName == userName) ? "Tầng này đang gán chính người đề nghị. Hệ thống không cho phép tự phê duyệt; vui lòng liên hệ quản trị viên." : string.Empty
            };
        }).ToList();
        return result;
    }

    private static RequestApprovalPreviewPersonDto Person(ApproverInfo person) => new() {
        UserName = person.UserName, FullName = string.IsNullOrWhiteSpace(person.FullName) ? person.UserName : person.FullName,
        Position = person.Position, SectionName = person.SectionName
    };

    private static string Assignment(ApprovalRole role) => role.RoleType switch {
        ApprovalRoleType.DepartmentManager => "Trưởng Phòng/Khoa/Trung tâm người đề nghị",
        ApprovalRoleType.SectionMainApprover => "Trưởng Bộ phận/Bộ môn người đề nghị",
        ApprovalRoleType.DepartmentManagerWithId => "Trưởng đơn vị được chỉ định",
        ApprovalRoleType.SectionManager => "Người phụ trách bộ phận/bộ môn người đề nghị",
        ApprovalRoleType.SectionManagerWithId => "Người phụ trách bộ phận/bộ môn được chỉ định",
        ApprovalRoleType.Leader => "Người phụ trách bộ phận/bộ môn",
        ApprovalRoleType.DeputySectionMgr => "Cấp phó phụ trách bộ phận/bộ môn",
        ApprovalRoleType.Requester => "Người đề nghị",
        ApprovalRoleType.NormalApprover when !string.IsNullOrWhiteSpace(role.IdentityRoleName) => "Nhóm người duyệt theo vai trò",
        ApprovalRoleType.NormalApprover => "Người duyệt được chỉ định",
        _ => "Theo cấu hình quy trình"
    };
}
