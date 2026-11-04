using Hangfire;
using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Identity;
using Web.Models.Dtos.User;
using Worker.Shared.Entities;
using Worker.Shared.Enums;
using Worker.Shared.Interfaces;

namespace Web.Services.Account
{
    public class UserStatusService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UserStatusService> _logger;
        private readonly IAccountNotificationService _accountNotification;

        public UserStatusService(
            ApplicationDbContext db,
            ILogger<UserStatusService> logger,
            IAccountNotificationService accountNotification)
        {
            _db = db;
            _logger = logger;
            _accountNotification = accountNotification;
        }

        #region User Status Management

        /// <summary>
        /// Toggles a user's status between active and inactive.
        /// - GoInactive = true + From/To: creates InactiveSchedule and schedules Hangfire jobs.
        ///   If From <= now → deactivate immediately, schedule reactivation at To.
        ///   If From > now → schedule deactivation at From AND reactivation at To.
        /// - GoInactive = false: re-activates user immediately, cancels pending Hangfire jobs,
        ///   and removes all active schedules (manual override).
        /// </summary>
        public async Task UpdateUserStatus(UpdateUserStatusRequestDto request)
        {
            try
            {
                var user = await GetUserOrThrowAsync(request.UserName);

                if (request.GoInactive && !user.IsOnline)
                    throw new InvalidOperationException($"User {request.UserName} is already inactive.");

                if (!request.GoInactive && user.IsOnline)
                    throw new InvalidOperationException($"User {request.UserName} is already active.");

                if (request.GoInactive)
                    await HandleGoInactive(user, request);
                else
                    await HandleManualActivation(user, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for user {UserName}.", request.UserName);
                throw; // Let the controller handle the exception and return an appropriate response
            }
        }

        private async Task<AppUser> GetUserOrThrowAsync(string userName)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                _logger.LogWarning("User {UserName} not found when trying to update status.", userName);
                throw new KeyNotFoundException($"User {userName} not found.");
            }
            return user;
        }

        private static void ValidateGoInactiveRequest(UpdateUserStatusRequestDto request)
        {
            if (!request.From.HasValue || !request.To.HasValue)
                throw new ArgumentException("From and To dates must be provided when setting user to inactive.");

            if (request.From.Value >= request.To.Value)
                throw new ArgumentException("From date must be earlier than To date.");

            if (request.To.Value <= DateTime.Now)
                throw new ArgumentException("To date must be in the future.");
        }

        private async Task HandleGoInactive(AppUser user, UpdateUserStatusRequestDto request)
        {
            ValidateGoInactiveRequest(request);

            var existingSchedule = await _db.InactiveSchedules
                .Where(s => s.UserName == request.UserName)
                .ToListAsync();            

            // Clear existing schedules if it exists, otherwise create a new one.
            if (existingSchedule.Any()){
                //throw new InvalidOperationException($"User {request.UserName} already has an inactive schedule. Please cancel the existing schedule first.");
                // Cancel existing Hangfire jobs before reusing the schedule
                ClearExistingSchedules(existingSchedule);
            }


            InactiveSchedule schedule = new InactiveSchedule
                {
                    UserName = request.UserName,
                    From = request.From!.Value,
                    To = request.To!.Value,
                    Comment = request.Comment,
                    CreatedAt = DateTime.Now,
                };
                _db.InactiveSchedules.Add(schedule);
            

           
            await _db.SaveChangesAsync(); // Persist to get schedule.Id before scheduling jobs

            if (schedule.From <= DateTime.Now)
                ApplyImmediateDeactivation(user, schedule, request.Comment);
            else
                schedule.DeactivateJobId = BackgroundJob.Schedule<IUserStatusJob>(
                    job => job.DeactivateUserAsync(schedule.Id),
                    schedule.From - DateTime.Now);

            // Always schedule reactivation at To date.
            // The schedule record is intentionally kept alive here so the reactivation job can find it.
            schedule.ReactivateJobId = BackgroundJob.Schedule<IUserStatusJob>(
                job => job.ReactivateUserAsync(schedule.Id),
                schedule.To - DateTime.Now);

            await _db.SaveChangesAsync();

            // Notify backup approvers via email + NotiToast
            await _accountNotification.SendInactiveNotificationAsync(
                request.UserName,
                schedule.From,
                schedule.To,
                schedule.Comment);
        }



        /// <summary>
        /// Deactivates the user immediately and logs the change.
        /// Does not delete the schedule — the reactivation job will clean it up at the To date.
        /// </summary>
        private void ApplyImmediateDeactivation(AppUser user, InactiveSchedule schedule, string comment)
        {
            user.IsOnline = false;

            _db.AccountStatusLogs.Add(new AccountStatusLog
            {
                Username = schedule.UserName,
                NewStatus = AccountStatus.Inactive,
                Reason = StatusChangeReason.ByImmediateDeactivation,
                Comment = $"Immediate deactivation (schedule {schedule.Id}): {comment}",
                ChangedAt = DateTime.Now
            });
        }

        /// <summary>
        /// User manually re-activates the account, overriding any existing schedules.
        /// </summary>
        private async Task HandleManualActivation(AppUser user, UpdateUserStatusRequestDto request)
        {
            user.IsOnline = true;

            var activeSchedules = await _db.InactiveSchedules
                .Where(s => s.UserName == request.UserName)
                .ToListAsync();

            // Cancel pending Hangfire jobs using the actual stored job IDs
            ClearExistingSchedules(activeSchedules);

            _db.AccountStatusLogs.Add(new AccountStatusLog
            {
                Username = request.UserName,
                NewStatus = AccountStatus.Active,
                Reason = StatusChangeReason.ByManualOverride,
                Comment = $"Manual activation - {activeSchedules.Count} schedule(s) overridden",
                ChangedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
        }

        private void ClearExistingSchedules(List<InactiveSchedule> existingSchedules)
        {
           
            foreach (var schedule in existingSchedules)
            {
                if (schedule.DeactivateJobId != null)
                    BackgroundJob.Delete(schedule.DeactivateJobId);
                if (schedule.ReactivateJobId != null)
                    BackgroundJob.Delete(schedule.ReactivateJobId);
                
            }
            _db.InactiveSchedules.RemoveRange(existingSchedules);

        }

        #endregion
    }
}