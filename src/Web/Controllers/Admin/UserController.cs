using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserModule.Models.ImportModels;
using UserModule.Models.ViewModels;
using Web.Services.Admin;
using Web.Data.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Web.Common;

namespace  Web.Controllers.Admin;

[Authorize]
public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly IImportUserService _importUserService;

    public UserController(IUserService userService, IImportUserService importUserService)
    {
        _userService = userService;
        _importUserService = importUserService;
    }

    [HttpGet]
    [Authorize(Roles = RoleConstants.ManagePersonnelAuthorizeRoles)]
    public IActionResult Index()
    {
        ViewData["AllowManage"] = AuthHelper.CheckRoles(User, RoleConstants.ManagePersonnel());
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Setting()
    {
        //var users = await _userService.GetAllAsync();
        return View("~/Views/Admin/User/Setting.cshtml");
    }

    [HttpGet]
    [Authorize(Roles = RoleConstants.PersonnelWriteRoles)]
    public IActionResult Create()
    {
        return View(new CreateUserViewModel());
    }   
    

    [HttpPost]
    [Authorize(Roles = RoleConstants.PersonnelWriteRoles)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError("", "Please select an Excel file (.xlsx).");
            return View(new ImportResult());
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
        {
            ModelState.AddModelError("", "Only .xlsx files are supported.");
            return View(new ImportResult());
        }
        
        var result = await _importUserService.ImportUserAsync(file);

        return Ok(result);
    }
}
