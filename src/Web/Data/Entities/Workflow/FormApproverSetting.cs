using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Web.Data.Entities.Workflow
{
    public class FormApproverSetting
    {
        public int Id { get; set; }

        [MaxLength(500)]
        [Required]
        public string Title { get; set; }

        [MaxLength(500)]
        [Required]
        public string DisplayTitle { get; set; }


        [MaxLength(1000)]
        [Required]
        public string UserNames { get; set; }

        [MaxLength(200)]
        [Required]
        public string FormCode { get; set; }

        public bool IsRemoved { get; set; } = true;
    }
}