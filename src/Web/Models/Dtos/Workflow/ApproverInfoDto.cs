using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web.Models.Dtos.Workflow
{
    public class ApproverInfoDto
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string SectionName { get; set; }
        public string Position { get; set; }

        /// <summary>
        /// Indicates if this approver is for an offline step
        /// This is needed for frontend 
        /// </summary>
        public bool IsOffline { get; set; }
    }
}