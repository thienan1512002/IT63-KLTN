using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Web.Data.Enums.Workflow;

namespace Web.Data.Entities.Workflow
{
    public class ApprovalStep
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; }
        public string ApprovalRoleId { get; set; }

        [ForeignKey(nameof(ApprovalRoleId))]

        public ApprovalRole ApprovalRole { get; set; }

        public string ApprovalCaseId { get; set; }

        [ForeignKey(nameof(ApprovalCaseId))]
        public ApprovalCase ApprovalCase { get; set; }


        /// <summary>
        /// Step order in the approval case
        /// </summary>
        public int StepOrder { get; set; }

        public int Revision { get; set; } = 1;


        /*---------------------[20251222 - Added]---------------------*/
        /// <summary>
        /// Actions permitted on this step (Approve, Reject, Return, Confirm)
        /// /// Examples:
        /// - 1 = Approve only
        /// - 3 = Approve + Reject (1 + 2)
        /// - 7 = Approve + Reject + Return (1 + 2 + 4)
        /// - 15 = Approve + Reject + Return + Confirm (1 + 2 + 4 + 8)
        /// </summary>
        [Column(TypeName = "int")]
        public ApprovalActionType ActionTypes { get; set; }

        /// <summary>
        /// Type of this approval step: Approval, Confirmation
        /// </summary>
        [Column(TypeName = "int")]
        public ApprovalType ApprovalType { get; set; }

        /// <summary>
        /// Step that continues to after returned from this step
        /// </summary>
        public string ReturnStepId { get; set; }
        [ForeignKey(nameof(ReturnStepId))]
        public virtual ApprovalStep ReturnStep { get; set; }

        /// <summary>
        /// Next step after this step
        /// </summary>
        public string NextStepId { get; set; }
        [ForeignKey(nameof(NextStepId))]
        public virtual ApprovalStep NextStep { get; set; }

        /// <summary>
        /// Send submittion notification to submitter when this step is reached
        /// </summary>
        public bool SendSubmissionNoty { get; set; }

        /// <summary>
        /// Whether this step need to process online/offline approvers.
        /// </summary>
        public bool ProcessOnlineOffline { get; set; }


        [MaxLength(300)]
        public string GroupTitle { get; set; } = string.Empty;


        /*------------------Bypass Settings-----------------------*/
        /// <summary>
        /// Is bypass enabled for this step 
        /// </summary>
        public bool CheckBypass { get; set; }

        /// <summary>
        /// Step actually do approval after bypassing
        /// After approved in this step, fill the approval info back to the bypassed step (BypassFromStep)
        /// </summary>
        public string BypassToStepId { get; set; }
        [ForeignKey(nameof(BypassToStepId))]
        public virtual ApprovalStep BypassToStep { get; set; }

        /// <summary>
        /// Indicates if this step is a parent step (has child steps)
        /// </summary>
        public bool? IsParentStep { get; set; }

        /// <summary>
        /// Soft delete flag for this step.
        /// Steps with IsRemoved=true will be ignored in the new approval process,
        /// but still kept in the database for historical purposes.
        /// </summary>
        public bool IsRemoved { get; set; }
    }
}
