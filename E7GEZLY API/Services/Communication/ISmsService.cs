// Services/Communication/ISmsService.cs
namespace E7GEZLY_API.Services.Communication
{
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
        Task<bool> SendVerificationCodeAsync(string phoneNumber, string code);
        Task<bool> SendPasswordResetCodeAsync(string phoneNumber, string code);
    }
}