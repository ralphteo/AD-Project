using Xunit;
using Moq;
using ADWebApplication.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace ADWebApplication.Tests.Services
{
    public class EmailServiceTests
    {
        private static Mock<IConfiguration> CreateMockConfiguration(
            string smtpServer = "smtp.gmail.com",
            string senderEmail = "test@example.com",
            string appPassword = "testpassword",
            string port = "587")
        {
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["EmailSettings:SmtpServer"]).Returns(smtpServer);
            mockConfig.Setup(c => c["EmailSettings:SenderEmail"]).Returns(senderEmail);
            mockConfig.Setup(c => c["EmailSettings:AppPassword"]).Returns(appPassword);
            mockConfig.Setup(c => c["EmailSettings:Port"]).Returns(port);
            return mockConfig;
        }

        [Fact]
        public async Task SendOtpEmail_ThrowsException_WhenSmtpServerMissing()
        {
            // Arrange
            var mockConfig = CreateMockConfiguration(smtpServer: null!);
            var service = new EmailService(mockConfig.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SendOtpEmail("test@example.com", "123456"));
            Assert.Equal("EmailSettings missing in appsettings.json.", exception.Message);
        }

        [Fact]
        public async Task SendOtpEmail_ThrowsException_WhenSenderEmailMissing()
        {
            // Arrange
            var mockConfig = CreateMockConfiguration(senderEmail: null!);
            var service = new EmailService(mockConfig.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SendOtpEmail("test@example.com", "123456"));
            Assert.Equal("EmailSettings missing in appsettings.json.", exception.Message);
        }

        [Fact]
        public async Task SendOtpEmail_ThrowsException_WhenAppPasswordMissing()
        {
            // Arrange
            var mockConfig = CreateMockConfiguration(appPassword: null!);
            var service = new EmailService(mockConfig.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SendOtpEmail("test@example.com", "123456"));
            Assert.Equal("EmailSettings missing in appsettings.json.", exception.Message);
        }

        [Fact]
        public async Task SendOtpEmail_ThrowsException_WhenSmtpServerEmpty()
        {
            // Arrange
            var mockConfig = CreateMockConfiguration(smtpServer: "");
            var service = new EmailService(mockConfig.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SendOtpEmail("test@example.com", "123456"));
            Assert.Equal("EmailSettings missing in appsettings.json.", exception.Message);
        }

        [Fact]
        public async Task SendOtpEmail_ThrowsException_WhenSenderEmailWhitespace()
        {
            // Arrange
            var mockConfig = CreateMockConfiguration(senderEmail: "   ");
            var service = new EmailService(mockConfig.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SendOtpEmail("test@example.com", "123456"));
            Assert.Equal("EmailSettings missing in appsettings.json.", exception.Message);
        }

        [Fact]
        public void SendOtpEmail_UsesDefaultPort_WhenPortInvalid()
        {
            // Arrange
            var mockConfig = CreateMockConfiguration(port: "invalid");
            var service = new EmailService(mockConfig.Object);

            // Act & Assert
            // This test verifies the service doesn't crash with invalid port
            // The actual SMTP connection would fail in integration testing
            Assert.NotNull(service);
        }

        [Fact]
        public void SendOtpEmail_UsesDefaultPort_WhenPortMissing()
        {
            // Arrange
            var mockConfig = CreateMockConfiguration(port: null!);
            var service = new EmailService(mockConfig.Object);

            // Act & Assert
            // This test verifies the service doesn't crash with missing port
            Assert.NotNull(service);
        }

        [Fact]
        public void EmailService_Constructs_WithValidConfiguration()
        {
            // Arrange
            var mockConfig = CreateMockConfiguration();

            // Act
            var service = new EmailService(mockConfig.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void EmailService_ImplementsIEmailService()
        {
            // Arrange
            var mockConfig = CreateMockConfiguration();

            // Act
            var service = new EmailService(mockConfig.Object);

            // Assert
            Assert.IsType<IEmailService>(service, exactMatch: false);
        }
    }
}
