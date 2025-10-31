using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Linq;
using System.Reflection;
using log4net;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Exceptions;
using HISWEBAPI.DTO;
using HISWEBAPI.Services;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace HISWEBAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IHomeRepository _homeRepository;
        private readonly IDistributedCache _distributedCache;
        private readonly IResponseMessageService _messageService;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public HomeController(
            IHomeRepository repository,
            IDistributedCache distributedCache,
            IResponseMessageService messageService
            )
        {
            _homeRepository = repository;
            _distributedCache = distributedCache;
            _messageService = messageService;

        }

        private (string Type, string Message) GetAlert(string alertCode)
        {
            return _messageService.GetMessageAndTypeByAlertCode(alertCode);
        }

        [HttpGet("getActiveBranchList")]
        [AllowAnonymous]
        public IActionResult GetActiveBranchList()
        {
            _log.Info("GetActiveBranchList called.");
            try
            {
                var branches = _homeRepository.GetActiveBranchList();
                if (branches == null || !branches.Any())
                {
                    _log.Warn("No branches found.");
                    return NotFound(new { result = false, messageType = GetAlert("DATA_NOT_FOUND").Type, message = GetAlert("DATA_NOT_FOUND").Message });
                }
                _log.Info($"Branches fetched, Count: {branches.Count()}");
                return Ok(new { result = true, data = branches });
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("SERVER_ERROR_FOUND").Type, message = GetAlert("SERVER_ERROR_FOUND").Message });
            }
        }


        [HttpGet("getPickListMaster")]
        [AllowAnonymous]
        public IActionResult GetPickListMaster(string fieldName)
        {
            _log.Info("GetPickListMaster called.");
            try
            {
                var pickList = _homeRepository.GetPickListMaster(fieldName);
                if (pickList == null || !pickList.Any())
                {
                    _log.Warn("No PickList found.");
                    return NotFound(new { result = false, messageType = GetAlert("DATA_NOT_FOUND").Type, message = GetAlert("DATA_NOT_FOUND").Message });
                }
                _log.Info($"PickList fetched, Count: {pickList.Count()}");
                return Ok(new { result = true, data = pickList });
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("SERVER_ERROR_FOUND").Type, message = GetAlert("SERVER_ERROR_FOUND").Message });
            }
        }
        
       

    }
}