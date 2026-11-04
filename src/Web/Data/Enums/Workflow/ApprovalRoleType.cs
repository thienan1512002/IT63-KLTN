namespace Web.Data.Enums.Workflow
{
    public enum ApprovalRoleType
    {
        NormalApprover = 1,
        Requester = 2,
        SectionManager = 3,
        DepartmentManager = 4,
        Leader = 5,
        NotDecided = 6,

        SectionManagerWithId = 7,
        DepartmentManagerWithId = 8,

        DeputySectionMgr = 9,
        // Distinct from legacy SectionManager (main + backup) so retained workflows keep their behavior.
        SectionMainApprover = 10,
    }
}
