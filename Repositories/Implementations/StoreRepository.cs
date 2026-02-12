using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using HISWEBAPI.Data.Helpers;
using HISWEBAPI.DTO;
using HISWEBAPI.Exceptions;
using HISWEBAPI.Models;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Services;
using log4net;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Distributed;

namespace HISWEBAPI.Repositories.Implementations
{
    public class StoreRepository : IStoreRepository
    {
        private readonly ICustomSqlHelper _sqlHelper;
        private readonly IResponseMessageService _messageService;
        private readonly IDistributedCache _distributedCache;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public StoreRepository(
            ICustomSqlHelper sqlHelper,
            IResponseMessageService messageService,
            IDistributedCache distributedCache)
        {
            _sqlHelper = sqlHelper;
            _messageService = messageService;
            _distributedCache = distributedCache;
        }

        public ServiceResult<CreateUpdateVendorMasterResponse> CreateUpdateVendorMaster(
            CreateUpdateVendorMasterRequest request,
            AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"CreateUpdateVendorMaster called. VendorId={request.VendorId}, VendorName={request.VendorName}");

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@hospId", globalValues.hospId),
                    new SqlParameter("@vendorId", request.VendorId),
                    new SqlParameter("@typeId", request.TypeId),
                    new SqlParameter("@type", request.Type),
                    new SqlParameter("@vendorName", request.VendorName),
                    new SqlParameter("@contactNo", request.ContactNo ?? (object)DBNull.Value),
                    new SqlParameter("@email", request.Email ?? (object)DBNull.Value),
                    new SqlParameter("@dlNo", request.DLNO ?? (object)DBNull.Value),
                    new SqlParameter("@gstinNo", request.GSTINNo ?? (object)DBNull.Value),
                    new SqlParameter("@address", request.Address ?? (object)DBNull.Value),
                    new SqlParameter("@countryId", request.CountryId ?? (object)DBNull.Value),
                    new SqlParameter("@stateId", request.StateId ?? (object)DBNull.Value),
                    new SqlParameter("@districtId", request.DistrictId ?? (object)DBNull.Value),
                    new SqlParameter("@cityId", request.CityId ?? (object)DBNull.Value),
                    new SqlParameter("@Pincode", request.Pincode),
                    new SqlParameter("@mappingBranch", request.MappingBranch ?? (object)DBNull.Value),
                    new SqlParameter("@isActive", request.IsActive),
                    new SqlParameter("@userId", globalValues.userId),
                    new SqlParameter("@IpAddress", globalValues.ipAddress),
                    new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output }
                };

                long result = _sqlHelper.RunProcedureInsert("IU_VendorMaster", parameters);

                // Clear all vendor cache entries
                ClearVendorCache();

                if (result == -1)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("RECORD_ALREADY_EXISTS");
                    _log.Warn($"Duplicate vendor name attempted: {request.VendorName}");
                    return ServiceResult<CreateUpdateVendorMasterResponse>.Failure(
                        alert.Type,
                        "Vendor name already exists",
                        409
                    );
                }

                if (request.VendorId == 0)
                {
                    var responseData = new CreateUpdateVendorMasterResponse { VendorId = (int)result };
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                    _log.Info($"Vendor created successfully. VendorId={result}");
                    return ServiceResult<CreateUpdateVendorMasterResponse>.Success(
                        responseData,
                        alert.Type,
                        alert.Message,
                        201
                    );
                }
                else
                {
                    var responseData = new CreateUpdateVendorMasterResponse { VendorId = (int)result };
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                    _log.Info($"Vendor updated successfully. VendorId={result}");
                    return ServiceResult<CreateUpdateVendorMasterResponse>.Success(
                        responseData,
                        alert.Type,
                        alert.Message,
                        200
                    );
                }
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<CreateUpdateVendorMasterResponse>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<VendorMasterModel>> GetVendorMasterList(
            int? vendorId = null,
            int? isActive = null)
        {
            try
            {
                _log.Info($"GetVendorMasterList called. VendorId={vendorId?.ToString() ?? "All"}, IsActive={isActive?.ToString() ?? "All"}");

                // Cache key for ALL vendors
                string cacheKey = "_VendorMaster_All";

                // Try to get all vendors from cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<VendorMasterModel> allVendors;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"VendorMaster data retrieved from cache. Key={cacheKey}");
                    allVendors = System.Text.Json.JsonSerializer.Deserialize<List<VendorMasterModel>>(cachedData);
                }
                else
                {
                    _log.Info($"VendorMaster cache miss. Fetching all data from database. Key={cacheKey}");

                    // Fetch ALL vendors from database (no filter in SP call)
                    var dataTable = _sqlHelper.GetDataTable(
                        "S_getdllVendorMasterList",
                        CommandType.StoredProcedure,
                        new { activeStatus = "" } // Empty string returns all
                    );

                    allVendors = dataTable?.AsEnumerable().Select(row => new VendorMasterModel
                    {
                        VendorId = row.Field<int>("VendorId"),
                        TypeId = row.Field<int?>("TypeId") ?? 0,
                        Type = row.Field<string>("Type") ?? string.Empty,
                        VendorName = row.Field<string>("VendorName") ?? string.Empty,
                        ContactNo = row.Field<string>("ContactNo") ?? string.Empty,
                        Email = row.Field<string>("Email") ?? string.Empty,
                        DLNO = row.Field<string>("DLNO") ?? string.Empty,
                        GSTINNo = row.Field<string>("GSTINNo") ?? string.Empty,
                        Address = row.Field<string>("Address") ?? string.Empty,
                        FullAddress = row.Field<string>("fullAddress") ?? string.Empty,
                        CountryId = row.Field<int?>("CountryId"),
                        StateId = row.Field<int?>("StateId"),
                        DistrictId = row.Field<int?>("DistrictId"),
                        CityId = row.Field<int?>("CityId"),
                        Pincode = row.Field<int?>("Pincode"),
                        MappingBranch = row.Field<string>("MappingBranch") ?? string.Empty,
                        IsActive = row.Field<int>("IsActive"),
                        CreatedBy = row.Field<string>("CreatedBy"),
                        CreatedOn = row.Field<string>("CreatedOn"),
                        LastModifiedBy = row.Field<string>("LastModifiedBy"),
                        LastModifiedOn = row.Field<string>("LastModifiedOn")
                    }).ToList() ?? new List<VendorMasterModel>();

                    // Store ALL vendors in cache (permanent until manually cleared)
                    if (allVendors.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(allVendors);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"All VendorMaster data cached permanently. Key={cacheKey}, Count={allVendors.Count}");
                    }
                }

                // Filter in memory based on parameters (always from cache)
                List<VendorMasterModel> filteredVendors = allVendors;

                if (vendorId.HasValue)
                {
                    _log.Info($"Filtering cached data by VendorId: {vendorId.Value}");
                    filteredVendors = filteredVendors.Where(v => v.VendorId == vendorId.Value).ToList();
                }

                if (isActive.HasValue)
                {
                    _log.Info($"Filtering cached data by IsActive: {isActive.Value}");
                    filteredVendors = filteredVendors.Where(v => v.IsActive == isActive.Value).ToList();
                }

                if (!filteredVendors.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No vendors found for VendorId={vendorId?.ToString() ?? "All"}, IsActive={isActive?.ToString() ?? "All"}");
                    return ServiceResult<IEnumerable<VendorMasterModel>>.Failure(
                        alert.Type,
                        "No vendors found",
                        404
                    );
                }

                _log.Info($"Retrieved {filteredVendors.Count} vendor(s) from cache");

                return ServiceResult<IEnumerable<VendorMasterModel>>.Success(
                    filteredVendors,
                    "Info",
                    $"{filteredVendors.Count} vendor(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<VendorMasterModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        /// <summary>
        /// Clear all vendor-related cache entries
        /// </summary>
        private void ClearVendorCache()
        {
            try
            {
                _distributedCache.Remove("_VendorMaster_All");
                _log.Info("Cleared VendorMaster cache");
            }
            catch (Exception ex)
            {
                _log.Error($"Error clearing VendorMaster cache: {ex.Message}");
            }
        }
    }
}