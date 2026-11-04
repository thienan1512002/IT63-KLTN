using Web.Data.Enums.Workflow;

namespace Web.Models.Dtos.Workflow
{
    public class RequestHeaderBaseDto
    {
        public int Id { get; set; }
        public string RequestName { get; set; }
        public string FaxCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string ApplicationNo { get; set; }
        public string DescriptionNo { get; set; }
        public string Department { set; get; }
        public string Section { set; get; }
        public string Position { set; get; }
        public int SectionId { set; get; }
        public string EmpId { set; get; }
        public int DepartmentId { set; get; }
    }

    public class RequestHeaderDto : RequestHeaderBaseDto
    {
        public DateTime SubmitDate { get; set; }
        public bool UrgencyLevel { get; set; }
        public RequestStatus Status { get; set; }
        public string ApprovalCaseName { get; set; }
        public string ApprovalCaseId { get; set; }
        public bool IsRemoved { get; set; }
    }


}