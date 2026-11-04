using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Web.Data.Entities.Workflow
{
    public class WorkflowAttachment
    {
        public int Id { get; set; }

        

        [MaxLength(500)]
        public string FileName { get; set; }

        [MaxLength(500)]
        public string FilePath { get; set; }

        public string Note { get; set; }

        public bool IsRemoved { get; set; }    

        [MaxLength(200)]
        public string UploadedBy { get; set; }
        public DateTime UploadedDate { get; set; }

        // [ForeignKey(nameof(ApprovalRequest))]
        // public int ApprovalRequestId { get; set; }
        // public virtual ApprovalRequest ApprovalRequest { get; set; }
    }
}