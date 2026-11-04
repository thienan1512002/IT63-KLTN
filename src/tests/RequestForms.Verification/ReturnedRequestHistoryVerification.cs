using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Web.Controllers.Workflow;
using Web.Data.DbContexts;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow;
using Web.Services.Admin;
using Web.Services.Localization;
using Web.Services.Workflow;
using Web.Services.Workflow.Core;
using Web.Services.Workflow.Operations;

internal static class ReturnedRequestHistoryVerification
{
    public static async Task RunAsync(ApplicationDbContext db)
    {
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        db.ChangeTracker.Clear();
        var creator = await db.Users.AsNoTracking().SingleAsync(x => x.UserName == "uni120");
        var replacement = await db.Users.AsNoTracking().SingleAsync(x => x.UserName == "uni119");
        var type = await db.RequestTypes.AsNoTracking().SingleAsync(x => x.Code == "LEAVE");
        var config = new WorkflowConfigurationService(db);
        var workflow = await config.GetAsync(type.ApprovalCaseId);
        workflow.Steps = [
            new() { StepOrder=1, Name="Trưởng Bộ môn", ApproverKind=ApproverKind.SectionHead },
            new() { StepOrder=2, Name="Trưởng Khoa", ApproverKind=ApproverKind.User, ApproverUserName="uni111" }
        ];
        var published = await config.PublishAsync(workflow.CaseId, workflow, "verification", type.Id);
        db.ChangeTracker.Clear();
        var resolver = new ApproverResolver(db, new UserService(db, NullLogger<UserService>.Instance));
        var requests = new RequestService(db, null!, new RequestBusinessService(db), new ApprovalWorkflowBuilder(db,resolver),
            new ApprovalValidator(resolver), new ApprovalActionProcessor(resolver), new RequestNumberGenerator(db), new SilentNotifier(),
            new ApprovalOperationsService(db, new LeaveAccountingService(db)));
        var input = new RequestEditDto { RequestTypeId=type.Id, Title="Kiểm tra lịch sử trả lại", Content="Bàn giao công việc", Reason="Nghỉ phép năm",
            RequestedDate=new(2026,11,4), BusinessData=new() { EmployeeId=creator.Id, ReplacementEmployeeId=replacement.Id,
                LeaveTypeId=1, StartAt=new(2026,11,4), EndAt=new(2026,11,4), StartSession="AM", EndSession="PM" } };
        var id = await requests.CreateDraftAsync(input, creator.UserName, []); db.ChangeTracker.Clear();
        await requests.SubmitAsync(id, creator.UserName); db.ChangeTracker.Clear();
        const string firstComment = "Bổ sung nội dung bàn giao.\n<script>alert('unsafe')</script>";
        const string secondComment = "Điều chỉnh ngày nghỉ và người nhận bàn giao.";
        await requests.ReturnAsync(id, "uni116", firstComment); db.ChangeTracker.Clear();
        var firstReturn = await requests.GetDetailsAsync(id, creator.UserName, false);
        Check(firstReturn.CanEdit && firstReturn.Status==RequestStatus.Returned, "The owner must be able to edit a returned request.");
        var firstEvent = firstReturn.Histories.Single(x=>x.Action==ApprovalAction.Returned);
        var firstActor = await db.AppUsers.Where(x=>x.UserName=="uni116").Select(x=>x.FullName).SingleAsync();
        Check(firstEvent.StepOrder==1 && firstEvent.StepName=="Trưởng Bộ môn" && firstEvent.ActionBy==firstActor && firstEvent.Comment==firstComment,
            "Return history must expose the real actor, assigned step, timestamp and original multiline comment.");
        Check(firstEvent.Id>0 && firstEvent.ActionAt>firstReturn.CreatedAt, "History entries must have persisted identities and action times.");
        try { await requests.GetDetailsAsync(id,"uni001",false); throw new Exception("Unrelated users must not see return comments."); }
        catch (UnauthorizedAccessException) { }
        await requests.UpdateDraftAsync(id,input,creator.UserName,[]); db.ChangeTracker.Clear();
        await requests.SubmitAsync(id,creator.UserName); db.ChangeTracker.Clear();
        await requests.ApproveAsync(id,"uni116","Đồng ý sau bổ sung"); db.ChangeTracker.Clear();
        await requests.ReturnAsync(id,"uni111",secondComment); db.ChangeTracker.Clear();
        // Republishing the form must not rename the request's already assigned layers.
        var newer = await config.GetAsync(published.CaseId);
        newer.Steps[1].Name="Tên tầng cho phiếu mới";
        await config.PublishAsync(newer.CaseId,newer,"verification",type.Id); db.ChangeTracker.Clear();
        var secondReturn = await requests.GetDetailsAsync(id,creator.UserName,false);
        var returns = secondReturn.Histories.Where(x=>x.Action==ApprovalAction.Returned).ToArray();
        Check(returns.Length==2 && returns[0].Id==firstEvent.Id && returns[0].Comment==firstComment &&
            returns[1].StepOrder==2 && returns[1].StepName=="Trưởng Khoa" && returns[1].Comment==secondComment,
            "Returns from separate layers and submission cycles must remain available with snapshot names.");
        Check(secondReturn.Histories.Any(x=>x.Action==ApprovalAction.Approved && x.StepOrder==1 && x.Comment=="Đồng ý sau bổ sung"),
            "Previous-layer approval comments must appear alongside the return events.");

        // Render the actual compiled Razor partials, including nested history and HTML encoding.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName=typeof(RequestController).Assembly.GetName().Name, Args=[] });
        builder.Logging.ClearProviders();
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(RequestController).Assembly);
        builder.Services.AddSingleton<LanguageService>();
        await using var app = builder.Build();
        using var scope = app.Services.CreateScope();
        async Task<string> Render(RequestDetailsDto model)
        {
            var context = new ActionContext(new DefaultHttpContext { RequestServices=scope.ServiceProvider }, new RouteData(), new ActionDescriptor());
            var engine = scope.ServiceProvider.GetRequiredService<IRazorViewEngine>();
            var view = engine.GetView(null,"/Views/Workflow/Request/_RequestReturnHistory.cshtml",false);
            Check(view.Success,"Returned-request history Razor view must be discoverable.");
            var data = new ViewDataDictionary<RequestDetailsDto>(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model=model };
            using var output = new StringWriter();
            await view.View.RenderAsync(new ViewContext(context,view.View,data,
                new TempDataDictionary(context.HttpContext,scope.ServiceProvider.GetRequiredService<ITempDataProvider>()),output,new HtmlHelperOptions()));
            return output.ToString();
        }
        var html = await Render(secondReturn);
        var readable = System.Net.WebUtility.HtmlDecode(html);
        Check(readable.Contains(secondComment) && readable.Contains(firstComment) && readable.Contains("Trưởng Bộ môn") && readable.Contains("Trưởng Khoa") &&
            readable.Contains("2 lần trả lại") && readable.Contains("Đồng ý sau bổ sung"), "The editor must render latest feedback and every prior layer's comments.");
        Check(!html.Contains("<script>") && html.Contains("&lt;script&gt;") && readable.IndexOf(secondComment)<readable.IndexOf("<details"),
            "Latest feedback must be visible without expanding history and comments must be safely encoded.");
        Check(string.IsNullOrWhiteSpace(await Render(new RequestDetailsDto())), "A fresh draft must not display a return warning.");
        await requests.SubmitAsync(id,creator.UserName); db.ChangeTracker.Clear();
        var resubmitted = await requests.GetDetailsAsync(id,creator.UserName,false);
        Check(resubmitted.Histories.Count(x=>x.Action==ApprovalAction.Returned)==2 &&
            resubmitted.Steps.Single(x=>x.StepOrder==2).Comment==null, "Resubmission may reset the current step but must preserve both history comments.");
        var sentHtml = System.Net.WebUtility.HtmlDecode(await Render(resubmitted));
        Check(sentHtml.Contains("Ý kiến trả lại gần nhất") && !sentHtml.Contains("Yêu cầu đang được trả lại để bổ sung"),
            "Historical feedback must not describe a resubmitted request as currently returned.");
        await requests.ApproveAsync(id,"uni111","Đồng ý sau điều chỉnh"); db.ChangeTracker.Clear();
        var completed = await requests.GetDetailsAsync(id,creator.UserName,false);
        Check(completed.Status==RequestStatus.Completed && completed.Histories.Count(x=>x.Action==ApprovalAction.Returned)==2,
            "Approval completion must retain the complete return history.");
        Console.WriteLine("PASS: multi-layer return comments, repeated submission history, snapshot titles, access control and compiled Razor rendering/HTML encoding.");
    }

    private sealed class SilentNotifier : IApprovalNotifier
    {
        public Task NotifySubmittedAsync(ApprovalActionContext c,Dictionary<string,string> additionalData=null)=>Task.CompletedTask;
        public Task NotifyApprovedAsync(ApprovalActionContext c,Dictionary<string,string> additionalData=null)=>Task.CompletedTask;
        public Task NotifyReturnedOrRejectedAsync(ApprovalActionContext c,bool isRejection=false,Dictionary<string,string> additionalData=null)=>Task.CompletedTask;
        public Task DismissNotificationsAsync(string userName,string url)=>Task.CompletedTask;
    }
}
