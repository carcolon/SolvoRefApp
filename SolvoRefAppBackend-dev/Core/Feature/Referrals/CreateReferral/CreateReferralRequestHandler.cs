using System.Net;
using System.Globalization;
using System.Text;
using AutoMapper;
using Core.Contracts.Azure;
using Core.Contracts.Configurations;
using Core.Contracts.Datalake;
using Core.Contracts.DataSourcing;
using Core.Contracts.Identity;
using Core.Contracts.Referrals;
using Core.DBContext;
using Core.Models.DataSourcing;
using Core.Models.Global;
using Core.Models.Referrals;
using Core.Feature.Referrals.Common;
using Core.Security;
using Core.Contracts.Fabric;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Feature.Referrals.CreateReferral
{
    public class CreateReferralRequestHandler : IRequestHandler<CreateReferralRequest, Response<string>>
    {
        private readonly IMapper _mapper;
        private readonly SolvoRefAppContext _context;
        private readonly IReferralRepository _referralRepository;
        private readonly IUserService _userService;
        private readonly IDataSourcingService _dataSourcingService;
        private readonly ICountryHuntyInformationRepository _countryHuntyInformationRepository;
        private readonly IFabricService _fabricService;

        public CreateReferralRequestHandler(IReferralRepository referralRepository, IMapper mapper, IUserService userService, SolvoRefAppContext context, IDataSourcingService dataSourcingService, ICountryHuntyInformationRepository countryHuntyInformationRepository, IFabricService fabricService)
        {
            _mapper = mapper;
            _referralRepository = referralRepository;
            _userService = userService;
            _context = context;
            _dataSourcingService = dataSourcingService;
            _countryHuntyInformationRepository = countryHuntyInformationRepository;
            _fabricService = fabricService;
        }

        public async Task<Response<string>> Handle(CreateReferralRequest request, CancellationToken cancellationToken)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            var sanitizedRequest = request.Data.Sanitize();

            var referrerId = string.IsNullOrWhiteSpace(request.ReferrerId)
                ? _userService.UserId
                : request.ReferrerId;
            var referrerProfile = await _context.Users
                .AsNoTracking()
                .Where(user => user.Id == referrerId && user.Status == "Active")
                .Select(user => new
                {
                    user.EmployeeId,
                    user.SolvoPartnerStatus
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(referrerProfile?.EmployeeId))
            {
                await transaction.RollbackAsync(cancellationToken);
                return Response<string>.ErrorResponse(
                    ["Your employee ID is missing. Sign out and sign in again, or contact the system administrator."],
                    HttpStatusCode.Forbidden);
            }

            var referral = _mapper.Map<Referral>(sanitizedRequest);
            referral.ReferrerID = referrerId;
            referral.ReferrerEmployeeId = referrerProfile.EmployeeId;
            referral.ReferrerSolvoPartnerStatus = ResolveSolvoPartnerStatus(referrerProfile.SolvoPartnerStatus);
            referral.ReferralFromSolvoPartner = IsActiveSolvoPartner(referrerProfile.SolvoPartnerStatus);
            referral.ReferralSubmissionKey = ReferralDuplicateKey.Create(referral.ReferrerID, sanitizedRequest.ReferralID, sanitizedRequest.Email);

            var duplicateCandidate = await _referralRepository.ExistsCandidateReferral(sanitizedRequest.ReferralID, sanitizedRequest.Email);
            if (duplicateCandidate)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Response<string>.ErrorResponse(["This candidate has already been referred."], HttpStatusCode.Conflict);
            }

            var duplicateSubmission = await _referralRepository.ExistsDuplicateSubmission(referral.ReferralSubmissionKey);
            if (duplicateSubmission)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Response<string>.ErrorResponse(["A referral for this candidate has already been submitted by you."], HttpStatusCode.Conflict);
            }

            var fabricValidation = await _fabricService.ReferredValidation(sanitizedRequest.Phone, sanitizedRequest.Email);
            if (!fabricValidation.Success)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Response<string>.ErrorResponse(fabricValidation.Errors, fabricValidation.StatusCode);
            }

            if (!fabricValidation.Data)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Response<string>.ErrorResponse(["The candidate does not meet the referral program requirements."], HttpStatusCode.BadRequest);
            }
            try
            {
                await _referralRepository.CreateReferral(referral);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Response<string>.ErrorResponse(["A referral for this candidate has already been submitted by you."], HttpStatusCode.Conflict);
            }

            var dataSourcing = _mapper.Map<DataSourcingTable>(referral);
            dataSourcing.Position = sanitizedRequest.Position ?? string.Empty;
            dataSourcing.ExternalVacancyId = NormalizeOptionalValue(sanitizedRequest.ExternalVacancyId);
            dataSourcing.Company = ReferralCompanyResolver.ResolveDataSourcingCompany(sanitizedRequest.Account, sanitizedRequest.Area, sanitizedRequest.Country, sanitizedRequest.City);
            dataSourcing.EnglishLevel = ResolveDataSourcingEnglishLevel(sanitizedRequest.EnglishLevel);
            dataSourcing.Experience = ResolveDataSourcingExperience(sanitizedRequest.Experience);
            var countriesHuntyInformation = await _countryHuntyInformationRepository.GetAll();
            var vacancyCountry = string.IsNullOrWhiteSpace(dataSourcing.ExternalVacancyId)
                ? sanitizedRequest.Country
                : sanitizedRequest.VacancyCountry;
            var countryForVacancyId = string.IsNullOrWhiteSpace(vacancyCountry)
                ? sanitizedRequest.Country
                : vacancyCountry;
            var normalizedCountryForVacancyId = NormalizeCountryForComparison(countryForVacancyId);
            var matchingCountryHuntyInformation = countriesHuntyInformation
                .Where(x => NormalizeCountryForComparison(x.Country).Equals(normalizedCountryForVacancyId, StringComparison.Ordinal))
                .ToList();
            var useTbpoVacancy = dataSourcing.Company.Equals(ReferralCompanyResolver.Transparent, StringComparison.OrdinalIgnoreCase);
            var countryHunty = matchingCountryHuntyInformation.FirstOrDefault(x =>
                    useTbpoVacancy &&
                    NormalizeForComparison(x.ProgramName).Contains("tbpo", StringComparison.Ordinal)) ??
                matchingCountryHuntyInformation.FirstOrDefault(x =>
                    !NormalizeForComparison(x.ProgramName).Contains("tbpo", StringComparison.Ordinal));
            if (countryHunty != null)
            {
                dataSourcing.VacancyId = countryHunty.VacancyId;
                dataSourcing.CompanyId = countryHunty.CompanyId;
                dataSourcing.Api_key = countryHunty.Api_key;
            }
            var dataSourcingResponse = await _dataSourcingService.Create(dataSourcing);
            if (!dataSourcingResponse.Success)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Response<string>.ErrorResponse(dataSourcingResponse.Errors, dataSourcingResponse.StatusCode);
            }
            if (!dataSourcingResponse.Data)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Response<string>.ErrorResponse(["error inserting in data sourcing"], HttpStatusCode.BadRequest);
            }
            await transaction.CommitAsync(cancellationToken);
            return Response<string>.SuccessResponse("Referred successfully created", HttpStatusCode.OK);
        }

        private static string ResolveDataSourcingEnglishLevel(string englishLevel)
        {
            return NormalizeForComparison(englishLevel) switch
            {
                "b2 intermediate" => "B2 - Intermediate",
                "c1 advanced" => "C1 - Advanced",
                "c2 professional" => "C2 - Professional",
                _ => englishLevel
            };
        }

        private static bool IsActiveSolvoPartner(string? status)
        {
            return string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveSolvoPartnerStatus(string? status)
        {
            return IsActiveSolvoPartner(status) ? "Active" : "Inactive";
        }

        private static string NormalizeOptionalValue(string? value)
        {
            var normalized = value?.Trim() ?? string.Empty;
            return normalized.Equals("undefined", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("null", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : normalized;
        }

        private static string NormalizeCountryForComparison(string value)
        {
            var normalized = NormalizeForComparison(value);
            if (normalized == "kenia")
            {
                return "kenya";
            }

            if (normalized == "peru" || normalized.StartsWith("pera", StringComparison.Ordinal))
            {
                return "peru";
            }

            return normalized;
        }

        private static string ResolveDataSourcingExperience(string experience)
        {
            return NormalizeForComparison(experience) == "mid level 1 to 3 year"
                ? "Mid- level (1 to 3 years)"
                : experience;
        }

        private static string NormalizeForComparison(string value)
        {
            var normalized = (value ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
            }

            return string.Join(
                ' ',
                builder
                    .ToString()
                    .Normalize(NormalizationForm.FormC)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
