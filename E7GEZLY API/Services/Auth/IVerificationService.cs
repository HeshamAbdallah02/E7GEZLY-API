// Services/Auth/IVerificationService.cs
namespace E7GEZLY_API.Services.Auth
{
    public interface IVerificationService
    {
        Task<(bool Success, string Code)> GenerateVerificationCodeAsync();
        Task<bool> SendPhoneVerificationAsync(string phoneNumber, string code);
        Task<bool> SendEmailVerificationAsync(string email, string code);
        Task<bool> SendEmailVerificationAsync(string email, string? userName, string code);
        Task<bool> SendPasswordResetEmailAsync(string email, string userName, string code);
        Task<bool> SendWelcomeEmailAsync(string email, string userName, string userType);
        Task<bool> ValidateVerificationCodeAsync(string providedCode, string? storedCode, DateTime? expiry);
    }
}