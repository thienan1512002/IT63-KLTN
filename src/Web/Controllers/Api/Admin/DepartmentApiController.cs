using DevExtreme.AspNet.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Web.Models.Dtos.Admin;
using Web.Services.Admin;

namespace Web.Controllers.Api.Admin;

[Authorize]
[ApiController]
[Route("api/admin/departments")]
public class DepartmentApiController : ControllerBase
{
    private readonly IDepartmentService _service;
    private readonly IImportDepartmentService _importService;
    private readonly ILogger<DepartmentApiController> _logger;

    public DepartmentApiController(IDepartmentService service, IImportDepartmentService importService, ILogger<DepartmentApiController> logger)
    {
        _service = service;
        _importService = importService;
        _logger = logger;
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

    [HttpPost]
    [Authorize(Roles = Web.Common.RoleConstants.PersonnelWriteRoles)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateDepartmentRequestDto request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        try
        {
            var entity = await _service.CreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, new { message = "Department created successfully.", id = entity.Id });
        }
        catch (InvalidDataException ex)
        {
            _logger.LogError(ex, "Validation error while creating department");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating department");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while creating the department.", details = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Web.Common.RoleConstants.PersonnelWriteRoles)]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateDepartmentRequestDto request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            await _service.UpdateAsync(id, request);
            return Ok(new { message = "Department updated successfully." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Department not found." });
        }
        catch (InvalidDataException ex)
        {
            _logger.LogError(ex, "Validation error while updating department with Id {DepartmentId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating department with Id {DepartmentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while updating the department.", details = ex.Message });
        }


    }

    [HttpPost("{id:int}/inactive")]
    [Authorize(Roles = Web.Common.RoleConstants.PersonnelWriteRoles)]
    public async Task<IActionResult> InactiveAsync(int id)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity is null)
            return NotFound(new { message = "Department not found." });

        await _service.SetActiveStatusAsync(id, false);
        return Ok(new { message = "Department has been deactivated." });
    }

    [Authorize(Roles = Web.Common.RoleConstants.PersonnelWriteRoles)]
    [HttpPost("{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity == null) return NotFound();
        await _service.SetActiveStatusAsync(id, !entity.IsActive);
        return NoContent();
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
            var fileName = $"NVU_Units_{DateTime.Now:yyyyMMddHHmm}.xlsx";
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting departments");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while exporting departments." });
        }
    }
}
