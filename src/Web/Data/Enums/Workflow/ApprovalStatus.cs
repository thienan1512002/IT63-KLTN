using Web.Common;
using Web.Services.Localization;

namespace Web.Data.Enums.Workflow
{
    public enum ApprovalStatus
    {
        Approved = 1,
        Confirmed,
        Rejected,
        Returned,
        Waiting,
        Bypassing,
        Agreed, // For agreement type approval steps        
    }

    // extention methods for ApprovalStatus

    // Get display name
    public static class ApprovalStatusExtensions
    {
        public static string ToDisplayName(this ApprovalStatus status, ApprovalType? approvalType, bool needToTranslate = false)
        {
            return status switch
            {
                ApprovalStatus.Approved => needToTranslate ? LanguageService.Translate("workflow_status_approved") : WorkflowConstants.ApprovalStatusText.Approved,
                ApprovalStatus.Confirmed => needToTranslate ? LanguageService.Translate("workflow_status_confirmed") : WorkflowConstants.ApprovalStatusText.Confirmed,
                ApprovalStatus.Rejected => needToTranslate ? LanguageService.Translate("workflow_status_rejected") : WorkflowConstants.ApprovalStatusText.Rejected,
                ApprovalStatus.Returned => needToTranslate ? LanguageService.Translate("workflow_status_returned") : WorkflowConstants.ApprovalStatusText.Returned,
                ApprovalStatus.Agreed => needToTranslate ? LanguageService.Translate("workflow_status_agreed") : WorkflowConstants.ApprovalStatusText.Agreed,
                ApprovalStatus.Waiting => approvalType.HasValue ? approvalType.Value.GetWaitingDisplay(needToTranslate) : string.Empty,
                _ => string.Empty,
            };
        }
    }
}