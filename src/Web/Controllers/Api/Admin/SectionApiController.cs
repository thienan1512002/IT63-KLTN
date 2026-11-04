using DevExtreme.AspNet.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Web.Models.Dtos.Admin;
using Web.Services.Admin;

namespace Web.Controllers.Api.Admin;

[Authorize]
[ApiController]
[Route("api/admin/sections")]
public class SectionApiController : ControllerBase
{
    private readonly ISectionService _service;
    private readonly IImportSectionService _importService;

    private readonly ILogger<SectionApiController> _logger;

    public SectionApiController(ISectionService service, IImportSectionService importService, ILogger<SectionApiController> logger)
    {
        _logger = logger;
        _service = service;
        _importService = importService;
    }

    [HttpGet]
    public async Task<IActionResult> GetListAsync(DataSourceLoadOptions loadOptions, CancellationToken cancellationToken)
    {
        var query = _service.GetListQuery();
        var result = await DataSourceLoader.LoadAsync(query, loadOptions, cancellationToken);
        return Ok(result);
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> GetLookupsAsync()
    {
        var result = await _service.GetLookupsAsync();
        return Ok(result);
    }

    [HttpGet("lookup-by-site/{siteId:int}")]
    public async Task<IActionResult> GetLookupsBySiteAsync(int siteId, CancellationToken cancellationToken)
    {
        var result = await _service.GetLookupsBySiteIdAsync(siteId);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Web.Common.RoleConstants.PersonnelWriteRoles)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateSectionRequestDto request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        try
        {
            var entity = await _service.CreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, new { message = "Section created successfully.", id = entity.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating section");
            return BadRequest(new { message = ex.Message });
        }


    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Web.Common.RoleConstants.PersonnelWriteRoles)]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateSectionRequestDto request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        try
        {
            await _service.UpdateAsync(id, request);
            return Ok(new { message = "Section updated successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Section update failed due to wrong section ID");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating section");
            return BadRequest(new { message = ex.Message });
        }

    }

    [HttpPost("{id:int}/inactive")]
    [Authorize(Roles = Web.Common.RoleConstants.PersonnelWriteRoles)]
    public async Task<IActionResult> InactiveAsync(int id)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity is null)
            return NotFound(new { message = "Section not found." });

        await _service.SetActiveStatusAsync(id, false);
        return Ok(new { message = "Section has been deactivated." });
    }

    [HttpPost("import")]
    [Authorize(Roles = Web.Common.RoleConstants.PersonnelWriteRoles)]
    public async Task<IActionResult> ImportAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "File is required." });

        var result = await _importService.ImportAsync(file);
        return Ok(result);
    }

    [HttpPost("export")]
    public async Task<IActionResult> ExportAsync(
        [FromBody] Dictionary<string, JsonElement> loadOptionsPayload,
        CancellationToken cancellationToken)
    {
        try
        {
            var loadOptions = DataSourceLoadOptions.FromJsonPayload(loadOptionsPayload);
            var bytes = await _importService.ExportAsync(loadOptions, cancellationToken);
            var fileName = $"NVU_Sections_{DateTime.Now:yyyyMMddHHmm}.xlsx";
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting sections");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while exporting sections." });
        }
    }
}
