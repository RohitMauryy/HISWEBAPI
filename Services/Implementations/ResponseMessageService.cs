using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using HISWEBAPI.Data.Helpers;
using HISWEBAPI.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace HISWEBAPI.Services
{
    public class ResponseMessageService : IResponseMessageService
    {
        private readonly ICustomSqlHelper _sqlHelper;
        private readonly IDistributedCache _distributedCache;


        public ResponseMessageService(ICustomSqlHelper sqlHelper, IDistributedCache distributedCache)
        {
            _sqlHelper = sqlHelper;
            _distributedCache = distributedCache;
        }

        public (string Type, string Message) GetMessageAndTypeByAlertCode(string alertCode)
        {
            try
            {
                if (string.IsNullOrEmpty(alertCode))
                    return ("Error", "Alert code is required.");

                // Try to read from cache
                var cachedData = _distributedCache.GetString("_GetAllResponseMessagesCache");
                List<ResponseMessage> messages;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    // Deserialize from JSON
                    messages = JsonSerializer.Deserialize<List<ResponseMessage>>(cachedData);
                }
                else
                {
                    // Cache miss → fetch from database
                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetAllResponseMessages",  // This SP should return all messages
                        CommandType.StoredProcedure
                        
                    );

                    messages = new List<ResponseMessage>();

                    if (dataTable != null && dataTable.Rows.Count > 0)
                    {
                        foreach (DataRow row in dataTable.Rows)
                        {
                            messages.Add(new ResponseMessage
                            {
                                AlertCode = Convert.ToString(row["AlertCode"]),
                                Type = Convert.ToString(row["Type"]),
                                Message = Convert.ToString(row["Message"])
                            });
                        }

                        // Store all data into Redis cache (JSON serialized)
                        var serialized = JsonSerializer.Serialize(messages);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6) // cache for 6 hours
                        };

                        _distributedCache.SetString("_GetAllResponseMessagesCache", serialized, cacheOptions);
                    }
                }

                // Filter message by AlertCode from cache
                var messageObj = messages?
                    .FirstOrDefault(m => m.AlertCode.Equals(alertCode, StringComparison.OrdinalIgnoreCase));

                if (messageObj != null)
                    return (messageObj.Type ?? "Info", messageObj.Message ?? "Message not found.");

                return ("Error", "Response message not found.");
            }
            catch (Exception)
            {
                return ("Error", "Something went wrong while retrieving response message.");
            }
        }

        // Optional: to manually clear cache (for admin or background job)
        public void ClearResponseMessageCache()
        {
            _distributedCache.Remove("_GetAllResponseMessagesCache");
        }

       
    }
}
