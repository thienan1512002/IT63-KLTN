using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Globalization;
using Web.Common;
using Web.Data.DbContexts;
using Web.Data.Entities.Notification;
using Web.Models.Dtos.Notification;
using Web.Services.Localization;
using Worker.Shared.Dtos;
using Worker.Shared.Enums;
using Worker.Shared.Interfaces;

namespace Web.Services.Workflow
{
    public class WorkflowNotificationService : IWorkflowNotificationService
    {
        private readonly ILogger<WorkflowNotificationService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly string _baseUrl;
        private readonly bool _emailEnabled;

        public WorkflowNotificationService(
            ILogger<WorkflowNotificationService> logger,
            ApplicationDbContext context,
            IBackgroundJobClient backgroundJobClient,
            IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _backgroundJobClient = backgroundJobClient;

            _baseUrl = configuration["BaseUrl"];
            _emailEnabled = !bool.TryParse(configuration["Notifications:EmailEnabled"], out var enabled) || enabled;

        }

        public async Task SendMailAndNotificationAsync(WorkflowAction action, EmailContent content, string currentApprover = null)
        {
            await SaveNotificationsAsync(action, content);
            if (!_emailEnabled)
            {
                _logger.LogInformation("Workflow email is disabled [Action={Action}, RequestId={RequestId}]", action, content.RequestId);
                return;
            }
            try
            {
                await EnqueueWorkflowEmailAsync(action, content, currentApprover);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending notifications for workflow action {Action} and request {RequestId}", action, content.RequestId);
            }
        }

        #region Public — Notification (NotiToast)

        public async Task SaveNotificationsAsync(
            WorkflowAction action,
            EmailContent content
           )
        {
            var notifications = BuildNotifications(action, content);
            if (notifications.Count == 0) return;

            _context.NotiToasts.AddRange(notifications);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Public — Email (Hangfire enqueue)

        public async Task EnqueueWorkflowEmailAsync(
            WorkflowAction action,
            EmailContent content,
            string currentApprover = null)
        {
            var recipientEmails = await ResolveRecipientEmailsAsync(action, content.FaxCode, content.Receivers?.ToList() ?? []);
            if (recipientEmails.Count == 0)
            {
                _logger.LogWarning("No recipient email configured [Action={Action}, RequestId={RequestId}]", action, content.RequestId);
                return;
            }

            var approver = (action is WorkflowAction.Returned or WorkflowAction.Rejected) && !string.IsNullOrEmpty(currentApprover)
                ? await GetApproverDisplayNameAsync(currentApprover)
                : string.Empty;

            // if (action is WorkflowAction.Finish or WorkflowAction.Returned or WorkflowAction.Rejected)
            //     content.SubmitDate = DateTime.Now;

            var request = await BuildEmailRequestAsync(action, content, recipientEmails, approver);
            _backgroundJobClient.Enqueue<IEmailJob>(x => x.ProcessEmailAsync(request));
        }

        #endregion

        #region Notification Builders

        private static List<NotiToast> BuildNotifications(
            WorkflowAction action,
            EmailContent content)
        {
            return action switch
            {
                WorkflowAction.Submitted => [],

                WorkflowAction.Approval or WorkflowAction.Confirmation
                    => BuildReceiverNotifications(content.Receivers, content, "workflow_noty_submitted_detail",
                        string.IsNullOrWhiteSpace(content.Name) ? content.FaxCode : content.Name,
                        string.IsNullOrWhiteSpace(content.RequestTitle) ? content.RequestName : content.RequestTitle, content.DescriptionNo),

                WorkflowAction.Finish
                    => [CreateNoti(content.FaxCode, "workflow_noty_finished", content, content.DescriptionNo)],

                WorkflowAction.Returned
                    => [CreateReturnRejectNoti(content.Receivers, content, "workflow_noty_returned", content.DescriptionNo)],

                WorkflowAction.Rejected
                    => [CreateReturnRejectNoti(content.Receivers, content, "workflow_noty_rejected", content.DescriptionNo)],

                _ => throw new ArgumentOutOfRangeException(nameof(action))
            };
        }

        private static List<NotiToast> BuildReceiverNotifications(
            IEnumerable<string> receivers,
            EmailContent content,
            string notiKey,
            params string[] args)
        {
            if (receivers == null) return [];

            return receivers
                .Select(userName => CreateNoti(userName, notiKey, content, args))
                .ToList();
        }

        private static NotiToast CreateNoti(
            string sendFor, string notiKey, EmailContent content, params string[] args)
        {
            var argsJson = args != null && args.Length > 0 ? System.Text.Json.JsonSerializer.Serialize(args) : null;

            return new NotiToast
            {
                Url = content.Url,
                Title = content.RequestName + " - " + content.DescriptionNo,
                RequestId = content.RequestId > 0 ? content.RequestId : null,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                Content = notiKey,
                ContentKey = notiKey,
                ContentArgsJson = argsJson,
                SendFor = sendFor,
                NeedTranslate = true,
            };
        }

        private static NotiToast CreateReturnRejectNoti(
            HashSet<string> receivers, EmailContent content, string notiKey, params string[] args)
        {
            var sendFor = receivers is { Count: 1 } ? receivers.First() : content.FaxCode;
            return CreateNoti(sendFor, notiKey, content, args);
        }

        #endregion

        #region Email Builders

        private async Task<EmailRequest> BuildEmailRequestAsync(
            WorkflowAction action,
            EmailContent content,
            List<string> recipientEmails,
            string approver)
        {
            var requester = await _context.Users.AsNoTracking()
                .Include(u => u.BelongSite)
                .FirstOrDefaultAsync(u => u.UserName == content.FaxCode);

            var sectionName = await _context.Sections.AsNoTracking()
                .Where(s => s.Id == content.SectionId)
                .Select(s => $"{s.EngName} / {s.Name}")
                .FirstOrDefaultAsync() ?? string.Empty;

            var requestTitle = string.IsNullOrWhiteSpace(content.RequestTitle) ? content.RequestName : content.RequestTitle;
            var requesterName = string.IsNullOrWhiteSpace(content.Name) ? requester?.FullName ?? content.FaxCode : content.Name;

            var emailContent =  new EmailRequest
            {
                ToAddress = string.Join(", ", recipientEmails),
                CcAddresses = await GetEmailsAsync(content.CcUserNames),
                Subject = $"[NVU] {GetSubject(action)} - {requestTitle} ({content.DescriptionNo})".Replace('\r', ' ').Replace('\n', ' '),
                NotificationType = Worker.Shared.Enums.NotificationType.Workflow,
                WorkflowAction = action,
                TemplateData = new Dictionary<string, string>
                {
                    ["RequestName"] = content.RequestName ?? "",
                    ["RequestTitle"] = requestTitle ?? "",
                    ["ApplicationNo"] = content.DescriptionNo ?? "",
                    ["RequesterName"] = requesterName,
                    ["DateRequest"] = content.SubmitDate.ToString(Contants.DateFormat.DateTimeFormat),
                    ["Link"] = $"{_baseUrl}{content.Url}",
                    ["Urgent"] = content.UrgencyLevel ? "Urgent/ Khẩn cấp" : "Normal/ Bình thường",
                    ["Note"] = content.Comment ?? "",
                    ["ApproverReturn"] = approver,
                    ["BelongSite"] = requester?.BelongSite?.Name ?? "",
                    ["RequesterSection"] = sectionName
                }
            };

            if(content.AdditionalData != null)
            {
                foreach (var kvp in content.AdditionalData)
                {
                    emailContent.TemplateData.TryAdd(kvp.Key, kvp.Value);
                }

            }

            return emailContent;
        }

        #endregion

        #region Recipient Resolvers

        private async Task<List<string>> ResolveRecipientEmailsAsync(
            WorkflowAction action, string requesterUserName, List<string> receivers)
        {
            return action switch
            {
                WorkflowAction.Submitted or WorkflowAction.Finish
                    => await GetEmailsAsync(requesterUserName),

                WorkflowAction.Approval or WorkflowAction.Confirmation
                    => await GetEmailsAsync(receivers),

                WorkflowAction.Returned or WorkflowAction.Rejected
                    => receivers is { Count: 1 } ? await GetEmailsAsync(receivers[0]) : [],

                _ => []
            };
        }

        private async Task<List<string>> GetEmailsAsync(string userName)
        {
            var email = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserName == userName)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            return string.IsNullOrWhiteSpace(email) ? [] : [email];
        }

        private async Task<List<string>> GetEmailsAsync(IEnumerable<string> userNames)
        {
            if (userNames == null) return [];

            var emails = await _context.Users
                .AsNoTracking()
                .Where(u => userNames.Contains(u.UserName) && u.Email != null && u.Email != "")
                .Select(u => u.Email)
                .ToListAsync();
            return emails.Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private async Task<string> GetApproverDisplayNameAsync(string rejectorUserName)
        {
            if (string.IsNullOrEmpty(rejectorUserName)) return string.Empty;

            var fullName = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserName == rejectorUserName)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();

            return !string.IsNullOrEmpty(fullName)
                ? $"{fullName} - {rejectorUserName}"
                : rejectorUserName;
        }

        #endregion

        #region Helpers

        private static string GetSubject(WorkflowAction action) => action switch
        {
            WorkflowAction.Submitted => "Submission Request Notice/ Thông báo gửi yêu cầu",
            WorkflowAction.Approval => "Approval Request Notice/ Thông báo yêu cầu phê duyệt",
            WorkflowAction.Confirmation => "Confirm Request Notice/ Thông báo yêu cầu xác nhận",
            WorkflowAction.Finish => "Finish Request Notice/ Thông báo yêu cầu hoàn tất",
            WorkflowAction.Returned => "Return Request Notice/ Thông báo yêu cầu trả lại",
            WorkflowAction.Rejected => "Reject Request Notice/ Thông báo yêu cầu bị từ chối",
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };

        #endregion
    }
}
