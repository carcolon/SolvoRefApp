using Core.BackgroundTask.UpdateReferralStatusTask;
using Core.BackgroundTask.UpdateUserExtraDataTask;
using Core.BackgroundTask.SyncActiveVacanciesTask;
using Hangfire;
using System.Runtime.InteropServices;

namespace Api.Middleware
{
    public static class HangFireMiddleWare
    {

        public static void StartRecurringJobs(this IApplicationBuilder app, IConfiguration configuration)
        {
            var cronExpression = configuration["BackgroundTask:OnceADayCronTask"] ?? "0 9 * * *";
            var colombiaTimeZone = ResolveColombiaTimeZone();

            app.ApplicationServices.GetService<IGlobalConfiguration>();
            RecurringJob.AddOrUpdate<UpdateReferralStatusTaskScheduler>(
            "Update Referral Status",
            x => x.ScheduleUpdateReferralStatusTasks(),
            cronExpression,
            new RecurringJobOptions { TimeZone = colombiaTimeZone }
            );
            app.ApplicationServices.GetService<IGlobalConfiguration>();
            RecurringJob.AddOrUpdate<UpdateUserExtraDataTaskScheduler>(
            "Update users extra data",
            x => x.ScheduleUpdateUserExtraDataTasks(),
            cronExpression,
            new RecurringJobOptions { TimeZone = colombiaTimeZone }
            );
            app.ApplicationServices.GetService<IGlobalConfiguration>();
            RecurringJob.AddOrUpdate<SyncActiveVacanciesTaskScheduler>(
            "Sync active vacancies from Fabric",
            x => x.ScheduleSyncActiveVacanciesTasks(),
            cronExpression,
            new RecurringJobOptions { TimeZone = colombiaTimeZone }
            );
            RecurringJob.TriggerJob("Sync active vacancies from Fabric");
            RecurringJob.TriggerJob("Update Referral Status");
        }

        private static TimeZoneInfo ResolveColombiaTimeZone()
        {
            var timeZoneIds = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new[] { "SA Pacific Standard Time" }
                : new[] { "America/Bogota" };

            foreach (var timeZoneId in timeZoneIds)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.Utc;
        }
    }
}
