using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Notification;
using Worker.Shared.Enums;

namespace Web.Services.Workflow.Core;

public class ApprovalNotifier : IApprovalNotifier
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkflowNotificationService _notificationService;

    public ApprovalNotifier(ApplicationDbContext context, IWorkflowNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    /// <inheritdoc />
    public async Task NotifySubmittedAsync(ApprovalActionContext context, Dictionary<string, string> additionalData = null)
    {
        var emailContent = CreateEmailContent(
            request: context.Request,
            url: context.DetailUrl,
            receivers: new HashSet<string> { context.Request.FaxCode },
            additionalData: additionalData        
        );

        await _notificationService.SendMailAndNotificationAsync(WorkflowAction.Submitted, emailContent);
    }

    /// <inheritdoc />
    public async Task NotifyApprovedAsync(ApprovalActionContext context, Dictionary<string, string> additionalData = null )
    {
        var emailContent = CreateEmailContent(context.Request, context.DetailUrl, additionalData: additionalData);

        if (context.Request.Status == RequestStatus.Completed)
        {
            // CC to all approvers in the workflow when it's completed, so everyone is aware of the completion
            var allApprovers = context.Steps.SelectMany(s => s.ApproverInfos)
                .Select(a => a.UserName)
                .Distinct()
                .ToList();
            
            emailContent.CcUserNames = allApprovers;    
            await _notificationService.SendMailAndNotificationAsync(WorkflowAction.Finish, emailContent);
            return;
        }

        // Just send to current approvers
        var nextApprovers = context.CurrentStep.ApproverInfos
            .Select(a => a.UserName)
            .ToList();

        // For online/offline manager steps: always notify main approver; also notify the online backup if main is offline
        if (context.CurrentStep.ProcessOnlineOffline && context.CurrentStep.ApprovalType == ApprovalType.Approval)
        {
            nextApprovers = await ResolveOnlineOfflineReceiversAsync(nextApprovers);
        }

        emailContent.Receivers = nextApprovers.ToHashSet();

        var action = context.CurrentStep.ApprovalType == ApprovalType.Approval
            ? WorkflowAction.Approval
            : WorkflowAction.Confirmation;

        await _notificationService.SendMailAndNotificationAsync(action, emailContent);

        if (context.CurrentStep.SendSubmissionNoty)
        {
            // Send submission notification if enabled
            await _notificationService.SendMailAndNotificationAsync(WorkflowAction.Submitted, emailContent);


        }
    }

    /// <inheritdoc />
    public async Task NotifyReturnedOrRejectedAsync(ApprovalActionContext context, bool isRejection = false, Dictionary<string, string> additionalData = null)
    {
       
        var actionStep = context.Steps.Where(d => d.ApprovedBy == context.UserId)
            .OrderByDescending(d => d.ApprovedDate).FirstOrDefault() ?? context.CurrentStep;
        var  previousApproverInfos = context.Steps
            .Where(d => d.IsApproved && actionStep != null && actionStep.StepOrder > d.StepOrder)
            .SelectMany(d => d.ApproverInfos)
            .Select(a => a.UserName)
            .Distinct()
            .ToList();
        
        

        var ccApprovers = previousApproverInfos;

        var emailContent = CreateEmailContent(
            request: context.Request,
            url: isRejection ? context.DetailUrl : context.EditUrl,
            comment: context.Comment,
            ccUserNames: ccApprovers,
            receivers: new HashSet<string> { context.Request.FaxCode },
            additionalData: additionalData
        );       
       

        var action = isRejection ? WorkflowAction.Rejected : WorkflowAction.Returned;
        await _notificationService.SendMailAndNotificationAsync(action, emailContent, context.UserId);
    }

    /// <inheritdoc />
    public async Task DismissNotificationsAsync(string faxCode, string url)
    {
        try
        {
            var toasts = await _context.NotiToasts
                .Where(x => !x.IsRead && x.Url.ToLower().Contains(url.ToLower()) && x.SendFor == faxCode)
                .ToListAsync();

            if (toasts.Count > 0)
            {
                foreach (var toast in toasts) toast.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            ex.Entries.Single().Reload();
        }
    }

    // Always include the main approver; also include the online backup if the main approver is offline.
    private async Task<List<string>> ResolveOnlineOfflineReceiversAsync(List<string> approvers)
    {
        var onlineMap = await _context.Users
            .Where(u => approvers.Any(a => a == u.UserName))
            .ToDictionaryAsync(u => u.UserName, u => u.IsOnline);

        var receivers = new List<string>();
        bool mainAdded = false;

        foreach (var approver in approvers)
        {
            if (!onlineMap.ContainsKey(approver))
                continue;

            if (!mainAdded)
            {
                // Always notify the main approver (first in list)
                receivers.Add(approver);
                mainAdded = true;

                // Main is online — no need to notify backup
                if (onlineMap[approver])
                    break;
            }
            else if (onlineMap[approver])
            {
                // Add online backup approver when main is offline
                receivers.Add(approver);
            }
        }

        return receivers;
    }

    private static EmailContent CreateEmailContent(
        ApprovalRequest request,
        string url,
        string comment = "",        
        List<string> ccUserNames = null,
        HashSet<string> receivers = null,
        Dictionary<string, string> additionalData = null
        )
    {
        return new EmailContent
        {
            RequestId = request.Id,
            DescriptionNo = request.ApplicationNo,
            SectionId = request.SectionId,
            RequestName = request.RequestName,
            RequestTitle = request.Title,
            FaxCode = request.FaxCode,
            Name = request.FullName,
            UrgencyLevel = request.UrgencyLevel,
            SubmitDate = request.SubmitDate,
            Comment = comment,
            Url = url + request.Id,
            CcUserNames = ccUserNames ?? [],
            Receivers = receivers,
            AdditionalData = additionalData ?? new Dictionary<string, string>()
        };
    }
}
