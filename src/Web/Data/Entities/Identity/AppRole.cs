using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Web.Data.Entities.Identity
{
    public class AppRole: IdentityRole
    {
        public AppRole() : base(){}

        public string DisplayName { get; set; }

        public string CategoryName { get; set; }

        public int? CategoryOrder { get; set; }

        public ICollection<AppUserRole> UserRoles { get; set; }
    }
}