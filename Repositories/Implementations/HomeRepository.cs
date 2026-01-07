using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Data.Helpers;
using HISWEBAPI.Models;
using HISWEBAPI.DTO;
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


        public ServiceResult<IEnumerable<CountryMasterModel>> GetCountryMaster(int? isActive)
        {
            try
            {
                _log.Info($"GetCountryMaster called. IsActive={isActive?.ToString() ?? "All"}");

                // Generate dynamic cache key based on isActive parameter
                string cacheKey = $"_CountryMaster_{(isActive.HasValue ? isActive.Value.ToString() : "All")}";

                // Try to get data from Redis cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<CountryMasterModel> countries;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"CountryMaster data retrieved from cache. Key={cacheKey}");
                    countries = System.Text.Json.JsonSerializer.Deserialize<List<CountryMasterModel>>(cachedData);
                }
                else
                {
                    _log.Info($"CountryMaster cache miss. Fetching data from database. Key={cacheKey}");

                    // Fetch data from database
                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetCountryMaster",
                        CommandType.StoredProcedure,
                        new { IsActive = isActive }
                    );

                    countries = dataTable?.AsEnumerable().Select(row => new CountryMasterModel
                    {
                        CountryId = row.Field<int>("CountryId"),
                        CountryName = row.Field<string>("CountryName") ?? string.Empty,
                        Currency = row.Field<string>("Currency") ?? string.Empty,
                        ConversionFactor = row.Field<decimal?>("ConversionFactor"),
                        IsActive = row.Field<int>("IsActive")
                    }).ToList() ?? new List<CountryMasterModel>();

                    // Store data in Redis cache (permanent until manually cleared)
                    if (countries.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(countries);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"CountryMaster data cached permanently. Key={cacheKey}, Count={countries.Count}");
                    }
                }

                if (!countries.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No countries found for IsActive: {isActive?.ToString() ?? "All"}");
                    return ServiceResult<IEnumerable<CountryMasterModel>>.Failure(
                        alert.Type,
                        "No countries found",
                        404
                    );
                }

                _log.Info($"Retrieved {countries.Count} country/countries from cache");

                return ServiceResult<IEnumerable<CountryMasterModel>>.Success(
                    countries,
                    "Info",
                    $"{countries.Count} country/countries retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<CountryMasterModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<StateMasterModel>> GetStateMaster(int countryId, int? isActive)
        {
            try
            {
                _log.Info($"GetStateMaster called. CountryId={countryId}, IsActive={isActive?.ToString() ?? "All"}");

                // Generate dynamic cache key based on countryId and isActive
                string cacheKey = $"_StateMaster_Country{countryId}_{(isActive.HasValue ? isActive.Value.ToString() : "All")}";

                // Try to get data from Redis cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<StateMasterModel> states;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"StateMaster data retrieved from cache. Key={cacheKey}");
                    states = System.Text.Json.JsonSerializer.Deserialize<List<StateMasterModel>>(cachedData);
                }
                else
                {
                    _log.Info($"StateMaster cache miss. Fetching data from database. Key={cacheKey}");

                    // Fetch data from database
                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetStateMaster",
                        CommandType.StoredProcedure,
                        new { CountryId = countryId, IsActive = isActive }
                    );

                    states = dataTable?.AsEnumerable().Select(row => new StateMasterModel
                    {
                        CountryId = row.Field<int>("CountryId"),
                        StateId = row.Field<int>("StateId"),
                        StateName = row.Field<string>("StateName") ?? string.Empty,
                        IsActive = row.Field<int>("IsActive")
                    }).ToList() ?? new List<StateMasterModel>();

                    // Store data in Redis cache (permanent until manually cleared)
                    if (states.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(states);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"StateMaster data cached permanently. Key={cacheKey}, Count={states.Count}");
                    }
                }

                if (!states.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No states found for CountryId={countryId}, IsActive: {isActive?.ToString() ?? "All"}");
                    return ServiceResult<IEnumerable<StateMasterModel>>.Failure(
                        alert.Type,
                        $"No states found for CountryId: {countryId}",
                        404
                    );
                }

                _log.Info($"Retrieved {states.Count} state(s) from cache");

                return ServiceResult<IEnumerable<StateMasterModel>>.Success(
                    states,
                    "Info",
                    $"{states.Count} state(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<StateMasterModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<DistrictMasterModel>> GetDistrictMaster(int stateId, int? isActive)
        {
            try
            {
                _log.Info($"GetDistrictMaster called. StateId={stateId}, IsActive={isActive?.ToString() ?? "All"}");

                // Generate dynamic cache key based on stateId and isActive
                string cacheKey = $"_DistrictMaster_State{stateId}_{(isActive.HasValue ? isActive.Value.ToString() : "All")}";

                // Try to get data from Redis cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<DistrictMasterModel> districts;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"DistrictMaster data retrieved from cache. Key={cacheKey}");
                    districts = System.Text.Json.JsonSerializer.Deserialize<List<DistrictMasterModel>>(cachedData);
                }
                else
                {
                    _log.Info($"DistrictMaster cache miss. Fetching data from database. Key={cacheKey}");

                    // Fetch data from database
                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetDistrictMaster",
                        CommandType.StoredProcedure,
                        new { StateId = stateId, IsActive = isActive }
                    );

                    districts = dataTable?.AsEnumerable().Select(row => new DistrictMasterModel
                    {
                        CountryId = row.Field<int>("CountryId"),
                        StateId = row.Field<int>("StateId"),
                        DistrictId = row.Field<int>("DistrictId"),
                        DistrictName = row.Field<string>("DistrictName") ?? string.Empty,
                        IsActive = row.Field<int>("IsActive")
                    }).ToList() ?? new List<DistrictMasterModel>();

                    // Store data in Redis cache (permanent until manually cleared)
                    if (districts.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(districts);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"DistrictMaster data cached permanently. Key={cacheKey}, Count={districts.Count}");
                    }
                }

                if (!districts.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No districts found for StateId={stateId}, IsActive: {isActive?.ToString() ?? "All"}");
                    return ServiceResult<IEnumerable<DistrictMasterModel>>.Failure(
                        alert.Type,
                        $"No districts found for StateId: {stateId}",
                        404
                    );
                }

                _log.Info($"Retrieved {districts.Count} district(s) from cache");

                return ServiceResult<IEnumerable<DistrictMasterModel>>.Success(
                    districts,
                    "Info",
                    $"{districts.Count} district(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<DistrictMasterModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<CityMasterModel>> GetCityMaster(int districtId, int? isActive)
        {
            try
            {
                _log.Info($"GetCityMaster called. DistrictId={districtId}, IsActive={isActive?.ToString() ?? "All"}");

                // Generate dynamic cache key based on districtId and isActive
                string cacheKey = $"_CityMaster_District{districtId}_{(isActive.HasValue ? isActive.Value.ToString() : "All")}";

                // Try to get data from Redis cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<CityMasterModel> cities;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"CityMaster data retrieved from cache. Key={cacheKey}");
                    cities = System.Text.Json.JsonSerializer.Deserialize<List<CityMasterModel>>(cachedData);
                }
                else
                {
                    _log.Info($"CityMaster cache miss. Fetching data from database. Key={cacheKey}");

                    // Fetch data from database
                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetCityMaster",
                        CommandType.StoredProcedure,
                        new { DistrictId = districtId, IsActive = isActive }
                    );

                    cities = dataTable?.AsEnumerable().Select(row => new CityMasterModel
                    {
                        CountryId = row.Field<int>("CountryId"),
                        StateId = row.Field<int>("StateId"),
                        DistrictId = row.Field<int>("DistrictId"),
                        CityId = row.Field<int>("CityId"),
                        CityName = row.Field<string>("CityName") ?? string.Empty,
                        Pincode = row.Field<string>("Pincode") ?? string.Empty,
                        IsActive = row.Field<int>("IsActive")
                    }).ToList() ?? new List<CityMasterModel>();

                    // Store data in Redis cache (permanent until manually cleared)
                    if (cities.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(cities);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"CityMaster data cached permanently. Key={cacheKey}, Count={cities.Count}");
                    }
                }

                if (!cities.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No cities found for DistrictId={districtId}, IsActive: {isActive?.ToString() ?? "All"}");
                    return ServiceResult<IEnumerable<CityMasterModel>>.Failure(
                        alert.Type,
                        $"No cities found for DistrictId: {districtId}",
                        404
                    );
                }

                _log.Info($"Retrieved {cities.Count} city/cities from cache");

                return ServiceResult<IEnumerable<CityMasterModel>>.Success(
                    cities,
                    "Info",
                    $"{cities.Count} city/cities retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<CityMasterModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }


        public ServiceResult<IEnumerable<PincodeMasterModel>> GetPincodeMaster(int cityId, int? isActive)
        {
            try
            {
                _log.Info($"GetPincodeMaster called. CityId={cityId}, IsActive={isActive?.ToString() ?? "All"}");

                // Generate dynamic cache key based on cityId and isActive
                string cacheKey = $"_PincodeMaster_City{cityId}_{(isActive.HasValue ? isActive.Value.ToString() : "All")}";

                // Try to get data from Redis cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<PincodeMasterModel> pincodes;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"PincodeMaster data retrieved from cache. Key={cacheKey}");
                    pincodes = System.Text.Json.JsonSerializer.Deserialize<List<PincodeMasterModel>>(cachedData);
                }
                else
                {
                    _log.Info($"PincodeMaster cache miss. Fetching data from database. Key={cacheKey}");

                    // Fetch data from database
                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetPincodeMaster",
                        CommandType.StoredProcedure,
                        new { CityId = cityId, IsActive = isActive }
                    );

                    pincodes = dataTable?.AsEnumerable().Select(row => new PincodeMasterModel
                    {
                        CityId = row.Field<int>("CityId"),
                        PincodeId = row.Field<int>("PincodeId"),
                        Pincode = row.Field<int>("Pincode"),
                        IsActive = row.Field<int>("IsActive")
                    }).ToList() ?? new List<PincodeMasterModel>();

                    // Store data in Redis cache (permanent until manually cleared)
                    if (pincodes.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(pincodes);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"PincodeMaster data cached permanently. Key={cacheKey}, Count={pincodes.Count}");
                    }
                }

                if (!pincodes.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No pincodes found for CityId={cityId}, IsActive: {isActive?.ToString() ?? "All"}");
                    return ServiceResult<IEnumerable<PincodeMasterModel>>.Failure(
                        alert.Type,
                        $"No pincodes found for CityId: {cityId}",
                        404
                    );
                }

                _log.Info($"Retrieved {pincodes.Count} pincode(s) from cache");

                return ServiceResult<IEnumerable<PincodeMasterModel>>.Success(
                    pincodes,
                    "Info",
                    $"{pincodes.Count} pincode(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<PincodeMasterModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<InsuranceCompanyModel>> GetAllInsuranceCompanyList()
        {
            try
            {
                _log.Info("GetAllInsuranceCompanyList called.");

                // Define cache key
                string cacheKey = "_InsuranceCompany_All";

                // Try to get data from Redis cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<InsuranceCompanyModel> insuranceCompanies;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"InsuranceCompany data retrieved from cache. Key={cacheKey}");
                    insuranceCompanies = System.Text.Json.JsonSerializer.Deserialize<List<InsuranceCompanyModel>>(cachedData);
                }
                else
                {
                    _log.Info($"InsuranceCompany cache miss. Fetching data from database. Key={cacheKey}");

                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetInsuranceCompanyMaster",
                        CommandType.StoredProcedure
                    );

                    insuranceCompanies = dataTable?.AsEnumerable().Select(row => new InsuranceCompanyModel
                    {
                        InsuranceCompanyId = row.Field<int>("InsuranceCompanyId"),
                        InsuranceCompanyName = row.Field<string>("InsuranceCompanyName") ?? string.Empty
                    }).ToList() ?? new List<InsuranceCompanyModel>();

                    // Store data in Redis cache (no expiration - cache persists until manually cleared)
                    if (insuranceCompanies.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(insuranceCompanies);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            // No expiration - cache persists until manually cleared
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"All InsuranceCompany data cached permanently. Key={cacheKey}, Count={insuranceCompanies.Count}");
                    }
                }

                if (!insuranceCompanies.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info("No insurance companies found");

                    return ServiceResult<IEnumerable<InsuranceCompanyModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                _log.Info($"Retrieved {insuranceCompanies.Count} insurance companies from cache");

                return ServiceResult<IEnumerable<InsuranceCompanyModel>>.Success(
                    insuranceCompanies,
                    "Info",
                    $"{insuranceCompanies.Count} insurance company(ies) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<InsuranceCompanyModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<CorporateModel>> GetCorporateListByInsuranceCompanyId(int? insuranceCompanyId, int? isActive)
        {
            try
            {
                _log.Info($"GetCorporateListByInsuranceCompanyId called. InsuranceCompanyId={insuranceCompanyId}, IsActive={isActive?.ToString() ?? "All"}");

                // Define cache key - cache ALL corporates together
                string cacheKey = "_Corporate_All";

                // Try to get all corporates from Redis cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<CorporateModel> allCorporates;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"Corporate data retrieved from cache. Key={cacheKey}");
                    allCorporates = System.Text.Json.JsonSerializer.Deserialize<List<CorporateModel>>(cachedData);
                }
                else
                {
                    _log.Info($"Corporate cache miss. Fetching all data from database. Key={cacheKey}");

                    // Fetch ALL corporates from database (no filtering in SP call)
                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetCorporateList",
                        CommandType.StoredProcedure,
                        new
                        {
                           
                        }
                    );

                    allCorporates = dataTable?.AsEnumerable().Select(row => new CorporateModel
                    {
                        CorporateId = row.Field<int>("CorporateId"),
                        CorporateName = row.Field<string>("CorporateName") ?? string.Empty,
                        InsuranceCompanyId = row.Field<int>("InsuranceCompanyId"),
                        IsActive = row.Field<int>("IsActive")
                    }).ToList() ?? new List<CorporateModel>();

                    // Store ALL corporates in cache (no expiration)
                    if (allCorporates.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(allCorporates);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            // No expiration - cache persists until manually cleared
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"All Corporate data cached permanently. Key={cacheKey}, Count={allCorporates.Count}");
                    }
                }

                // Filter in memory based on parameters (always from cache)
                List<CorporateModel> filteredCorporates = allCorporates;

                if (insuranceCompanyId.HasValue)
                {
                    _log.Info($"Filtering cached data by InsuranceCompanyId: {insuranceCompanyId.Value}");
                    filteredCorporates = filteredCorporates.Where(c => c.InsuranceCompanyId == insuranceCompanyId.Value).ToList();
                }

                if (isActive.HasValue)
                {
                    _log.Info($"Filtering cached data by IsActive: {isActive.Value}");
                    filteredCorporates = filteredCorporates.Where(c => c.IsActive == isActive.Value).ToList();
                }

                if (!filteredCorporates.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No corporates found for InsuranceCompanyId={insuranceCompanyId?.ToString() ?? "All"}, IsActive={isActive?.ToString() ?? "All"}");

                    return ServiceResult<IEnumerable<CorporateModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                _log.Info($"Retrieved {filteredCorporates.Count} corporates from cache");

                return ServiceResult<IEnumerable<CorporateModel>>.Success(
                    filteredCorporates,
                    "Info",
                    $"{filteredCorporates.Count} corporate(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<CorporateModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }
    }
}