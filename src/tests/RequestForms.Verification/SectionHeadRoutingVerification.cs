using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Web.Data.DbContexts;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow;
using Web.Services.Admin;
using Web.Services.Workflow;
using Web.Services.Workflow.Core;
using Web.Services.Workflow.Operations;

internal static class SectionHeadRoutingVerification
{
    public static async Task RunAsync(ApplicationDbContext db)
    {
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        db.ChangeTracker.Clear();
        var creator = await db.Users.AsNoTracking().SingleAsync(x => x.UserName == "uni120");
        var replacement = await db.Users.AsNoTracking().SingleAsync(x => x.UserName == "uni119");
        var type = await db.RequestTypes.AsNoTracking().SingleAsync(x => x.Code == "LEAVE");
        var resolver = new ApproverResolver(db, new UserService(db, NullLogger<UserService>.Instance));
        var preview = new RequestApprovalPreviewService(db, resolver);
        var requests = new RequestService(db,null!,new RequestBusinessService(db),new ApprovalWorkflowBuilder(db,resolver),
            new ApprovalValidator(resolver),new ApprovalActionProcessor(resolver),new RequestNumberGenerator(db),new SilentNotifier(),
            new ApprovalOperationsService(db,new LeaveAccountingService(db)));
        RequestEditDto Input(int day) => new() { RequestTypeId=type.Id,Title="Kiểm tra trưởng Bộ môn",Content="Bàn giao giáo án",Reason="Nghỉ phép năm",
            RequestedDate=new DateTime(2026,11,day),BusinessData=new() {EmployeeId=creator.Id,ReplacementEmployeeId=replacement.Id,LeaveTypeId=1,
                StartAt=new DateTime(2026,11,day),EndAt=new DateTime(2026,11,day),StartSession="AM",EndSession="PM"} };
        var oldDraft=await requests.CreateDraftAsync(Input(2),creator.UserName,[]); db.ChangeTracker.Clear();
        Check((await preview.GetAsync(type.Id,null,creator.UserName)).Steps.Single().Approvers.Single().UserName=="uni111",
            "The previous DepartmentManager rule must reproduce the reported faculty-head assignment.");
        var config=new WorkflowConfigurationService(db);var current=await config.GetAsync(type.ApprovalCaseId);
        current.Steps=[new(){StepOrder=1,Name="Trưởng Bộ phận/Bộ môn",ApproverKind=ApproverKind.SectionHead}];
        var published=await config.PublishAsync(current.CaseId,current,"verification",type.Id);db.ChangeTracker.Clear();
        Check((await config.GetAsync(published.CaseId)).Steps.Single().ApproverKind==ApproverKind.SectionHead,"Section-head assignment must round-trip through workflow configuration.");
        var newPreview=await preview.GetAsync(type.Id,null,creator.UserName);
        Check(newPreview.Steps.Single().Approvers.Single().UserName=="uni116","uni120's section-head preview must resolve only uni116, not the faculty head or backup.");
        Check((await preview.GetAsync(type.Id,oldDraft,creator.UserName)).Steps.Single().Approvers.Single().UserName=="uni111",
            "Adding the section rule must preserve old pinned drafts and their DepartmentManager semantics.");
        var fresh=await requests.CreateDraftAsync(Input(3),creator.UserName,[]);db.ChangeTracker.Clear();
        await requests.SubmitAsync(fresh,creator.UserName);db.ChangeTracker.Clear();
        var actual=await db.ApprovalDetails.AsNoTracking().Include(x=>x.ApproverInfos).SingleAsync(x=>x.RequestId==fresh);
        Check(actual.ApproverInfos.Single().UserName=="uni116","Submission must agree with preview and assign the correct section head.");
        try {await requests.ApproveAsync(fresh,"uni111","Wrong level");throw new Exception("The faculty head must not receive section-head approval permission.");}
        catch(InvalidOperationException){db.ChangeTracker.Clear();}
        await requests.ApproveAsync(fresh,"uni116","Đồng ý");db.ChangeTracker.Clear();
        Check(await db.ApprovalRequests.AnyAsync(x=>x.Id==fresh&&x.Status==RequestStatus.Completed),"The assigned section head must be able to complete approval.");
        await requests.SubmitAsync(oldDraft,creator.UserName);db.ChangeTracker.Clear();
        Check((await db.ApprovalDetails.AsNoTracking().Include(x=>x.ApproverInfos).SingleAsync(x=>x.RequestId==oldDraft)).ApproverInfos.Single().UserName=="uni111",
            "Requests pinned before the fix must still follow their original workflow.");
        Console.WriteLine("PASS: uni120 section-head routing to uni116, faculty/backup exclusion, preview/submission parity, permission enforcement and retained old workflows.");
    }
    private sealed class SilentNotifier:IApprovalNotifier
    {
        public Task NotifySubmittedAsync(ApprovalActionContext c,Dictionary<string,string> additionalData=null)=>Task.CompletedTask;
        public Task NotifyApprovedAsync(ApprovalActionContext c,Dictionary<string,string> additionalData=null)=>Task.CompletedTask;
        public Task NotifyReturnedOrRejectedAsync(ApprovalActionContext c,bool isRejection=false,Dictionary<string,string> additionalData=null)=>Task.CompletedTask;
        public Task DismissNotificationsAsync(string userName,string url)=>Task.CompletedTask;
    }
}
