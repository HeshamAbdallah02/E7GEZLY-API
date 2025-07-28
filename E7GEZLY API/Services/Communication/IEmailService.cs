// Services/Communication/IEmailService.cs
namespace E7GEZLY_API.Services.Communication
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string htmlContent, string? plainTextContent = null);
        Task<bool> SendVerificationEmailAsync(string to, string userName, string code, string language = "ar");
        Task<bool> SendPasswordResetEmailAsync(string to, string userName, string code, string language = "ar");
        Task<bool> SendWelcomeEmailAsync(string to, string userName, string userType, string language = "ar");
        Task<bool> SendLoginAlertEmailAsync(string to, string userName, string deviceName, string ipAddress, DateTime loginTime, string language = "ar");
    }
}