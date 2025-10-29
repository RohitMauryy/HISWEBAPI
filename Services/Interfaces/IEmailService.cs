namespace HISWEBAPI.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);
        Task<bool> SendOtpEmail(string toEmail, string otp, string purpose = "Password Reset");
    }
}