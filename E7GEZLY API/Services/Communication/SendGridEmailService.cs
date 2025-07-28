// Services/Communication/SendGridEmailService.cs
using SendGrid;
using SendGrid.Helpers.Mail;

namespace E7GEZLY_API.Services.Communication
{
    public class SendGridEmailService : IEmailService
    {
        private readonly ISendGridClient _sendGridClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SendGridEmailService> _logger;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public SendGridEmailService(
            IConfiguration configuration,
            ILogger<SendGridEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var apiKey = _configuration["Email:SendGrid:ApiKey"];
            _sendGridClient = new SendGridClient(apiKey);

            _fromEmail = _configuration["Email:FromEmail"] ?? "hesham.abdalla2002@gmail.com";
            _fromName = _configuration["Email:FromName"] ?? "E7GEZLY";
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string htmlContent, string? plainTextContent = null)
        {
            try
            {
                var from = new EmailAddress(_fromEmail, _fromName);
                var toAddress = new EmailAddress(to);

                _logger.LogInformation($"Attempting to send email from {_fromEmail} to {to}");

                var msg = MailHelper.CreateSingleEmail(
                    from,
                    toAddress,
                    subject,
                    plainTextContent ?? StripHtml(htmlContent),
                    htmlContent
                );

                var response = await _sendGridClient.SendEmailAsync(msg);

                _logger.LogInformation($"SendGrid Response Status: {response.StatusCode}");

                if (response.Body != null)
                {
                    var body = await response.Body.ReadAsStringAsync();
                    _logger.LogWarning($"SendGrid Response Body: {body}");
                }

                if (response.Headers != null)
                {
                    foreach (var header in response.Headers)
                    {
                        _logger.LogDebug($"SendGrid Header: {header.Key} = {string.Join(", ", header.Value)}");
                    }
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    _logger.LogInformation($"Email sent successfully to {to}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Failed to send email to {to}. Status: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {to}");
                return false;
            }
        }

        public async Task<bool> SendVerificationEmailAsync(string to, string userName, string code, string language = "ar")
        {
            var template = GetVerificationEmailTemplate(userName, code, language);
            return await SendEmailAsync(to, template.Subject, template.HtmlContent, template.PlainTextContent);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string to, string userName, string code, string language = "ar")
        {
            var template = GetPasswordResetEmailTemplate(userName, code, language);
            return await SendEmailAsync(to, template.Subject, template.HtmlContent, template.PlainTextContent);
        }

        public async Task<bool> SendWelcomeEmailAsync(string to, string userName, string userType, string language = "ar")
        {
            var template = GetWelcomeEmailTemplate(userName, userType, language);
            return await SendEmailAsync(to, template.Subject, template.HtmlContent, template.PlainTextContent);
        }

        public async Task<bool> SendLoginAlertEmailAsync(string to, string userName, string deviceName, string ipAddress, DateTime loginTime, string language = "ar")
        {
            var template = GetLoginAlertEmailTemplate(userName, deviceName, ipAddress, loginTime, language);
            return await SendEmailAsync(to, template.Subject, template.HtmlContent, template.PlainTextContent);
        }

        #region Email Templates

        private EmailTemplate GetVerificationEmailTemplate(string userName, string code, string language)
        {
            if (language == "ar")
            {
                return new EmailTemplate
                {
                    Subject = "تأكيد بريدك الإلكتروني - E7GEZLY",
                    HtmlContent = $@"
                        <div dir='rtl' style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <div style='background-color: #f8f9fa; padding: 20px; text-align: center;'>
                                <h1 style='color: #333;'>E7GEZLY</h1>
                            </div>
                            <div style='padding: 30px;'>
                                <h2 style='color: #333;'>مرحباً {userName}!</h2>
                                <p style='font-size: 16px; line-height: 1.6;'>
                                    شكراً لتسجيلك في E7GEZLY. لتفعيل حسابك، يرجى استخدام رمز التحقق التالي:
                                </p>
                                <div style='background-color: #f0f0f0; padding: 20px; text-align: center; margin: 20px 0;'>
                                    <h1 style='color: #007bff; letter-spacing: 5px; margin: 0;'>{code}</h1>
                                </div>
                                <p style='font-size: 14px; color: #666;'>
                                    هذا الرمز صالح لمدة 10 دقائق فقط.
                                </p>
                                <p style='font-size: 14px; color: #666; margin-top: 30px;'>
                                    إذا لم تقم بإنشاء حساب على E7GEZLY، يرجى تجاهل هذا البريد.
                                </p>
                            </div>
                            <div style='background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666;'>
                                <p>© 2024 E7GEZLY. جميع الحقوق محفوظة.</p>
                            </div>
                        </div>
                    ",
                    PlainTextContent = $@"
                        مرحباً {userName}!

                        شكراً لتسجيلك في E7GEZLY. لتفعيل حسابك، يرجى استخدام رمز التحقق التالي:

                        {code}

                        هذا الرمز صالح لمدة 10 دقائق فقط.

                        إذا لم تقم بإنشاء حساب على E7GEZLY، يرجى تجاهل هذا البريد.

                        © 2024 E7GEZLY. جميع الحقوق محفوظة.
                    "
                };
            }
            else
            {
                return new EmailTemplate
                {
                    Subject = "Verify Your Email - E7GEZLY",
                    HtmlContent = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <div style='background-color: #f8f9fa; padding: 20px; text-align: center;'>
                                <h1 style='color: #333;'>E7GEZLY</h1>
                            </div>
                            <div style='padding: 30px;'>
                                <h2 style='color: #333;'>Hello {userName}!</h2>
                                <p style='font-size: 16px; line-height: 1.6;'>
                                    Thank you for registering with E7GEZLY. To activate your account, please use the following verification code:
                                </p>
                                <div style='background-color: #f0f0f0; padding: 20px; text-align: center; margin: 20px 0;'>
                                    <h1 style='color: #007bff; letter-spacing: 5px; margin: 0;'>{code}</h1>
                                </div>
                                <p style='font-size: 14px; color: #666;'>
                                    This code is valid for 10 minutes only.
                                </p>
                                <p style='font-size: 14px; color: #666; margin-top: 30px;'>
                                    If you didn't create an account on E7GEZLY, please ignore this email.
                                </p>
                            </div>
                            <div style='background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666;'>
                                <p>© 2024 E7GEZLY. All rights reserved.</p>
                            </div>
                        </div>
                    ",
                    PlainTextContent = $@"
                        Hello {userName}!

                        Thank you for registering with E7GEZLY. To activate your account, please use the following verification code:

                        {code}

                        This code is valid for 10 minutes only.

                        If you didn't create an account on E7GEZLY, please ignore this email.

                        © 2024 E7GEZLY. All rights reserved.
                    "
                };
            }
        }

        private EmailTemplate GetPasswordResetEmailTemplate(string userName, string code, string language)
        {
            if (language == "ar")
            {
                return new EmailTemplate
                {
                    Subject = "إعادة تعيين كلمة المرور - E7GEZLY",
                    HtmlContent = $@"
                        <div dir='rtl' style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <div style='background-color: #f8f9fa; padding: 20px; text-align: center;'>
                                <h1 style='color: #333;'>E7GEZLY</h1>
                            </div>
                            <div style='padding: 30px;'>
                                <h2 style='color: #333;'>إعادة تعيين كلمة المرور</h2>
                                <p style='font-size: 16px; line-height: 1.6;'>
                                    مرحباً {userName}، لقد طلبت إعادة تعيين كلمة المرور لحسابك.
                                </p>
                                <p style='font-size: 16px; line-height: 1.6;'>
                                    استخدم الرمز التالي لإعادة تعيين كلمة المرور:
                                </p>
                                <div style='background-color: #f0f0f0; padding: 20px; text-align: center; margin: 20px 0;'>
                                    <h1 style='color: #dc3545; letter-spacing: 5px; margin: 0;'>{code}</h1>
                                </div>
                                <p style='font-size: 14px; color: #666;'>
                                    هذا الرمز صالح لمدة 15 دقيقة فقط.
                                </p>
                                <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; margin-top: 20px;'>
                                    <p style='font-size: 14px; color: #856404; margin: 0;'>
                                        <strong>تنبيه:</strong> إذا لم تطلب إعادة تعيين كلمة المرور، يرجى تجاهل هذا البريد وتأمين حسابك.
                                    </p>
                                </div>
                            </div>
                            <div style='background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666;'>
                                <p>© 2024 E7GEZLY. جميع الحقوق محفوظة.</p>
                            </div>
                        </div>
                    ",
                    PlainTextContent = $@"
                        إعادة تعيين كلمة المرور

                        مرحباً {userName}، لقد طلبت إعادة تعيين كلمة المرور لحسابك.

                        استخدم الرمز التالي لإعادة تعيين كلمة المرور:

                        {code}

                        هذا الرمز صالح لمدة 15 دقيقة فقط.

                        تنبيه: إذا لم تطلب إعادة تعيين كلمة المرور، يرجى تجاهل هذا البريد وتأمين حسابك.

                        © 2024 E7GEZLY. جميع الحقوق محفوظة.
                    "
                };
            }
            else
            {
                // English version
                return new EmailTemplate
                {
                    Subject = "Password Reset - E7GEZLY",
                    HtmlContent = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <!-- Similar English template -->
                        </div>
                    ",
                    PlainTextContent = $"Password reset code for {userName}: {code}"
                };
            }
        }

        private EmailTemplate GetWelcomeEmailTemplate(string userName, string userType, string language)
        {
            // Welcome email template
            return new EmailTemplate
            {
                Subject = language == "ar" ? "مرحباً بك في E7GEZLY!" : "Welcome to E7GEZLY!",
                HtmlContent = "...",
                PlainTextContent = "..."
            };
        }

        private EmailTemplate GetLoginAlertEmailTemplate(string userName, string deviceName, string ipAddress, DateTime loginTime, string language)
        {
            // Security alert template
            return new EmailTemplate
            {
                Subject = language == "ar" ? "تنبيه أمني - تسجيل دخول جديد" : "Security Alert - New Login",
                HtmlContent = "...",
                PlainTextContent = "..."
            };
        }

        private string StripHtml(string html)
        {
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        }

        #endregion

        private class EmailTemplate
        {
            public string Subject { get; set; } = string.Empty;
            public string HtmlContent { get; set; } = string.Empty;
            public string PlainTextContent { get; set; } = string.Empty;
        }
    }
}