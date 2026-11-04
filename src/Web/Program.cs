using Microsoft.AspNetCore.Localization;
using Identity.Services;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Web.Common;
using Web.Configuration;
using Web.Data.DbContexts;
using Web.Data.Entities.Identity;
using Web.Middlewares;
using Web.Services.Admin;
using Web.Services.Portal.Noti;
using Web.Services.Portal.Noti.Impl;
using Web.Services.UploadFile;
using Web.Services.UploadFile.Impl;
using Web.Services.Account;
using Web.Services.Workflow;
using Web.Services.Workflow.Core;
using Web.Services.Localization;
using Serilog;
using Worker.Shared.Data;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Hook Serilog into ASP.NET Core's ILogger pipeline.
// Uses builder.Configuration so appsettings.Development.json and env vars are respected.
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

var fileServerConfigSection = builder.Configuration.GetSection("FileServerConfig");
builder.Services.Configure<FileServerConfig>(fileServerConfigSection);
builder.Services.PostConfigure<FileServerConfig>(options =>
    options.Path = Path.GetFullPath(options.Path, builder.Environment.ContentRootPath));


// Global upload request limits (ASP.NET Core + Kestrel).
builder.ConfigureUploadRequestLimits(fileServerConfigSection.GetValue<long>("MaxUploadBytes"));
// Add services to the container.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supported = new[] { "en-US", "vi-VN" };
    options.SetDefaultCulture("en-US");
    options.AddSupportedCultures(supported);
    options.AddSupportedUICultures(supported);
    options.RequestCultureProviders =
    [
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    ];
});
builder.Services.AddScoped<LanguageService>();
builder.Services.AddScoped<ILocalizationResxExcelService, LocalizationResxExcelService>();
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()))
    .AddRazorOptions(options =>
    {
        // Allow controllers in Admin sub-folder to resolve views from Views/Admin/{controller}/
        options.ViewLocationFormats.Add("/Views/Admin/{1}/{0}.cshtml");
    });
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    options.ConfigureWarnings(w =>
        w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
});


builder.Services.AddDbContext<WorkerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<AppUser, AppRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Phiên cookie so khớp SecurityStamp mỗi request (ValidationInterval = 0). Stamp đổi khi: (1) đổi role — UserRoleService.UpdateSecurityStampAsync,
// (2) đổi email/SĐT qua UserManager — AccountService chỉ gọi khi giá trị đổi và RefreshSignIn khi user tự sửa (principalUserName).
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
builder.Services.AddLoginSessionNotice();



builder.Services.AddDataProtection()
    .SetApplicationName("iKSN-iWorkspace")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")));
    
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api")) context.Response.StatusCode = StatusCodes.Status403Forbidden;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddScoped<IImportUserService, ImportUserService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IBelongSiteService, BelongSiteService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IImportDepartmentService, ImportDepartmentService>();
builder.Services.AddScoped<ISectionService, SectionService>();
builder.Services.AddScoped<IImportSectionService, ImportSectionService>();
builder.Services.AddScoped<IUploadFileService, UploadFileService>();
builder.Services.AddScoped<IWorkflowDashboardService, WorkflowDashboardService>();
builder.Services.AddScoped<ApprovalDashboardOverviewService>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddSingleton<RequestPdfService>();
builder.Services.AddScoped<RequestBusinessService>();
builder.Services.AddScoped<WorkflowConfigurationService>();
builder.Services.AddScoped<RequestApprovalPreviewService>();
builder.Services.AddScoped<Web.Services.Workflow.Operations.LeaveAccountingService>();
builder.Services.AddScoped<Web.Services.Workflow.Operations.ApprovalOperationsService>();
builder.Services.AddScoped<Web.Services.Workflow.Operations.VehicleDispatchService>();
builder.Services.AddScoped<IEmailLogService, EmailLogService>();
builder.Services.AddScoped<INotiToastService, NotiToastService>();

// Hangfire client (enqueue only — Worker processes jobs)
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IWorkflowNotificationService, WorkflowNotificationService>();
builder.Services.AddScoped<IAccountNotificationService, AccountNotificationService>();
builder.Services.AddScoped<UserStatusService>();
builder.Services.AddScoped<IApproverSettingService, ApproverSettingService>();

// Workflow Core services (composition pattern)
builder.Services.AddScoped<IApproverResolver, ApproverResolver>();
builder.Services.AddScoped<IApprovalActionProcessor, ApprovalActionProcessor>();
builder.Services.AddScoped<IApprovalValidator, ApprovalValidator>();
builder.Services.AddScoped<IRequestNumberGenerator, RequestNumberGenerator>();
builder.Services.AddScoped<IApprovalWorkflowBuilder, ApprovalWorkflowBuilder>();
builder.Services.AddScoped<IApprovalNotifier, ApprovalNotifier>();

builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseRequestLocalization();

app.UseAuthentication();
app.UseMiddleware<LoginSessionNoticeMiddleware>();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    // Request documents are served only by the ownership-checked download endpoint.
    if (context.Request.Path.StartsWithSegments("/Files/requests", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next(context);
});


// turn on hangfire-dashboard
app.AddHangfireDashboard(builder.Configuration);

var fileServerConfig = app.Services.GetRequiredService<IOptions<FileServerConfig>>().Value;
var uploadRoot = Path.IsPathRooted(fileServerConfig.Path)
    ? fileServerConfig.Path
    : Path.GetFullPath(fileServerConfig.Path, app.Environment.ContentRootPath);
Directory.CreateDirectory(uploadRoot);
app.UseWhen(context => context.Request.Path.StartsWithSegments("/Files"), branch =>
{
    branch.UseMiddleware<AuthorizeMiddleware>();
    branch.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadRoot),
        RequestPath = "/Files"
    });
});

app.MapStaticAssets();
app.MapControllers();

// Host fallback route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
