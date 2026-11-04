using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Worker.Shared.Enums.Workflow;

namespace Worker.Shared.Entities.Workflow.Core
{
    public class ApprovalRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string ApplicationNo { get; set; }

        [MaxLength(200)]
        public string DescriptionNo { get; set; }

        [Required]
        public DateTime SubmitDate { get; set; }

        [Required]
        [MaxLength(400)]

        public string RequestName { get; set; }

        [Required]
        public bool UrgencyLevel { get; set; }

        //User Info
        [Required]
        [MaxLength(200)]
        public string FaxCode { get; set; }


        [MaxLength(100)]
        public string SiteName { get; set; }

        // public int SiteId { get; set; }
        // public BelongSite BelongSite { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        //section
        [Required]
        [MaxLength(200)]

        public string SectionName { get; set; }

        [Required]
        public int SectionId { get; set; }

        // department
        [Required]
        [MaxLength(400)]
        public string DeptName { get; set; }
        public int DeptId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Email { get; set; }

        [MaxLength(200)]
        public string Position { get; set; } = string.Empty;

        [MaxLength(100)]
        public string PhoneNumber { get; set; }

        public int Process { get; set; }

        public int TotalStep { get; set; }

        [Required]
        [MaxLength(200)]
        public string MainApproval { get; set; }

        public bool Complete { get; set; }

        [Required]
        [MaxLength(200)]
        public ApprovalType CurrentApprovalType { get; set; }

        [Required]
        [MaxLength(100)]
        public string FormCode { get; set; }

        public virtual ICollection<ApprovalDetail> ApprovalDetails { get; set; }


        /*---------------------[20251222 - Added]---------------------*/

        /// <summary>
        /// Request status: Ongoing, Completed, Rejected, Returned
        /// </summary>
        [MaxLength(50)]
        public string ApprovalCaseId { get; set; }
        [ForeignKey(nameof(ApprovalCaseId))]
        public ApprovalCase ApprovalCase { get; set; }


        [Column(TypeName = "int")]
        public RequestStatus Status { get; set; }

        public bool IsRemoved { get; set; } = false;

    }
}
