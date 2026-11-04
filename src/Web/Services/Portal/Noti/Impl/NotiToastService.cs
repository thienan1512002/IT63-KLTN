using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.Data.DbContexts;
using Web.Data.Entities.Notification;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Web.Services.Localization;

namespace Web.Services.Portal.Noti.Impl
{
    public class NotiToastService : INotiToastService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _baseUrl;
        private readonly ILogger<NotiToastService> _logger;

        public NotiToastService(ApplicationDbContext context, IConfiguration configuration, ILogger<NotiToastService> logger)
        {
            _context = context;
            _baseUrl = configuration["BaseUrl"];
            _logger = logger;
        }
        public async Task<List<NotiToast>> GetNotiToasts(bool isRead, string username)
        {
            // var userInfo = await _context.AppUsers
            // .Where(x => x.UserName == username)
            // .Select(x => new { x.UserName, x.SectionId.Vasue, x.SiteId })
            // .FirstOrDefaultAsync();

            List<NotiToast> notitoasts = await _context.NotiToasts
                .Where(x => x.IsRead == isRead && (x.SendFor == username))
                .Select(x => new NotiToast
                {
                    IsRead = x.IsRead,
                    SendFor = x.SendFor,
                    Content = x.Content,
                    Id = x.Id,
                    Url = x.Url,
                    NeedTranslate = x.NeedTranslate,
                    ContentKey = x.ContentKey,
                    ContentArgsJson = x.ContentArgsJson,
                    Title = x.Title,
                    CreatedAt = x.CreatedAt,
                    RequestId = x.RequestId,
                })
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            // Enrich legacy submission notices on read so existing bell items also show meaningful names.
            // Only requests referenced by this user's own notifications are loaded; no stored data is rewritten.
            var submitted = notitoasts.Where(x => (x.ContentKey ?? x.Content) is "workflow_noty_submitted" or "workflow_noty_submitted_detail").ToList();
            var requestIds = submitted.Where(x => x.RequestId.HasValue).Select(x => x.RequestId.Value).Distinct().ToList();
            var requests = await _context.ApprovalRequests.AsNoTracking().Where(x => requestIds.Contains(x.Id))
                .Select(x => new { x.Id, x.FullName, x.FaxCode, x.Title, x.RequestName, x.ApplicationNo }).ToDictionaryAsync(x => x.Id);
            foreach (var item in submitted)
            {
                if (!item.RequestId.HasValue || !requests.TryGetValue(item.RequestId.Value, out var request)) continue;
                item.ContentKey = "workflow_noty_submitted_detail";
                item.ContentArgsJson = JsonSerializer.Serialize(new[] {
                    string.IsNullOrWhiteSpace(request.FullName) ? request.FaxCode : request.FullName,
                    string.IsNullOrWhiteSpace(request.Title) ? request.RequestName : request.Title,
                    request.ApplicationNo
                });
                item.NeedTranslate = true;
            }

            foreach (var item in notitoasts)
            {

                // Translate content if it contains specific keywords
                if (item.NeedTranslate)
                {
                    try
                    {
                        // Pass arguments to translation if ContentArgsJson is not null or empty
                        if (!string.IsNullOrWhiteSpace(item.ContentArgsJson))
                        {
                            var args = JsonSerializer.Deserialize<string[]>(item.ContentArgsJson) ?? Array.Empty<string>();
                            item.Content = args.Length > 0
                                ? LanguageService.Translate(item.ContentKey, args.Cast<object>().ToArray())
                                : LanguageService.Translate(item.ContentKey);
                        }
                        else
                        {
                            item.Content = LanguageService.Translate(item.ContentKey);
                        }

                        item.NeedTranslate = false;
                    }
                    catch
                    {
                        // Fallback: keep original content without translation.
                    }
                }

            }
            return notitoasts;
        }

        public async Task ClearAllNoty(string userName)
        {
            var data = await _context.NotiToasts.Where(n => n.SendFor == userName).ToListAsync();
            if (data.Count > 0)
            {
                try
                {
                    foreach (var item in data) item.IsRead = true;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // _context.Logs.Add(new Log
                    // {
                    //     ActionBy = userName,
                    //     Content = "Error clear all noty. " + ex.Message,
                    //     Date = DateTime.Now.ToString(),
                    // });
                    _logger.LogError(ex, "Error clear all noty for user {UserName}", userName);
                    await _context.SaveChangesAsync();
                }
            }
        }

    }
}
