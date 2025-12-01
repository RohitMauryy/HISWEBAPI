using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Data.Helpers;
using HISWEBAPI.Models;
using HISWEBAPI.Services;
using Microsoft.Extensions.Logging;
using HISWEBAPI.Exceptions;
using System.Reflection;
using log4net;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace HISWEBAPI.Repositories.Implementations
{
    public class HomeRepository : IHomeRepository
    {
        private readonly ICustomSqlHelper _sqlHelper;
        private readonly IResponseMessageService _messageService;
        private readonly IDistributedCache _distributedCache;
        private readonly IConfiguration _configuration;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public HomeRepository(
            ICustomSqlHelper sqlHelper,
            IResponseMessageService messageService,
            IDistributedCache distributedCache,
            IConfiguration configuration)
        {
            _sqlHelper = sqlHelper;
            _messageService = messageService;
            _distributedCache = distributedCache;
            _configuration = configuration;
        }

        public ServiceResult<string> ClearAllCache()
        {
            try
            {
                _log.Info("ClearAllCache called - Attempting to clear all Redis cache");

                // Get Redis connection string from configuration
                var redisConnection = _configuration.GetValue<string>("Redis:Configuration") ?? "localhost:6379";

                using (var redis = ConnectionMultiplexer.Connect(redisConnection))
                {
                    var server = redis.GetServer(redis.GetEndPoints().First());
                    var db = redis.GetDatabase();

                    // Get all keys from Redis
                    var keys = server.Keys(pattern: "*").ToList();

                    if (!keys.Any())
                    {
                        _log.Info("No cache keys found in Redis");
                        var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                        return ServiceResult<string>.Success(
                            "No cache entries found to clear",
                            alert.Type,
                            "No cache entries found",
                            200
                        );
                    }

                    int clearedCount = 0;
                    foreach (var key in keys)
                    {
                        try
                        {
                            db.KeyDelete(key);
                            _log.Info($"Cleared cache key: {key}");
                            clearedCount++;
                        }
                        catch (Exception ex)
                        {
                            _log.Warn($"Failed to clear cache key '{key}': {ex.Message}");
                        }
                    }

                    _log.Info($"Total {clearedCount} cache entries cleared out of {keys.Count}");

                    var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                    return ServiceResult<string>.Success(
                        $"{clearedCount} cache entries cleared successfully",
                        alert1.Type,
                        $"Successfully cleared {clearedCount} cache entries from Redis",
                        200
                    );
                }
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(
                    alert.Type,
                    $"Failed to clear cache: {ex.Message}",
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<BranchModel>> GetActiveBranchList()
        {
            try
            {
                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetActiveBranchList",
                    CommandType.StoredProcedure
                );

                var branches = dataTable?.AsEnumerable().Select(row => new BranchModel
                {
                    branchId = row.Field<int>("BranchId"),
                    branchName = row.Field<string>("BranchName")
                }).ToList() ?? new List<BranchModel>();

                if (!branches.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info("No active branches found in database");

                    return ServiceResult<IEnumerable<BranchModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                 _log.Info($"Retrieved {branches.Count} active branches");

                return ServiceResult<IEnumerable<BranchModel>>.Success(
                    branches,
                    "Info",
                    $"{branches.Count} branch(es) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<BranchModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<PickListModel>> GetPickListMaster(string fieldName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fieldName))
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                     _log.Warn("GetPickListMaster called with empty fieldName");

                    return ServiceResult<IEnumerable<PickListModel>>.Failure(
                        alert.Type,
                        "Field name is required",
                        400
                    );
                }

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetPickListMaster",
                    CommandType.StoredProcedure,
                    new { fieldName = fieldName }
                );

                var pickList = dataTable?.AsEnumerable().Select(row => new PickListModel
                {
                    id = row.Field<int>("Id"),
                    fieldName = row.Field<string>("FieldName"),
                    value = row.Field<string>("Value"),
                    key = row.Field<string>("Key")
                }).ToList() ?? new List<PickListModel>();

                if (!pickList.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                     _log.Info($"No picklist items found for field: {fieldName}");

                    return ServiceResult<IEnumerable<PickListModel>>.Failure(
                        alert.Type,
                        $"No data found for field: {fieldName}",
                        404
                    );
                }

                 _log.Info($"Retrieved {pickList.Count} picklist items for field: {fieldName}");

                return ServiceResult<IEnumerable<PickListModel>>.Success(
                    pickList,
                    "Info",
                    $"{pickList.Count} item(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<PickListModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<AllGlobalValues> GetAllGlobalValues()
        {
            try
            {
               
                var alert = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                _log.Info("GetAllGlobalValues method called successfully");

                // Return empty model - controller will populate with actual values
                return ServiceResult<AllGlobalValues>.Success(
                    new AllGlobalValues(),
                    alert.Type,
                    "Global values retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<AllGlobalValues>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }


    }
}