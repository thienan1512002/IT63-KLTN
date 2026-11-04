using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Web.Data.Entities.Workflow;
using Web.Models.Dtos.Workflow;

namespace Web.Models.Dtos.Admin
{
    public class SectionLookupMasterData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string EngName { get; set; }

        public List<ApproverInfoDto> ApproverInfo { get; set; }

        [JsonPropertyName("picApproverInfo")]
        public List<ApproverInfoDto> PICApproverInfo { get; set; }
    }

}