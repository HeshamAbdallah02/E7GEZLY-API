using E7GEZLY_API.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace E7GEZLY_API.Application.Common.Behaviors
{
    /// <summary>
    /// Logging behavior for MediatR requests
    /// </summary>
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        private readonly ICurrentUserService _currentUserService;

        public LoggingBehavior(
            ILogger<LoggingBehavior<TRequest, TResponse>> logger,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var userId = _currentUserService.UserId;
            var userName = _currentUserService.UserName;

            _logger.LogInformation("E7GEZLY Request: {Name} {@UserId} {@UserName} {@Request}",
                requestName, userId, userName, request);

            try
            {
                var response = await next();

                _logger.LogInformation("E7GEZLY Request Completed: {Name} {@UserId} {@UserName}",
                    requestName, userId, userName);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E7GEZLY Request Failed: {Name} {@UserId} {@UserName} {@Request}",
                    requestName, userId, userName, request);
                throw;
            }
        }
    }
}