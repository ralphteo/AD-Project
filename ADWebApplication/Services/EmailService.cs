using System.Net;
using System.Net.Mail;

namespace ADWebApplication.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _cfg;

        public EmailService(IConfiguration cfg)
        {
            _cfg = cfg;
        }

        public async Task SendOtpEmail(string toEmail, string otp)
        {
            string smtpServer = _cfg["EmailSettings:SmtpServer"] ?? "";
            string senderEmail = _cfg["EmailSettings:SenderEmail"] ?? "";
            string appPassword = _cfg["EmailSettings:AppPassword"] ?? "";
            string portText = _cfg["EmailSettings:Port"] ?? "587";

            if (string.IsNullOrWhiteSpace(smtpServer) ||
                string.IsNullOrWhiteSpace(senderEmail) ||
                string.IsNullOrWhiteSpace(appPassword))
            {
                throw new InvalidOperationException("EmailSettings missing in appsettings.json.");
            }

            int port;
            if (!int.TryParse(portText, out port)) port = 587;

            using var client = new SmtpClient(smtpServer)
            {
                Port = port,
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(senderEmail, appPassword)
            };

            using var msg = new MailMessage
            {
                From = new MailAddress(senderEmail, "E-Waste System"),
                Subject = "Login OTP",
                Body = $"Your OTP is {otp}. It is valid for 1 minutes.",
                IsBodyHtml = false
            };

            msg.To.Add(toEmail);

            await client.SendMailAsync(msg);
        }
    }
}