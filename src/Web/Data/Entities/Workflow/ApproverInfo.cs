using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Data.Entities.Workflow
{
    public class ApproverInfo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ApprovalDetailId { get; set; }

        [ForeignKey(nameof(ApprovalDetailId))]
        public ApprovalDetail ApprovalDetail { get; set; }

        [MaxLength(200)]
        public string UserName { get; set; }

        [MaxLength(500)]
        public string FullName { get; set; }

        [MaxLength(500)]
        public string Email { get; set; }

        [MaxLength(11)]
        public string PhoneNumber { get; set; }

        [MaxLength(200)]
        public string SectionName { get; set; }

        public int SectionId { get; set; }

        [MaxLength(100)]
        public string Position { get; set; }
    }
}