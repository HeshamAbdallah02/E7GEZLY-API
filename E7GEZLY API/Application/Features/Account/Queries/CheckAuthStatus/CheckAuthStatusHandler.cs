using E7GEZLY_API.Application.Common.Models;
using MediatR;

namespace E7GEZLY_API.Application.Features.Account.Queries.CheckAuthStatus
{
    /// <summary>
    /// Handler for CheckAuthStatusQuery - simple authentication check
    /// </summary>
    public class CheckAuthStatusHandler : IRequestHandler<CheckAuthStatusQuery, ApplicationResult<object>>
    {
        private readonly ILogger<CheckAuthStatusHandler> _logger;

        public CheckAuthStatusHandler(ILogger<CheckAuthStatusHandler> logger)
        {
            _logger = logger;
        }

        public async Task<ApplicationResult<object>> Handle(CheckAuthStatusQuery request, CancellationToken cancellationToken)
        {
            await Task.CompletedTask; // Async compliance

            if (string.IsNullOrEmpty(request.UserId))
            {
                return ApplicationResult<object>.Failure("Not authenticated");
            }

            var response = new { authenticated = true, userId = request.UserId };
            return ApplicationResult<object>.Success(response);
        }
    }
}