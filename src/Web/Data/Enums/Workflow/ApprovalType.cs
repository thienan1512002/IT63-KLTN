using System.ComponentModel;
using Web.Common;
using Web.Services.Localization;
namespace Web.Data.Enums.Workflow
{
    public enum ApprovalType
    {
        [Description("Approval")]
        Approval = 1,

        [Description("Confirm")]
        Confirmation,

        [Description("Agreement")]
        Agreement,
    }


    // Extention method to get wating confirmation display

    public static class ApprovalTypeExtensions
    {
        public static string ToDisplayName(this ApprovalType approvalType)
        {
            return approvalType switch
            {
                ApprovalType.Approval => LanguageService.Translate("workflow_approval_type_approval"),
                ApprovalType.Confirmation => LanguageService.Translate("workflow_approval_type_confirm"),
                ApprovalType.Agreement => LanguageService.Translate("workflow_approval_type_agreement"),
                _ => "Unknown",
            };
        }
        /// <summary>
        /// Get waiting display text based on approval type
        /// </summary>
        public static string GetWaitingDisplay(this ApprovalType approvalType, bool needToTranslate = false)
        {
            return approvalType switch
            {
                ApprovalType.Approval => needToTranslate ? LanguageService.Translate("workflow_status_waiting_approval") : WorkflowConstants.WaitingText.ApprovalWaiting,
                ApprovalType.Confirmation => needToTranslate ? LanguageService.Translate("workflow_status_waiting_confirm") : WorkflowConstants.WaitingText.ConfirmWaiting,
                ApprovalType.Agreement => needToTranslate ? LanguageService.Translate("workflow_status_waiting_agreement") : WorkflowConstants.WaitingText.AgreementWaiting,
                _ => "Unknown",
            };
        }

        /// <summary>
        /// Convert ApprovalType to corresponding ApprovalStatus when approved
        /// E.g. Approval => Approved, Confirmation => Confirmed, Agreement => Agreed
        /// </summary>
        public static ApprovalStatus ConvertToStatus(this ApprovalType approvalType)
        {
            return approvalType switch
            {
                ApprovalType.Approval => ApprovalStatus.Approved,
                ApprovalType.Confirmation => ApprovalStatus.Confirmed,
                ApprovalType.Agreement => ApprovalStatus.Agreed,
                _ => throw new ArgumentOutOfRangeException(nameof(approvalType), $"Not expected approval type value: {approvalType}"),
            };
        }

        public static ApprovalActionType GetActionType(this ApprovalType approvalType)
        {
            return approvalType switch
            {
                ApprovalType.Approval => ApprovalActionType.Approve,
                ApprovalType.Confirmation => ApprovalActionType.Confirm,
                ApprovalType.Agreement => ApprovalActionType.Agree,
                _ => throw new ArgumentOutOfRangeException(nameof(approvalType), $"Not expected approval type value: {approvalType}"),
            };
        }

    }

}