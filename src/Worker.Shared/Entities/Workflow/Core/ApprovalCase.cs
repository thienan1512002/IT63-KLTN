using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Worker.Shared.Entities.Workflow.Core
{
    public class ApprovalCase
    {
        [Key]
        [MaxLength(50)]
        public string CaseId { get; set; }

        [MaxLength(200)]
        public string CaseName { get; set; }

        [MaxLength(300)]
        public string Description { get; set; }

        [MaxLength(100)]
        public string BelongSite { get; set; }

        [MaxLength(100)]
        public string FormCode { get; set; }

        public ICollection<ApprovalStep> ApprovalSteps { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        [MaxLength(100)]
        public string UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

    }
}
