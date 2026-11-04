namespace Web.Common
{
    public static class RoleConstants
    {
        public const string SuperUser = "SuperUser";
        public const string BOD = "BOD";
        public const string Employee = "Employee";
        public const string Approver = "Approver";
        public const string PersonnelWriteRoles = SuperUser + "," + HRPersonnelManage;

        public const string HRPersonnelView = "HR.Personnel.View";
        public const string HRPersonnelManage = "HR.Personnel.Manage";
        public const string TransportDispatch = "Transport.Dispatch";
        public const string HRLeaveView = "HR.Leave.View";
        public const string HRLeaveManage = "HR.Leave.Manage";


        public const string ManagePersonnelAuthorizeRoles =
            SuperUser + "," + HRPersonnelManage + "," + HRPersonnelView;

        public static List<string> AccessPersonnel() =>
        [
            SuperUser,
            HRPersonnelManage,
            HRPersonnelView,
        ];

        public static List<string> ManagePersonnel() =>
        [
            SuperUser,
            HRPersonnelManage,
        ];
    }
}
