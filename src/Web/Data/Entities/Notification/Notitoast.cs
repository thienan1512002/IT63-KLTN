using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Web.Data.Entities.Notification
{
    public class NotiToast
    {      
        public int Id { get; set; }

        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Content { get; set; }

        [MaxLength(255)]
        public string SendFor { get; set; }

        [MaxLength(500)]
        public string Url { get; set; }

        public bool IsRead { get; set; }

        public int? RequestId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Resource key for localization (used when NeedTranslate is true).
        /// </summary>
        [MaxLength(500)]
        public string ContentKey { get; set; }

        /// <summary>
        /// JSON array of string args for localization formatting, e.g. ["FAX001","FV2-0001"].
        /// That used to tranlate content like "Your form {0} with code {1} has been approved" to "Your form FAX001 with code FV2-0001 has been approved".
        /// </summary>
        [MaxLength(2000)]
        public string ContentArgsJson { get; set; }

        /// <summary>
        /// Indicates whether translation is needed
        /// </summary>
        public bool NeedTranslate { get; set; }
    }
}
