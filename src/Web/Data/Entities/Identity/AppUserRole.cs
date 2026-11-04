
using Microsoft.AspNetCore.Identity;

namespace Web.Data.Entities.Identity
{
    public class AppUserRole : IdentityUserRole<string>
    {
        public virtual AppUser User { get; set; } = null!;

        public virtual AppRole Role { get; set; } = null!;
    }
}