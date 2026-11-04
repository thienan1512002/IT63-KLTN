
using System.ComponentModel.DataAnnotations;

namespace Web.Models.Dtos.Workflow
{
    public class ApprovalActionInput
    {
        [Required]
        public string RequestId { get; set; }

        public string Comment { get; set; }
        
    }
}