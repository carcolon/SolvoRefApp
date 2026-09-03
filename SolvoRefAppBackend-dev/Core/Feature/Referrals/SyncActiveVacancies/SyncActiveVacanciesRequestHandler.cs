using System.Net;
using Core.Contracts.Fabric;
using Core.Contracts.Referrals;
using Core.Models.Global;
using Core.Models.Referrals;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Feature.Referrals.SyncActiveVacancies
{
    public class SyncActiveVacanciesRequestHandler : IRequestHandler<SyncActiveVacanciesRequest, Response<Unit>>
    {
        private readonly IFabricService _fabricService;
        private readonly IReferralVacancyRepository _vacancyRepository;
        private readonly ILogger<SyncActiveVacanciesRequestHandler> _logger;

        public SyncActiveVacanciesRequestHandler(
            IFabricService fabricService,
            IReferralVacancyRepository vacancyRepository,
            ILogger<SyncActiveVacanciesRequestHandler> logger)
        {
            _fabricService = fabricService;
            _vacancyRepository = vacancyRepository;
            _logger = logger;
        }

        public async Task<Response<Unit>> Handle(SyncActiveVacanciesRequest request, CancellationToken cancellationToken)
        {
            var fabricResponse = await _fabricService.GetActiveJobPostings();
            if (!fabricResponse.Success || fabricResponse.Data is null)
            {
                var errors = fabricResponse.Errors is { Count: > 0 }
                    ? fabricResponse.Errors
                    : ["An unexpected error occurred while syncing vacancies from Fabric."];

                _logger.LogError(
                    "Fabric vacancy sync failed. Errors: {Errors}",
                    string.Join(" | ", errors));

                return Response<Unit>.ErrorResponse(
                    errors,
                    fabricResponse.StatusCode == default ? HttpStatusCode.BadRequest : fabricResponse.StatusCode);
            }

            var fabricVacancies = fabricResponse.Data;

            static bool IsPlaceholder(string? value)
            {
                var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
                return string.IsNullOrWhiteSpace(normalized)
                    || System.Text.RegularExpressions.Regex.IsMatch(normalized, "^x+$")
                    || normalized == "n/a"
                    || normalized == "na";
            }

            var discardedCount = fabricVacancies.Count(item =>
                IsPlaceholder(item.ExternalVacancyId) ||
                IsPlaceholder(item.PositionName) ||
                IsPlaceholder(item.Country));

            var normalizedVacancies = fabricVacancies
                .Where(item =>
                    !IsPlaceholder(item.ExternalVacancyId) &&
                    !IsPlaceholder(item.PositionName) &&
                    !IsPlaceholder(item.Country))
                .Select(item => new ReferralVacancy
                {
                    ExternalVacancyId = item.ExternalVacancyId.Trim(),
                    PositionName = item.PositionName.Trim(),
                    Country = item.Country?.Trim() ?? string.Empty,
                    Active = true
                })
                .OrderBy(item => item.PositionName)
                .ToList();

            _logger.LogInformation(
                "Fabric vacancy sync normalized {NormalizedCount} row(s) from {RawCount} raw row(s).",
                normalizedVacancies.Count,
                fabricVacancies.Count);

            _logger.LogInformation(
                "Fabric vacancy sync discarded {DiscardedCount} placeholder row(s).",
                discardedCount);

            _logger.LogInformation(
                "Fabric vacancy sync sample rows: {Sample}",
                string.Join(" || ", normalizedVacancies
                    .Take(5)
                    .Select(item => $"code={item.ExternalVacancyId}, title={item.PositionName}, country={item.Country}")));

            await _vacancyRepository.ReplaceAll(normalizedVacancies, cancellationToken);

            _logger.LogInformation(
                "Fabric vacancy sync saved {SavedCount} active vacancy row(s) to the app database.",
                normalizedVacancies.Count);

            return Response<Unit>.SuccessResponse(Unit.Value, HttpStatusCode.OK);
        }
    }
}
