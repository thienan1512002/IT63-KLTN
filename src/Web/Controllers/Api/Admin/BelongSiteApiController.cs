using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models.Dtos.Admin;
using Web.Services.Admin;

namespace Web.Controllers.Api.Admin;

[Authorize]
[ApiController]
[Route("api/admin/belong-sites")]
public class BelongSiteApiController : ControllerBase
{
    private readonly IBelongSiteService _service;

    public BelongSiteApiController(IBelongSiteService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> GetLookupsAsync()
    {
        var result = await _service.GetLookupsAsync();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Web.Common.RoleConstants.PersonnelWriteRoles)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateBelongSiteRequestDto request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var entity = await _service.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created, new { message = "BelongSite created successfully.", id = entity.Id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Web.Common.RoleConstants.PersonnelWriteRoles)]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateBelongSiteRequestDto request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var entity = await _service.GetByIdAsync(id);
        if (entity is null)
            return NotFound(new { message = "BelongSite not found." });

        await _service.UpdateAsync(id, request);
        return Ok(new { message = "BelongSite updated successfully." });
    }

    [HttpPost("{id:int}/inactive")]
    [Authorize(Roles = Web.Common.RoleConstants.PersonnelWriteRoles)]
    public async Task<IActionResult> InactiveAsync(int id)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity is null)
            return NotFound(new { message = "BelongSite not found." });

        await _service.SetActiveStatusAsync(id, false);
        return Ok(new { message = "BelongSite has been deactivated." });
    }
}
