using Web.Data.Enums.Workflow;

namespace Web.Models.FilterModels.Users
{
    public class UserFM
    {
        public string UserName { get; set; } = string.Empty;
        
    }

    public class EmployeeLookupFM : UserFM
    {
        public int? DepartmentId { get; set; }
        public int? SectionId { get; set; }
    }
}
