using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using HISWEBAPI.Services.Interfaces;
using log4net;
using System.Reflection;

namespace HISWEBAPI.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            _log.Info($"Attempting to send email to: {toEmail}");

            try
            {
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress(
                    _configuration["SMTP_EMAIL:FROM_NAME"],
                    _configuration["SMTP_EMAIL:FROM"]
                ));
                emailMessage.To.Add(MailboxAddress.Parse(toEmail));
                emailMessage.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };
                emailMessage.Body = bodyBuilder.ToMessageBody();

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Timeout = 30000;

                    await smtpClient.ConnectAsync(
                        _configuration["SMTP_EMAIL:HOST"],
                        int.Parse(_configuration["SMTP_EMAIL:PORT"]),
                        SecureSocketOptions.StartTls
                    );

                    await smtpClient.AuthenticateAsync(
                        _configuration["SMTP_EMAIL:USER_NAME"],
                        _configuration["SMTP_EMAIL:PASSWORD"]
                    );

                    await smtpClient.SendAsync(emailMessage);
                    await smtpClient.DisconnectAsync(true);

                    _log.Info($"Email sent successfully to: {toEmail}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to send email to {toEmail}: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> SendOtpEmail(string toEmail, string otp, string purpose = "Password Reset")
        {
            var subject = $"Your OTP for {purpose}";
            var body = GetOTPEmailTemplate(otp, purpose);
            return await SendEmailAsync(toEmail, subject, body);
        }

        private string GetOTPEmailTemplate(string otp, string purpose)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f4f4f4; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ background-color: #ffffff; padding: 40px 30px; border-radius: 0 0 10px 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .otp-box {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 25px; text-align: center; font-size: 25px; font-weight: bold; letter-spacing: 8px; margin: 20px 0; border-radius: 10px; box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4); }}
        .info-box {{ background-color: #f8f9fa; border-left: 4px solid #667eea; padding: 15px; margin: 20px 0; border-radius: 5px; }}
        .warning-box {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 5px; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #777; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        ul {{ padding-left: 20px; }}
        li {{ margin: 8px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 GWS HIS</h1>
            <p style='margin: 10px 0 0 0; font-size: 16px;'>Security Verification</p>
        </div>
        <div class='content'>
            <h2 style='color: #667eea; margin-top: 0;'>OTP Verification Required</h2>
            <p style='font-size: 16px;'>Hello,</p>
            <p style='font-size: 16px;'>You have requested an OTP for <strong>{purpose}</strong>.</p>
            
            <div class='info-box'>
                <p style='margin: 0; font-size: 15px;'><strong>📱 Your One-Time Password (OTP) is:</strong></p>
            </div>
            
            <div class='otp-box'>{otp}</div>
            
            <div class='info-box'>
                <p style='margin: 5px 0; font-size: 12px;'><strong>⏰ Valid for:</strong> 5 minutes</p>
                <p style='margin: 5px 0; font-size: 12px;'><strong>🎯 Purpose:</strong> {purpose}</p>
            </div>

            <div class='warning-box'>
                <p style='margin: 0; font-size: 14px; color: #856404;'><strong>⚠️ Security Notice:</strong></p>
                <ul style='margin: 10px 0; font-size: 14px; color: #856404;'>
                    <li>Never share this OTP with anyone</li>
                    <li>GWS HIS staff will never ask for your OTP</li>
                    <li>This OTP expires in 5 minutes</li>
                </ul>
            </div>

            <p style='font-size: 14px; color: #666;'>If you did not request this OTP, please ignore this email or contact our support team immediately.</p>
        </div>
        <div class='footer'>
            <p style='margin: 5px 0;'>© 2025 <strong>GRAVITY WEB TECHNOLOGIES</strong>. All rights reserved.</p>
            <p style='margin: 5px 0;'>This is an automated email. Please do not reply.</p>
            <p style='margin: 5px 0; font-size: 11px;'>GWS HIS - Healthcare Information System</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}