
namespace Web.Models.Dtos.Notification
{
    public class EmailContent
    {
        public int RequestId { get; set; }
        public int SectionId { get; set; }
        public string DescriptionNo { get; set; }

        public string RequestName { get; set; }
        public string RequestTitle { get; set; }

        public string FaxCode { get; set; }

        public string Name { get; set; }

        public bool UrgencyLevel { get; set; }

        public DateTime SubmitDate { get; set; }

        public string Url { get; set; }

        public List<string> CcUserNames { get; set; } = [];        

        public string Comment { get; set; } = "";

        public HashSet<string> Receivers { get; set; } = new HashSet<string>();

        public Dictionary<string, string> AdditionalData { get; set; } = new Dictionary<string, string>();
       
    }
}
