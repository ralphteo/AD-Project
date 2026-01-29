namespace ADWebApplication.Services
{
    public interface IEmailService
    {
        Task SendOtpEmail(string toEmail, string otp);
    }
}