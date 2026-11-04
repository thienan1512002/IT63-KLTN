using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.Data.Entities.Notification;

namespace Web.Services.Portal.Noti
{
    public interface INotiToastService
    {
        public Task<List<NotiToast>> GetNotiToasts(bool isRead, string username);
        public Task ClearAllNoty(string userName);
    }
}