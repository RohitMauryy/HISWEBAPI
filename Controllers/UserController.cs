using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Reflection;
using log4net;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Exceptions;
using HISWEBAPI.DTO;
using HISWEBAPI.Services;
using Microsoft.AspNetCore.Authorization;
using HISWEBAPI.Models;

namespace HISWEBAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IResponseMessageService _messageService;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public UserController(
            IUserRepository repository,
            IResponseMessageService messageService)
        {
            _userRepository = repository;
            _messageService = messageService;
        }

        private AllGlobalValues GetGlobalValues()
        {
            var hospIdClaim = User.Claims.FirstOrDefault(c => c.Type == "hospId")?.Value;
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            return new AllGlobalValues
            {
                hospId = int.TryParse(hospIdClaim, out int hospId) ? hospId : 0,
                userId = int.TryParse(userIdClaim, out int userId) ? userId : 0,
                ipAddress = ipAddress ?? "Unknown"
            };
        }

        [HttpPost("userLogin")]
        [AllowAnonymous]
        public IActionResult UserLogin([FromBody] UserLoginRequest request)
        {
            _log.Info($"UserLogin called. BranchId={request.BranchId}, UserName={request.UserName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for user login.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            var serviceResult = _userRepository.UserLogin(request);

            if (serviceResult.Result)
                _log.Info($"User login successful: {serviceResult.Message}");
            else
                _log.Warn($"User login failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("refreshToken")]
        [AllowAnonymous]
        public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
        {
            _log.Info("RefreshToken called.");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for refresh token.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            var serviceResult = _userRepository.RefreshToken(request);

            if (serviceResult.Result)
                _log.Info($"Token refreshed successfully: {serviceResult.Message}");
            else
                _log.Warn($"Token refresh failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout([FromBody] LogoutRequest request)
        {
            _log.Info("Logout called.");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for logout.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            var serviceResult = _userRepository.Logout(request);

            if (serviceResult.Result)
                _log.Info($"User logout successful: {serviceResult.Message}");
            else
                _log.Warn($"User logout failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message
            });
        }

        [HttpPost("newUserSignUp")]
        [AllowAnonymous]
        public IActionResult NewUserSignUp([FromBody] UserSignupRequest request)
        {
            _log.Info($"NewUserSignUp called. UserName={request.UserName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for user signup.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            var serviceResult = _userRepository.NewUserSignUp(request);

            if (serviceResult.Result)
                _log.Info($"User signup successful: {serviceResult.Message}");
            else
                _log.Warn($"User signup failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("sendSmsOtp")]
        [AllowAnonymous]
        public IActionResult SendSmsOtp([FromBody] SendSmsOtpRequest request)
        {
            _log.Info($"SendSmsOtp called. UserName={request.UserName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for send SMS OTP.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            var serviceResult = _userRepository.SendSmsOtp(request);

            if (serviceResult.Result)
                _log.Info($"SMS OTP sent successfully: {serviceResult.Message}");
            else
                _log.Warn($"SMS OTP send failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("verifySmsOtp")]
        [AllowAnonymous]
        public IActionResult VerifySmsOtp([FromBody] VerifySmsOtpRequest request)
        {
            _log.Info($"VerifySmsOtp called. UserId={request.UserId}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for verify SMS OTP.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            var serviceResult = _userRepository.VerifySmsOtp(request);

            if (serviceResult.Result)
                _log.Info($"SMS OTP verified successfully: {serviceResult.Message}");
            else
                _log.Warn($"SMS OTP verification failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("sendEmailOtp")]
        [AllowAnonymous]
        public IActionResult SendEmailOtp([FromBody] SendEmailOtpRequest request)
        {
            _log.Info($"SendEmailOtp called. UserName={request.UserName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for send email OTP.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            var serviceResult = _userRepository.SendEmailOtp(request);

            if (serviceResult.Result)
                _log.Info($"Email OTP sent successfully: {serviceResult.Message}");
            else
                _log.Warn($"Email OTP send failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("verifyEmailOtp")]
        [AllowAnonymous]
        public IActionResult VerifyEmailOtp([FromBody] VerifyEmailOtpRequest request)
        {
            _log.Info($"VerifyEmailOtp called. UserId={request.UserId}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for verify email OTP.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            var serviceResult = _userRepository.VerifyEmailOtp(request);

            if (serviceResult.Result)
                _log.Info($"Email OTP verified successfully: {serviceResult.Message}");
            else
                _log.Warn($"Email OTP verification failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("resetPasswordByUserId")]
        [AllowAnonymous]
        public IActionResult ResetPasswordByUserId([FromBody] ResetPasswordRequest request)
        {
            _log.Info($"ResetPasswordByUserId called. UserId={request.UserId}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for reset password.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            var serviceResult = _userRepository.ResetPasswordByUserId(request);

            if (serviceResult.Result)
                _log.Info($"Password reset successful: {serviceResult.Message}");
            else
                _log.Warn($"Password reset failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message
            });
        }

        [HttpPatch("updatePassword")]
        [Authorize]
        public IActionResult UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            _log.Info($"UpdatePassword called. UserId={request.UserId}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for update password.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            var serviceResult = _userRepository.UpdatePassword(request);

            if (serviceResult.Result)
                _log.Info($"Password updated successfully: {serviceResult.Message}");
            else
                _log.Warn($"Password update failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message
            });
        }

        [HttpGet("getUserRoles")]
        [Authorize]
        public IActionResult GetUserRoles([FromQuery] int branchId, int userId)
        {
            _log.Info($"GetUserRoles called. UserId={userId}");

            var request = new UserRoleRequest { BranchId = branchId, UserId = userId };
            var serviceResult = _userRepository.GetUserRoles(request);

            if (serviceResult.Result)
                _log.Info($"User roles fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"User roles fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

    }
}