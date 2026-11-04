namespace Web.Models.Dtos.Admin
{
    public class EmployeeLookupDto
    {
        public string EmpId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int? SectionId { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
