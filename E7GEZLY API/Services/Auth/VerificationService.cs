// Services/Auth/VerificationService.cs
using System.Security.Cryptography;
using E7GEZLY_API.Services.Communication;

namespace E7GEZLY_API.Services.Auth
{
    public class VerificationService : IVerificationService
    {
        private readonly ILogger<VerificationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;

        public VerificationService(
            ILogger<VerificationService> logger,
            IConfiguration configuration,
            IEmailService emailService,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _configuration = configuration;
            _emailService = emailService;
            _environment = environment;
        }

        public Task<(bool Success, string Code)> GenerateVerificationCodeAsync()
        {
            // Generate a 6-digit verification code
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var code = (BitConverter.ToUInt32(bytes, 0) % 900000 + 100000).ToString();

            return Task.FromResult((true, code));
        }

        public async Task<bool> SendPhoneVerificationAsync(string phoneNumber, string code)
        {
            try
            {
                // TODO: Integrate with SMS service provider
                // For now, in development we log, in production we'd use real SMS service

                if (_environment.IsDevelopment())
                {
                    _logger.LogInformation($"[DEV MODE] Phone verification code for +2{phoneNumber}: {code}");
                    await Task.CompletedTask;
                    return true;
                }

                // Production SMS implementation would go here
                // Example with Twilio:
                // var message = await MessageResource.CreateAsync(
                //     body: $"رمز التحقق الخاص بك في E7GEZLY هو: {code}",
                //     from: new PhoneNumber(_configuration["Sms:Twilio:PhoneNumber"]),
                //     to: new PhoneNumber($"+2{phoneNumber}")
                // );

                _logger.LogWarning($"SMS service not configured. Would send code {code} to +2{phoneNumber}");
                return false; // Return false in production if SMS not configured
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending SMS verification to {phoneNumber}");
                return false;
            }
        }

        public async Task<bool> SendEmailVerificationAsync(string email, string code)
        {
            return await SendEmailVerificationAsync(email, null, code);
        }

        public async Task<bool> SendEmailVerificationAsync(string email, string? userName, string code)
        {
            try
            {
                // Use the email service to send verification
                var language = _configuration["Email:DefaultLanguage"] ?? "ar";
                var displayName = userName ?? email.Split('@')[0]; // Use email prefix if no name

                var sent = await _emailService.SendVerificationEmailAsync(email, displayName, code, language);

                if (sent)
                {
                    _logger.LogInformation($"Email verification sent successfully to {email}");
                }
                else
                {
                    _logger.LogWarning($"Failed to send email verification to {email}");
                }

                return sent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email verification to {email}");
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string userName, string code)
        {
            try
            {
                var language = _configuration["Email:DefaultLanguage"] ?? "ar";
                var sent = await _emailService.SendPasswordResetEmailAsync(email, userName, code, language);

                if (sent)
                {
                    _logger.LogInformation($"Password reset email sent successfully to {email}");
                }
                else
                {
                    _logger.LogWarning($"Failed to send password reset email to {email}");
                }

                return sent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending password reset email to {email}");
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string email, string userName, string userType)
        {
            try
            {
                var language = _configuration["Email:DefaultLanguage"] ?? "ar";
                var sent = await _emailService.SendWelcomeEmailAsync(email, userName, userType, language);

                if (sent)
                {
                    _logger.LogInformation($"Welcome email sent successfully to {email}");
                }

                return sent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending welcome email to {email}");
                return false;
            }
        }

        public Task<bool> ValidateVerificationCodeAsync(string providedCode, string? storedCode, DateTime? expiry)
        {
            if (string.IsNullOrEmpty(storedCode) || expiry == null)
                return Task.FromResult(false);

            if (DateTime.UtcNow > expiry)
                return Task.FromResult(false);

            return Task.FromResult(providedCode == storedCode);
        }
    }
}