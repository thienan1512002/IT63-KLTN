
using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;

namespace Web.Services.Workflow
{
    /// <summary>
    /// Context for processing an approval action
    /// </summary>
    public class ApprovalActionContext
    {
        public ApprovalRequest Request { get; set; }
        //public ApprovalDetail CurrentStep { get; set; }

        /// <summary>
        /// Type of action being performed (Approve, Reject, Return, Confirm)
        /// </summary>
        public ApprovalActionType ActionType { get; set; }

        // For sake of Approval Type bypass 
        public List<ApprovalDetail> Steps { get; set; }

        public Dictionary<string, bool> UserStatusMap { get; set; } // Map of username to online status for approvers in the current step

        /// <summary>
        /// ID of the user performing the action
        /// </summary>
        public string UserId { get; set; }
        public string Comment { get; set; }

        public string EditUrl { get; set; }

        public string DetailUrl { get; set; }

        public bool Validated { get; set; } = false;

        // Getter to get the current step based on Steps list, in case CurrentStep is not explicitly set (e.g. after bypass)
        public ApprovalDetail CurrentStep
        {
            get
            {
                if (Steps == null || !Steps.Any())
                    throw new InvalidOperationException("Steps must be provided to determine the current step.");

                return Steps.FirstOrDefault(s => s.IsCurrentStep);
            }
        }

        public ApprovalDetail NextStep
        {
            get
            {
                if (Steps == null || !Steps.Any())
                    throw new InvalidOperationException("Steps must be provided to determine the next step.");

                return Steps.FirstOrDefault(s => s.Id == CurrentStep.NextStepId);
            }
        }


    }
}