using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow;
using Web.Services.Admin;
using Web.Services.Workflow;
using Web.Services.Workflow.Core;
using Web.Controllers.Api.Workflow;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

internal static class WorkflowVersionsVerification
{
    public static async Task RunAsync(ApplicationDbContext db)
    {
        void Check(bool value, string message) { if (!value) throw new Exception(message); }
        db.ChangeTracker.Clear();
        var internalType = await db.RequestTypes.AsNoTracking().SingleAsync(x => x.TemplateCode == "INTERNAL");
        var sharedCase = internalType.ApprovalCaseId;
        var sibling = await db.RequestTypes.AsNoTracking().FirstAsync(x => x.Id != internalType.Id && x.ApprovalCaseId == sharedCase);
        var config = new WorkflowConfigurationService(db);
        var resolver = new ApproverResolver(db, new UserService(db, NullLogger<UserService>.Instance));
        var preview = new RequestApprovalPreviewService(db, resolver);
        var requests = new RequestService(db, null!, new RequestBusinessService(db), new ApprovalWorkflowBuilder(db, resolver),
            new ApprovalValidator(resolver), new ApprovalActionProcessor(resolver), new RequestNumberGenerator(db), new SilentNotifier(),
            new Web.Services.Workflow.Operations.ApprovalOperationsService(db, new Web.Services.Workflow.Operations.LeaveAccountingService(db)));
        RequestEditDto Input() => new() { RequestTypeId = internalType.Id, Title = "Kiểm thử phiên bản luồng", Content = "Nội dung đề xuất",
            Reason = "Kiểm thử", RequestedDate = DateTime.Today.AddDays(1),
            BusinessData = new() { TargetDepartmentId = 1, ProposalCategoryId = 100, ExpectedBenefit = "Kiểm tra bảo toàn luồng cũ" } };
        async Task<int> Draft() { var id = await requests.CreateDraftAsync(Input(), "employee1", []); db.ChangeTracker.Clear(); return id; }
        async Task<ApprovalRequest> Read(int id) => await db.ApprovalRequests.AsNoTracking().Include(x => x.ApprovalDetails).ThenInclude(x => x.ApproverInfos).SingleAsync(x => x.Id == id);
        async Task Submit(int id) { await requests.SubmitAsync(id, "employee1"); db.ChangeTracker.Clear(); }
        async Task Approve(int id, string actor) { await requests.ApproveAsync(id, actor, "Đồng ý"); db.ChangeTracker.Clear(); }
        Check(await db.ApprovalWorkflowVersions.CountAsync() == 2, "Migration must backfill published versions for existing cases.");
        var oldDraft = await Draft(); var oldRunning = await Draft(); var oldReturned = await Draft();
        var initialPreview = await preview.GetAsync(internalType.Id, null, "employee1");
        Check(!initialPreview.IsPinned && initialPreview.Revision == 1 && initialPreview.Steps.Count == 2 &&
            initialPreview.Steps[0].Approvers.Single().UserName == "manager1", "Preview must resolve the requester's actual department head.");
        try { await preview.GetAsync(internalType.Id, oldDraft, "manager1"); throw new Exception("Preview must not expose another user's draft."); }
        catch (UnauthorizedAccessException) { }
        Check((await Read(oldDraft)).WorkflowRevision == 1, "Creation must pin the initial workflow version.");
        await Submit(oldRunning); await Submit(oldReturned);
        RequestApiController Badge(string name) => new(requests,db,null!) { ControllerContext=new ControllerContext { HttpContext=new DefaultHttpContext {
            User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name,name)],"Test")) } } };
        int Count(IActionResult result) => System.Text.Json.JsonSerializer.SerializeToElement(((OkObjectResult)result).Value).GetProperty("count").GetInt32();
        var beforeReturn=Count(await Badge("manager1").PendingCount(default));
        Check(beforeReturn==await requests.BuildPendingApprovalsQuery("manager1").CountAsync() && beforeReturn>=2,
            "Sidebar count must match the pending approval list for the signed-in approver.");
        Check(Count(await Badge("employee1").PendingCount(default))==await requests.BuildPendingApprovalsQuery("employee1").CountAsync(),
            "A different account must see only its own pending approval count.");
        await requests.ReturnAsync(oldReturned, "manager1", "Bổ sung nội dung"); db.ChangeTracker.Clear();
        Check(Count(await Badge("manager1").PendingCount(default))==beforeReturn-1,
            "Returning a request must remove it from the approver's badge count.");
        var returnedDetailIds = (await Read(oldReturned)).ApprovalDetails.OrderBy(x => x.StepOrder).Select(x => x.Id).ToArray();
        var first = await config.GetAsync(sharedCase);
        first.Name = "Luồng đề xuất riêng";
        first.Steps = [
            new() { StepOrder = 1, Name = "Giám đốc kiểm tra", ApproverKind = ApproverKind.User, ApproverUserName = "director1" },
            new() { StepOrder = 2, Name = "Trưởng bộ phận người tạo", ApproverKind = ApproverKind.DepartmentManager },
            new() { StepOrder = 3, Name = "Lãnh đạo phòng chỉ định", ApproverKind = ApproverKind.DepartmentManagerByDepartment, ApproverDepartmentId = 3 }
        ];
        var v2 = await config.PublishAsync(first.CaseId, first, "admin", internalType.Id); db.ChangeTracker.Clear();
        var oldPreview = await preview.GetAsync(internalType.Id, oldDraft, "employee1");
        Check(oldPreview.IsPinned && oldPreview.Revision == 1 && oldPreview.Steps.Count == 2,
            "A draft must preview its retained workflow even after its form has forked to a newer case.");
        var detailCount = await db.ApprovalDetails.CountAsync();
        var requestCount = await db.ApprovalRequests.CountAsync();
        var newPreview = await preview.GetAsync(internalType.Id, null, "employee1");
        Check(newPreview.Steps.SelectMany(x => x.Approvers).Select(x => x.UserName).SequenceEqual(new[] { "director1", "manager1", "director1" }),
            "Preview must match submission for explicit users, department heads and repeated approvers in different layers.");
        Check(!db.ChangeTracker.HasChanges() && detailCount == await db.ApprovalDetails.CountAsync() && requestCount == await db.ApprovalRequests.CountAsync(),
            "Reading a preview must not create requests, approval details or tracked writes.");
        await db.Users.Where(x => x.UserName == "director1").ExecuteUpdateAsync(x => x.SetProperty(u => u.IsActive, false));
        var unavailable = await preview.GetAsync(internalType.Id, null, "employee1");
        Check(unavailable.Steps[0].Warning.Length > 0 && unavailable.Steps[1].Approvers.Single().UserName == "manager1",
            "A missing or locked approver must show a warning without hiding valid layers.");
        await db.Users.Where(x => x.UserName == "director1").ExecuteUpdateAsync(x => x.SetProperty(u => u.IsActive, true));
        Check(v2.Revision == 2 && v2.CaseId != sharedCase, "A form sharing a workflow must receive an independent versioned configuration.");
        Check((await db.RequestTypes.AsNoTracking().SingleAsync(x => x.Id == sibling.Id)).ApprovalCaseId == sharedCase && (await config.GetAsync(sharedCase)).Steps.Count == 2,
            "Publishing for one form must not modify its sibling form.");
        Check((await config.GetAsync(v2.CaseId, 1)).Steps.Count == 2, "A fork must preserve readable historical configuration.");
        await requests.UpdateDraftAsync(oldDraft, Input(), "employee1", []); db.ChangeTracker.Clear();
        Check((await Read(oldDraft)).ApprovalCaseId == sharedCase && (await Read(oldDraft)).WorkflowRevision == 1,
            "Editing a draft of the same form must keep its original case and revision.");
        await Submit(oldDraft);
        Check((await Read(oldDraft)).ApprovalDetails.Count == 2, "An old draft submitted after publication must instantiate its original step set.");
        var newDraft = await Draft(); await Submit(newDraft);
        var newRequest = await Read(newDraft);
        Check(newRequest.WorkflowRevision == 2 && newRequest.ApprovalDetails.Count == 3 &&
            newRequest.ApprovalDetails.OrderBy(x => x.StepOrder).Select(x => x.ApproverInfos.Single().UserName).SequenceEqual(new[] { "director1", "manager1", "director1" }),
            "New requests must resolve explicit users, requester department heads, and chosen department heads in their configured order.");
        var snapshot = newRequest.ApprovalDetails.OrderBy(x => x.StepOrder).Select(x => (x.Id, x.StepTitle, User: x.ApproverInfos.Single().UserName)).ToArray();
        var delayedV2Draft = await Draft();
        var second = await config.GetAsync(v2.CaseId);
        second.Steps = [new() { StepOrder = 1, Name = "Chỉ giám đốc", ApproverKind = ApproverKind.User, ApproverUserName = "director1" }];
        var v3 = await config.PublishAsync(second.CaseId, second, "admin", internalType.Id); db.ChangeTracker.Clear();
        Check((await preview.GetAsync(internalType.Id, delayedV2Draft, "employee1")).Steps.Count == 3,
            "Preview must include layers removed from newer versions of the same case.");
        var returnedPreview = await preview.GetAsync(internalType.Id, oldReturned, "employee1");
        Check(returnedPreview.IsSnapshot && returnedPreview.Steps.Count == 2 && returnedPreview.Steps[0].Approvers.Single().UserName == "manager1",
            "Returned requests must preview their assigned people instead of today's template.");
        Check(v3.Revision == 3 && (await config.GetAsync(v3.CaseId, 2)).Steps.Count == 3, "Removing layers must retain the previous step set and audit version.");
        await requests.UpdateDraftAsync(delayedV2Draft, Input(), "employee1", []); db.ChangeTracker.Clear(); await Submit(delayedV2Draft);
        Check((await Read(delayedV2Draft)).WorkflowRevision == 2 && (await Read(delayedV2Draft)).ApprovalDetails.Count == 3,
            "A draft pinned to the same case must load removed template steps from its retained revision.");
        Check((await Read(newDraft)).ApprovalDetails.OrderBy(x => x.StepOrder).Select(x => (x.Id, x.StepTitle, User: x.ApproverInfos.Single().UserName)).SequenceEqual(snapshot),
            "Publishing changed layers must not rewrite active request snapshots.");
        try { await config.PublishAsync(second.CaseId, second, "another-admin", internalType.Id); throw new Exception("Stale workflow saves must fail."); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); }
        await requests.UpdateDraftAsync(oldReturned, Input(), "employee1", []); db.ChangeTracker.Clear(); await Submit(oldReturned);
        Check((await Read(oldReturned)).ApprovalDetails.OrderBy(x => x.StepOrder).Select(x => x.Id).SequenceEqual(returnedDetailIds),
            "Returning, editing and resubmitting must retain the old approval detail records.");
        foreach (var id in new[] { oldDraft, oldRunning, oldReturned }) { await Approve(id, "manager1"); await Approve(id, "director1"); Check((await Read(id)).Status == RequestStatus.Completed, "Old workflows must remain executable after later publication."); }
        await Approve(newDraft, "director1"); await Approve(newDraft, "manager1");
        Check((await Read(newDraft)).Status == RequestStatus.Ongoing, "Repeated approvers must not skip later layers.");
        await Approve(newDraft, "director1"); Check((await Read(newDraft)).Status == RequestStatus.Completed, "A retained version must finish after its own last layer.");
        var fresh = await Draft(); await Submit(fresh);
        var pausedDraft = await Draft();
        Check((await Read(fresh)).ApprovalDetails.Count == 1 && (await Read(fresh)).WorkflowRevision == 3, "Requests created after layer removal must use the new shorter workflow.");
        var v4input = await config.GetAsync(v3.CaseId); v4input.IsActive = false;
        await config.PublishAsync(v4input.CaseId, v4input, "admin", internalType.Id); db.ChangeTracker.Clear();
        try { await Draft(); throw new Exception("Paused workflows must block new drafts."); } catch (InvalidOperationException) { db.ChangeTracker.Clear(); }
        await Submit(pausedDraft); Check((await Read(pausedDraft)).WorkflowRevision == 3, "Pausing a template must not prevent pinned drafts from submitting.");
        await Approve(fresh, "director1"); Check((await Read(fresh)).Status == RequestStatus.Completed, "Pausing new requests must not block existing approvals.");
        var invalid = await config.GetAsync(v3.CaseId); invalid.Steps[0].ApproverUserName = "missing-user";
        try { await config.PublishAsync(invalid.CaseId, invalid, "admin", internalType.Id); throw new Exception("Unknown approvers must be rejected."); } catch (ArgumentException) { db.ChangeTracker.Clear(); }
        Check((await config.GetAsync(v3.CaseId)).Revision == 4, "Rejected configurations must not publish a version.");

        var controller = new Web.Controllers.Api.Workflow.WorkflowAdminApiController(db, config);
        System.Text.Json.JsonElement Json(Microsoft.AspNetCore.Mvc.IActionResult action) => System.Text.Json.JsonSerializer.SerializeToElement(
            ((Microsoft.AspNetCore.Mvc.OkObjectResult)action).Value, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        var formJson = Json(await controller.Form(internalType.Id, default));
        Check(formJson.GetProperty("workflow").GetProperty("revision").GetInt32() == 4 && formJson.GetProperty("versions").GetArrayLength() == 4,
            "The admin API must return current configuration and all immutable version metadata.");
        var lookupJson = Json(await controller.Lookups(default));
        Check(lookupJson.GetProperty("users").GetArrayLength() > 0 && lookupJson.GetProperty("departments").GetArrayLength() > 0, "Admin master selectors must query active users and departments.");
        Check(Json(await controller.Forms(default)).GetArrayLength() == 8, "Form configuration list must cover all eight business templates.");
        var roleVersion = await config.GetAsync(v3.CaseId); roleVersion.IsActive = true;
        roleVersion.Steps = [new() { StepOrder = 1, Name = "Vai trò giám đốc", ApproverKind = ApproverKind.Role, ApproverRoleName = "Director" }];
        // Use the actual seeded role name so this also verifies role master resolution.
        roleVersion.Steps[0].ApproverRoleName = await (from ur in db.UserRoles join u in db.Users on ur.UserId equals u.Id
            join role in db.Roles on ur.RoleId equals role.Id where u.UserName == "director1" select role.Name).FirstAsync();
        await config.PublishAsync(roleVersion.CaseId, roleVersion, "admin", internalType.Id); db.ChangeTracker.Clear();
        var roleDraft = await Draft(); await Submit(roleDraft);
        var rolePreview = await preview.GetAsync(internalType.Id, null, "employee1");
        Check(rolePreview.Steps.Single().Approvers.Any(x => x.UserName == "director1"), "Preview must resolve identity-role approvers.");
        Console.WriteLine("PASS: Read-only approval preview, actual resolver, privacy, retained/forked drafts, returned assignments, missing approvers and role groups.");
        Check((await Read(roleDraft)).ApprovalDetails.Single().ApproverInfos.Any(x => x.UserName == "director1"), "Role-based layers must resolve their active identity users.");
        Console.WriteLine("PASS: Per-form workflow isolation, immutable versions, pinned drafts, retained active/returned requests, old approvals, repeated approvers and stale-save protection.");
    }

    private sealed class SilentNotifier : IApprovalNotifier
    {
        public Task NotifySubmittedAsync(ApprovalActionContext context, Dictionary<string, string> additionalData = null) => Task.CompletedTask;
        public Task NotifyApprovedAsync(ApprovalActionContext context, Dictionary<string, string> additionalData = null) => Task.CompletedTask;
        public Task NotifyReturnedOrRejectedAsync(ApprovalActionContext context, bool isRejection = false, Dictionary<string, string> additionalData = null) => Task.CompletedTask;
        public Task DismissNotificationsAsync(string faxCode, string url) => Task.CompletedTask;
    }
}
