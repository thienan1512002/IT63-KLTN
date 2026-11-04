#nullable disable
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Web.Middlewares;

/// <summary>Phiên cookie bị reject (stamp lệch sau đổi quyền): cookie một lần → GET Login hiển thị Swal.</summary>
public static class LoginSessionNotice
{
    public const string CookieName = "login_notice";
    public const string CookieValue = "session_invalid";
    public const string ItemKey = "LoginNotice";
}

public sealed class LoginSessionNoticeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            && context.Request.Path.StartsWithSegments("/Account/Login")
            && context.Request.Cookies.TryGetValue(LoginSessionNotice.CookieName, out var v)
            && !string.IsNullOrEmpty(v))
        {
            context.Items[LoginSessionNotice.ItemKey] = v;
            context.Response.Cookies.Delete(LoginSessionNotice.CookieName, new CookieOptions { Path = "/" });
        }

        await next(context);
    }
}

public sealed class LoginSessionNoticePostConfigure : IPostConfigureOptions<CookieAuthenticationOptions>
{
    public int Order => 10000;

    public void PostConfigure(string name, CookieAuthenticationOptions options)
    {
        if (name != IdentityConstants.ApplicationScheme) return;

        var inner = options.Events.OnValidatePrincipal;
        options.Events.OnValidatePrincipal = async context =>
        {
            var okBefore = context.Principal?.Identity?.IsAuthenticated == true;
            if (inner is not null) await inner(context);
            var okAfter = context.Principal?.Identity?.IsAuthenticated == true;

            if (okBefore && !okAfter && !context.Response.HasStarted)
            {
                context.Response.Cookies.Append(LoginSessionNotice.CookieName, LoginSessionNotice.CookieValue, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    MaxAge = TimeSpan.FromSeconds(90),
                    Path = "/",
                });
            }
        };
    }
}

public static class LoginSessionNoticeExtensions
{
    public static IServiceCollection AddLoginSessionNotice(this IServiceCollection services) =>
        services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, LoginSessionNoticePostConfigure>();
}
