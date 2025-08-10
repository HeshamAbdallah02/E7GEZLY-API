using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace E7GEZLY_API.Application.Features.Testing.Queries.GetAnonymousTest
{
    /// <summary>
    /// Handler for anonymous test query
    /// </summary>
    public class GetAnonymousTestHandler : IRequestHandler<GetAnonymousTestQuery, ApplicationResult<AnonymousTestResponse>>
    {
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<GetAnonymousTestHandler> _logger;

        public GetAnonymousTestHandler(
            IDateTimeService dateTimeService,
            ILogger<GetAnonymousTestHandler> logger)
        {
            _dateTimeService = dateTimeService;
            _logger = logger;
        }

        public async Task<ApplicationResult<AnonymousTestResponse>> Handle(
            GetAnonymousTestQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing anonymous test request");

            await Task.Delay(1, cancellationToken); // Simulate some work

            var response = new AnonymousTestResponse(
                "This endpoint is public",
                _dateTimeService.UtcNow
            );

            _logger.LogInformation("Anonymous test completed successfully");

            return ApplicationResult<AnonymousTestResponse>.Success(response);
        }
    }
}