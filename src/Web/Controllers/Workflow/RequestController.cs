using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Services.Workflow;

namespace Web.Controllers.Workflow;

[Authorize]
public class RequestController(ApplicationDbContext db, IRequestService requests) : Controller
{
    [HttpGet("Requests")]
    public IActionResult Index() => View("~/Views/Workflow/Request/Index.cshtml");
    [HttpGet("RequestReports")]
    public IActionResult Reports() => View("~/Views/Workflow/Request/Reports.cshtml");

    [HttpGet("Requests/Create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewBag.RequestTypes = await db.RequestTypes.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).ToListAsync(ct);
        return View("~/Views/Workflow/Request/Edit.cshtml");
    }

    [HttpGet("Requests/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        ViewBag.RequestTypes = await db.RequestTypes.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).ToListAsync(ct);
        var detail = await requests.GetDetailsAsync(id, User.Identity!.Name!, User.IsInRole("SuperUser"), ct);
        if (!detail.CanEdit) return Forbid();
        ViewBag.Request = detail;
        return View("~/Views/Workflow/Request/Edit.cshtml");
    }

    [HttpGet("Requests/Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct) =>
        View("~/Views/Workflow/Request/Details.cshtml",
            await requests.GetDetailsAsync(id, User.Identity!.Name!, User.IsInRole("SuperUser"), ct));

    [HttpGet("Requests/Details/{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id, [FromServices] RequestPdfService pdf, CancellationToken ct)
    {
        var detail = await requests.GetDetailsAsync(id, User.Identity!.Name!, User.IsInRole("SuperUser"), ct);
        var bytes = pdf.Generate(detail);
        Response.Headers.CacheControl = "no-store, private";
        var filename = string.Concat(detail.ApplicationNo.Select(c => char.IsAsciiLetterOrDigit(c) || c == '-' ? c : '_'));
        return File(bytes, "application/pdf", $"Yeu-cau-{filename}.pdf");
    }

    [HttpGet("Approvals")]
    public IActionResult Approvals() => View("~/Views/Workflow/Request/Approvals.cshtml");
}
