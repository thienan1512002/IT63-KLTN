
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Worker.Shared.Enums.Workflow;

namespace Worker.Shared.Entities.Workflow.Core
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
        public bool IsActive { get; set; }

        [MaxLength(200)]
        public string CreatedBy { get; set; }

        [MaxLength(200)]
        public string UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

    }
}
