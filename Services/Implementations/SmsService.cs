using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using log4net;
using System.Reflection;
using HISWEBAPI.Services.Interfaces;


namespace HISWEBAPI.Services.Implementations
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SmsService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public bool SendOtp(string contactNumber, string otp)
        {
            try
            {
                // Get SMS API URL from configuration
                string smsApiUrl = _configuration["SMSSetting:value"];

                if (string.IsNullOrEmpty(smsApiUrl))
                {
                    _log.Error("SMS API URL not configured in appsettings.json");
                    return false;
                }

                // Create OTP message
                // Dont Modify this OTP Template message Format
                   string message = $"Your OTP for password reset is: {otp}. Valid for 5 minutes. Do not share with anyone Regards GRAVTT WEB TECHNOLOGIES";

                // Replace placeholders in API URL
                smsApiUrl = smsApiUrl.Replace("##U+0026##", "&");
                smsApiUrl = smsApiUrl.Replace("##ContactNo##", contactNumber);
                smsApiUrl = smsApiUrl.Replace("##Message##", System.Web.HttpUtility.UrlEncode(message));

                _log.Info($"Sending OTP SMS to: {contactNumber}");

                // Send SMS using HttpClient
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var response = httpClient.GetAsync(smsApiUrl).Result;

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    _log.Info($"SMS sent successfully to {contactNumber}. Response: {responseContent}");
                    return true;
                }
                else
                {
                    _log.Error($"Failed to send SMS. Status Code: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error sending SMS: {ex.Message}", ex);
                return false;
            }
        }
    }
}