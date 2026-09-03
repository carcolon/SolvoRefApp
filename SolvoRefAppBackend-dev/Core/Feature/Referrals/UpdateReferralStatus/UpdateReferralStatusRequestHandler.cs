using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.Contracts.Fabric;
using Core.Contracts.Referrals;
using Core.Feature.Referrals.Common;
using Core.Models.Global;
using Core.Models.Referrals;
using MediatR;

namespace Core.Feature.Referrals.UpdateReferralStatus
{
    public class UpdateReferralStatusRequestHandler : IRequestHandler<UpdateReferralStatusRequest, Response<Unit>>
    {

        private readonly IReferralRepository _referralRepository;
        private readonly IFabricService _fabricService;
        private readonly IPaymentScheduleRepository _paymentScheduleRepository;

        public UpdateReferralStatusRequestHandler(IFabricService fabricService, IReferralRepository referralRepository, IPaymentScheduleRepository paymentScheduleRepository)
        {
            _fabricService = fabricService;
            _referralRepository = referralRepository;
            _paymentScheduleRepository = paymentScheduleRepository;
        }

        public async Task<Response<Unit>> Handle(UpdateReferralStatusRequest request, CancellationToken cancellationToken)
        {
            var referralsOpen = await _referralRepository.GetReferralsOpen();
            var openSources = new List<string> { ReferralDataSourcingConstants.Source };
            var personalIds = referralsOpen.Select(x => x.ReferralID).Distinct().ToList();
            var emails = referralsOpen.Select(x => x.Email).Distinct().ToList();
            var newStatus = await _fabricService.GetReferralStatuses(openSources, emails);
            var huntyEmailsResponse = await _fabricService.GetHuntyEmails(emails);
            var employees = await _fabricService.GetEmployeesByPersonalId(personalIds);
            var paymentRows = await _paymentScheduleRepository.GetAll();
            var applicantStatuses = newStatus.Success ? newStatus.Data ?? [] : [];
            var huntyEmails = huntyEmailsResponse.Success
                ? (huntyEmailsResponse.Data ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var employeeRows = employees.Success ? employees.Data ?? [] : [];

            foreach (var item in referralsOpen)
            {

                var isTransparent = ReferralCompanyResolver.IsTransparentReferral(item.Account, item.Area, item.Country, item.City);
                var solverData = employeeRows
                    .Where(x =>
                        x.PersonalId.Equals(item.ReferralID, StringComparison.OrdinalIgnoreCase) &&
                        x.StartDate > item.CreationDate)
                    .OrderBy(x => x.StartDate)
                    .FirstOrDefault();
                if (solverData != null)
                {
                    item.Status = "Hired";
                    item.Updatable = true;
                    item.StartDate = solverData.StartDate == DateTime.MinValue ? null : solverData.StartDate;
                    ApplyReferralPaymentMessage(paymentRows, item, solverData);
                    continue;
                }

                if (!item.Updatable)
                {
                    continue;
                }

                var expectedSource = ReferralDataSourcingConstants.Source;
                var normalizedExpectedSource = NormalizeStatusLookupValue(expectedSource);
                var normalizedEmail = NormalizeStatusLookupValue(item.Email);
                var updateData = ResolveApplicantStatus(applicantStatuses, normalizedExpectedSource, normalizedEmail);

                if (isTransparent && IsWithinTransparentOnHoldWindow(item.CreationDate))
                {
                    item.Status = "On Hold/ Waiting for the right match";
                    continue;
                }

                if (isTransparent && IsReferralExpired(item.CreationDate, isTransparent))
                {
                    item.Status = "Referral expired";
                    item.Updatable = false;
                    continue;
                }

                if (isTransparent)
                {
                    var transparentStatus = ResolveTransparentReferralStatus(updateData, item.CreationDate);
                    if (!string.IsNullOrWhiteSpace(transparentStatus))
                    {
                        item.Status = transparentStatus;
                        item.StatusLead = updateData?.StatusLead ?? item.StatusLead;
                    }

                    continue;
                }

                var solvoStatus = ResolveSolvoReferralStatus(
                    updateData,
                    item.CreationDate,
                    item.Status,
                    huntyEmails.Contains(item.Email.Trim().ToLowerInvariant()));

                if (!string.IsNullOrWhiteSpace(solvoStatus))
                {
                    item.Status = solvoStatus;
                    item.StatusLead = updateData?.StatusLead ?? item.StatusLead;
                    if (solvoStatus.Equals("Referral expired", StringComparison.OrdinalIgnoreCase))
                    {
                        item.Updatable = false;
                    }

                    continue;
                }
            }
            await _referralRepository.Update(referralsOpen);
            return Response<Unit>.SuccessResponse(Unit.Value, HttpStatusCode.OK);
        }

        private static string ResolveSolvoReferralStatus(UpdateReferralStatusDto? updateData, DateTime creationDate, string currentStatus, bool existsInHunty)
        {
            var daysSinceCreation = GetDaysSinceCreation(creationDate);
            var resumeAvailable = updateData?.ResumeAvailable.Trim() ?? string.Empty;
            var hasResume = IsResumeAvailableYes(resumeAvailable);
            var hasNoResume = IsResumeAvailableNo(resumeAvailable) ||
                (updateData != null && string.IsNullOrWhiteSpace(resumeAvailable));

            if ((hasNoResume && daysSinceCreation >= 15) ||
                (currentStatus.Equals("No Call No Show (NCNS)", StringComparison.OrdinalIgnoreCase) && daysSinceCreation >= 15))
            {
                return "Referral expired";
            }

            if (hasResume && daysSinceCreation >= 30 && daysSinceCreation <= 59)
            {
                return "On Hold/ Waiting for the right match";
            }

            if (hasResume && daysSinceCreation >= 1 && daysSinceCreation <= 29)
            {
                return "Seeking a position";
            }

            if (hasNoResume && daysSinceCreation >= 3 && daysSinceCreation <= 14)
            {
                return "No Call No Show (NCNS)";
            }

            if (existsInHunty && daysSinceCreation >= 1 && daysSinceCreation <= 2)
            {
                return "First Contact";
            }

            return string.Empty;
        }

        private static string ResolveTransparentReferralStatus(UpdateReferralStatusDto? updateData, DateTime creationDate)
        {
            if (updateData == null)
            {
                return string.Empty;
            }

            if (IsWithinTransparentSeekingWindow(creationDate) &&
                IsTransparentSeekingStatus(updateData.ApplicantStatus))
            {
                return "Seeking a position";
            }

            if (IsWithinTransparentFirstContactWindow(creationDate) &&
                !string.IsNullOrWhiteSpace(updateData.Ownership))
            {
                return "First Contact";
            }

            return string.Empty;
        }

        private static bool IsWithinTransparentFirstContactWindow(DateTime creationDate)
        {
            var daysSinceCreation = GetDaysSinceCreation(creationDate);
            return daysSinceCreation >= 1 && daysSinceCreation <= 2;
        }

        private static bool IsWithinTransparentSeekingWindow(DateTime creationDate)
        {
            var daysSinceCreation = GetDaysSinceCreation(creationDate);
            return daysSinceCreation >= 1 && daysSinceCreation <= 29;
        }

        private static bool IsWithinTransparentOnHoldWindow(DateTime creationDate)
        {
            var daysSinceCreation = GetDaysSinceCreation(creationDate);
            return daysSinceCreation >= 30 && daysSinceCreation <= 59;
        }

        private static bool IsReferralExpired(DateTime creationDate, bool isTransparent)
        {
            if (isTransparent)
            {
                return GetDaysSinceCreation(creationDate) >= 60;
            }

            return GetDaysSinceCreation(creationDate) >= 60;
        }

        private static int GetDaysSinceCreation(DateTime creationDate)
        {
            return (DateTime.Now.Date - creationDate.Date).Days + 1;
        }

        private static string NormalizeStatusLookupValue(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static string NormalizeApplicantStatusValue(string value)
        {
            return NormalizeStatusLookupValue(value)
                .Replace("with a vacancy", "with vacancy", StringComparison.OrdinalIgnoreCase)
                .Replace(" - ", "-", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .ToLowerInvariant();
        }

        private static UpdateReferralStatusDto? ResolveApplicantStatus(
            List<UpdateReferralStatusDto> applicantStatuses,
            string normalizedExpectedSource,
            string normalizedEmail)
        {
            var emailMatches = applicantStatuses
                .Where(x => normalizedEmail.Equals(NormalizeStatusLookupValue(x.Email), StringComparison.OrdinalIgnoreCase))
                .ToList();
            var sourceAndEmailMatches = emailMatches
                .Where(x => normalizedExpectedSource.Equals(NormalizeStatusLookupValue(x.Source), StringComparison.OrdinalIgnoreCase));

            return ChooseBestApplicantStatus(sourceAndEmailMatches)
                ?? ChooseBestApplicantStatus(emailMatches);
        }

        private static UpdateReferralStatusDto? ChooseBestApplicantStatus(IEnumerable<UpdateReferralStatusDto> applicantStatuses)
        {
            return applicantStatuses
                .OrderByDescending(x => IsResumeAvailableYes(x.ResumeAvailable))
                .ThenByDescending(x => IsTransparentSeekingStatus(x.ApplicantStatus))
                .ThenByDescending(x => !string.IsNullOrWhiteSpace(x.Ownership))
                .ThenByDescending(x => IsResumeAvailableNo(x.ResumeAvailable))
                .FirstOrDefault();
        }

        private static bool IsResumeAvailableYes(string value)
        {
            return NormalizeStatusLookupValue(value).Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsResumeAvailableNo(string value)
        {
            return NormalizeStatusLookupValue(value).Equals("no", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTransparentSeekingStatus(string value)
        {
            return NormalizeApplicantStatusValue(value)
                .Equals(NormalizeApplicantStatusValue("Pending match with Vacancy - Talent Pool"), StringComparison.Ordinal);
        }

        private static void ApplyReferralPaymentMessage(List<PaymentSchedule> paymentRows, Referral item, ExtraUser solverData)
        {
            if (IsKenyaReferrer(item))
            {
                item.PaymentMessage = string.Empty;
                return;
            }

            var today = DateTime.Now.Date;
            var applicationDate1 = solverData.StartDate.Date.AddDays(30);
            var applicationDate2 = solverData.StartDate.Date.AddDays(60);
            var referralIsActive = IsActive(solverData.Status);
            var referrerIsActive = IsActive(item.Referrer.Status);

            if (today < applicationDate1)
            {
                item.PaymentMessage = "The referred person has not yet completed 30 active days at Solvo Global S.A.S. The first payment will be enabled once the first eligibility period is completed.";
                return;
            }

            if (!referralIsActive || !referrerIsActive)
            {
                item.Updatable = false;
                item.PaymentMessage = BuildInactivePaymentMessage();
                return;
            }

            if (today >= applicationDate2)
            {
                DateTime? firstPeriodPaymentDate = GetPaymentDate(paymentRows, item, applicationDate1);
                if (firstPeriodPaymentDate.HasValue && item.FirstPayment == DateTime.MinValue)
                {
                    item.FirstPayment = firstPeriodPaymentDate.Value;
                }

                DateTime? paymentDate = GetPaymentDate(paymentRows, item, applicationDate2);
                if (paymentDate.HasValue)
                {
                    item.SecondPayment = paymentDate.Value;
                }

                item.Updatable = false;
                item.PaymentMessage = BuildSecondPaymentMessage(paymentDate);
                return;
            }

            DateTime? firstPaymentDate = GetPaymentDate(paymentRows, item, applicationDate1);
            if (firstPaymentDate.HasValue)
            {
                item.FirstPayment = firstPaymentDate.Value;
            }

            item.PaymentMessage = BuildFirstPaymentMessage(firstPaymentDate);
        }

        private static bool IsActive(string value)
        {
            return NormalizeStatusLookupValue(value).Equals("Active", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildFirstPaymentMessage(DateTime? paymentDate)
        {
            return "Hi, Soulver\n\n" +
                "We want to let you know that your referral has completed their first employment period. " +
                "Therefore, in accordance with the program terms and conditions, you will receive the corresponding payment " +
                $"in the payroll period of {FormatPaymentDate(paymentDate)}.";
        }

        private static string BuildSecondPaymentMessage(DateTime? paymentDate)
        {
            return "Hi, Soulver\n\n" +
                "We want to let you know that your referral has completed their second employment period. " +
                "Therefore, in accordance with the program terms and conditions, you will receive the total corresponding payment " +
                $"in the payroll period of {FormatPaymentDate(paymentDate)}.";
        }

        private static string BuildInactivePaymentMessage()
        {
            return "Hi, Soulver\n\n" +
                "We want to let you know that your referral is no longer active with the company. Therefore, the incentive payment cannot be completed.\n\n" +
                "Remember that the referral bonus is paid in two installments: the first when your referral completes 30 active days and the second when your referral completes 60 active days.\n\n" +
                "In accordance with the program terms and conditions, both the Soulver and the referral must be active when each period is completed in order to receive each payment.\n\n" +
                "If you have any questions about this case, you can contact the program support channels.";
        }

        private static string FormatPaymentDate(DateTime? paymentDate)
        {
            return paymentDate.HasValue ? paymentDate.Value.ToString("MM-dd-yyyy") : "the applicable payroll date";
        }



        private static DateTime? ResolveQuincenal(DateTime date, List<PaymentSchedule> rows)
        {
            return ResolvePaymentDateByDeadline(date, rows);
        }


        private static DateTime? GetPaymentDate(List<PaymentSchedule> payment, Referral item, DateTime applicationDate)
        {
            DateTime? paymentDate = null;
            switch (item.Referrer.PaymentFrequency.ToLower())
            {
                case "me":
                    item.Referrer.PaymentFrequency = "mensual";
                    break;
                case "qc":
                    item.Referrer.PaymentFrequency = "quincenal";
                    break;
            }
            List<PaymentSchedule> companyPayment = payment.Where(x =>
                ValuesMatch(x.Employer, item.Referrer.PayrollCompany) &&
                ValuesMatch(x.PaymentFrequency, item.Referrer.PaymentFrequency))
                .ToList();

            if (companyPayment.Count > 0)
            {
                switch (item.Referrer.PaymentFrequency.ToLower())
                {
                    case "mensual":
                        paymentDate = ResolveMensual(applicationDate, companyPayment);
                        break;
                    case "quincenal":
                        paymentDate = ResolveQuincenal(applicationDate, companyPayment);
                        break;
                }

            }
            return paymentDate;
        }

        private static bool ValuesMatch(string left, string right)
        {
            return NormalizeStatusLookupValue(left).Equals(NormalizeStatusLookupValue(right), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsKenyaReferrer(Referral item)
        {
            return string.Equals(item.Referrer.Country?.Trim(), "Kenya", StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime? ResolveMensual(DateTime date, List<PaymentSchedule> rows)
        {
            return ResolvePaymentDateByDeadline(date, rows);
        }

        private static DateTime? ResolvePaymentDateByDeadline(DateTime date, List<PaymentSchedule> rows)
        {
            if (rows == null || rows.Count == 0)
                return null;

            rows = rows
                .OrderBy(r => r.DeadLine1 ?? r.DeadLine2 ?? DateTime.MaxValue)
                .ThenBy(r => r.DeadLine2)
                .ToList();

            foreach (var row in rows)
            {
                if (row.DeadLine1.HasValue && date.Date <= row.DeadLine1.Value.Date)
                {
                    return row.PaymentDate1 ?? row.PaymentDate2;
                }

                if (row.DeadLine2.HasValue && date.Date <= row.DeadLine2.Value.Date)
                {
                    return row.PaymentDate2 ?? row.PaymentDate1;
                }
            }

            var last = rows[^1];
            return last.PaymentDate2 ?? last.PaymentDate1;
        }

    }
}
