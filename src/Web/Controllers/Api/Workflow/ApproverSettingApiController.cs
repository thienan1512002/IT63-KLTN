using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Common;
using Web.Models.Dtos.Workflow.ApproverSetting;
using Web.Services.Workflow;

namespace Web.Controllers.Api.Workflow;

[Authorize(Roles = RoleConstants.SuperUser)]
[ApiController]
[Route("api/workflow/approver-settings")]
public class ApproverSettingApiController : ControllerBase
{
    private readonly IApproverSettingService _service;

    public ApproverSettingApiController(IApproverSettingService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var items = await _service.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] ApproverSettingUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var id = await _service.CreateAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] ApproverSettingUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            await _service.UpdateAsync(id, dto, cancellationToken);
            return Ok(new { id });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Approver setting not found." });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await _service.SoftDeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new { message = "Approver setting not found." });

        return Ok(new { message = "Deleted successfully." });
    }
}

