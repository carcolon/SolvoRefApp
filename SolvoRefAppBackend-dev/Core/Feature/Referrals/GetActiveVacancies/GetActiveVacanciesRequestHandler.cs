using System.Net;
using AutoMapper;
using Core.Contracts.Fabric;
using Core.Models.Global;
using Core.Contracts.Configurations;
using MediatR;
using Core.Contracts.Referrals;
using Core.Feature.Referrals.SyncActiveVacancies;
using Microsoft.Extensions.Logging;

namespace Core.Feature.Referrals.GetActiveVacancies
{
    public class GetActiveVacanciesRequestHandler : IRequestHandler<GetActiveVacanciesRequest, Response<List<GetActiveVacanciesDto>>>
    {
        private readonly IReferralVacancyRepository _repository;
        private readonly IFabricService _fabricService;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;
        private readonly ILogger<GetActiveVacanciesRequestHandler> _logger;

        public GetActiveVacanciesRequestHandler(
            IReferralVacancyRepository repository,
            IFabricService fabricService,
            IMapper mapper,
            IMediator mediator,
            ILogger<GetActiveVacanciesRequestHandler> logger)
        {
            _repository = repository;
            _fabricService = fabricService;
            _mapper = mapper;
            _mediator = mediator;
            _logger = logger;
        }
        
        public async Task<Response<List<GetActiveVacanciesDto>>> Handle(
            GetActiveVacanciesRequest request,
            CancellationToken cancellationToken)
        {
            var syncResponse = await _mediator.Send(new SyncActiveVacanciesRequest(), cancellationToken);
            if (!syncResponse.Success)
            {
                _logger.LogWarning(
                    "Active vacancies request continued after sync failure. Errors: {Errors}",
                    syncResponse.Errors is { Count: > 0 } ? string.Join(" | ", syncResponse.Errors) : "Unknown error");
            }

            var fabricResponse = await _fabricService.GetActiveJobPostings();
            if (fabricResponse.Success && fabricResponse.Data is { Count: > 0 })
            {
                var fabricMapped = fabricResponse.Data
                    .Select(item => new GetActiveVacanciesDto
                    {
                        VacancyId = item.ExternalVacancyId,
                        PositionName = item.PositionName,
                        Country = item.Country
                    })
                    .ToList();

                _logger.LogInformation(
                    "Active vacancies endpoint returned {Count} row(s) directly from Fabric.",
                    fabricMapped.Count);

                return Response<List<GetActiveVacanciesDto>>.SuccessResponse(fabricMapped, HttpStatusCode.OK);
            }

            var data = await _repository.GetAllActive();
            var mapped = _mapper.Map<List<GetActiveVacanciesDto>>(data);

            _logger.LogInformation(
                "Active vacancies endpoint returned {Count} row(s) from the app database fallback.",
                mapped.Count);

            return Response<List<GetActiveVacanciesDto>>.SuccessResponse(mapped, HttpStatusCode.OK);
        }
    }
}
