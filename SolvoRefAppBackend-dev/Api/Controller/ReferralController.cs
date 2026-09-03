using Core.Feature.Referrals.GetAllReferralAccount;
using Core.Feature.Referrals.GetAllReferralApplyArea;
using Core.Feature.Referrals.CreateReferral;
using Core.Models.Global;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.Feature.Referrals.GetAllReferralCityByCountry;
using Core.Feature.Referrals.GetAllReferralEnglishLevel;
using Core.Feature.Referrals.GetAllReferralExperience;
using Core.Feature.Referrals.GetAllReferralFound;
using Core.Feature.Referrals.GetAllReferralCountry;
using Core.Feature.Referrals.GetAllReferralCountryPhoneCode;
using Core.Feature.Referrals.GetAllReferrerByUser;
using Core.Feature.Referrals.GetAllReferralStatus;
using Core.Feature.Referrals.GetActiveVacancies;
using Api.Models;
using Core.Contracts.Fabric;
using Core.Contracts.DataSourcing;
using Core.Contracts.Identity;
using Core.Models.Fabric;
using Core.Models.Referrals;
using System.Text;
using System.Security.Cryptography;
using Core.Contracts.Security;
using Core.Contracts.Referrals;
using Core.DBContext;
using Core.Feature.Referrals.Common;
using Core.Feature.Referrals.SyncActiveVacancies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controller
{
    [ApiController]
    [Route("api/referral")]
    public class ReferralController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IFabricService _fabricService;
        private readonly IDataSourcingService _dataSourcingService;
        private readonly IReferralVacancyRepository _vacancyRepository;
        private readonly IReferralRepository _referralRepository;
        private readonly SolvoRefAppContext _context;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly ITurnstileService _turnstileService;

        public ReferralController(
            IMediator mediator,
            IFabricService fabricService,
            IDataSourcingService dataSourcingService,
            IReferralVacancyRepository vacancyRepository,
            IReferralRepository referralRepository,
            SolvoRefAppContext context,
            IUserService userService,
            IConfiguration configuration,
            ITurnstileService turnstileService)
        {
            _mediator = mediator;
            _fabricService = fabricService;
            _dataSourcingService = dataSourcingService;
            _vacancyRepository = vacancyRepository;
            _referralRepository = referralRepository;
            _context = context;
            _userService = userService;
            _configuration = configuration;
            _turnstileService = turnstileService;
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        [EnableRateLimiting("referral-create")]
        [Produces<Response<string>>]
        public async Task<ActionResult<Response<string>>> CreateReferral([FromForm] CreateReferralDto request)
        {
            return await _mediator.Send(new CreateReferralRequest(request));
        }

        [Authorize(Roles = "User")]
        [HttpGet("link")]
        [Produces<Response<ReferralLinkResponseDto>>]
        public async Task<ActionResult<Response<ReferralLinkResponseDto>>> GetOrCreateReferralLink(CancellationToken cancellationToken)
        {
            var userId = _userService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(Response<ReferralLinkResponseDto>.ErrorResponse(
                    ["User session was not found."],
                    System.Net.HttpStatusCode.Unauthorized));
            }

            var canCreateLink = await _context.Users
                .AsNoTracking()
                .AnyAsync(user =>
                    user.Id == userId &&
                    user.Status == "Active" &&
                    user.EmployeeId != null &&
                    user.EmployeeId != string.Empty,
                    cancellationToken);

            if (!canCreateLink)
            {
                return StatusCode(StatusCodes.Status403Forbidden, Response<ReferralLinkResponseDto>.ErrorResponse(
                    ["Your employee ID is missing. Sign out and sign in again, or contact the system administrator."],
                    System.Net.HttpStatusCode.Forbidden));
            }

            var link = await _context.ReferralLinks
                .Where(x => x.ReferrerId == userId && x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (link == null)
            {
                link = new ReferralLink
                {
                    ReferrerId = userId,
                    Token = GenerateReferralToken()
                };
                _context.ReferralLinks.Add(link);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Response<ReferralLinkResponseDto>.SuccessResponse(
                new ReferralLinkResponseDto
                {
                    Token = link.Token,
                    Url = BuildReferralLinkUrl(link.Token)
                },
                System.Net.HttpStatusCode.OK);
        }

        [AllowAnonymous]
        [HttpGet("public/{token}")]
        [Produces<Response<object>>]
        public async Task<ActionResult<Response<object>>> GetPublicReferralLink([FromRoute] string token, CancellationToken cancellationToken)
        {
            var linkResponse = await GetActiveReferralLink(token, cancellationToken);
            if (!linkResponse.Success || linkResponse.Data == null)
            {
                return StatusCode((int)linkResponse.StatusCode, Response<object>.ErrorResponse(
                    linkResponse.Errors ?? ["Referral link is not valid."],
                    linkResponse.StatusCode));
            }

            return Response<object>.SuccessResponse(new
            {
                ReferrerName = linkResponse.Data.Referrer.FullName
            }, System.Net.HttpStatusCode.OK);
        }

        [AllowAnonymous]
        [HttpPost("public/{token}")]
        [EnableRateLimiting("referral-create")]
        [Produces<Response<string>>]
        public async Task<ActionResult<Response<string>>> CreatePublicReferral(
            [FromRoute] string token,
            [FromBody] PublicCreateReferralRequestDto request,
            CancellationToken cancellationToken)
        {
            var linkResponse = await GetActiveReferralLink(token, cancellationToken);
            if (!linkResponse.Success || linkResponse.Data == null)
            {
                return StatusCode((int)linkResponse.StatusCode, Response<string>.ErrorResponse(
                    linkResponse.Errors ?? ["Referral link is not valid."],
                    linkResponse.StatusCode));
            }

            var turnstileResponse = await _turnstileService.ValidateToken(
                request.TurnstileToken,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);
            if (!turnstileResponse.Success)
            {
                return StatusCode((int)turnstileResponse.StatusCode, Response<string>.ErrorResponse(
                    turnstileResponse.Errors ?? ["Captcha validation failed."],
                    turnstileResponse.StatusCode));
            }

            var response = await _mediator.Send(
                new CreateReferralRequest(request.Referral, linkResponse.Data.ReferrerId),
                cancellationToken);

            return StatusCode((int)response.StatusCode, response);
        }

        [AllowAnonymous]
        [HttpPost("public/{token}/validate/referred")]
        [EnableRateLimiting("fabric-validate")]
        [Produces<Response<Core.Feature.Fabric.GetValidateReferred.GetValidateReferredDto>>]
        public async Task<ActionResult<Response<Core.Feature.Fabric.GetValidateReferred.GetValidateReferredDto>>> ValidatePublicReferred(
            [FromRoute] string token,
            [FromBody] ValidateReferredRequestDto request,
            CancellationToken cancellationToken)
        {
            var linkResponse = await GetActiveReferralLink(token, cancellationToken);
            if (!linkResponse.Success || linkResponse.Data == null)
            {
                return StatusCode((int)linkResponse.StatusCode, Response<Core.Feature.Fabric.GetValidateReferred.GetValidateReferredDto>.ErrorResponse(
                    linkResponse.Errors ?? ["Referral link is not valid."],
                    linkResponse.StatusCode));
            }

            return await _mediator.Send(new Core.Feature.Fabric.GetValidateReferred.GetValidateReferredRequest(
                request.Phone,
                request.Email,
                request.ReferralId), cancellationToken);
        }

        [AllowAnonymous]
        [HttpGet("public/account/all")]
        [Produces<Response<List<SelectStructure>>>]
        public async Task<ActionResult<Response<List<SelectStructure>>>> GetAllPublicReferralAccount()
        {
            return await _mediator.Send(new GetAllReferralAccountRequest());
        }

        [AllowAnonymous]
        [HttpGet("public/applyarea/all")]
        [Produces<Response<List<SelectStructure>>>]
        public async Task<ActionResult<Response<List<SelectStructure>>>> GetAllPublicReferralApplyArea()
        {
            return await _mediator.Send(new GetAllReferralApplyAreaRequest());
        }

        [AllowAnonymous]
        [HttpGet("public/country/all")]
        [Produces<Response<List<SelectStructure>>>]
        public async Task<ActionResult<Response<List<SelectStructure>>>> GetAllPublicCountry()
        {
            return await _mediator.Send(new GetAllReferralCountryRequest());
        }

        [AllowAnonymous]
        [HttpGet("public/city/all/{countryid}")]
        [Produces<Response<List<SelectStructure>>>]
        public async Task<ActionResult<Response<List<SelectStructure>>>> GetAllPublicReferralCityByCountry([FromRoute] string countryid)
        {
            return await _mediator.Send(new GetAllReferralCityByCountryRequest(countryid));
        }

        [AllowAnonymous]
        [HttpGet("public/englishlevel/all")]
        [Produces<Response<List<SelectStructure>>>]
        public async Task<ActionResult<Response<List<SelectStructure>>>> GetAllPublicReferralEnglishLevel()
        {
            return await _mediator.Send(new GetAllReferralEnglishLevelRequest());
        }

        [AllowAnonymous]
        [HttpGet("public/experience/all")]
        [Produces<Response<List<SelectStructure>>>]
        public async Task<ActionResult<Response<List<SelectStructure>>>> GetAllPublicReferralExperience()
        {
            return await _mediator.Send(new GetAllReferralExperienceRequest());
        }

        [AllowAnonymous]
        [HttpGet("public/phonecode/all")]
        [Produces<Response<List<SelectStructure>>>]
        public async Task<ActionResult<Response<List<SelectStructure>>>> GetAllPublicCountryCode()
        {
            return await _mediator.Send(new GetAllReferralCountryPhoneCodeRequest());
        }

        [Authorize(Roles = "User")]
        [HttpGet("account/all")]
        [Produces<Response<List<SelectStructure>>>]
        public async Task<ActionResult<Response<List<SelectStructure>>>> GetAllReferralAccount()
        {
            return await _mediator.Send(new GetAllReferralAccountRequest());
        }

        [Authorize(Roles = "User")]
        [HttpGet("applyarea/all")]
        [Produces<Response<List<SelectStructure>>>]
        public async Task<ActionResult<Response<List<SelectStructure>>>> GetAllReferralApplyArea()
        {
            return await _mediator.Send(new GetAllReferralApplyAreaRequest());
        }

        [Authorize(Roles = "User")]
        [HttpGet("country/all")]
        [Produces<Response<List<SelectStructure>>>]
        public async Task<ActionResult<Response<List<SelectStructure>>>> GetAllCountry()
        {
            return await _mediator.Send(new GetAllReferralCountryRequest());
        }

        [Authorize(Roles = "User")]
        [HttpGet("city/all/{countryid}")]
        [Produces<Response<List<SelectStructure>>>]
        public async Task<ActionResult<Response<List<SelectStructure>>>> GetAllReferralCityByCountry([FromRoute] string countryid)
        {
            return await _mediator.Send(new GetAllReferralCityByCountryRequest(countryid));
        }

        [Authorize(Roles = "User")]
        [HttpGet("englishlevel/all")]
        [Produces<Response<List<SelectStructure>>>]
        public async Task<ActionResult<Response<List<SelectStructure>>>> GetAllReferralEnglishLevel()
        {
            return await _mediator.Send(new GetAllReferralEnglishLevelRequest());
        }

        [Authorize(Roles = "User")]
        [HttpGet("experience/all")]
        [Produces<Response<List<SelectStructure>>>]
        public async Task<ActionResult<Response<List<SelectStructure>>>> GetAllReferralExperience()
        {
            return await _mediator.Send(new GetAllReferralExperienceRequest());
        }

        [Authorize(Roles = "User")]
        [HttpGet("found/all")]
        [Produces<Response<List<SelectStructure>>>]
        public async Task<ActionResult<Response<List<SelectStructure>>>> GetAllReferralFound()
        {
            return await _mediator.Send(new GetAllReferralFoundRequest());
        }

        [Authorize(Roles = "User")]
        [HttpGet("phonecode/all")]
        [Produces<Response<List<SelectStructure>>>]
        public async Task<ActionResult<Response<List<SelectStructure>>>> GetAllCountryCode()
        {
            return await _mediator.Send(new GetAllReferralCountryPhoneCodeRequest());
        }

        [Authorize(Roles = "User")]
        [HttpGet("all/user")]
        [Produces<PagedResponse<List<GetAllReferralByUserDto>>>]
        public async Task<ActionResult<PagedResponse<List<GetAllReferralByUserDto>>>> GetAllReferralUser([FromQuery] QueryReferralUser query)
        {
            return await _mediator.Send(new GetAllReferralByUserRequest(query));
        }

        [Authorize(Roles = "User")]
        [HttpGet("all/status/user")]
        [Produces<Response<GetAllReferralStatusDto>>]
        public async Task<ActionResult<Response<GetAllReferralStatusDto>>> GetAllReferralStatusUser([FromQuery] QueryReferralUser query)
        {
            return await _mediator.Send(new GetAllReferralStatusRequest());
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("status-diagnostics")]
        [Produces<Response<object>>]
        public async Task<ActionResult<Response<object>>> GetReferralStatusDiagnostics([FromQuery] string email)
        {
            var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return BadRequest(Response<object>.ErrorResponse(
                    ["A valid email is required."],
                    System.Net.HttpStatusCode.BadRequest));
            }

            var referral = await _referralRepository
                .GetQueryReferral(new QueryReferralBase())
                .Where(x => x != null && x.Email.Trim().ToLower() == normalizedEmail)
                .OrderByDescending(x => x!.CreationDate)
                .FirstOrDefaultAsync();

            if (referral == null)
            {
                return NotFound(Response<object>.ErrorResponse(
                    ["Referral was not found for the provided email."],
                    System.Net.HttpStatusCode.NotFound));
            }

            var expectedSource = ReferralDataSourcingConstants.Source;
            var fabricResponse = await _fabricService.GetReferralStatuses([expectedSource], [referral.Email]);
            var applicantDiagnostics = await _fabricService.GetApplicantStatusDiagnostics(expectedSource, referral.Email);
            var headcountResponse = await _fabricService.GetActiveEmployeesByPersonalId([referral.ReferralID]);
            var headcountDiagnostics = await _fabricService.GetActiveEmployeeDiagnostics(referral.ReferralID);
            var headcountRows = headcountResponse.Data ?? [];
            var hiredRuleMatches = headcountRows
                .Where(x =>
                    x.PersonalId.Equals(referral.ReferralID, StringComparison.OrdinalIgnoreCase) &&
                    x.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                .Select(x => new
                {
                    x.PersonalId,
                    x.Status,
                    x.StartDate,
                    ReferralCreationDate = referral.CreationDate,
                    MatchesCurrentHiredRule = x.StartDate > referral.CreationDate,
                    MatchesCalendarDateHiredRule = x.StartDate.Date >= referral.CreationDate.Date
                })
                .ToList();

            return Response<object>.SuccessResponse(new
            {
                Referral = new
                {
                    referral.Id,
                    ExpectedSource = expectedSource,
                    referral.FirstName,
                    referral.LastName,
                    referral.Email,
                    referral.Status,
                    referral.Account,
                    referral.Area,
                    referral.City,
                    referral.CreationDate,
                    referral.Updatable,
                    IsTransparent = ReferralCompanyResolver.IsTransparentReferral(referral.Account, referral.Area, referral.Country, referral.City)
                },
                FabricQuery = new
                {
                    Sources = new[] { expectedSource },
                    Emails = new[] { referral.Email },
                    fabricResponse.Success,
                    fabricResponse.Errors,
                    RowCount = fabricResponse.Data?.Count ?? 0,
                    Rows = fabricResponse.Data ?? []
                },
                ApplicantDiagnostics = applicantDiagnostics.Data,
                HeadcountQuery = new
                {
                    PersonalIds = new[] { referral.ReferralID },
                    headcountResponse.Success,
                    headcountResponse.Errors,
                    RowCount = headcountRows.Count,
                    Rows = headcountRows,
                    HiredRule = new
                    {
                        RequiredPersonalId = referral.ReferralID,
                        RequiredStatus = "Active",
                        RequiredStartDateRule = "StartDate > Referral.CreationDate",
                        AnyCurrentRuleMatch = hiredRuleMatches.Any(x => x.MatchesCurrentHiredRule),
                        AnyCalendarDateRuleMatch = hiredRuleMatches.Any(x => x.MatchesCalendarDateHiredRule),
                        Matches = hiredRuleMatches
                    }
                },
                HeadcountDiagnostics = headcountDiagnostics.Data
            }, System.Net.HttpStatusCode.OK);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("applicant-diagnostics")]
        [Produces<Response<object>>]
        public async Task<ActionResult<Response<object>>> GetApplicantDiagnostics([FromQuery] string email)
        {
            var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return BadRequest(Response<object>.ErrorResponse(
                    ["A valid email is required."],
                    System.Net.HttpStatusCode.BadRequest));
            }

            var diagnostics = await _fabricService.GetApplicantStatusDiagnostics(
                ReferralDataSourcingConstants.Source,
                normalizedEmail,
                includeSourceSearch: false);
            if (!diagnostics.Success)
            {
                return BadRequest(diagnostics);
            }

            return diagnostics;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("datasourcing-diagnostics")]
        [Produces<Response<object>>]
        public async Task<ActionResult<Response<object>>> GetDataSourcingDiagnostics([FromQuery] string email)
        {
            var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return BadRequest(Response<object>.ErrorResponse(
                    ["A valid email is required."],
                    System.Net.HttpStatusCode.BadRequest));
            }

            var referral = await _referralRepository
                .GetQueryReferral(new QueryReferralBase())
                .Where(x => x != null && x.Email.Trim().ToLower() == normalizedEmail)
                .OrderByDescending(x => x!.CreationDate)
                .FirstOrDefaultAsync();

            var expectedSource = referral == null
                ? null
                : ReferralDataSourcingConstants.Source;
            var diagnostics = await _dataSourcingService.GetLeadDiagnostics(normalizedEmail, expectedSource);
            if (!diagnostics.Success)
            {
                return StatusCode((int)diagnostics.StatusCode, diagnostics);
            }

            return Response<object>.SuccessResponse(new
            {
                LocalReferral = referral == null
                    ? null
                    : new
                    {
                        referral.Id,
                        ExpectedSource = expectedSource,
                        referral.Email,
                        referral.Country,
                        referral.City,
                        referral.Area,
                        referral.Account,
                        referral.CreationDate,
                        Company = ReferralCompanyResolver.ResolveDataSourcingCompany(referral.Account, referral.Area, referral.Country, referral.City),
                        IsTransparent = ReferralCompanyResolver.IsTransparentReferral(referral.Account, referral.Area, referral.Country, referral.City)
                    },
                DataSourcing = diagnostics.Data
            }, System.Net.HttpStatusCode.OK);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("peoplehr-diagnostics")]
        [Produces<Response<object>>]
        public async Task<ActionResult<Response<object>>> GetPeopleHrDiagnostics([FromQuery] string personalId)
        {
            var normalizedPersonalId = (personalId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedPersonalId))
            {
                return BadRequest(Response<object>.ErrorResponse(
                    ["A valid personalId is required."],
                    System.Net.HttpStatusCode.BadRequest));
            }

            var diagnostics = await _fabricService.GetPeopleHrDiagnostics(normalizedPersonalId);
            if (!diagnostics.Success)
            {
                return BadRequest(diagnostics);
            }

            return diagnostics;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("peoplehr-table-diagnostics")]
        [Produces<Response<object>>]
        public async Task<ActionResult<Response<object>>> GetPeopleHrTableDiagnostics(
            [FromQuery] string schema,
            [FromQuery] string table,
            [FromQuery] string? personalId)
        {
            var normalizedSchema = (schema ?? string.Empty).Trim();
            var normalizedTable = (table ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedSchema) || string.IsNullOrWhiteSpace(normalizedTable))
            {
                return BadRequest(Response<object>.ErrorResponse(
                    ["A valid schema and table are required."],
                    System.Net.HttpStatusCode.BadRequest));
            }

            var diagnostics = await _fabricService.GetPeopleHrTableDiagnostics(
                normalizedSchema,
                normalizedTable,
                personalId);
            if (!diagnostics.Success)
            {
                return BadRequest(diagnostics);
            }

            return diagnostics;
        }

        [Authorize (Roles = "User")]
        [HttpGet("vacancies/active")]
        [Produces<Response<List<GetActiveVacanciesDto>>>]
        public async Task<ActionResult<Response<List<GetActiveVacanciesDto>>>> GetActiveVacancies()
        {
            return await _mediator.Send(new GetActiveVacanciesRequest());
        }

        [Authorize(Roles = "User")]
        [HttpGet("vacancies/active/resync")]
        [Produces<Response<string>>]
        public async Task<ActionResult<Response<string>>> ResyncActiveVacancies(CancellationToken cancellationToken)
        {
            var syncResponse = await _mediator.Send(new SyncActiveVacanciesRequest(), cancellationToken);
            if (!syncResponse.Success)
            {
                return BadRequest(Response<string>.ErrorResponse(
                    syncResponse.Errors,
                    syncResponse.StatusCode));
            }

            var savedCount = (await _vacancyRepository.GetAllActive()).Count;
            return Response<string>.SuccessResponse(
                $"Active vacancies resynced successfully. Saved rows: {savedCount}",
                System.Net.HttpStatusCode.OK);
        }

        [Authorize(Roles = "User")]
        [HttpGet("vacancies/active/schema-export")]
        public async Task<IActionResult> ExportActiveVacancySchema()
        {
            var response = await _fabricService.ExportActiveJobPostingSchemaProfileCsv();
            if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
            {
                return BadRequest(response);
            }

            var fileName = $"job_posting-testing-profile-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(response.Data), "text/csv", fileName);
        }

        [Authorize(Roles = "User")]
        [HttpGet("vacancies/active/raw-export")]
        public async Task<IActionResult> ExportActiveVacancyRaw()
        {
            var response = await _fabricService.ExportActiveJobPostingRawCsv();
            if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
            {
                return BadRequest(response);
            }

            var fileName = $"job_posting-testing-raw-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(response.Data), "text/csv", fileName);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("vacancies/active/diagnostics")]
        [Produces<Response<FabricConnectionDiagnostics>>]
        public async Task<ActionResult<Response<FabricConnectionDiagnostics>>> GetActiveVacancyDiagnostics()
        {
            return await _fabricService.GetActiveJobPostingDiagnostics();
        }

        private async Task<Response<ReferralLink>> GetActiveReferralLink(string token, CancellationToken cancellationToken)
        {
            var normalizedToken = token?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedToken) || normalizedToken.Length > 128)
            {
                return Response<ReferralLink>.ErrorResponse(["Referral link is not valid."], System.Net.HttpStatusCode.NotFound);
            }

            var link = await _context.ReferralLinks
                .Include(x => x.Referrer)
                .Where(x => x.Token == normalizedToken && x.IsActive && x.Referrer.Status == "Active")
                .FirstOrDefaultAsync(cancellationToken);

            if (link == null)
            {
                return Response<ReferralLink>.ErrorResponse(["Referral link is not valid."], System.Net.HttpStatusCode.NotFound);
            }

            return Response<ReferralLink>.SuccessResponse(link, System.Net.HttpStatusCode.OK);
        }

        private string BuildReferralLinkUrl(string token)
        {
            var frontendUrl = _configuration["ReferralLinks:FrontendBaseUrl"]
                ?? _configuration["FrontendUrl"]
                ?? _configuration["frontRedirect"]
                ?? "https://solvoreferralapp.solvoglobal.com";

            return $"{frontendUrl.TrimEnd('/')}/refer/{token}";
        }

        private static string GenerateReferralToken()
        {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
