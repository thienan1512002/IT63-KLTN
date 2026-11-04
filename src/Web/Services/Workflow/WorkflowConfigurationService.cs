using System.Data;
using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow;

namespace Web.Services.Workflow;

/// <summary>Publishes immutable step sets; requests retain their case and revision.</summary>
public class WorkflowConfigurationService(ApplicationDbContext db)
{
    public async Task<WorkflowSaveDto> GetAsync(string caseId, int? revision = null, CancellationToken ct = default)
    {
        var workflow = await db.ApprovalCases.AsNoTracking().SingleOrDefaultAsync(x => x.CaseId == caseId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy quy trình.");
        var version = revision ?? workflow.Revision;
        var metadata = await db.ApprovalWorkflowVersions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ApprovalCaseId == caseId && x.Revision == version, ct);
        var steps = await db.ApprovalSteps.AsNoTracking().Include(x => x.ApprovalRole)
            .Where(x => x.ApprovalCaseId == caseId && x.Revision == version).OrderBy(x => x.StepOrder).ToListAsync(ct);
        if (steps.Count == 0 && revision.HasValue) throw new KeyNotFoundException("Phiên bản này không có tầng phê duyệt.");
        var settingIds = steps.Where(x => x.ApprovalRole.FormApprovalId.HasValue).Select(x => x.ApprovalRole.FormApprovalId.Value).ToList();
        var settings = await db.FormApprovers.AsNoTracking().Where(x => settingIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.UserNames, ct);
        return new WorkflowSaveDto
        {
            CaseId = caseId, Revision = version, Name = metadata?.Name ?? workflow.CaseName,
            Description = metadata?.Description ?? workflow.Description, IsActive = workflow.IsActive,
            Steps = steps.Select(s => new WorkflowStepSaveDto
            {
                StepOrder = s.StepOrder, Name = s.ApprovalRole.Title,
                ApproverKind = s.ApprovalRole.RoleType switch
                {
                    ApprovalRoleType.DepartmentManager => ApproverKind.DepartmentManager,
                    ApprovalRoleType.SectionMainApprover => ApproverKind.SectionHead,
                    ApprovalRoleType.DepartmentManagerWithId => ApproverKind.DepartmentManagerByDepartment,
                    ApprovalRoleType.NormalApprover when !string.IsNullOrEmpty(s.ApprovalRole.IdentityRoleName) => ApproverKind.Role,
                    ApprovalRoleType.NormalApprover => ApproverKind.User,
                    _ => (ApproverKind)(-1) // Never silently translate a legacy rule into another rule.
                },
                ApproverDepartmentId = s.ApprovalRole.DepartmentId, ApproverRoleName = s.ApprovalRole.IdentityRoleName,
                ApproverUserName = s.ApprovalRole.FormApprovalId.HasValue ? settings.GetValueOrDefault(s.ApprovalRole.FormApprovalId.Value) : null
            }).ToList()
        };
    }

    public async Task<(string CaseId, int Revision)> PublishAsync(string? routeId, WorkflowSaveDto input, string actor,
        int? requestTypeId = null, CancellationToken ct = default)
    {
        if (input.Steps == null || input.Steps.Count is < 1 or > 100 || input.Steps.Any(x => x == null) ||
            string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 200 || input.Description?.Length > 300 ||
            !input.Steps.OrderBy(x => x.StepOrder).Select(x => x.StepOrder).SequenceEqual(Enumerable.Range(1, input.Steps.Count)))
            throw new ArgumentException("Quy trình cần từ 1 đến 100 tầng liên tiếp và có tên hợp lệ.");
        foreach (var step in input.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Name) || step.Name.Length > 200 || !Enum.IsDefined(step.ApproverKind))
                throw new ArgumentException($"Tầng {step.StepOrder}: hãy nhập tên và chọn cách xác định người duyệt.");
            if (step.ApproverKind == ApproverKind.User && !await db.Users.AnyAsync(x => x.UserName == step.ApproverUserName && x.IsActive &&
                    (x.LockoutEnd == null || x.LockoutEnd <= DateTimeOffset.UtcNow), ct))
                throw new ArgumentException($"Tầng {step.StepOrder}: người duyệt không tồn tại hoặc đã bị khóa.");
            if (step.ApproverKind == ApproverKind.Role && !await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id
                    join u in db.Users on ur.UserId equals u.Id where r.Name == step.ApproverRoleName && u.IsActive &&
                    (u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow) select u.Id).AnyAsync(ct))
                throw new ArgumentException($"Tầng {step.StepOrder}: vai trò chưa có người dùng đang hoạt động.");
            if (step.ApproverKind == ApproverKind.DepartmentManagerByDepartment)
            {
                var managerNames = await db.Departments.Where(x => x.Id == step.ApproverDepartmentId && x.IsActive).Select(x => x.MainApprover).SingleOrDefaultAsync(ct);
                var names = (managerNames ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var validCount = await db.Users.CountAsync(x => names.Contains(x.UserName) && x.IsActive &&
                    (x.LockoutEnd == null || x.LockoutEnd <= DateTimeOffset.UtcNow), ct);
                if (names.Length == 0 || validCount != names.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                    throw new ArgumentException($"Tầng {step.StepOrder}: đơn vị chưa có trưởng bộ phận hợp lệ. Hãy cập nhật danh mục đơn vị.");
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var type = requestTypeId.HasValue ? await db.RequestTypes.SingleOrDefaultAsync(x => x.Id == requestTypeId, ct) : null;
        if (requestTypeId.HasValue && type == null) throw new KeyNotFoundException("Không tìm thấy loại form.");
        if (type != null && type.ApprovalCaseId != input.CaseId) throw new DbUpdateConcurrencyException("Cấu hình form đã thay đổi. Hãy tải lại trước khi lưu.");
        var caseId = routeId ?? input.CaseId.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(caseId) || caseId.Length > 50) throw new ArgumentException("Mã quy trình không hợp lệ.");
        var entity = await db.ApprovalCases.Include(x => x.ApprovalSteps).SingleOrDefaultAsync(x => x.CaseId == caseId, ct);
        if (entity != null && routeId == null) throw new DbUpdateConcurrencyException("Mã quy trình đã tồn tại.");
        if (entity == null && routeId != null) throw new KeyNotFoundException("Không tìm thấy quy trình.");
        if (entity != null && entity.Revision != input.Revision) throw new DbUpdateConcurrencyException("Một quản trị viên khác đã cập nhật quy trình. Hãy tải lại và kiểm tra thay đổi.");
        if (entity == null)
        {
            entity = new ApprovalCase { CaseId = caseId, Revision = 0, CreatedAt = DateTime.UtcNow, CreatedBy = actor, ApprovalSteps = new List<ApprovalStep>() };
            db.ApprovalCases.Add(entity);
        }
        else if (type != null && await db.RequestTypes.AnyAsync(x => x.Id != type.Id && x.ApprovalCaseId == caseId, ct))
        {
            // Fork all published versions so other forms sharing the source remain unchanged.
            var source = entity;
            caseId = $"FORM-{type.Id}-{Guid.NewGuid():N}";
            entity = new ApprovalCase { CaseId = caseId, Revision = source.Revision, CreatedAt = DateTime.UtcNow, CreatedBy = actor, ApprovalSteps = new List<ApprovalStep>() };
            db.ApprovalCases.Add(entity);
            foreach (var old in source.ApprovalSteps.Where(x => x.Revision > 0))
                entity.ApprovalSteps.Add(new ApprovalStep {
                    Id = Guid.NewGuid().ToString("N"), ApprovalCaseId = caseId, ApprovalRoleId = old.ApprovalRoleId,
                    Revision = old.Revision, StepOrder = old.StepOrder, ActionTypes = old.ActionTypes, ApprovalType = old.ApprovalType,
                    GroupTitle = old.GroupTitle, IsRemoved = true, SendSubmissionNoty = old.SendSubmissionNoty
                });
            var history = await db.ApprovalWorkflowVersions.AsNoTracking().Where(x => x.ApprovalCaseId == source.CaseId).ToListAsync(ct);
            db.ApprovalWorkflowVersions.AddRange(history.Select(x => new ApprovalWorkflowVersion {
                ApprovalCaseId = caseId, Revision = x.Revision, Name = x.Name, Description = x.Description,
                PublishedAt = x.PublishedAt, PublishedBy = x.PublishedBy
            }));
            type.ApprovalCaseId = caseId;
        }
        foreach (var old in entity.ApprovalSteps) old.IsRemoved = true;
        entity.Revision++;
        entity.CaseName = input.Name.Trim(); entity.Description = input.Description?.Trim();
        entity.FormCode = caseId; entity.BelongSite = "ALL"; entity.IsActive = input.IsActive;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedBy = actor;
        if (type != null) type.UpdatedAt = entity.UpdatedAt;
        var steps = new List<ApprovalStep>();
        foreach (var item in input.Steps.OrderBy(x => x.StepOrder))
        {
            var role = new ApprovalRole { Id = Guid.NewGuid().ToString("N"), Title = item.Name.Trim(), FormCode = caseId,
                IsActive = true, CreatedBy = actor, CreatedAt = DateTime.UtcNow };
            switch (item.ApproverKind)
            {
                case ApproverKind.DepartmentManager: role.RoleType = ApprovalRoleType.DepartmentManager; break;
                case ApproverKind.SectionHead: role.RoleType = ApprovalRoleType.SectionMainApprover; break;
                case ApproverKind.DepartmentManagerByDepartment: role.RoleType = ApprovalRoleType.DepartmentManagerWithId; role.DepartmentId = item.ApproverDepartmentId; break;
                case ApproverKind.Role: role.RoleType = ApprovalRoleType.NormalApprover; role.IdentityRoleName = item.ApproverRoleName; break;
                case ApproverKind.User:
                    var setting = new FormApproverSetting { Title = item.Name.Trim(), DisplayTitle = item.Name.Trim(), UserNames = item.ApproverUserName, FormCode = caseId, IsRemoved = false };
                    db.FormApprovers.Add(setting); await db.SaveChangesAsync(ct);
                    role.RoleType = ApprovalRoleType.NormalApprover; role.FormApprovalId = setting.Id; break;
            }
            var step = new ApprovalStep { Id = Guid.NewGuid().ToString("N"), ApprovalCaseId = caseId, ApprovalRole = role,
                Revision = entity.Revision, StepOrder = item.StepOrder, ActionTypes = ApprovalActionType.Approve | ApprovalActionType.Reject | ApprovalActionType.Return | ApprovalActionType.Comment,
                ApprovalType = ApprovalType.Approval, GroupTitle = item.Name.Trim() };
            steps.Add(step); db.ApprovalSteps.Add(step);
        }
        for (var i = 0; i + 1 < steps.Count; i++) steps[i].NextStepId = steps[i + 1].Id;
        db.ApprovalWorkflowVersions.Add(new ApprovalWorkflowVersion { ApprovalCaseId = caseId, Revision = entity.Revision,
            Name = entity.CaseName, Description = entity.Description, PublishedAt = entity.UpdatedAt.Value, PublishedBy = actor });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return (caseId, entity.Revision);
    }
}
