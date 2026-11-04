using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Web.Common;
using Web.Data.DbContexts;
using Web.Data.Entities.Identity;

namespace Web.Controllers.Api.Admin;

[Authorize(Roles = RoleConstants.SuperUser)]
[ApiController]
[Route("api/admin/roles")]
public class RoleApiController(ApplicationDbContext db, RoleManager<AppRole> roles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.Roles.AsNoTracking().Select(x => new { x.Id, x.Name, x.DisplayName, x.CategoryName }).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(RoleInput input)
    {
        var result = await roles.CreateAsync(new AppRole {
            Name = input.Name.Trim(), DisplayName = input.DisplayName.Trim(), CategoryName = "Phê duyệt", CategoryOrder = 1
        });
        return result.Succeeded ? Ok() : BadRequest(new { message = string.Join("; ", result.Errors.Select(x => x.Description)) });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, RoleInput input)
    {
        var entity = await roles.FindByIdAsync(id);
        if (entity == null) return NotFound();
        if (entity.Name != input.Name.Trim()) return BadRequest(new { message = "Không đổi mã vai trò đang được sử dụng." });
        entity.DisplayName = input.DisplayName.Trim();
        var result = await roles.UpdateAsync(entity);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }
}

public class RoleInput
{
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Required, MaxLength(200)] public string DisplayName { get; set; } = "";
}
