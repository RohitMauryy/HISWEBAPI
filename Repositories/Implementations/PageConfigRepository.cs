using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using log4net;
using Microsoft.Extensions.Caching.Distributed;
using HISWEBAPI.Data.Helpers;
using HISWEBAPI.DTO;
using HISWEBAPI.Exceptions;
using HISWEBAPI.Models;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Services;

namespace HISWEBAPI.Repositories.Implementations
{
    public class PageConfigRepository : IPageConfigRepository
    {
        private readonly ICustomSqlHelper _sqlHelper;
        private readonly IResponseMessageService _messageService;
        private readonly IDistributedCache _distributedCache;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string CACHE_KEY_ALL = "_PageConfig_All";

        public PageConfigRepository(
            ICustomSqlHelper sqlHelper,
            IResponseMessageService messageService,
            IDistributedCache distributedCache)
        {
            _sqlHelper = sqlHelper;
            _messageService = messageService;
            _distributedCache = distributedCache;
        }

        public ServiceResult<int> CreateUpdatePageConfig(PageConfigRequest request, AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"CreateUpdatePageConfig called. ConfigKey={request.ConfigKey}, Id={request.Id}");

                // Validate JSON format
                if (!IsValidJson(request.ConfigJson))
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_JSON_FORMAT");
                    _log.Warn($"Invalid JSON format for ConfigKey: {request.ConfigKey}");
                    return ServiceResult<int>.Failure(
                        alert.Type,
                        alert.Message,
                        400
                    );
                }

                var dataTable = _sqlHelper.GetDataTable(
                    "IU_PageConfigMaster",
                    CommandType.StoredProcedure,
                    new
                    {
                        Id = request.Id,
                        ConfigKey = request.ConfigKey,
                        ConfigJson = request.ConfigJson,
                        IsActive = request.IsActive,
                        UserId = globalValues.userId,
                        IpAddress = globalValues.ipAddress
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                    _log.Error("No result returned from stored procedure");
                    return ServiceResult<int>.Failure(
                        alert.Type,
                        alert.Message,
                        500
                    );
                }

                int result = Convert.ToInt32(dataTable.Rows[0]["Result"]);

                if (result == -1)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("CONFIG_KEY_EXISTS");
                    _log.Warn($"Duplicate ConfigKey: {request.ConfigKey}");
                    return ServiceResult<int>.Failure(
                        alert.Type,
                        $"Configuration key '{request.ConfigKey}' already exists",
                        409
                    );
                }

                if (result == -2)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Warn($"Record not found for Id: {request.Id}");
                    return ServiceResult<int>.Failure(
                        alert.Type,
                        "Configuration record not found",
                        404
                    );
                }

                if (result > 0)
                {
                    // Clear cache after successful operation (no expiration, so cache persists until manually cleared)
                    _distributedCache.Remove(CACHE_KEY_ALL);
                    _log.Info($"Cleared cache for key: {CACHE_KEY_ALL}");

                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.Id == 0 ? "DATA_SAVED_SUCCESSFULLY" : "DATA_UPDATED_SUCCESSFULLY"
                    );

                    _log.Info($"PageConfig {(request.Id == 0 ? "created" : "updated")} successfully. Result={result}. Cache cleared.");

                    return ServiceResult<int>.Success(
                        result,
                        alert.Type,
                        alert.Message,
                        request.Id == 0 ? 201 : 200
                    );
                }

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_FAILED");
                _log.Error($"Operation failed with result: {result}");
                return ServiceResult<int>.Failure(
                    alert1.Type,
                    alert1.Message,
                    500
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<int>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

   
        public ServiceResult<IEnumerable<PageConfigResponse>> GetPageConfig(string configKey = null)
        {
            try
            {
                _log.Info($"GetPageConfig called. ConfigKey={configKey ?? "All"}");

                // Always use the same cache key for all configurations
                string cacheKey = CACHE_KEY_ALL;

                // Try to get all configurations from cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<PageConfigResponse> allConfigurations;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"PageConfig data retrieved from cache. Key={cacheKey}");
                    allConfigurations = JsonSerializer.Deserialize<List<PageConfigResponse>>(cachedData);
                }
                else
                {
                    _log.Info($"PageConfig cache miss. Fetching all data from database. Key={cacheKey}");

                    // Fetch ALL configurations from database (pass NULL to SP)
                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetPageConfigMaster",
                        CommandType.StoredProcedure,
                        new { ConfigKey = (string)null }
                    );

                    allConfigurations = dataTable?.AsEnumerable().Select(row => new PageConfigResponse
                    {
                        Id = row.Field<int>("Id"),
                        ConfigKey = row.Field<string>("ConfigKey"),
                        ConfigJson = row.Field<string>("ConfigJson"),
                        IsActive = row.Field<bool>("IsActive"),
                        CreatedBy = row.Field<int?>("CreatedBy"),
                        CreatedOn = row.Field<DateTime>("CreatedOn"),
                        LastModifiedBy = row.Field<int?>("LastModifiedBy"),
                        LastModifiedOn = row.Field<DateTime?>("LastModifiedOn"),
                        IpAddress = row.Field<string>("IpAddress")
                    }).ToList() ?? new List<PageConfigResponse>();

                    // Store ALL configurations in cache (no expiration)
                    if (allConfigurations.Any())
                    {
                        var serialized = JsonSerializer.Serialize(allConfigurations);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            // No expiration - cache persists until manually cleared
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"All PageConfig data cached permanently. Key={cacheKey}, Count={allConfigurations.Count}");
                    }
                }

                // Filter in memory based on ConfigKey parameter
                List<PageConfigResponse> filteredConfigurations;
                if (!string.IsNullOrWhiteSpace(configKey))
                {
                    _log.Info($"Filtering cached data by ConfigKey: {configKey}");
                    filteredConfigurations = allConfigurations
                        .Where(c => c.ConfigKey.Equals(configKey, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else
                {
                    _log.Info("Returning all cached configurations");
                    filteredConfigurations = allConfigurations;
                }

                if (!filteredConfigurations.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No PageConfig found for ConfigKey: {configKey ?? "All"}");
                    return ServiceResult<IEnumerable<PageConfigResponse>>.Failure(
                        alert.Type,
                        string.IsNullOrWhiteSpace(configKey)
                            ? "No page configurations found"
                            : $"Configuration not found for key: {configKey}",
                        404
                    );
                }

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                _log.Info($"Retrieved {filteredConfigurations.Count} page configuration(s) from cache");

                return ServiceResult<IEnumerable<PageConfigResponse>>.Success(
                    filteredConfigurations,
                    alert1.Type,
                    $"{filteredConfigurations.Count} configuration(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<PageConfigResponse>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        #region Private Helper Methods

        private bool IsValidJson(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return false;

            try
            {
                JsonDocument.Parse(jsonString);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

       

        #endregion
    }
}