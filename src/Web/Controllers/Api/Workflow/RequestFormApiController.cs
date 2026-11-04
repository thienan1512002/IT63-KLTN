using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Common;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Seeding;
using Web.Models.Dtos.Workflow;
using Web.Services.Workflow;
using System.ComponentModel.DataAnnotations;

namespace Web.Controllers.Api.Workflow;

[Authorize, ApiController, Route("api/request-forms")]
public class RequestFormApiController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public IActionResult Definitions() => Ok(RequestFormCatalog.All);

    [HttpGet("lookups")]
    public async Task<IActionResult> Lookups(CancellationToken ct)
    {
        var current = await db.AppUsers.AsNoTracking().Where(x=>x.UserName==User.Identity.Name).Select(x=>new {x.Id,DepartmentId=(int?)x.Section.DepartmentId,x.SiteId}).SingleAsync(ct);
        return Ok(new {
            CurrentEmployeeId=current.Id, CurrentDepartmentId=current.DepartmentId, CurrentSiteId=current.SiteId,
            Drivers=await (from p in db.DriverProfiles join u in db.Users on p.EmployeeId equals u.Id where p.IsActive && u.IsActive && u.SiteId==current.SiteId && (u.LockoutEnd==null || u.LockoutEnd<=DateTimeOffset.UtcNow) select new {u.Id,Name=u.FullName,u.EmpId}).ToListAsync(ct),
            Employees=await db.AppUsers.AsNoTracking().Where(x=>current.SiteId.HasValue && x.SiteId==current.SiteId && x.Id!=IdentityModelSeed.AdminUserId && x.IsActive && (x.LockoutEnd==null || x.LockoutEnd<=DateTimeOffset.UtcNow)).OrderBy(x=>x.FullName).Select(x=>new {x.Id,Name=x.FullName,x.EmpId,DepartmentId=(int?)x.Section.DepartmentId}).ToListAsync(ct),
            Departments=await db.Departments.AsNoTracking().Where(x=>x.IsActive && x.SiteId==current.SiteId).OrderBy(x=>x.Name).Select(x=>new {x.Id,x.Name,x.SiteId}).ToListAsync(ct),
            Sites=await db.BelongSites.AsNoTracking().Where(x=>x.IsActive && x.Id==current.SiteId).OrderBy(x=>x.Name).Select(x=>new {x.Id,x.Name}).ToListAsync(ct),
            MasterItems=await db.RequestMasterItems.AsNoTracking().OrderBy(x=>x.Name).Select(x=>new{x.Id,x.Category,x.Code,x.Name,x.ParentId,x.Unit,x.Capacity,x.IsActive}).ToListAsync(ct)
        });
    }
}

public class RequestMasterSaveDto
{
    [Required,MaxLength(40)] public string Category {get;set;}
    [Required,MaxLength(50)] public string Code {get;set;}
    [Required,MaxLength(250)] public string Name {get;set;}
    public int? ParentId {get;set;}
    [MaxLength(50)] public string Unit {get;set;}
    [Range(1,100)] public int? Capacity {get;set;}
    public bool IsActive {get;set;}=true;
}
[Authorize(Roles=RoleConstants.SuperUser),ApiController,Route("api/admin/request-master-data")]
public class RequestMasterDataApiController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await db.RequestMasterItems.AsNoTracking().OrderBy(x=>x.Category).ThenBy(x=>x.Name).Select(x=>new {x.Id,x.Category,x.Code,x.Name,x.ParentId,x.Unit,x.Capacity,x.IsActive}).ToListAsync(ct));
    [HttpGet("categories")] public IActionResult Categories()=>Ok(RequestFormCatalog.MasterCategories.Select(x=>new{Code=x.Key,Name=x.Value}));
    [HttpPost] public Task<IActionResult> Create(RequestMasterSaveDto input,CancellationToken ct)=>Save(null,input,ct);
    [HttpPut("{id:int}")] public Task<IActionResult> Update(int id,RequestMasterSaveDto input,CancellationToken ct)=>Save(id,input,ct);
    private async Task<IActionResult> Save(int? id,RequestMasterSaveDto input,CancellationToken ct)
    {
        if(!RequestFormCatalog.MasterCategories.ContainsKey(input.Category)) return BadRequest(new{message="Nhóm danh mục không hợp lệ."});
        var code=input.Code.Trim().ToUpperInvariant();
        if(string.IsNullOrWhiteSpace(code)||string.IsNullOrWhiteSpace(input.Name))return BadRequest(new{message="Vui lòng nhập mã và tên danh mục."});
        if(await db.RequestMasterItems.AnyAsync(x=>x.Id!=id&&x.Category==input.Category&&x.Code==code,ct))return Conflict(new{message="Mã đã tồn tại trong nhóm danh mục."});
        var parentCategory=input.Category switch {"City"=>"Country","Vehicle"=>"VehicleType",_=>null};
        if(parentCategory!=null && !await db.RequestMasterItems.AnyAsync(x=>x.Id==input.ParentId&&x.Category==parentCategory&&(!input.IsActive||x.IsActive),ct))return BadRequest(new{message="Vui lòng chọn quốc gia / loại xe đang sử dụng."});
        var item=id.HasValue ? await db.RequestMasterItems.FindAsync([id.Value],ct) : new RequestMasterItem();
        if(item==null)return NotFound();
        if(id.HasValue && item.Code!=code)return BadRequest(new{message="Không được đổi mã danh mục đã tạo. Hãy ngừng sử dụng và tạo bản ghi mới."});
        if(id.HasValue && (item.Category!=input.Category || item.ParentId!=(parentCategory==null?null:input.ParentId)))return BadRequest(new{message="Không được đổi nhóm hoặc danh mục cha của dữ liệu đã tạo. Hãy ngừng sử dụng và tạo bản ghi mới để giữ đúng dữ liệu báo cáo."});
        item.Category=input.Category;item.Code=code;item.Name=input.Name.Trim();item.ParentId=parentCategory==null?null:input.ParentId;
        item.Unit=input.Unit?.Trim();item.Capacity=input.Category is "Vehicle" or "VehicleType"?input.Capacity:null;item.IsActive=input.IsActive;
        if(input.Category is "Product" or "Equipment" or "ExpenseCategory" && string.IsNullOrWhiteSpace(item.Unit))return BadRequest(new{message="Vui lòng nhập đơn vị tính."});
        if(input.Category is "Vehicle" or "VehicleType" && item.Capacity==null)return BadRequest(new{message="Vui lòng nhập sức chứa của xe (số hành khách)."});
        if(!id.HasValue)db.RequestMasterItems.Add(item);
        await db.SaveChangesAsync(ct);return Ok(new{item.Id});
    }
}
