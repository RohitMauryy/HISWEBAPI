using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using HISWEBAPI.Data.Helpers;
using HISWEBAPI.Models;
using HISWEBAPI.DTO;
using Microsoft.Extensions.Caching.Distributed;
using HISWEBAPI.Exceptions;
using System.Reflection;

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
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return ("Error", "Something went wrong while retrieving response message.");
            }
        }


        public string CreateUpdateResponseMessage(ResponseMessageRequest request, AllGlobalValues globalValues)
        {
            try
            {
                var result = _sqlHelper.DML("IU_ResponseMessageMaster", CommandType.StoredProcedure, new
                {
                    @id = request.Id,
                    @typeId = request.TypeId,
                    @type = request.Type,
                    @alertCode = request.AlertCode,
                    @message = request.Message,
                    @isActive = request.IsActive,
                    @userId = globalValues.userId,
                    @ipAddress = globalValues.ipAddress
                },
                new
                {
                    result = 0
                });

                if (result < 0)
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { result = false, messageType = "Warn", message = "RECORD_ALREADY_EXISTS" });

                if (request.Id == 0)
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { result = true, messageType = "Info", message = "DATA_SAVED_SUCCESSFULLY" });
                else
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { result = true, messageType = "Info", message ="DATA_UPDATED_SUCCESSFULLY" });
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { result = false, messageType = "Error", message = "SERVER_ERROR_FOUND" });
            }
        }
    }
}
