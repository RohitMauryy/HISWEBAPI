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

namespace HISWEBAPI.Repositories.Implementations
{
    public class HomeRepository : IHomeRepository
    {
        private readonly ICustomSqlHelper _sqlHelper;
        private readonly IResponseMessageService _messageService;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public HomeRepository(
            ICustomSqlHelper sqlHelper,
            IResponseMessageService messageService)
        {
            _sqlHelper = sqlHelper;
            _messageService = messageService;
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
    }
}