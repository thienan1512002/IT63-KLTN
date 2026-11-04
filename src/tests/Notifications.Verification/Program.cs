using System.Globalization;
using System.Text.Json;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Web.Data.DbContexts;
using Web.Data.Entities.Notification;
using Web.Models.Dtos.Workflow;
using Web.Services.Admin;
using Web.Services.Portal.Noti.Impl;
using Web.Services.Workflow;
using Web.Services.Workflow.Core;
using Web.Services.Workflow.Operations;
using Worker.Services;
using Worker.Services.Handlers;
using Worker.Settings;
using Worker.Shared.Dtos;
using Worker.Shared.Enums;
using Worker.Shared.Interfaces;
using MailDbContext = Worker.Data.WorkerDbContext;

var root = Directory.GetCurrentDirectory();
var smtpValidator = new SmtpSettingsValidator();
var gmail = new SmtpSettings { Host="smtp.gmail.com", Port=587, EnableSsl=true, FromAddress="sender@gmail.com", Username="sender@gmail.com", Password="test-app-password" };
if (smtpValidator.Validate(null,gmail).Failed) throw new Exception("Complete Gmail STARTTLS settings must validate.");
gmail.Password="";
if (!smtpValidator.Validate(null,gmail).Failed) throw new Exception("Gmail must reject missing credentials before jobs start.");
gmail.Password="test-app-password"; gmail.EnableSsl=false;
if (!smtpValidator.Validate(null,gmail).Failed) throw new Exception("Gmail must require STARTTLS.");
if (smtpValidator.Validate(null,new SmtpSettings {PickupDirectory=Path.GetTempPath()}).Failed) throw new Exception("Local pickup must remain supported without credentials.");
Console.WriteLine("PASS: Gmail STARTTLS settings, missing-credential validation and local pickup configuration.");
if (!File.Exists(Path.Combine(root, "Web/appsettings.json"))) throw new Exception("Run verification from the source root.");
var config = new ConfigurationBuilder().SetBasePath(Path.Combine(root, "Web")).AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", true).AddEnvironmentVariables().Build();
var connection = new SqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection"));
if (args.Contains("--audit"))
{
    // Read aggregate delivery state only. Never print addresses, bodies, job arguments or credentials.
    await using var sql = new SqlConnection(connection.ConnectionString); await sql.OpenAsync();
    foreach (var query in new[] {
        "IF OBJECT_ID('[HangFire].[Job]') IS NOT NULL SELECT COALESCE(StateName,'Unknown') AS State, COUNT(*) AS Total FROM [HangFire].[Job] WHERE InvocationData LIKE '%IEmailJob%' GROUP BY StateName;",
        "IF OBJECT_ID('[worker].[EmailMasterDatas]') IS NOT NULL SELECT Status AS State, COUNT(*) AS Total FROM [worker].[EmailMasterDatas] GROUP BY Status;"
    }) {
        await using var command = new SqlCommand(query, sql);
        await using var reader = await command.ExecuteReaderAsync();
        Console.WriteLine(query.Contains("HangFire") ? "EMAIL JOB STATES:" : "EMAIL DELIVERY STATES:");
        var any = false; while (await reader.ReadAsync()) { any = true; Console.WriteLine($"{reader[0]}: {reader[1]}"); }
        if (!any) Console.WriteLine("No recorded emails.");
    }
    return;
}
var host = connection.DataSource.ToLowerInvariant().Replace("tcp:", "").Split(new[] { '\\', ',' })[0];
if (!new[] { "localhost", "127.0.0.1", ".", "(local)", "(localdb)", Environment.MachineName.ToLowerInvariant() }.Contains(host))
    throw new Exception("Verification may create disposable databases only on a local SQL Server.");
var databaseName = "Codex_Notifications_Test_" + Guid.NewGuid().ToString("N");
connection.InitialCatalog = databaseName;
var pickup = Path.Combine(Path.GetTempPath(), databaseName);
await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(connection.ConnectionString, x => x.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)).Options);
bool created = false;
void Check(bool value, string message) { if (!value) throw new Exception(message); }
try
{
    created = true; await db.Database.MigrateAsync();
    await using var mailDb = new MailDbContext(new DbContextOptionsBuilder<MailDbContext>().UseSqlServer(connection.ConnectionString,
        x => x.MigrationsHistoryTable("__EFMigrationsHistory", "worker")).Options);
    await mailDb.Database.MigrateAsync();
    // Test recipients are reserved .test addresses. Pickup delivery never contacts an external SMTP server.
    await db.Users.Where(x => x.UserName == "employee1").ExecuteUpdateAsync(x => x.SetProperty(u => u.Email, "requester@example.test"));
    await db.Users.Where(x => x.UserName == "manager1").ExecuteUpdateAsync(x => x.SetProperty(u => u.Email, "manager@example.test"));
    await db.Users.Where(x => x.UserName == "director1").ExecuteUpdateAsync(x => x.SetProperty(u => u.Email, "director@example.test"));
    var jobs = new CapturedJobs();
    var enabled = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string> {
        ["Notifications:EmailEnabled"] = "true", ["BaseUrl"] = "https://approval.example.test"
    }).Build();
    var notifications = new WorkflowNotificationService(NullLogger<WorkflowNotificationService>.Instance, db, jobs, enabled);
    var notifier = new ApprovalNotifier(db, notifications);
    var resolver = new ApproverResolver(db, new UserService(db, NullLogger<UserService>.Instance));
    var requests = new RequestService(db, null!, new RequestBusinessService(db), new ApprovalWorkflowBuilder(db, resolver),
        new ApprovalValidator(resolver), new ApprovalActionProcessor(resolver), new RequestNumberGenerator(db), notifier,
        new ApprovalOperationsService(db, new LeaveAccountingService(db)));
    var type = await db.RequestTypes.SingleAsync(x => x.Code == "INTERNAL");
    const string title = "Đề xuất bổ sung học liệu <script>alert(1)</script>";
    var id = await requests.CreateDraftAsync(new RequestEditDto {
        RequestTypeId = type.Id, Title = title, Content = "Mua học liệu cho thư viện", Reason = "Phục vụ đào tạo",
        BusinessData = new() { TargetDepartmentId = 1, ProposalCategoryId = 100, ExpectedBenefit = "Bổ sung tài liệu giảng dạy" }
    }, "employee1", []);
    await requests.SubmitAsync(id, "employee1"); db.ChangeTracker.Clear();
    var request = await db.ApprovalRequests.AsNoTracking().SingleAsync(x => x.Id == id);
    var first = jobs.Emails.Single(x => x.WorkflowAction == Worker.Shared.Enums.WorkflowAction.Approval);
    Check(first.ToAddress == "manager@example.test" && !first.ToAddress.Contains("director"), "Submission must email only the current approval layer.");
    Check(first.Subject.Contains(title) && first.Subject.Contains(request.ApplicationNo), "Email subjects must identify the request title and reference.");
    Check(first.TemplateData["RequesterName"] == request.FullName && first.TemplateData["RequestTitle"] == title,
        "Email content must carry the requester's full name and actual request title.");
    var toasts = new NotiToastService(db, enabled, NullLogger<NotiToastService>.Instance);
    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("vi-VN");
    var toast = (await toasts.GetNotiToasts(false, "manager1")).Single();
    Check(toast.Content.Contains(request.FullName + " đã gửi yêu cầu") && toast.Content.Contains(title) && toast.Content.Contains(request.ApplicationNo),
        "Bell notices must show full name, request title and reference.");
    var legacy = new NotiToast { SendFor="manager1", RequestId=id, Url=$"/Requests/Details/{id}", Content="workflow_noty_submitted",
        ContentKey="workflow_noty_submitted", ContentArgsJson=JsonSerializer.Serialize(new[]{"employee1", request.ApplicationNo}), NeedTranslate=true };
    db.NotiToasts.Add(legacy); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    Check((await toasts.GetNotiToasts(false,"manager1")).Single(x=>x.Id==legacy.Id).Content.Contains(title), "Existing bell notices must also show readable names and titles.");
    Check((await db.NotiToasts.AsNoTracking().SingleAsync(x=>x.Id==legacy.Id)).ContentKey=="workflow_noty_submitted", "Legacy display enrichment must not rewrite history.");
    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
    Check((await toasts.GetNotiToasts(false,"manager1")).All(x=>x.Content.Contains("submitted the request")), "Both notice languages must resolve the new message.");
    await requests.ApproveAsync(id, "manager1", "Đồng ý"); db.ChangeTracker.Clear();
    Check(jobs.Emails.Last().WorkflowAction == Worker.Shared.Enums.WorkflowAction.Approval && jobs.Emails.Last().ToAddress=="director@example.test",
        "Advancing a request must email the next approval layer.");

    var templateService = new EmailTemplateService(new TemplateEnvironment(Path.Combine(root,"Worker")));
    var handler = new WorkflowEmailHandler(templateService);
    var dispatcher = new NotificationEmailDispatcher([handler], NullLogger<NotificationEmailDispatcher>.Instance);
    var sender = new EmailSender(Options.Create(new SmtpSettings { FromAddress="approval@example.test", FromName="Nam Viet University", PickupDirectory=pickup }),
        mailDb, dispatcher, NullLogger<EmailSender>.Instance);
    var emailId = await sender.CreateMasterDataAsync(first); await sender.SendAsync(emailId);
    var sent = await mailDb.EmailMasterDatas.Include(x=>x.Logs).SingleAsync(x=>x.Id==emailId);
    Check(sent.Status==EmailMasterDataStatus.Sent && sent.Logs.Single().IsSuccess && Directory.GetFiles(pickup,"*.eml").Length==1,
        "The actual sender must generate a deliverable message and record a successful attempt.");
    var body = dispatcher.BuildBody(sent);
    Check(body.Contains("&lt;script&gt;") && !body.Contains("<script>") && body.Contains(request.ApplicationNo) &&
        body.Contains($"https://approval.example.test/Requests/Details/{id}") && !body.Contains("{{"),
        "Workflow email HTML must encode input, show the reference and include the approval link without missing placeholders.");
    var queuedBefore=jobs.Emails.Count;
    var disabled = new WorkflowNotificationService(NullLogger<WorkflowNotificationService>.Instance, db, jobs,
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string>{{"Notifications:EmailEnabled","false"}}).Build());
    await disabled.SendMailAndNotificationAsync(Worker.Shared.Enums.WorkflowAction.Submitted, new(){FaxCode="employee1"});
    Check(jobs.Emails.Count==queuedBefore,"The explicit email-disable switch must remain supported.");
    Console.WriteLine("PASS: Real submission and approval notifications, full names/titles/references, legacy and bilingual bell messages, current/next-layer email queues, actual pickup delivery, delivery logs, HTML encoding and email-disable switch.");
}
finally
{
    if(created && db.Database.GetDbConnection().Database==databaseName && databaseName.StartsWith("Codex_Notifications_Test_")) await db.Database.EnsureDeletedAsync();
    if(Directory.Exists(pickup)) foreach(var file in Directory.GetFiles(pickup,"*.eml")) File.Delete(file);
    if(Directory.Exists(pickup) && !Directory.EnumerateFileSystemEntries(pickup).Any()) Directory.Delete(pickup);
}

sealed class CapturedJobs : IBackgroundJobClient
{
    public List<EmailRequest> Emails { get; } = [];
    public string Create(Job job, IState state) {
        if(job.Type!=typeof(IEmailJob) || job.Method.Name!=nameof(IEmailJob.ProcessEmailAsync)) throw new Exception("Unexpected email job.");
        Emails.Add((EmailRequest)job.Args.Single()); return Emails.Count.ToString();
    }
    public bool ChangeState(string jobId, IState state, string expectedState) => throw new NotSupportedException();
}
sealed class TemplateEnvironment(string contentRoot) : IHostEnvironment
{
    public string EnvironmentName {get;set;} = "Verification";
    public string ApplicationName {get;set;} = "Notifications.Verification";
    public string ContentRootPath {get;set;} = contentRoot;
    public IFileProvider ContentRootFileProvider {get;set;} = new NullFileProvider();
}
