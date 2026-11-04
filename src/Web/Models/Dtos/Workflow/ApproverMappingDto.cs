namespace Web.Models.Dtos.Workflow
{

    public class ApproverMappingDto
    {
        public List<DynamicApproverDto> FromDeparts { get; set; }
        public List<DynamicApproverDto> FromSects { get; set; }
        public List<DynamicApproverDto> FromForms { get; set; }
    }
    public class DynamicApproverDto
    {
        public string MainApprover { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int Id { get; set; }
        public string Approvers { get; set; }
        // PIC
        public string Leader { get; set; }
        public string EO { get; set; }
        public string Deputy { get; set; }
    }

    public class ApproverDictionary
    {
        public Dictionary<int, DynamicApproverDto> FromDeparts { get; set; }
        public Dictionary<int, DynamicApproverDto> FromSects { get; set; }
        public Dictionary<int, DynamicApproverDto> FromForms { get; set; }
    }
}

