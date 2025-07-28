// Services/Communication/MockSmsService.cs
namespace E7GEZLY_API.Services.Communication
{
    public class MockSmsService : ISmsService
    {
        private readonly ILogger<MockSmsService> _logger;

        public MockSmsService(ILogger<MockSmsService> logger)
        {
            _logger = logger;
        }

        public Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            _logger.LogInformation($"[MOCK SMS] To: {phoneNumber}, Message: {message}");
            return Task.FromResult(true);
        }

        public Task<bool> SendVerificationCodeAsync(string phoneNumber, string code)
        {
            _logger.LogInformation($"[MOCK SMS - VERIFICATION] To: {phoneNumber}, Code: {code}");
            return Task.FromResult(true);
        }

        public Task<bool> SendPasswordResetCodeAsync(string phoneNumber, string code)
        {
            _logger.LogInformation($"[MOCK SMS - PASSWORD RESET] To: {phoneNumber}, Code: {code}");
            return Task.FromResult(true);
        }
    }
}