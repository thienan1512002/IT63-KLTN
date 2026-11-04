using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.Data.Enums.Workflow;

namespace Web.Models.Dtos.Workflow
{
    public class ApprovalStepDto
    {
        public string ApprovalStepId { get; set; }
        public string ApprovalCaseId { get; set; }
        public string ApprovalRoleId { get; set; }
        public bool IsCurrentStep { get; set; }
        public List<string> AvailableActions { get; set; }
        public int StepOrder { get; set; }
        public string StepTitle { get; set; }
        public string GroupTitle { get; set; }
        public string Approvers { get; set; }
        public List<ApproverInfoDto> ApproverInfos { get; set; }
        public string ApprovalType { get; set; }

        /// <summary>
        /// For display purpose only
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Status flag to indicate current status of this approval step,
        ///  which can be used for conditional logic in the UI (e.g. whether to show approval stamp).
        /// </summary>
        public ApprovalStatus ApprovalStatus { get; set; }
        public bool ProcessOnlineOffline { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string ApprovedBy { get; set; }
        public string Comment { get; set; }

    }
}
