using DevExtreme.AspNet.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models.Dtos.Workflow;
using Web.Services.Workflow;
using Web.Services.UploadFile;
using Web.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Web.Common;

namespace Web.Controllers.Api.Workflow;

[Authorize]
[ApiController]
[Route("api/requests")]
[InvariantFormValues]
public class RequestApiController(IRequestService requests, ApplicationDbContext db, IUploadFileService filesService) : ControllerBase
{
    private string UserName => User.Identity?.Name ?? throw new UnauthorizedAccessException();

    [HttpGet("approval-preview")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ApprovalPreview([FromQuery] int requestTypeId, [FromQuery] int? requestId,
        [FromServices] RequestApprovalPreviewService preview, CancellationToken ct) =>
        Ok(await preview.GetAsync(requestTypeId, requestId, UserName, ct));

    [HttpGet("{id:int}/attachments/{attachmentId:int}")]
    public async Task<IActionResult> Download(int id, int attachmentId, CancellationToken ct)
    {
        await requests.GetDetailsAsync(id, UserName, User.IsInRole("SuperUser"), ct);
        var file = await db.RequestAttachments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == attachmentId && x.RequestId == id, ct);
        if (file == null) return NotFound();
        var path = filesService.GetPhysicalPath(file.FilePath);
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(Path.GetFullPath(path), "application/octet-stream", file.FileName);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(DataSourceLoadOptions options, CancellationToken ct) =>
        Ok(await DataSourceLoader.LoadAsync(requests.BuildMyRequestsQuery(UserName), options, ct));

    [HttpGet("pending")]
    public async Task<IActionResult> Pending(DataSourceLoadOptions options, CancellationToken ct) =>
        Ok(await DataSourceLoader.LoadAsync(requests.BuildPendingApprovalsQuery(UserName), options, ct));

    [HttpGet("pending-count")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> PendingCount(CancellationToken ct) =>
        Ok(new { count = await requests.BuildPendingApprovalsQuery(UserName).CountAsync(ct) });

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct) =>
        Ok(await requests.GetDetailsAsync(id, UserName, User.IsInRole("SuperUser"), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] RequestEditDto input, [FromForm] List<IFormFile> files, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var id = await requests.CreateDraftAsync(input, UserName, files, ct);
        return CreatedAtAction(nameof(Details), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromForm] RequestEditDto input, [FromForm] List<IFormFile> files, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await requests.UpdateDraftAsync(id, input, UserName, files, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id, CancellationToken ct)
    { await requests.SubmitAsync(id, UserName, ct); return NoContent(); }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, RequestActionDto input, CancellationToken ct)
    { await requests.ApproveAsync(id, UserName, input.Comment, ct); return NoContent(); }

    [HttpPost("{id:int}/return")]
    public async Task<IActionResult> Return(int id, RequestActionDto input, CancellationToken ct)
    { await requests.ReturnAsync(id, UserName, input.Comment ?? string.Empty, ct); return NoContent(); }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, RequestActionDto input, CancellationToken ct)
    { await requests.RejectAsync(id, UserName, input.Comment ?? string.Empty, ct); return NoContent(); }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, RequestActionDto input, CancellationToken ct)
    { await requests.CancelAsync(id, UserName, input.Comment, ct); return NoContent(); }
}

