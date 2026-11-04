using System.ComponentModel.DataAnnotations;
using Web.Data.Entities.Identity;

namespace Web.Data.Entities.Workflow;

// Typed columns keep dates, quantities and master references available to SQL reports.
public abstract class RequestBusinessValues
{
    [MaxLength(450)] public string EmployeeId { get; set; }
    [MaxLength(450)] public string ReplacementEmployeeId { get; set; }
    public int? TargetDepartmentId { get; set; }
    public int? WorkSiteId { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public decimal? Duration { get; set; }
    public int? LeaveTypeId { get; set; }
    [MaxLength(20)] public string StartSession { get; set; }
    [MaxLength(20)] public string EndSession { get; set; }
    public int? PurposeId { get; set; }
    public int? CountryId { get; set; }
    public int? CityId { get; set; }
    public int? PartnerId { get; set; }
    public int? CurrencyId { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal? OriginalAmount { get; set; }
    [MaxLength(500)] public string Location { get; set; }
    public int? OvertimeTypeId { get; set; }
    public int? PaymentMethodId { get; set; }
    [MaxLength(200)] public string Payee { get; set; }
    [MaxLength(100)] public string BankAccount { get; set; }
    [MaxLength(200)] public string BankName { get; set; }
    public int? BankId { get; set; }
    [MaxLength(100)] public string InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public int? ProposalCategoryId { get; set; }
    [MaxLength(2000)] public string ExpectedBenefit { get; set; }
    public int? VehicleTypeId { get; set; }
    public int? VehicleId { get; set; }
    [MaxLength(450)] public string DriverEmployeeId { get; set; }
    [MaxLength(500)] public string PickupLocation { get; set; }
    [MaxLength(500)] public string Destination { get; set; }
    public int? PassengerCount { get; set; }
    [MaxLength(30)] public string ContactPhone { get; set; }
}

public class RequestBusinessData : RequestBusinessValues
{
    [Key] public int RequestId { get; set; }
    public ApprovalRequest Request { get; set; }
    [Required, MaxLength(30)] public string TemplateCode { get; set; } = "INTERNAL";
    public ICollection<RequestLineItem> Items { get; set; } = [];
    public ICollection<RequestParticipant> Participants { get; set; } = [];
}

public class RequestLineItem
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public RequestBusinessData BusinessData { get; set; }
    public int LineOrder { get; set; }
    public int? CatalogItemId { get; set; }
    public RequestMasterItem CatalogItem { get; set; }
    [MaxLength(500)] public string Description { get; set; }
    [MaxLength(50)] public string Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    [MaxLength(1000)] public string Note { get; set; }
}

public class RequestParticipant
{
    public int RequestId { get; set; }
    public string EmployeeId { get; set; }
    public RequestBusinessData BusinessData { get; set; }
    public AppUser Employee { get; set; }
}

public class RequestMasterItem
{
    public int Id { get; set; }
    [Required, MaxLength(40)] public string Category { get; set; }
    [Required, MaxLength(50)] public string Code { get; set; }
    [Required, MaxLength(250)] public string Name { get; set; }
    public int? ParentId { get; set; }
    public RequestMasterItem Parent { get; set; }
    [MaxLength(50)] public string Unit { get; set; }
    public bool IsActive { get; set; } = true;
    public int? Capacity { get; set; }
}
