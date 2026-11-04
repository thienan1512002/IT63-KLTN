
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Web.Data.Enums.Workflow;

namespace Web.Data.Entities.Workflow
{
    public class ApprovalRole
    {
        [Key]
        [MaxLength(50)]
        public string Id { get; set; }

        [MaxLength(500)]
        [Required]
        public string Title { get; set; }

        [MaxLength(200)]
        public string LocalizationKey { get; set; }

        public int BelongSite { get; set; }

        [MaxLength(200)]
        public string FormCode { get; set; }

        public ApprovalRoleType RoleType { get; set; }
        public int? DepartmentId { get; set; }
        public int? SectionId { get; set; }
        public int? FormApprovalId { get; set; }
        [MaxLength(256)]
        public string IdentityRoleName { get; set; }
        public bool IsActive { get; set; }

        [MaxLength(200)]
        public string CreatedBy { get; set; }

        [MaxLength(200)]
        public string UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

    }

    public class ManagerResolveResult
    {
        /// <summary>
        /// Whether current user is main approver in a Section or Department
        /// </summary>
        public bool IsManager { get; set; }
        public bool IsDeputy {get;set;}
        public ApprovalRoleType RoleType { get; set; }
        
        /// <summary>
        /// Section Manager | Department Manager | BOD
        /// </summary>
        public string StepTitle { get; set; }

        /// <summary>
        /// Department used by the runtime approval role override.
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// Section used by the runtime approval role override.
        /// </summary>
        public int? SectionId { get; set; }
    }
}
