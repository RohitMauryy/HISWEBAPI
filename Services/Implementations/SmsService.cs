using System.Net.Http;
using System.Threading.Tasks;
using HISWEBAPI.Services.Interfaces;
using HISWEBAPI.Models;
using HISWEBAPI.Data.Helpers;
using log4net;
using System.Reflection;
using System.Data;

namespace HISWEBAPI.Services.Implementations
{
    public class SmsService : ISmsService
    {
        private readonly ICustomSqlHelper _sqlHelper;
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // Global variables to store SMS configuration
        private static SMSAPIConfiguration _smsConfig;
        private static DateTime _lastConfigLoadTime = DateTime.MinValue;
        private static readonly TimeSpan ConfigCacheExpiry = TimeSpan.FromMinutes(10);
        private static readonly object _configLock = new object();

        public SmsService(ICustomSqlHelper sqlHelper, IHttpClientFactory httpClientFactory)
        {
            _sqlHelper = sqlHelper;
            _httpClientFactory = httpClientFactory;
            LoadSmsConfiguration();
        }

        #region Configuration Management

        /// <summary>
        /// Load SMS configuration from database
        /// </summary>
        private void LoadSmsConfiguration()
        {
            try
            {
                // Check if configuration needs to be reloaded (cache expiry)
                if (_smsConfig != null && (DateTime.Now - _lastConfigLoadTime) < ConfigCacheExpiry)
                {
                    _log.Info("Using cached SMS configuration");
                    return;
                }

                lock (_configLock)
                {
                    // Double-check after acquiring lock
                    if (_smsConfig != null && (DateTime.Now - _lastConfigLoadTime) < ConfigCacheExpiry)
                    {
                        return;
                    }

                    DataTable dt = _sqlHelper.GetDataTable(
                        "sp_GetSMSAPIConfiguration",
                        CommandType.StoredProcedure
                    );

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];
                        _smsConfig = new SMSAPIConfiguration
                        {
                            Id = Convert.ToInt32(row["Id"]),
                            BaseUrl = Convert.ToString(row["BaseUrl"]),
                            ApiKey = Convert.ToString(row["ApiKey"]),
                            SenderId = Convert.ToString(row["SenderId"]),
                            NumberPlaceholder = Convert.ToString(row["NumberPlaceholder"]),
                            MessagePlaceholder = Convert.ToString(row["MessagePlaceholder"]),
                            Format = Convert.ToString(row["Format"]),
                            Timeout = Convert.ToInt32(row["Timeout"]),
                            IsActive = Convert.ToBoolean(row["IsActive"]),
                            CreatedDate = Convert.ToDateTime(row["CreatedDate"]),
                            ModifiedDate = row["ModifiedDate"] != DBNull.Value
                                ? Convert.ToDateTime(row["ModifiedDate"])
                                : (DateTime?)null
                        };

                        _lastConfigLoadTime = DateTime.Now;
                        _log.Info($"SMS configuration loaded successfully from database. Provider: {_smsConfig.SenderId}");
                    }
                    else
                    {
                        _log.Error("No active SMS API configuration found in database");
                        throw new InvalidOperationException("SMS API configuration not found");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error loading SMS configuration: {ex.Message}", ex);
                throw;
            }
        }

       

        #endregion

        #region Template Management

        /// <summary>
        /// Get SMS template by TemplateId
        /// </summary>
        public SMSTemplateMaster GetSMSTemplateById(int templateId)
        {
            try
            {
                DataTable dt = _sqlHelper.GetDataTable(
                    "sp_GetSMSTemplateById",
                    CommandType.StoredProcedure,
                    new { TemplateId = templateId }
                );

                if (dt != null && dt.Rows.Count > 0)
                {
                    return MapDataRowToTemplate(dt.Rows[0]);
                }

                _log.Warn($"SMS template not found for TemplateId: {templateId}");
                return null;
            }
            catch (Exception ex)
            {
                _log.Error($"Error retrieving SMS template by Id {templateId}: {ex.Message}", ex);
                return null;
            }
        }

        private SMSTemplateMaster MapDataRowToTemplate(DataRow row)
        {
            return new SMSTemplateMaster
            {
                TemplateId = Convert.ToInt32(row["TemplateId"]),
                BranchId = Convert.ToInt32(row["BranchId"]),
                Type = Convert.ToString(row["Type"]),
                SMSTemplate = Convert.ToString(row["SMSTemplate"]),
                IsActive = Convert.ToBoolean(row["IsActive"]),
                CreatedBy = Convert.ToString(row["CreatedBy"]),
                CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                TypeId = row["TypeId"] != DBNull.Value ? Convert.ToInt32(row["TypeId"]) : (int?)null,
                Whatsapp = Convert.ToBoolean(row["Whatsapp"]),
                SMS = Convert.ToBoolean(row["SMS"]),
                Email = Convert.ToBoolean(row["Email"]),
                ForSMSDetails = Convert.ToString(row["ForSMSDetails"])
            };
        }


        #endregion

        #region SMS Sending Methods

        /// <summary>
        /// Send OTP SMS (backward compatibility)
        /// </summary>
        public bool SendOtp(string contactNumber, string otp)
        {
            try
            {
                var templateId = 3043;  //for OTP Template
                var template = GetSMSTemplateById(templateId);

                if (template == null)
                {
                    _log.Error($"SMS template not found for TemplateId: {templateId}");
                    return false;
                }

                // Format message with replacements
                var replacements = new Dictionary<string, string>
                  {
                         { "{OTP}", otp }
                };
                string message = template.FormatMessage(replacements);

                _log.Info($"Sending SMS using Template ID: {templateId}, Type: {template.Type}");


                return SendSms(contactNumber, message);
            }
            catch (Exception ex)
            {
                _log.Error($"Error sending OTP to {contactNumber}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Send SMS using custom message
        /// </summary>
        public bool SendSms(string contactNumber, string message)
        {
            try
            {
                // Ensure configuration is loaded
                LoadSmsConfiguration();

                if (_smsConfig == null)
                {
                    _log.Error("SMS configuration is not available");
                    return false;
                }

                // Validate inputs
                if (string.IsNullOrWhiteSpace(contactNumber) || string.IsNullOrWhiteSpace(message))
                {
                    _log.Error("Contact number and message are required");
                    return false;
                }

                // Build SMS API URL
                string smsApiUrl = _smsConfig.BuildSmsUrl(contactNumber, message);

                _log.Info($"Sending SMS to: {contactNumber}");

                // Send SMS using HttpClient
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromMilliseconds(_smsConfig.Timeout);

                var response = httpClient.GetAsync(smsApiUrl).Result;

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    _log.Info($"SMS sent successfully to {contactNumber}. Response: {responseContent}");
                    return true;
                }
                else
                {
                    _log.Error($"Failed to send SMS to {contactNumber}. Status Code: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error sending SMS to {contactNumber}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
       
        #endregion
    }
}