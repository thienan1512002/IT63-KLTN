using DevExtreme.AspNet.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Common;
using Web.Services.Admin;

namespace Web.Controllers.Api.Admin;

[Authorize(Roles = RoleConstants.SuperUser)]
[ApiController]
[Route("api/admin/email-log")]
public class EmailLogApiController : ControllerBase
{
    private readonly IEmailLogService _service;
    private readonly ILogger<EmailLogApiController> _logger;

    public EmailLogApiController(IEmailLogService service, ILogger<EmailLogApiController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// DevExtreme CustomStore endpoint — server-side paging, sorting and filtering
    /// over EmailMasterData rows.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] DataSourceLoadOptions loadOptions,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = _service.GetListQuery();
            var result = await DataSourceLoader.LoadAsync(query, loadOptions, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading email log data");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to load email log.");
        }
    }

    /// <summary>Returns all send-attempt log entries for one email master record.</summary>
    [HttpGet("{id:long}/attempts")]
    public async Task<IActionResult> GetAttemptsAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetAttemptsAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading attempt logs for email {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to load attempt logs.");
        }
    }
}
