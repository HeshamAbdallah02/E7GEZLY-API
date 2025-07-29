using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Moq;
using E7GEZLY_API.Services.Auth;
using E7GEZLY_API.Services.Communication;
using E7GEZLY_API.Tests.Categories;
using System;
using System.Threading.Tasks;

namespace E7GEZLY_API.Tests.Unit.Services
{
    [TestClass]
    [TestCategory(TestCategories.Unit)]
    [TestCategory(TestCategories.Authentication)]
    public class VerificationServiceTests
    {
        private VerificationService? _verificationService;
        private Mock<ILogger<VerificationService>>? _mockLogger;
        private Mock<IConfiguration>? _mockConfiguration;
        private Mock<IEmailService>? _mockEmailService;
        private Mock<IWebHostEnvironment>? _mockWebHostEnvironment;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<VerificationService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockEmailService = new Mock<IEmailService>();
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();

            // Setup environment as Development - mock the property, not the extension method
            _mockWebHostEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);

            // Setup email service to return success by default
            _mockEmailService.Setup(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Setup configuration for default language
            _mockConfiguration.Setup(x => x["Email:DefaultLanguage"]).Returns("ar");

            _verificationService = new VerificationService(
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockEmailService.Object,
                _mockWebHostEnvironment.Object);
        }

        [TestMethod]
        public async Task GenerateVerificationCodeAsync_ReturnsSuccessAndSixDigitCode()
        {
            // Act
            var result = await _verificationService!.GenerateVerificationCodeAsync();

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Code);
            Assert.AreEqual(6, result.Code.Length);
            Assert.IsTrue(int.TryParse(result.Code, out int code));
            Assert.IsTrue(code >= 100000 && code <= 999999);
        }

        [TestMethod]
        public async Task GenerateVerificationCodeAsync_GeneratesUniqueCodesOnMultipleCalls()
        {
            // Arrange
            var codes = new HashSet<string>();

            // Act
            for (int i = 0; i < 100; i++)
            {
                var result = await _verificationService!.GenerateVerificationCodeAsync();
                codes.Add(result.Code);
            }

            // Assert
            // We expect at least 90% unique codes out of 100 generations
            Assert.IsTrue(codes.Count > 90, $"Expected more than 90 unique codes, but got {codes.Count}");
        }

        [TestMethod]
        public async Task SendPhoneVerificationAsync_LogsCorrectly_ReturnsTrue()
        {
            // Arrange
            var phoneNumber = "1234567890";
            var code = "123456";

            // Act
            var result = await _verificationService!.SendPhoneVerificationAsync(phoneNumber, code);

            // Assert
            Assert.IsTrue(result);
            _mockLogger!.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"[DEV MODE] Phone verification code for +2{phoneNumber}: {code}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task SendEmailVerificationAsync_LogsCorrectly_ReturnsTrue()
        {
            // Arrange
            var email = "test@example.com";
            var code = "123456";

            // Act
            var result = await _verificationService!.SendEmailVerificationAsync(email, code);

            // Assert
            Assert.IsTrue(result);
            _mockLogger!.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Email verification sent successfully to {email}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ValidateVerificationCodeAsync_ValidCode_ReturnsTrue()
        {
            // Arrange
            var providedCode = "123456";
            var storedCode = "123456";
            var expiry = DateTime.UtcNow.AddMinutes(5);

            // Act
            var result = await _verificationService!.ValidateVerificationCodeAsync(providedCode, storedCode, expiry);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ValidateVerificationCodeAsync_InvalidCode_ReturnsFalse()
        {
            // Arrange
            var providedCode = "123456";
            var storedCode = "654321";
            var expiry = DateTime.UtcNow.AddMinutes(5);

            // Act
            var result = await _verificationService!.ValidateVerificationCodeAsync(providedCode, storedCode, expiry);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ValidateVerificationCodeAsync_ExpiredCode_ReturnsFalse()
        {
            // Arrange
            var providedCode = "123456";
            var storedCode = "123456";
            var expiry = DateTime.UtcNow.AddMinutes(-5); // Expired

            // Act
            var result = await _verificationService!.ValidateVerificationCodeAsync(providedCode, storedCode, expiry);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ValidateVerificationCodeAsync_NullStoredCode_ReturnsFalse()
        {
            // Arrange
            var providedCode = "123456";
            string? storedCode = null;
            var expiry = DateTime.UtcNow.AddMinutes(5);

            // Act
            var result = await _verificationService!.ValidateVerificationCodeAsync(providedCode, storedCode, expiry);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ValidateVerificationCodeAsync_NullExpiry_ReturnsFalse()
        {
            // Arrange
            var providedCode = "123456";
            var storedCode = "123456";
            DateTime? expiry = null;

            // Act
            var result = await _verificationService!.ValidateVerificationCodeAsync(providedCode, storedCode, expiry);

            // Assert
            Assert.IsFalse(result);
        }
    }
}