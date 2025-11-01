using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using HISWEBAPI.Services.Interfaces;
using HISWEBAPI.Models;
using HISWEBAPI.Data.Helpers;
using log4net;
using System.Reflection;
using System.Data;
using System.Security.Cryptography.X509Certificates;

namespace HISWEBAPI.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly ICustomSqlHelper _sqlHelper;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // Global variables to store mail configuration
        private static MailServerConfiguration _mailConfig;
        private static DateTime _lastConfigLoadTime = DateTime.MinValue;
        private static readonly TimeSpan ConfigCacheExpiry = TimeSpan.FromMinutes(10);
        private static readonly object _configLock = new object();

        public EmailService(ICustomSqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
            LoadMailConfiguration();
        }

        /// <summary>
        /// Load mail configuration from database
        /// </summary>
        private void LoadMailConfiguration()
        {
            try
            {
                // Check if configuration needs to be reloaded (cache expiry)
                if (_mailConfig != null && (DateTime.Now - _lastConfigLoadTime) < ConfigCacheExpiry)
                {
                    _log.Info("Using cached mail configuration");
                    return;
                }

                lock (_configLock)
                {
                    // Double-check after acquiring lock
                    if (_mailConfig != null && (DateTime.Now - _lastConfigLoadTime) < ConfigCacheExpiry)
                    {
                        return;
                    }

                    DataTable dt = _sqlHelper.GetDataTable(
                        "sp_GetMailServerConfiguration",
                        CommandType.StoredProcedure
                    );

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];
                        _mailConfig = new MailServerConfiguration
                        {
                            Id = Convert.ToInt32(row["Id"]),
                            Host = Convert.ToString(row["Host"]),
                            Port = Convert.ToInt32(row["Port"]),
                            UserName = Convert.ToString(row["UserName"]),
                            Password = Convert.ToString(row["Password"]),
                            EnableSSL = Convert.ToBoolean(row["EnableSSL"]),
                            FromEmail = Convert.ToString(row["FromEmail"]),
                            FromName = Convert.ToString(row["FromName"]),
                            IsBodyHtml = Convert.ToBoolean(row["IsBodyHtml"]),
                            Timeout = Convert.ToInt32(row["Timeout"]),
                            IsActive = Convert.ToBoolean(row["IsActive"]),
                            CreatedDate = Convert.ToDateTime(row["CreatedDate"]),
                            ModifiedDate = row["ModifiedDate"] != DBNull.Value
                                ? Convert.ToDateTime(row["ModifiedDate"])
                                : (DateTime?)null
                        };

                        _lastConfigLoadTime = DateTime.Now;
                        _log.Info($"Mail configuration loaded successfully from database. Host: {_mailConfig.Host}");
                    }
                    else
                    {
                        _log.Error("No active mail server configuration found in database");
                        throw new InvalidOperationException("Mail server configuration not found");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error loading mail configuration: {ex.Message}", ex);
                throw;
            }
        }


        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            _log.Info($"Attempting to send email to: {toEmail}");

            try
            {
                // Ensure configuration is loaded
                LoadMailConfiguration();

                if (_mailConfig == null)
                {
                    _log.Error("Mail configuration is not available");
                    return false;
                }

                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress(
                    _mailConfig.FromName,
                    _mailConfig.FromEmail
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
                    // Add secure certificate validation callback
                    smtpClient.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        // If no errors, certificate is valid
                        if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                        {
                            _log.Info("SSL certificate validation passed without errors");
                            return true;
                        }

                        // Check chain status for specific errors
                        if (chain != null && chain.ChainStatus != null)
                        {
                            foreach (var status in chain.ChainStatus)
                            {
                                // Only allow revocation-related errors to be bypassed
                                if (status.Status != X509ChainStatusFlags.RevocationStatusUnknown &&
                                    status.Status != X509ChainStatusFlags.OfflineRevocation &&
                                    status.Status != X509ChainStatusFlags.NoError)
                                {
                                    _log.Error($"Certificate validation failed: {status.Status} - {status.StatusInformation}");
                                    return false;
                                }
                            }
                        }

                        // Log warning but accept certificate with revocation check bypass
                        _log.Warn($"Certificate accepted with revocation check bypass. SSL Policy Errors: {sslPolicyErrors}");
                        return true;
                    };

                    smtpClient.Timeout = _mailConfig.Timeout;

                    // Determine secure socket options
                    SecureSocketOptions secureOptions = _mailConfig.EnableSSL
                        ? SecureSocketOptions.StartTls
                        : SecureSocketOptions.None;

                    await smtpClient.ConnectAsync(
                        _mailConfig.Host,
                        _mailConfig.Port,
                        secureOptions
                    );

                    await smtpClient.AuthenticateAsync(
                        _mailConfig.UserName,
                        _mailConfig.Password
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

        public async Task<bool> SendOtpEmail(string toEmail, string otp, string purpose = "Email Verification")
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
        ul {{ padding-left: 20px; }}
        li {{ margin: 8px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'> 
            <h1>GWS HIS</h1>
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