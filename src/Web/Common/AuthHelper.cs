using System.Security.Claims;

namespace Web.Common
{
    public class AuthHelper
    {
        public static bool CheckRoles(ClaimsPrincipal user, List<string> roles)
        {
            return roles.Any(r => user.IsInRole(r));
        }
    }
}
