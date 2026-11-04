using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Web.Models.FilterModels.Users;
using Web.Services.Admin;

namespace Web.Controllers.Api.Workflow
{
    [Authorize]
    [Route("api/lookup")]
    [ApiController]
    public class LookupApiController : ControllerBase
    {
        private readonly IUserService _userService;

        public LookupApiController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployeeLookupAsync([FromQuery] EmployeeLookupFM filter, CancellationToken cancellationToken)
        {
            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
                return Unauthorized();
            var users = await _userService.GetEmployeeLookupsAsync(
                userName, filter, cancellationToken);
            return Ok(users);
        }
    }
}
