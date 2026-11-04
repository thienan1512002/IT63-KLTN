using System.ComponentModel;
namespace Worker.Shared.Enums.Workflow
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