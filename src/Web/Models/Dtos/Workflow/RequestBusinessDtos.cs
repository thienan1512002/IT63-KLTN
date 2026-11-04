using System.ComponentModel.DataAnnotations;
using Web.Data.Entities.Workflow;
namespace Web.Models.Dtos.Workflow;

public class RequestBusinessDataDto : RequestBusinessValues
{
    public List<RequestLineInputDto> Items { get; set; } = [];
    public List<string> ParticipantIds { get; set; } = [];
}
public class RequestLineInputDto
{
    public int? CatalogItemId { get; set; }
    [MaxLength(500)] public string Description { get; set; }
    [MaxLength(50)] public string Unit { get; set; }
    [Range(typeof(decimal), "0.01", "1000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)] public decimal Quantity { get; set; } = 1;
    [Range(typeof(decimal), "0", "9999999999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)] public decimal UnitPrice { get; set; }
    [MaxLength(1000)] public string Note { get; set; }
}
public record RequestBusinessDisplayField(string Label, string Value);
public record RequestLineDisplayDto(string Item, string Description, string Unit, decimal Quantity, decimal UnitPrice, decimal LineTotal, string Note);
