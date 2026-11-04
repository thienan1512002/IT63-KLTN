using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web.Common
{
    public static class NotificationConstants
    {
        public static class NotificationType
        {
            public const string Confirmation = "Confirmation";
            public const string Approval = "Approval";
            public const string Finish = "Finish";
            public const string Reject = "Reject";
            public const string Return = "Return";
            public const string Submission = "Submission";
        }

        public static class WorkflowNotyContent
        {
            public const string SubmittedRequest = "Noti_SubmittedRequest";
            public const string YourRequestFinishedApproval = "Noti_YourRequestFinishedApproval";
            public const string YourRequestReturned = "Noti_YourRequestReturned";
            public const string YourRequestRejected = "Noti_YourRequestRejected";
        }
    }
}