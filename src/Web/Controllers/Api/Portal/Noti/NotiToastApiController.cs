
using Microsoft.AspNetCore.Mvc;
using Web.Services.Portal.Noti;

namespace Web.Controllers.Api.Portal
{
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("api/[controller]")]
    public class NotiToastApiController : ControllerBase
    {
        private readonly INotiToastService _service;
        public NotiToastApiController(INotiToastService service)
        {
            _service = service;
        }

        [HttpGet("/api/notitoast/all")]
        public async Task<IActionResult> GetNotiToasts()
        {
            return Ok(await _service.GetNotiToasts(false, User.Identity.Name));
        }
        [HttpPost("/api/notitoast/{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id, [FromServices] Web.Data.DbContexts.ApplicationDbContext db)
        {
            var item = await db.NotiToasts.FindAsync(id);
            if (item == null || item.SendFor != User.Identity.Name) return NotFound();
            item.IsRead = true;
            await db.SaveChangesAsync();
            return NoContent();
        }
        [HttpDelete("/api/notitoast/clear")]
        public async Task<IActionResult> ClearAll()
        {
            var username = User.Identity.Name;
            await _service.ClearAllNoty(username);
            return Ok(200);
        }
    }
}
