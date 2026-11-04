
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Web.Data.Enums.Workflow;
namespace Web.Data.Entities.Workflow
{
    public class ApprovalDetail
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string ApprovalStepId { get; set; }

        [ForeignKey(nameof(ApprovalStepId))]
        public ApprovalStep ApprovalStep { get; set; }

        [Required]
        [ForeignKey(nameof(ApprovalRequest))]
        public int RequestId { get; set; }
        public ApprovalRequest ApprovalRequest { get; set; }

        public string Comment { get; set; }
        [MaxLength(300)]


        public DateTime? ApprovedDate { get; set; }

        public bool IsApproved { get; set; }

        [MaxLength(200)]
        public string ApprovedBy { get; set; }

        public int StepOrder { get; set; }

        public bool IsRemove { get; set; }

        public bool IsCurrentStep { get; set; }

        /// <summary>
        /// Status of this approval step: Waiting, Approved, Rejected, Returned, Confirmed
        /// This is new version that alternates for old Status string field
        /// </summary>
        public ApprovalStatus CurrentStatus { get; set; }
        /// <summary>
        /// Indicates if this step is for a manager => need to process online/offline
        /// </summary>
        public bool ProcessOnlineOffline { get; set; }
        public bool IsRequester { get; set; }
        public bool IsLeader { get; set; }

        /// <summary>
        /// Title of this approval step. E.g., "Department Manager, Section Manager,..."
        /// </summary>
        [MaxLength(500)]
        public string StepTitle { get; set; }

        /// <summary>
        /// Group Title for this approval step
        /// </summary>
        [MaxLength(300)]
        public string GroupTitle { get; set; } = string.Empty;

        /// <summary>
        /// Available actions for this approval step (stored as bitmask integer)
        /// Examples:
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


        public ICollection<ApproverInfo> ApproverInfos { get; set; }

        /// <summary>
        /// Step to return to after return from this step
        /// </summary>
        public int? ReturnToStepId { get; set; }
        [ForeignKey(nameof(ReturnToStepId))]
        public virtual ApprovalDetail ReturnToStep { get; set; }


        /// <summary>
        /// Next step after this step
        /// </summary>
        public int? NextStepId { get; set; }
        [ForeignKey(nameof(NextStepId))]
        public virtual ApprovalDetail NextStep { get; set; }

        public ApprovalDetail NextStepDetail
        {
            get
            {
                if (NextStepId.HasValue)
                {
                    if (NextStep == null)
                    {
                        throw new NullReferenceException("NextStep is null but NextStepId has value");
                    }
                    return NextStep;
                }

                return null;
            }
        }


        /// <summary>
        /// Check if specific action is available for this step
        /// </summary>
        /// <param name="action">Action to check</param>
        /// <returns>True if action is available</returns>
        public bool HasAction(ApprovalActionType action)
        {
            return ActionTypes.HasAction(action);
        }


        /// <summary>
        /// Send submittion notification to submitter when this step is reached
        /// </summary>
        public bool SendSubmissionNoty { get; set; }


        /*------------------Bypass Settings-----------------------*/
        /// <summary>
        /// Is bypass enabled for this step 
        /// </summary>
        public bool CheckBypass { get; set; }

        /// <summary>
        /// Step that need to bypass
        /// </summary>
        public int? BypassFromStepId { get; set; }
        [ForeignKey(nameof(BypassFromStepId))]
        public virtual ApprovalDetail BypassFromStep { get; set; }

        /// <summary>
        /// Step actually do approval after bypassing
        /// After approved in this step, fill the approval info back to the bypassed step (BypassFromStep)
        /// </summary>
        public int? BypassToStepId { get; set; }
        [ForeignKey(nameof(BypassToStepId))]
        public virtual ApprovalDetail BypassToStep { get; set; }

    }

}
