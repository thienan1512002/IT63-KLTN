using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;

namespace Web.Common.ActionFilter
{
    public class AllowedSectionGuard : TypeFilterAttribute
    {
        public AllowedSectionGuard(string allowedShortName, params int[] sites)
            : this(0, allowedShortName, sites)
        {
        }

        public AllowedSectionGuard(int sectionId, string allowedShortName, params int[] sites)
            : base(typeof(AllowedSectionGuardImplement))
        {
            Arguments = [sectionId, allowedShortName ?? "", sites ?? []];
        }

        private class AllowedSectionGuardImplement : IActionFilter
        {
            private readonly int _sectionId;
            private readonly string _allowedShortName;
            private readonly int[] _sites;
            private readonly ApplicationDbContext _context;
            private readonly IHttpContextAccessor _contextAccessor;

            public AllowedSectionGuardImplement(
                ApplicationDbContext dbContext,
                IHttpContextAccessor contextAccessor,
                int sectionId,
                string allowedShortName,
                int[] sites)
            {
                _context = dbContext;
                _contextAccessor = contextAccessor;
                _sectionId = sectionId;
                _allowedShortName = allowedShortName ?? "";
                _sites = sites ?? [];
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {
                var user = _contextAccessor.HttpContext?.User;
                var userFaxCode = user?.Identity?.Name;
                if (string.IsNullOrEmpty(userFaxCode)
                    || (_sectionId <= 0 && string.IsNullOrWhiteSpace(_allowedShortName)))
                {
                    Deny(context);
                    return;
                }

                var userSection = _context.AppUsers
                    .AsNoTracking()
                    .Where(x => x.UserName.Equals(userFaxCode))
                    .Select(u => new
                    {
                        u.SectionId,
                        ShortName = u.Section != null ? u.Section.ShortName : null,
                        IsActive = u.Section != null && u.Section.IsActive,
                        u.SiteId,
                    })
                    .FirstOrDefault();

                if (userSection == null)
                {
                    Deny(context);
                    return;
                }

                if (_sites.Length > 0 && !_sites.Any(s => s == userSection.SiteId))
                {
                    Deny(context);
                    return;
                }

                if (!userSection.IsActive)
                {
                    Deny(context);
                    return;
                }

                if (IsAllowedSection(userSection.SectionId, userSection.ShortName))
                    return;

                Deny(context);
            }

            private bool IsAllowedSection(int? userSectionId, string userShortName)
            {
                if (_sectionId > 0)
                    return userSectionId == _sectionId;

                return !string.IsNullOrWhiteSpace(userShortName)
                    && string.Equals(
                        _allowedShortName.Trim(),
                        userShortName.Trim(),
                        StringComparison.OrdinalIgnoreCase);
            }

            public void OnActionExecuted(ActionExecutedContext context)
            {
            }

            private static void Deny(ActionExecutingContext context)
            {
                if (IsApiRequest(context))
                {
                    context.Result = new ObjectResult(new { message = "Not Access" })
                    {
                        StatusCode = StatusCodes.Status403Forbidden,
                    };
                    return;
                }

                context.Result = new RedirectToRouteResult(new RouteValueDictionary(
                    new { action = "NotAccess", controller = "Home" }));
            }

            private static bool IsApiRequest(ActionExecutingContext context)
            {
                var request = context.HttpContext.Request;
                bool isAjax = request.Headers["X-Requested-With"] == "XMLHttpRequest";
                bool acceptsJson = request.Headers["Accept"].Any(h => h != null && h.Contains("application/json"));

                return isAjax || acceptsJson || request.Path.StartsWithSegments("/api");
            }
        }
    }
}
