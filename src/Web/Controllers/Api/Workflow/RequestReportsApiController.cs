using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevExtreme.AspNet.Data;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;
using Web.Services.Workflow;

namespace Web.Controllers.Api.Workflow;

[Authorize,ApiController,Route("api/request-reports")]
public class RequestReportsApiController(ApplicationDbContext db) : ControllerBase
{
    private IQueryable<ApprovalRequest> Visible() => db.ApprovalRequests.AsNoTracking().Where(x => !x.IsRemoved &&
        (User.IsInRole("SuperUser") || x.FaxCode==User.Identity.Name || x.ApprovalDetails.Any(d=>d.ApproverInfos.Any(a=>a.UserName==User.Identity.Name))));
    private IQueryable<ApprovalRequest> Filter(string templateCode, DateTime? from, DateTime? to, int? departmentId, string employeeId, RequestStatus? status)
    {
        if(templateCode!=null) RequestFormCatalog.Get(templateCode);
        if(from.HasValue && to.HasValue && from.Value.Date>to.Value.Date || to?.Date==DateTime.MaxValue.Date) throw new InvalidOperationException("Khoảng ngày báo cáo không hợp lệ.");
        if(status.HasValue && !Enum.IsDefined(status.Value)) throw new InvalidOperationException("Trạng thái báo cáo không hợp lệ.");
        var query=Visible();
        if(!string.IsNullOrEmpty(templateCode))query=query.Where(x=>(x.BusinessData!=null?x.BusinessData.TemplateCode:x.RequestType.TemplateCode)==templateCode);
        if(from.HasValue)query=query.Where(x=>(x.BusinessData.StartAt ?? x.RequestedDate ?? x.CreatedAt)>=from.Value.Date);
        if(to.HasValue){var end=to.Value.Date.AddDays(1);query=query.Where(x=>(x.BusinessData.StartAt ?? x.RequestedDate ?? x.CreatedAt)<end);}
        if(departmentId.HasValue)query=query.Where(x=>(x.BusinessData.TargetDepartmentId ?? x.DepartmentId)==departmentId);
        if(!string.IsNullOrWhiteSpace(employeeId))query=query.Where(x=>x.BusinessData.EmployeeId==employeeId||x.BusinessData.Participants.Any(p=>p.EmployeeId==employeeId));
        if(status.HasValue)query=query.Where(x=>x.Status==status);
        return query;
    }
    [HttpGet("requests")]
    public async Task<IActionResult> Requests(DataSourceLoadOptions options,string templateCode,DateTime? from,DateTime? to,int? departmentId,string employeeId,RequestStatus? status,CancellationToken ct,int? partnerId=null)
    {
        var filtered=Filter(templateCode,from,to,departmentId,employeeId,status);
        if(partnerId.HasValue) filtered=filtered.Where(x=>x.BusinessData.PartnerId==partnerId);
        var query=filtered.Select(x=>new {
            x.Id,x.ApplicationNo,x.Title,RequestType=x.RequestType.Name,TemplateCode=x.BusinessData!=null?x.BusinessData.TemplateCode:x.RequestType.TemplateCode,x.Status,
            HasBusinessData=x.BusinessData!=null,CreatedBy=x.FullName, WorkSite=db.BelongSites.Where(s=>s.Id==(x.BusinessData.WorkSiteId ?? x.SiteId)).Select(s=>s.Name).FirstOrDefault(),
            Department=db.Departments.Where(d=>d.Id==(x.BusinessData.TargetDepartmentId ?? x.DepartmentId)).Select(d=>d.Name).FirstOrDefault(),
            Employee=db.AppUsers.Where(e=>e.Id==x.BusinessData.EmployeeId).Select(e=>e.FullName).FirstOrDefault(),
            ReportDate=x.BusinessData.StartAt ?? x.RequestedDate ?? x.CreatedAt,x.BusinessData.StartAt,x.BusinessData.EndAt,
            LeaveType=db.RequestMasterItems.Where(m=>m.Id==x.BusinessData.LeaveTypeId).Select(m=>m.Name).FirstOrDefault(),
            Purpose=db.RequestMasterItems.Where(m=>m.Id==x.BusinessData.PurposeId).Select(m=>m.Name).FirstOrDefault(),
            Country=db.RequestMasterItems.Where(m=>m.Id==x.BusinessData.CountryId).Select(m=>m.Name).FirstOrDefault(),
            City=db.RequestMasterItems.Where(m=>m.Id==x.BusinessData.CityId).Select(m=>m.Name).FirstOrDefault(),
            Partner=db.RequestMasterItems.Where(m=>m.Id==x.BusinessData.PartnerId).Select(m=>m.Name).FirstOrDefault(),
            Currency=db.RequestMasterItems.Where(m=>m.Id==x.BusinessData.CurrencyId).Select(m=>m.Code).FirstOrDefault(),
            x.BusinessData.ExchangeRate,x.BusinessData.OriginalAmount,AmountVnd=x.BusinessData!=null?x.Amount:null,
            LeaveDays=x.BusinessData.TemplateCode=="LEAVE"?x.BusinessData.Duration:null,
            LeaveDebitedDays=db.LeaveAllocations.Where(l=>l.RequestId==x.Id && l.Status==Web.Data.Entities.Workflow.Operations.LeaveAllocationStatus.Consumed && l.DeductsBalance).Sum(l=>(decimal?)l.Days),
            LeaveReservedDays=db.LeaveAllocations.Where(l=>l.RequestId==x.Id && l.Status==Web.Data.Entities.Workflow.Operations.LeaveAllocationStatus.Reserved && l.DeductsBalance).Sum(l=>(decimal?)l.Days),
            CalendarEntryCount=db.EmployeeWorkEvents.Count(e=>e.RequestId==x.Id),
            DispatchStatus=db.VehicleDispatches.Where(d=>d.RequestId==x.Id).Select(d=>(int?)d.Status).FirstOrDefault(),
            ActualVehicle=(from d in db.VehicleDispatches join v in db.RequestMasterItems on d.VehicleId equals v.Id where d.RequestId==x.Id select v.Name).FirstOrDefault(),
            ActualDriver=(from d in db.VehicleDispatches join u in db.Users on d.DriverEmployeeId equals u.Id where d.RequestId==x.Id select u.FullName).FirstOrDefault(),
            TripDays=x.BusinessData.TemplateCode=="BUSINESS_TRIP"?x.BusinessData.Duration:null,
            OvertimeHours=x.BusinessData.TemplateCode=="OVERTIME"?x.BusinessData.Duration:null,
            OvertimePersonHours=x.BusinessData.TemplateCode=="OVERTIME"?x.BusinessData.Duration*x.BusinessData.Participants.Count:null,
            VehicleHours=x.BusinessData.TemplateCode=="VEHICLE"?x.BusinessData.Duration:null,
            ParticipantCount=x.BusinessData.Participants.Count,
            ItemQuantity=x.BusinessData.Items.Sum(i=>(decimal?)i.Quantity),
            OvertimeType=db.RequestMasterItems.Where(m=>m.Id==x.BusinessData.OvertimeTypeId).Select(m=>m.Name).FirstOrDefault(),
            PaymentMethod=db.RequestMasterItems.Where(m=>m.Id==x.BusinessData.PaymentMethodId).Select(m=>m.Name).FirstOrDefault(),
            x.BusinessData.Payee,x.BusinessData.InvoiceNumber,x.BusinessData.InvoiceDate,Bank=x.BusinessData.BankName,
            ProposalCategory=db.RequestMasterItems.Where(m=>m.Id==x.BusinessData.ProposalCategoryId).Select(m=>m.Name).FirstOrDefault(),x.BusinessData.ExpectedBenefit,
            VehicleType=db.RequestMasterItems.Where(m=>m.Id==x.BusinessData.VehicleTypeId).Select(m=>m.Name).FirstOrDefault(),
            Vehicle=db.RequestMasterItems.Where(m=>m.Id==x.BusinessData.VehicleId).Select(m=>m.Name).FirstOrDefault(),
            x.BusinessData.PassengerCount,x.BusinessData.PickupLocation,x.BusinessData.Destination
        });
        return Ok(await DataSourceLoader.LoadAsync(query,options,ct));
    }
    [HttpGet("items")]
    public async Task<IActionResult> Items(DataSourceLoadOptions options,string templateCode,DateTime? from,DateTime? to,int? departmentId,string employeeId,RequestStatus? status,CancellationToken ct,int? partnerId=null)
    {
        var filtered=Filter(templateCode,from,to,departmentId,employeeId,status);
        if(partnerId.HasValue) filtered=filtered.Where(x=>x.BusinessData.PartnerId==partnerId);
        var visible=filtered.Select(x=>x.Id);
        var query=db.RequestLineItems.AsNoTracking().Where(x=>visible.Contains(x.RequestId)).Select(x=>new {
            x.Id,x.RequestId,x.BusinessData.Request.ApplicationNo,RequestType=x.BusinessData.Request.RequestType.Name,x.BusinessData.TemplateCode,x.BusinessData.Request.Status,
            ReportDate=x.BusinessData.StartAt ?? x.BusinessData.Request.RequestedDate ?? x.BusinessData.Request.CreatedAt,
            Department=db.Departments.Where(d=>d.Id==(x.BusinessData.TargetDepartmentId ?? x.BusinessData.Request.DepartmentId)).Select(d=>d.Name).FirstOrDefault(),
            WorkSite=db.BelongSites.Where(s=>s.Id==(x.BusinessData.WorkSiteId ?? x.BusinessData.Request.SiteId)).Select(s=>s.Name).FirstOrDefault(),Partner=db.RequestMasterItems.Where(m=>m.Id==x.BusinessData.PartnerId).Select(m=>m.Name).FirstOrDefault(),CatalogItem=x.CatalogItem.Name,x.Description,x.Unit,x.Quantity,x.UnitPrice,x.LineTotal,x.Note,
            Currency=db.RequestMasterItems.Where(m=>m.Id==x.BusinessData.CurrencyId).Select(m=>m.Code).FirstOrDefault(),x.BusinessData.ExchangeRate,
            AmountVnd=x.BusinessData.ExchangeRate.HasValue?Math.Round(x.LineTotal*x.BusinessData.ExchangeRate.Value,2): (decimal?)null
        });
        return Ok(await DataSourceLoader.LoadAsync(query,options,ct));
    }
    [HttpGet("purchase-summary")]
    public async Task<IActionResult> PurchaseSummary(DateTime? from,DateTime? to,int? departmentId,string employeeId,RequestStatus? status,int? partnerId,CancellationToken ct)
    {
        var query=Filter("PURCHASE",from,to,departmentId,employeeId,status);
        if(partnerId.HasValue)query=query.Where(x=>x.BusinessData.PartnerId==partnerId);
        var totals=await query.GroupBy(x=>1).Select(g=>new {
            Count=g.Count(), TotalVnd=g.Sum(x=>(decimal?)(x.BusinessData!=null?x.Amount:null)) ?? 0,
            CompletedCount=g.Count(x=>x.Status==RequestStatus.Completed),
            CompletedVnd=g.Where(x=>x.Status==RequestStatus.Completed).Sum(x=>(decimal?)(x.BusinessData!=null?x.Amount:null)) ?? 0
        }).SingleOrDefaultAsync(ct);
        var suppliers=await query.GroupBy(x=>x.BusinessData.PartnerId)
            .Select(g=>new { PartnerId=g.Key, Count=g.Count(), TotalVnd=g.Sum(x=>(decimal?)(x.BusinessData!=null?x.Amount:null)) ?? 0 })
            .OrderByDescending(x=>x.TotalVnd).ThenBy(x=>x.PartnerId).ToListAsync(ct);
        var ids=suppliers.Where(x=>x.PartnerId.HasValue).Select(x=>x.PartnerId.Value).ToArray();
        var names=await db.RequestMasterItems.AsNoTracking().Where(x=>ids.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.Name,ct);
        return Ok(new { Count=totals?.Count??0, TotalVnd=totals?.TotalVnd??0, CompletedCount=totals?.CompletedCount??0, CompletedVnd=totals?.CompletedVnd??0,
            Suppliers=suppliers.Select(x=>new { x.PartnerId, Name=x.PartnerId.HasValue?names.GetValueOrDefault(x.PartnerId.Value,"Không xác định"):"Chưa đề xuất",x.Count,x.TotalVnd }) });
    }
}

