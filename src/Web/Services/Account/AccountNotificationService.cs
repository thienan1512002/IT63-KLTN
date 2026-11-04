using Hangfire;
using Microsoft.EntityFrameworkCore;
using Web.Common;
using Web.Data.DbContexts;
using Web.Data.Entities.Notification;
using Worker.Shared.Dtos;
using Worker.Shared.Enums;
using Worker.Shared.Interfaces;

namespace Web.Services.Account;


public class AccountNotificationService : IAccountNotificationService
{
    // ─── Inactive (User Status) ───────────────────────────────────────────────────
    private readonly ApplicationDbContext db;
    private readonly ILogger<AccountNotificationService> logger;
    private readonly IBackgroundJobClient backgroundJobClient;

    private readonly string _baseUrl;
    public AccountNotificationService(
        ApplicationDbContext db,
        ILogger<AccountNotificationService> logger,
        IBackgroundJobClient backgroundJobClient,
        IConfiguration configuration
        )
    {
        this.db = db;
        this.logger = logger;
        this.backgroundJobClient = backgroundJobClient;
        _baseUrl = configuration.GetValue<string>("BaseUrl")?.TrimEnd('/') ?? "";
    }
    public async Task SendInactiveNotificationAsync(
        string mainApproverUserName,
        DateTime from,
        DateTime to,
        string comment)
    {
        var mainApproverFullName = await db.Users
            .AsNoTracking()
            .Where(u => u.UserName == mainApproverUserName)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync();

        if (String.IsNullOrWhiteSpace(mainApproverFullName))
        {
            logger.LogWarning("SendInactiveNotificationAsync: user {UserName} not found.", mainApproverUserName);
            return;
        }

        var backupUserNames = await db.Sections
            .AsNoTracking()
            .Where(s => s.MainApprover == mainApproverUserName && !string.IsNullOrWhiteSpace(s.BackupApprover))
            .Select(s => s.BackupApprover)
            .Union(db.Departments
                .AsNoTracking()
                .Where(d => d.MainApprover == mainApproverUserName && !string.IsNullOrWhiteSpace(d.BackupApprover))
                .Select(d => d.BackupApprover))
            .Distinct()
            .ToListAsync();

        if (backupUserNames.Count == 0)
        {
            logger.LogInformation("No backup approvers found for {UserName}, skipping notification.", mainApproverUserName);
            return;
        }

        var URL_PATH = $"{_baseUrl}{WorkflowConstants.WorkflowPath.WorkflowListPath}";

        // NotiToast — in-app notification for each backup approver
        var notiContent = $"Main Approver {mainApproverFullName} ({mainApproverUserName}) will be inactive from {from:Contants.DateFormat.LongDateTimeFormat} to {to:Contants.DateFormat.LongDateTimeFormat}";
        var arguments = new string[] { mainApproverFullName, mainApproverUserName, from.ToString(Contants.DateFormat.LongDateTimeFormat), to.ToString(Contants.DateFormat.LongDateTimeFormat) };

        db.NotiToasts.AddRange(backupUserNames.Select(backup => new NotiToast
        {
            SendFor = backup,
            Content = notiContent,
            ContentKey = "notitoast_offline_notice",
            IsRead = false,
            NeedTranslate = true,
            ContentArgsJson = System.Text.Json.JsonSerializer.Serialize(arguments),
            Url = URL_PATH,
        }));
        await db.SaveChangesAsync();



        // Email — one email listing all backup approvers as recipients
        var recipientEmails = await db.Users
            .AsNoTracking()
            .Where(u => backupUserNames.Contains(u.UserName) && !string.IsNullOrWhiteSpace(u.Email))
            .Select(u => u.Email)
            .Distinct()
            .ToListAsync();

        if (recipientEmails.Count == 0) return;

        var request = new EmailRequest
        {
            ToAddress = string.Join(", ", recipientEmails),
            Subject = $"[NVU] Main Approver Inactive Notice/ Thông báo người phê duyệt chính không hoạt động - {mainApproverFullName} ({mainApproverUserName})",
            NotificationType = NotificationType.UserStatus,
            TemplateData = new Dictionary<string, string>
            {
                ["CreateName"] = $"{mainApproverFullName} - {mainApproverUserName}",
                ["DateTime"] = $"{from.ToString(Contants.DateFormat.LongDateTimeFormat)} → {to.ToString(Contants.DateFormat.LongDateTimeFormat)}",
                ["Title"] = comment ?? "",
                ["Detail"] = URL_PATH,
            }
        };

        backgroundJobClient.Enqueue<IEmailJob>(x => x.ProcessEmailAsync(request));

        logger.LogInformation(
            "Inactive notification enqueued for {MainApprover} → {Count} backup approver(s).",
            mainApproverUserName, recipientEmails.Count);
    }

    // ─── Reset Password ───────────────────────────────────────────────────────────

    public Task SendResetPasswordEmailAsync(string toAddress, string recipientName, string resetLink, string refreshLink)
    {
        var request = new EmailRequest
        {
            ToAddress = toAddress,
            Subject = "[NVU] Reset your password/ Đặt lại mật khẩu của bạn",
            NotificationType = NotificationType.ResetPassword,
            TemplateData = new Dictionary<string, string>
            {
                ["RecipientName"] = recipientName,
                ["ResetLink"] = resetLink,
                ["RefreshLink"] = refreshLink,
            }
        };

        backgroundJobClient.Enqueue<IEmailJob>(x => x.ProcessEmailAsync(request));

        logger.LogInformation("Reset-password email enqueued for {Email}.", toAddress);
        return Task.CompletedTask;
    }
}
