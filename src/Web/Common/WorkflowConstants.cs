namespace Web.Common
{
    public static class WorkflowConstants
    {
        public static class WorkflowPath
        {
            public const string WorkflowListPath = "/Workflow/Dashboard";
        }
        public static class WaitingText
        {
            public const string ApprovalWaiting = "Waiting for approval";
            public const string ConfirmWaiting = "Waiting for confirm";
            public const string AgreementWaiting = "Waiting for agreement";
        }

        public static class ApprovalStatusText
        {
            public const string Approved = "Approved";
            public const string Confirmed = "Confirmed";
            public const string Returned = "Returned";
            public const string Rejected = "Rejected";
            public const string Agreed = "Agreed";
        }

        public static class ApprovalTypeText
        {
            public const string Approval = "Approval";
            public const string Confirm = "Confirm";
            public const string Finish = "Finish";
            public const string Reject = "Reject";
            public const string Return = "Return";
            public const string Agreement = "Agreement";
        }

        public static class ApprovalStatusCode
        {
            public const int Returned = 0;
            public const int Rejected = -99;
        }
    }
}
