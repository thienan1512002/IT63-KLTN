using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Common;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow;
using Web.Services.Workflow;

namespace Web.Controllers.Api.Workflow;

[Authorize(Roles = RoleConstants.SuperUser)]
[ApiController]
[Route("api/admin/workflows")]
public class WorkflowAdminApiController(ApplicationDbContext db, WorkflowConfigurationService configuration) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await db.ApprovalCases.AsNoTracking()
        .OrderBy(x => x.CaseName).Select(x => new {
            x.CaseId, Name = x.CaseName, x.Description, x.IsActive, x.Revision,
            Steps = x.ApprovalSteps.Where(s => !s.IsRemoved).OrderBy(s => s.StepOrder).Select(s => new { s.StepOrder, Name = s.ApprovalRole.Title })
        }).ToListAsync(ct));

    [HttpGet("forms")]
    public async Task<IActionResult> Forms(CancellationToken ct) => Ok(await db.RequestTypes.AsNoTracking()
        .OrderBy(x => x.Name).Select(x => new {
            x.Id, x.Code, x.Name, x.TemplateCode, x.IsActive, x.ApprovalCaseId,
            Revision = x.ApprovalCase.Revision,
            StepCount = x.ApprovalCase.ApprovalSteps.Count(s => !s.IsRemoved),
            WorkflowActive = x.ApprovalCase.IsActive
        }).ToListAsync(ct));

    [HttpGet("lookups")]
    public async Task<IActionResult> Lookups(CancellationToken ct) => Ok(new {
        Users = await db.Users.AsNoTracking().Where(x => x.IsActive && (x.LockoutEnd == null || x.LockoutEnd <= DateTimeOffset.UtcNow))
            .OrderBy(x => x.FullName).Select(x => new { x.UserName, x.FullName, Department = x.Section != null ? x.Section.Department.Name : "" }).ToListAsync(ct),
        Departments = await db.Departments.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.MainApprover }).ToListAsync(ct),
        Roles = await db.Roles.AsNoTracking().OrderBy(x => x.Name).Select(x => new { x.Name, x.DisplayName }).ToListAsync(ct)
    });

    [HttpGet("forms/{id:int}")]
    public async Task<IActionResult> Form(int id, CancellationToken ct)
    {
        var type = await db.RequestTypes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (type == null) return NotFound(new { message = "Kh?ng t?m th?y lo?i form." });
        try {
            return Ok(new {
                Form = new { type.Id, type.Name, type.Code, type.IsActive },
                Workflow = await configuration.GetAsync(type.ApprovalCaseId, ct: ct),
                SharedFormCount = await db.RequestTypes.CountAsync(x => x.ApprovalCaseId == type.ApprovalCaseId, ct),
                ExistingRequests = await db.ApprovalRequests.CountAsync(x => x.RequestTypeId == id && !x.IsRemoved &&
                    (x.Status == RequestStatus.Draft || x.Status == RequestStatus.Ongoing || x.Status == RequestStatus.Returned), ct),
                Versions = await db.ApprovalWorkflowVersions.AsNoTracking().Where(x => x.ApprovalCaseId == type.ApprovalCaseId)
                    .OrderByDescending(x => x.Revision).Select(x => new { x.Revision, x.Name, x.PublishedAt, x.PublishedBy,
                        StepCount = db.ApprovalSteps.Count(s => s.ApprovalCaseId == x.ApprovalCaseId && s.Revision == x.Revision) }).ToListAsync(ct)
            });
        } catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("forms/{id:int}/versions/{revision:int}")]
    public async Task<IActionResult> FormVersion(int id, int revision, CancellationToken ct)
    {
        var caseId = await db.RequestTypes.Where(x => x.Id == id).Select(x => x.ApprovalCaseId).SingleOrDefaultAsync(ct);
        if (caseId == null) return NotFound();
        return await Read(caseId, revision, ct);
    }

    [HttpPut("forms/{id:int}")]
    public Task<IActionResult> PublishForm(int id, WorkflowSaveDto input, CancellationToken ct) => Save(input.CaseId, input, id, ct);
    [HttpGet("{caseId}")]
    public Task<IActionResult> Get(string caseId, CancellationToken ct) => Read(caseId, null, ct);
    [HttpPost]
    public Task<IActionResult> Create(WorkflowSaveDto input, CancellationToken ct) => Save(null, input, null, ct);
    [HttpPut("{caseId}")]
    public Task<IActionResult> Update(string caseId, WorkflowSaveDto input, CancellationToken ct) => Save(caseId, input, null, ct);

    private async Task<IActionResult> Read(string caseId, int? revision, CancellationToken ct)
    {
        try { return Ok(await configuration.GetAsync(caseId, revision, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
    private async Task<IActionResult> Save(string? caseId, WorkflowSaveDto input, int? typeId, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try {
            var saved = await configuration.PublishAsync(caseId, input, User.Identity!.Name!, typeId, ct);
            return Ok(new { id = saved.CaseId, revision = saved.Revision });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (DbUpdateConcurrencyException ex) { return Conflict(new { message = ex.Message }); }
    }
}

[Authorize(Roles = RoleConstants.SuperUser)]
[ApiController]
[Route("api/admin/request-types")]
public class RequestTypeAdminApiController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await db.RequestTypes.AsNoTracking()
        .Include(x => x.ApprovalCase).OrderBy(x => x.Name).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(RequestTypeSaveDto input, CancellationToken ct)
    {
        if (!RequestFormCatalog.All.Any(x => x.Code == input.TemplateCode)) return BadRequest(new { message = "Mẫu biểu không hợp lệ." });
        if (input.RequireAmount && input.TemplateCode != "INTERNAL" && !RequestFormCatalog.Get(input.TemplateCode).PricedLines)
            return BadRequest(new { message = "Mẫu biểu này không có trường số tiền. Vui lòng bỏ tùy chọn cần số tiền." });
        if (!await db.ApprovalCases.AnyAsync(x => x.CaseId == input.ApprovalCaseId, ct))
            return BadRequest(new { message = "Quy trình không tồn tại." });
        if (await db.RequestTypes.AnyAsync(x => x.Code == input.Code, ct)) return Conflict(new { message = "Mã đã tồn tại." });
        var entity = new RequestType(); Apply(entity, input); db.RequestTypes.Add(entity);
        await db.SaveChangesAsync(ct); return Ok(new { entity.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, RequestTypeSaveDto input, CancellationToken ct)
    {
        if (!RequestFormCatalog.All.Any(x => x.Code == input.TemplateCode)) return BadRequest(new { message = "Mẫu biểu không hợp lệ." });
        if (input.RequireAmount && input.TemplateCode != "INTERNAL" && !RequestFormCatalog.Get(input.TemplateCode).PricedLines)
            return BadRequest(new { message = "Mẫu biểu này không có trường số tiền. Vui lòng bỏ tùy chọn cần số tiền." });
        if (!await db.ApprovalCases.AnyAsync(x => x.CaseId == input.ApprovalCaseId, ct))
            return BadRequest(new { message = "Quy trình không tồn tại." });
        if (await db.RequestTypes.AnyAsync(x => x.Id != id && x.Code == input.Code.Trim().ToUpperInvariant(), ct))
            return Conflict(new { message = "Mã đã tồn tại." });
        var entity = await db.RequestTypes.FindAsync([id], ct);
        if (entity == null) return NotFound();
        if (entity.TemplateCode != input.TemplateCode && await db.ApprovalRequests.AnyAsync(x => x.RequestTypeId == id && x.BusinessData != null, ct))
            return BadRequest(new { message = "Loại yêu cầu đã phát sinh dữ liệu không được đổi mẫu biểu. Hãy tạo loại yêu cầu mới." });
        Apply(entity, input); entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpPost("{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id, CancellationToken ct)
    {
        var entity = await db.RequestTypes.FindAsync([id], ct); if (entity == null) return NotFound();
        entity.IsActive = !entity.IsActive; entity.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); return NoContent();
    }

    private static void Apply(RequestType entity, RequestTypeSaveDto input)
    {
        entity.Code = input.Code.Trim().ToUpperInvariant(); entity.Name = input.Name.Trim();
        entity.TemplateCode = input.TemplateCode;
        entity.Description = input.Description?.Trim() ?? string.Empty; entity.ApprovalCaseId = input.ApprovalCaseId;
        entity.IsActive = input.IsActive; entity.RequireRequestedDate = input.RequireRequestedDate;
        entity.RequireAmount = input.RequireAmount; entity.RequireReason = input.RequireReason;
        entity.AllowAttachment = input.AllowAttachment;
    }
}

