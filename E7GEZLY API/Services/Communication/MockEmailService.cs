// Services/Communication/MockEmailService.cs
namespace E7GEZLY_API.Services.Communication
{
    public class MockEmailService : IEmailService
    {
        private readonly ILogger<MockEmailService> _logger;

        public MockEmailService(ILogger<MockEmailService> logger)
        {
            _logger = logger;
        }

        public Task<bool> SendEmailAsync(string to, string subject, string htmlContent, string? plainTextContent = null)
        {
            _logger.LogInformation($"[MOCK EMAIL] To: {to}");
            _logger.LogInformation($"[MOCK EMAIL] Subject: {subject}");
            _logger.LogInformation($"[MOCK EMAIL] Content: {plainTextContent ?? "HTML content"}");
            return Task.FromResult(true);
        }

        public Task<bool> SendVerificationEmailAsync(string to, string userName, string code, string language = "ar")
        {
            _logger.LogInformation($"[MOCK EMAIL - VERIFICATION] To: {to}, User: {userName}, Code: {code}, Language: {language}");
            return Task.FromResult(true);
        }

        public Task<bool> SendPasswordResetEmailAsync(string to, string userName, string code, string language = "ar")
        {
            _logger.LogInformation($"[MOCK EMAIL - PASSWORD RESET] To: {to}, User: {userName}, Code: {code}, Language: {language}");
            return Task.FromResult(true);
        }

        public Task<bool> SendWelcomeEmailAsync(string to, string userName, string userType, string language = "ar")
        {
            _logger.LogInformation($"[MOCK EMAIL - WELCOME] To: {to}, User: {userName}, Type: {userType}, Language: {language}");
            return Task.FromResult(true);
        }

        public Task<bool> SendLoginAlertEmailAsync(string to, string userName, string deviceName, string ipAddress, DateTime loginTime, string language = "ar")
        {
            _logger.LogInformation($"[MOCK EMAIL - LOGIN ALERT] To: {to}, User: {userName}, Device: {deviceName}, IP: {ipAddress}, Time: {loginTime}, Language: {language}");
            return Task.FromResult(true);
        }
    }
}