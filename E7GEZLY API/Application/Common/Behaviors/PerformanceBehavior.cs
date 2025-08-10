using E7GEZLY_API.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace E7GEZLY_API.Application.Common.Behaviors
{
    /// <summary>
    /// Performance behavior for monitoring slow MediatR requests
    /// </summary>
    public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly Stopwatch _timer;
        private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
        private readonly ICurrentUserService _currentUserService;

        public PerformanceBehavior(
            ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
            ICurrentUserService currentUserService)
        {
            _timer = new Stopwatch();
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _timer.Start();

            var response = await next();

            _timer.Stop();

            var elapsedMilliseconds = _timer.ElapsedMilliseconds;

            if (elapsedMilliseconds > 500) // Log if request takes more than 500ms
            {
                var requestName = typeof(TRequest).Name;
                var userId = _currentUserService.UserId;
                var userName = _currentUserService.UserName;

                _logger.LogWarning("E7GEZLY Slow Request: {Name} ({ElapsedMilliseconds} ms) {@UserId} {@UserName} {@Request}",
                    requestName, elapsedMilliseconds, userId, userName, request);
            }

            return response;
        }
    }
}