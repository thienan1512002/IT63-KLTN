using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Dashboard;

namespace Web.Configuration
{
    public static class HangfireConfiguration
    {
        public static void AddHangfireDashboard(this IApplicationBuilder app, IConfiguration configuration)
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                DashboardTitle = "Hangfire Job for IKSN server",
                DarkModeEnabled = true,
                Authorization = new[]
                {
                   new HangfireAuthorizationFilter()
                }
            });
        }
    }

    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Allow only users in the "SuperUser" role to see the Dashboard.
            // return httpContext.User.IsInRole("SuperUser");
            return httpContext.User.Identity.IsAuthenticated
                || httpContext.User.IsInRole("SuperUser");
        }
    }
}