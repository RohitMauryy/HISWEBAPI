using Microsoft.AspNetCore.Mvc;
using System;
using System.Reflection;
using log4net;
using HISWEBAPI.DTO;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Exceptions;
using System.Text;
using HISWEBAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using HISWEBAPI.Services;
using HISWEBAPI.Utilities;
using System.Data;
using Azure.Core;
using Microsoft.AspNetCore.Identity.Data;

namespace HISWEBAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ISmsService _smsService;
        private readonly IEmailService _emailService;
        private readonly IJwtService _jwtService;
        private readonly IResponseMessageService _messageService;

        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public UserController(
            IUserRepository userRepository,
            ISmsService smsService,
            IJwtService jwtService,
            IEmailService emailService,
            IResponseMessageService messageService)
        {
            _userRepository = userRepository;
            _smsService = smsService;
            _jwtService = jwtService;
            _emailService = emailService;
            _messageService = messageService;
        }

        private (string Type, string Message) GetAlert(string alertCode)
        {
            return _messageService.GetMessageAndTypeByAlertCode(alertCode);
        }


        [HttpPost("userLogin")]
        [AllowAnonymous]
        public IActionResult UserLogin([FromBody] DTO.UserLoginRequest request)
        {
            _log.Info($"UserLogin called. BranchId={request.BranchId}, UserName={request.UserName}");
            try
            {
                if (!ModelState.IsValid)
                {
                    _log.Warn("Invalid login request model.");
                    return BadRequest(new { result = false, messageType = GetAlert("INVALID_REQUEST_DATA").Type, message = GetAlert("INVALID_REQUEST_DATA").Message, errors = ModelState });
                }

                // Authenticate user
                var loginResponse = _userRepository.UserLogin(request.BranchId, request.UserName, request.Password);

                if (loginResponse != null && loginResponse.UserId > 0)
                {
                    // Extract browser and device info
                    var userAgent = Request.Headers["User-Agent"].ToString();
                    var ipAddress = GetClientIpAddress();
                    var (browser, browserVersion, os, device, deviceType) = BrowserDetectionHelper.ParseUserAgent(userAgent);

                    // Create login session
                    var sessionRequest = new LoginSessionRequest
                    {
                        UserId = loginResponse.UserId,
                        BranchId = request.BranchId,
                        IpAddress = ipAddress,
                        UserAgent = userAgent,
                        Browser = browser,
                        BrowserVersion = browserVersion,
                        OperatingSystem = os,
                        Device = device,
                        DeviceType = deviceType,
                        Location = null
                    };

                    long sessionId = _userRepository.CreateLoginSession(sessionRequest);

                    if (sessionId == 0)
                    {
                        _log.Error(GetAlert("SESSION_CREATE_FAILED").Message);
                    }

                    // Generate JWT tokens
                    var accessToken = _jwtService.GenerateToken(
                        loginResponse.UserId.ToString(),
                        request.UserName,
                        loginResponse.Email
                    );

                    var refreshToken = _jwtService.GenerateRefreshToken();

                    // Save refresh token
                    if (sessionId > 0)
                    {
                        bool tokenSaved = _userRepository.SaveRefreshToken(
                            loginResponse.UserId,
                            sessionId,
                            refreshToken,
                            DateTime.UtcNow.AddDays(1)
                        );

                        if (!tokenSaved)
                        {
                            _log.Error($"{GetAlert("TOKEN_SAVE_FAILED").Message} sessionId={sessionId}");
                        }
                    }

                    _log.Info($"Login successful. UserId={loginResponse.UserId}, SessionId={sessionId}");

                    return Ok(new
                    {
                        result = true,
                        message = GetAlert("LOGIN_SUCCESS").Message,
                        messageType = GetAlert("LOGIN_SUCCESS").Type,
                        data = new
                        {
                            userId = loginResponse.UserId,
                            userName = request.UserName,
                            email = loginResponse.Email,
                            contact = loginResponse.Contact,
                            isContactVerified = loginResponse.IsContactVerified,
                            isEmailVerified = loginResponse.IsEmailVerified,
                            branchId = request.BranchId,
                            sessionId = sessionId,
                            accessToken = accessToken,
                            refreshToken = refreshToken,
                            tokenType = "Bearer",
                            expiresIn = 3600,
                            loginInfo = new
                            {
                                ipAddress = ipAddress,
                                browser = browser,
                                browserVersion = browserVersion,
                                operatingSystem = os,
                                device = device,
                                deviceType = deviceType
                            }
                        }
                    });
                }

                _log.Warn($"Invalid credentials for UserName={request.UserName}, BranchId={request.BranchId}");
                return Unauthorized(new { result = false, messageType = GetAlert("INVALID_CREDENTIALS").Type, message = GetAlert("INVALID_CREDENTIALS").Message });
            }
            catch (Exception ex)
            {
                _log.Error($"Error during login: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("SERVER_ERROR").Type, message = GetAlert("SERVER_ERROR").Message });
            }
        }

        [HttpPost("refreshToken")]
        [AllowAnonymous]
        public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
        {
            _log.Info("RefreshToken called.");
            try
            {
                if (string.IsNullOrEmpty(request.AccessToken) || string.IsNullOrEmpty(request.RefreshToken))
                {
                    _log.Warn("Invalid refresh token request.");
                    return BadRequest(new { result = false, messageType = GetAlert("TOKEN_REQUIRED").Type, message = GetAlert("TOKEN_REQUIRED").Message });
                }

                // Validate the expired token and extract claims
                var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
                if (principal == null)
                {
                    _log.Warn("Invalid access token provided for refresh.");
                    return BadRequest(new { result = false, messageType = GetAlert("INVALID_ACCESS_TOKEN").Type, message = GetAlert("INVALID_ACCESS_TOKEN").Message });
                }

                var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var username = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                var email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var roles = principal.FindAll(System.Security.Claims.ClaimTypes.Role)
                    .Select(c => c.Value).ToList();

                // Validate refresh token from database
                var (isValid, sessionId, userIdFromToken) = _userRepository.ValidateRefreshToken(request.RefreshToken);

                if (!isValid || userIdFromToken.ToString() != userId)
                {
                    _log.Warn($"Invalid refresh token for UserId={userId}");
                    return Unauthorized(new { result = false, messageType = GetAlert("INVALID_REFRESH_TOKEN").Type, message = GetAlert("INVALID_REFRESH_TOKEN").Message });
                }

                // Generate new tokens
                var newAccessToken = _jwtService.GenerateToken(userId ?? "", username ?? "", email ?? "", roles);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                // Update refresh token in database
                bool tokenSaved = _userRepository.SaveRefreshToken(
                    userIdFromToken,
                    sessionId,
                    newRefreshToken,
                    DateTime.UtcNow.AddDays(7)
                );

                if (!tokenSaved)
                {
                    _log.Error($"{GetAlert("TOKEN_SAVE_FAILED").Message} sessionId={sessionId}");
                }

                _log.Info($"Token refreshed successfully for UserId={userId}");

                return Ok(new
                {
                    result = true,
                    messageType = GetAlert("TOKEN_REFRESHED").Type,
                    message = GetAlert("TOKEN_REFRESHED").Message,
                    data = new
                    {
                        accessToken = newAccessToken,
                        refreshToken = newRefreshToken,
                        tokenType = "Bearer",
                        expiresIn = 3600
                    }
                });
            }
            catch (Exception ex)
            {
                _log.Error($"Error refreshing token: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("TOKEN_REFRESH_ERROR").Type, message = GetAlert("TOKEN_REFRESH_ERROR").Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout([FromBody] LogoutRequest request)
        {
            _log.Info("Logout called.");
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

                if (request?.SessionId > 0)
                {
                    // Update session status to 'Logged Out'
                    bool sessionUpdated = _userRepository.UpdateLoginSession(
                        request.SessionId,
                        "Logged Out",
                        request.LogoutReason ?? "User Logout"
                    );

                    // Invalidate refresh token
                    bool tokenInvalidated = _userRepository.InvalidateRefreshToken(request.SessionId);

                    if (!sessionUpdated || !tokenInvalidated)
                    {
                        _log.Warn($"{GetAlert("LOGOUT_FAILED")} SessionId={request.SessionId}");
                    }
                }

                _log.Info($"User logged out successfully. UserId={userId}, UserName={username}");

                return Ok(new { result = true, messageType = GetAlert("LOGOUT_SUCCESS").Type, message = GetAlert("LOGOUT_SUCCESS").Message });
            }
            catch (Exception ex)
            {
                _log.Error($"Error during logout: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("LOGOUT_ERROR").Type, message = GetAlert("LOGOUT_ERROR").Message });
            }
        }

        [HttpPost("logoutAllSessions")]
        [Authorize]
        public IActionResult LogoutAllSessions()
        {
            _log.Info("LogoutAllSessions called.");
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { result = false, messageType = GetAlert("INVALID_USER").Type, message = GetAlert("INVALID_USER").Message });
                }

                bool result = _userRepository.InvalidateAllUserSessions(long.Parse(userId));

                if (result)
                {
                    _log.Info($"All sessions logged out for UserId={userId}");
                    return Ok(new { result = true, messageType = GetAlert("ALL_SESSIONS_LOGGED_OUT").Type, message = GetAlert("ALL_SESSIONS_LOGGED_OUT").Message });
                }

                return StatusCode(500, new { result = false, messageType = GetAlert("LOGOUT_ALL_SESSIONS_FAILED").Type, message = GetAlert("LOGOUT_ALL_SESSIONS_FAILED").Message });
            }
            catch (Exception ex)
            {
                _log.Error($"Error during logoutAllSessions: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("SERVER_ERROR").Type, message = GetAlert("SERVER_ERROR").Message });
            }
        }

        [HttpGet("loginHistory")]
        [Authorize]
        public IActionResult GetLoginHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            _log.Info("GetLoginHistory called.");
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { result = false, messageType = GetAlert("INVALID_USER").Type, message = GetAlert("INVALID_USER").Message });
                }

                DataTable dt = _userRepository.GetUserLoginHistory(long.Parse(userId), pageNumber, pageSize);

                if (dt == null || dt.Rows.Count == 0)
                {
                    return Ok(new { result = true, messageType = GetAlert("NO_LOGIN_HISTORY").Type, message = GetAlert("NO_LOGIN_HISTORY").Message, data = new List<LoginHistoryResponse>() });
                }

                var history = dt.AsEnumerable().Select(row => new LoginHistoryResponse
                {
                    SessionId = Convert.ToInt64(row["SessionId"]),
                    LoginTime = Convert.ToDateTime(row["LoginTime"]),
                    LogoutTime = row["LogoutTime"] != DBNull.Value ? Convert.ToDateTime(row["LogoutTime"]) : (DateTime?)null,
                    IpAddress = row["IpAddress"]?.ToString(),
                    Browser = row["Browser"]?.ToString(),
                    OperatingSystem = row["OperatingSystem"]?.ToString(),
                    Device = row["Device"]?.ToString(),
                    Location = row["Location"]?.ToString(),
                    Status = row["Status"]?.ToString(),
                    LogoutReason = row["LogoutReason"]?.ToString()
                }).ToList();

                _log.Info($"Login history retrieved for UserId={userId}");
                return Ok(new { result = true, messageType = GetAlert("LOGIN_HISTORY_RETRIEVED").Type, message = GetAlert("LOGIN_HISTORY_RETRIEVED").Message, data = history });
            }
            catch (Exception ex)
            {
                _log.Error($"Error retrieving login history: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("LOGIN_HISTORY_ERROR").Type, message = GetAlert("LOGIN_HISTORY_ERROR").Message });
            }
        }

        [HttpGet("activeSessions")]
        [Authorize]
        public IActionResult GetActiveSessions()
        {
            _log.Info("GetActiveSessions called.");
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { result = false, messageType = GetAlert("INVALID_USER").Type, message = GetAlert("INVALID_USER").Message });
                }

                DataTable dt = _userRepository.GetActiveUserSessions(long.Parse(userId));

                if (dt == null || dt.Rows.Count == 0)
                {
                    return Ok(new { result = true, messageType = GetAlert("NO_ACTIVE_SESSIONS").Type, message = GetAlert("NO_ACTIVE_SESSIONS").Message, data = new List<ActiveSessionResponse>() });
                }

                var sessions = dt.AsEnumerable().Select(row => new ActiveSessionResponse
                {
                    SessionId = Convert.ToInt64(row["SessionId"]),
                    LoginTime = Convert.ToDateTime(row["LoginTime"]),
                    LastActivityTime = Convert.ToDateTime(row["LastActivityTime"]),
                    IpAddress = row["IpAddress"]?.ToString(),
                    Browser = row["Browser"]?.ToString(),
                    OperatingSystem = row["OperatingSystem"]?.ToString(),
                    Device = row["Device"]?.ToString(),
                    IsCurrentSession = false
                }).ToList();

                _log.Info($"Active sessions retrieved for UserId={userId}");
                return Ok(new { result = true, messageType = GetAlert("ACTIVE_SESSIONS_RETRIEVED").Type, message = GetAlert("ACTIVE_SESSIONS_RETRIEVED").Message, data = sessions });
            }
            catch (Exception ex)
            {
                _log.Error($"Error retrieving active sessions: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("ACTIVE_SESSIONS_ERROR").Type, message = GetAlert("ACTIVE_SESSIONS_ERROR").Message });
            }
        }

        [HttpPost("terminateSession")]
        [Authorize]
        public IActionResult TerminateSession([FromBody] TerminateSessionRequest request)
        {
            _log.Info($"TerminateSession called. SessionId={request.SessionId}");
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { result = false, messageType = GetAlert("INVALID_USER").Type, message = GetAlert("INVALID_USER").Message });
                }

                // Update session status
                bool sessionUpdated = _userRepository.UpdateLoginSession(
                    request.SessionId,
                    "Terminated",
                    "Terminated by user from another device"
                );

                // Invalidate refresh token
                bool tokenInvalidated = _userRepository.InvalidateRefreshToken(request.SessionId);

                if (sessionUpdated && tokenInvalidated)
                {
                    _log.Info($"Session terminated successfully. SessionId={request.SessionId}");
                    return Ok(new { result = true, messageType = GetAlert("SESSION_TERMINATED").Type, message = GetAlert("SESSION_TERMINATED").Message });
                }

                return StatusCode(500, new { result = false, messageType = GetAlert("SESSION_TERMINATE_FAILED").Type, message = GetAlert("SESSION_TERMINATE_FAILED").Message });
            }
            catch (Exception ex)
            {
                _log.Error($"Error terminating session: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("SESSION_TERMINATE_ERROR").Type, message = GetAlert("SESSION_TERMINATE_ERROR").Message });
            }
        }

        private string GetClientIpAddress()
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                if (Request.Headers.ContainsKey("X-Forwarded-For"))
                {
                    ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                }
                else if (Request.Headers.ContainsKey("X-Real-IP"))
                {
                    ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
                }

                return ipAddress ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        [HttpPost("NewUserSignUp")]
        public IActionResult NewUserSignUp([FromBody] UserSignupRequest request)
        {
            _log.Info($"NewUserSignUp called. UserName={request.UserName}");

            try
            {
                if (!ModelState.IsValid)
                {
                    _log.Warn("Invalid model state for user insert.");
                    return BadRequest(new { result = false, messageType = GetAlert("INVALID_SIGNUP_DATA").Type, message = GetAlert("INVALID_SIGNUP_DATA").Message, errors = ModelState });
                }

                var result = _userRepository.NewUserSignUp(request);

                if (result == -1)
                {
                    _log.Warn($"Duplicate username attempted: {request.UserName}");
                    return Conflict(new { result = false, messageType = GetAlert("USERNAME_EXISTS").Type, message = GetAlert("USERNAME_EXISTS").Message });
                }

                if (result == -2)
                {
                    _log.Warn("License validation failed for user insert.");
                    return BadRequest(new { result = false, messageType = GetAlert("LICENSE_LIMIT_REACHED").Type, message = GetAlert("LICENSE_LIMIT_REACHED").Message });
                }

                if (result > 0)
                {
                    _log.Info($"User inserted successfully. UserId={result}");
                    return Ok(new { result = true, userId = result, messageType = GetAlert("USER_CREATED").Type, message = GetAlert("USER_CREATED").Message });
                }

                _log.Error("Failed to insert/update user. Result=0");
                return StatusCode(500, new { result = false, messageType = GetAlert("USER_SAVE_FAILED").Type, message = GetAlert("USER_SAVE_FAILED").Message });
            }
            catch (Exception ex)
            {
                _log.Error($"Error in NewUserSignUp: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("SIGNUP_ERROR").Type, message = GetAlert("SIGNUP_ERROR").Message });
            }
        }

        #region SMS & Email OTP Methods

        [HttpPost("sendSmsOtp")]
        [AllowAnonymous]
        public IActionResult SendSmsOtp([FromBody] SendSmsOtpRequest request)
        {
            _log.Info($"sendSmsOtp called. UserName={request.UserName}");

            try
            {
                if (!ModelState.IsValid)
                {
                    _log.Warn("Invalid model state for send SMS OTP.");
                    return BadRequest(new { result = false, messageType = GetAlert("INVALID_SMS_OTP_DATA").Type, message = GetAlert("INVALID_SMS_OTP_DATA").Message, errors = ModelState });
                }

                var (userExists, contactMatch, userId, registeredContact) = _userRepository.ValidateUserForPasswordReset(
                    request.UserName,
                    request.Contact
                );

                if (!userExists)
                {
                    _log.Warn($"Username not found: {request.UserName}");
                    return NotFound(new
                    {
                        result = false,
                        messageType = GetAlert("USERNAME_NOT_FOUND").Type,
                        message = GetAlert("USERNAME_NOT_FOUND").Message
                    });
                }

                if (!contactMatch)
                {
                    _log.Warn($"Contact mismatch for username: {request.UserName}");
                    string contactHint = GenerateContactHint(registeredContact);

                    return BadRequest(new
                    {
                        result = false,
                        messageType = GetAlert("CONTACT_NOT_MATCH").Type,
                        message = GetAlert("CONTACT_NOT_MATCH").Message,
                        contactHint = contactHint
                    });
                }

                string otp = GenerateOtp();
                bool otpStored = _userRepository.StoreOtpForPasswordReset(userId, otp, 5);

                if (!otpStored)
                {
                    _log.Error($"Failed to store OTP for user: {request.UserName}");
                    return StatusCode(500, new { result = false, messageType = GetAlert("OTP_GENERATION_FAILED").Type, message = GetAlert("OTP_GENERATION_FAILED").Message });
                }

                bool smsSent = _smsService.SendOtp(registeredContact, otp);

                if (!smsSent)
                {
                    _log.Error($"{GetAlert("SMS_SEND_FAILED").Message} to: {registeredContact}");
                    _log.Warn($"OTP stored but SMS failed for user: {request.UserName}");
                }

                _log.Info($"OTP generated for user {request.UserName}");

                string contactHintSuccess = GenerateContactHint(registeredContact);

                _log.Info($"OTP sent successfully for username: {request.UserName}, UserId: {userId}");

                return Ok(new
                {
                    result = true,
                    messageType = GetAlert("OTP_SENT_SMS").Type,
                    message = $"{GetAlert("OTP_SENT_SMS").Message} to {contactHintSuccess}",
                    userId = userId,
                    contactHint = contactHintSuccess
                });
            }
            catch (Exception ex)
            {
                _log.Error($"Error in sendSmsOtp: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("SMS_OTP_ERROR").Type, message = GetAlert("SMS_OTP_ERROR").Message });
            }
        }

        [HttpPost("verifySmsOtp")]
        [AllowAnonymous]
        public IActionResult VerifySmsOtp([FromBody] VerifySmsOtpRequest request)
        {
            _log.Info($"verifySmsOtp called. UserId={request.UserId}");

            try
            {
                if (!ModelState.IsValid)
                {
                    _log.Warn("Invalid model state for verify SMS OTP.");
                    return BadRequest(new { result = false, messageType = GetAlert("INVALID_VERIFY_OTP_DATA").Type, message = GetAlert("INVALID_VERIFY_OTP_DATA").Message, errors = ModelState });
                }

                var (result, message) = _userRepository.VerifySmsOtp(request.UserId, request.Otp);

                switch (result)
                {
                    case 1:
                        _log.Info($"OTP verified successfully for UserId: {request.UserId}");
                        return Ok(new
                        {
                            result = true,
                            messageType = GetAlert("OTP_VERIFIED").Type,
                            message = GetAlert("OTP_VERIFIED").Message,
                            userId = request.UserId,
                            otp = request.Otp
                        });

                    case -1:
                        _log.Warn($"User not found: UserId={request.UserId}");
                        return NotFound(new
                        {
                            result = false,
                            messageType = GetAlert("USER_NOT_FOUND_OTP").Type,
                            message = GetAlert("USER_NOT_FOUND_OTP").Message,
                            userId = request.UserId,
                            otp = request.Otp
                        });

                    case -2:
                        _log.Warn($"Inactive user attempted OTP verification: UserId={request.UserId}");
                        return BadRequest(new
                        {
                            result = false,
                            messageType = GetAlert("USER_INACTIVE").Type,
                            message = GetAlert("USER_INACTIVE").Message,
                            userId = request.UserId,
                            otp = request.Otp
                        });

                    case -3:
                    case -4:
                    case -5:
                        _log.Warn($"OTP validation failed for UserId {request.UserId}: {message}");
                        return BadRequest(new
                        {
                            result = false,
                            messageType = GetAlert("OTP_VALIDATION_FAILED").Type,
                            message = GetAlert("OTP_VALIDATION_FAILED").Message,
                            userId = request.UserId,
                            otp = request.Otp
                        });

                    case -6:
                        _log.Warn($"Invalid OTP entered for UserId: {request.UserId}");
                        return BadRequest(new
                        {
                            result = false,
                            messageType = GetAlert("INVALID_OTP").Type,
                            message = GetAlert("INVALID_OTP").Message,
                            userId = request.UserId,
                            otp = request.Otp
                        });

                    default:
                        _log.Error($"Unknown error during OTP verification for UserId: {request.UserId}. Result code: {result}");
                        return StatusCode(500, new
                        {
                            result = false,
                            messageType = GetAlert("OTP_VERIFICATION_ERROR").Type,
                            message = GetAlert("OTP_VERIFICATION_ERROR").Message,
                            userId = request.UserId,
                            otp = request.Otp
                        });
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error in verifySmsOtp: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("VERIFY_OTP_ERROR").Type, message = GetAlert("VERIFY_OTP_ERROR").Message });
            }
        }

        [HttpPost("resetPasswordByUserId")]
        [AllowAnonymous]
        public IActionResult ResetPasswordByUserId([FromBody] DTO.ResetPasswordRequest request)
        {
            _log.Info($"ResetPasswordByUserId called. UserId={request.UserId}");

            try
            {
                if (!ModelState.IsValid)
                {
                    _log.Warn("Invalid model state for reset password.");
                    return BadRequest(new { result = false, messageType = GetAlert("INVALID_RESET_PASSWORD_DATA").Type, message = GetAlert("INVALID_RESET_PASSWORD_DATA").Message, errors = ModelState });
                }

                if (request.NewPassword != request.ConfirmPassword)
                {
                    _log.Warn($"Password mismatch for UserId: {request.UserId}");
                    return BadRequest(new
                    {
                        result = false,
                        messageType = GetAlert("PASSWORD_MISMATCH").Type,
                        message = GetAlert("PASSWORD_MISMATCH").Message
                    });
                }

                string hashedPassword = PasswordHasher.HashPassword(request.NewPassword);

                var (result, message) = _userRepository.ResetPasswordByUserId(request.UserId, request.Otp, hashedPassword);

                if (result)
                {
                    _log.Info($"Password reset successfully for UserId: {request.UserId}");
                    return Ok(new
                    {
                        result = true,
                        messageType = GetAlert("PASSWORD_RESET_SUCCESS").Type,
                        message = GetAlert("PASSWORD_RESET_SUCCESS").Message
                    });
                }
                else
                {
                    _log.Warn($"Password reset failed for UserId: {request.UserId}. Message: {message}");
                    return BadRequest(new
                    {
                        result = false,
                        messageType = GetAlert("PASSWORD_RESET_FAILED").Type,
                        message = GetAlert("PASSWORD_RESET_FAILED").Message
                    });
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error in ResetPasswordByUserId: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("RESET_PASSWORD_ERROR").Type, message = GetAlert("RESET_PASSWORD_ERROR").Message });
            }
        }

        [HttpPost("sendEmailOtp")]
        [AllowAnonymous]
        public async Task<IActionResult> SendEmailOtp([FromBody] SendEmailOtpRequest request)
        {
            _log.Info($"sendEmailOtp called. UserName={request.UserName}");

            try
            {
                if (!ModelState.IsValid)
                {
                    _log.Warn("Invalid model state for send email OTP.");
                    return BadRequest(new { result = false, messageType = GetAlert("INVALID_EMAIL_OTP_DATA").Type, message = GetAlert("INVALID_EMAIL_OTP_DATA").Message, errors = ModelState });
                }

                var (userExists, emailMatch, userId, registeredEmail) = _userRepository.ValidateUserForEmailPasswordReset(
                    request.UserName,
                    request.Email
                );

                if (!userExists)
                {
                    _log.Warn($"Username not found: {request.UserName}");
                    return NotFound(new
                    {
                        result = false,
                        messageType = GetAlert("USERNAME_NOT_FOUND").Type,
                        message = GetAlert("USERNAME_NOT_FOUND").Message
                    });
                }

                if (!emailMatch)
                {
                    _log.Warn($"Email mismatch for username: {request.UserName}");
                    string emailHint = GenerateEmailHint(registeredEmail);

                    return BadRequest(new
                    {
                        result = false,
                        messageType = GetAlert("EMAIL_NOT_MATCH").Type,
                        message = GetAlert("EMAIL_NOT_MATCH").Message,
                        emailHint = emailHint
                    });
                }

                string otp = GenerateOtp();
                bool otpStored = _userRepository.StoreEmailOtpForPasswordReset(userId, otp, 5);

                if (!otpStored)
                {
                    _log.Error($"Failed to store OTP for user: {request.UserName}");
                    return StatusCode(500, new { result = false, messageType = GetAlert("OTP_GENERATION_FAILED").Type, message = GetAlert("OTP_GENERATION_FAILED").Message });
                }

                bool emailSent = await _emailService.SendOtpEmail(registeredEmail, otp, "Password Reset");

                if (!emailSent)
                {
                    _log.Error($"Failed to send email to: {registeredEmail}");
                    return StatusCode(500, new { result = false, messageType = GetAlert("EMAIL_SEND_FAILED").Type, message = GetAlert("EMAIL_SEND_FAILED").Message });
                }

                _log.Info($"OTP generated and sent via email for user {request.UserName}: {otp}");

                string emailHintSuccess = GenerateEmailHint(registeredEmail);

                _log.Info($"OTP sent successfully via email for username: {request.UserName}, UserId: {userId}");

                return Ok(new
                {
                    result = true,
                    messageType = GetAlert("OTP_SENT_EMAIL").Type,
                    message = $"{GetAlert("OTP_SENT_EMAIL").Message} to {emailHintSuccess}",
                    userId = userId,
                    emailHint = emailHintSuccess
                });
            }
            catch (Exception ex)
            {
                _log.Error($"Error in sendEmailOtp: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("EMAIL_OTP_ERROR").Type, message = GetAlert("EMAIL_OTP_ERROR").Message });
            }
        }

        [HttpPost("verifyEmailOtp")]
        [AllowAnonymous]
        public IActionResult VerifyEmailOtp([FromBody] VerifyEmailOtpRequest request)
        {
            _log.Info($"verifyEmailOtp called. UserId={request.UserId}");

            try
            {
                if (!ModelState.IsValid)
                {
                    _log.Warn("Invalid model state for verify email OTP.");
                    return BadRequest(new { result = false, messageType = GetAlert("INVALID_VERIFY_OTP_DATA").Type, message = GetAlert("INVALID_VERIFY_OTP_DATA").Message, errors = ModelState });
                }

                var (result, message) = _userRepository.VerifyEmailOtp(request.UserId, request.Otp);

                switch (result)
                {
                    case 1:
                        _log.Info($"Email OTP verified successfully for UserId: {request.UserId}");
                        return Ok(new
                        {
                            result = true,
                            messageType = GetAlert("EMAIL_OTP_VERIFIED").Type,
                            message = GetAlert("EMAIL_OTP_VERIFIED").Message
                        });

                    case -1:
                        _log.Warn($"User not found: UserId={request.UserId}");
                        return NotFound(new
                        {
                            result = false,
                            messageType = GetAlert("USER_NOT_FOUND_OTP").Type,
                            message = GetAlert("USER_NOT_FOUND_OTP").Message
                        });

                    case -2:
                        _log.Warn($"Inactive user attempted OTP verification: UserId={request.UserId}");
                        return BadRequest(new
                        {
                            result = false,
                            messageType = GetAlert("USER_INACTIVE").Type,
                            message = GetAlert("USER_INACTIVE").Message
                        });

                    case -3:
                    case -4:
                    case -5:
                        _log.Warn($"Email OTP validation failed for UserId {request.UserId}: {message}");
                        return BadRequest(new
                        {
                            result = false,
                            messageType = GetAlert("EMAIL_OTP_VALIDATION_FAILED").Type,
                            message = GetAlert("EMAIL_OTP_VALIDATION_FAILED").Message
                        });

                    case -6:
                        _log.Warn($"Invalid email OTP entered for UserId: {request.UserId}");
                        return BadRequest(new
                        {
                            result = false,
                            messageType = GetAlert("INVALID_EMAIL_OTP").Type,
                            message = GetAlert("INVALID_EMAIL_OTP").Message
                        });

                    default:
                        _log.Error($"Unknown error during email OTP verification for UserId: {request.UserId}. Result code: {result}");
                        return StatusCode(500, new
                        {
                            result = false,
                            messageType = GetAlert("EMAIL_OTP_VERIFICATION_ERROR").Type,
                            message = GetAlert("EMAIL_OTP_VERIFICATION_ERROR").Message
                        });
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error in verifyEmailOtp: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("VERIFY_EMAIL_OTP_ERROR").Type, message = GetAlert("VERIFY_EMAIL_OTP_ERROR").Message });
            }
        }

        private string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private string GenerateContactHint(string contact)
        {
            if (string.IsNullOrEmpty(contact))
                return string.Empty;

            int length = contact.Length;

            if (length < 4)
            {
                return new string('*', length);
            }

            string first2 = contact.Substring(0, 2);
            string last2 = contact.Substring(length - 2, 2);
            string middle = new string('*', length - 4);

            return $"{first2}{middle}{last2}";
        }

        private string GenerateEmailHint(string email)
        {
            if (string.IsNullOrEmpty(email))
                return string.Empty;

            var parts = email.Split('@');
            if (parts.Length != 2)
                return email;

            string localPart = parts[0];
            string domain = parts[1];

            if (localPart.Length <= 2)
            {
                return $"{new string('*', localPart.Length)}@{domain}";
            }

            string first2 = localPart.Substring(0, 2);
            string lastChar = localPart.Substring(localPart.Length - 1, 1);
            string middle = new string('*', localPart.Length - 3);

            return $"{first2}{middle}{lastChar}@{domain}";
        }

        #endregion

        [HttpPatch("updatePassword")]
        [Authorize]
        public IActionResult UpdatePassword([FromBody] UpdatePasswordRequest model)
        {
            _log.Info($"UpdatePassword called. UserId={model.UserId}");
            try
            {
                var (result, message) = _userRepository.UpdateUserPassword(model);

                if (!result)
                {
                    _log.Warn($"{message} UserId={model.UserId}");
                    return BadRequest(new { result = false, messageType = "Error", message });
                }

                _log.Info($"Password Updated successfully for UserId={model.UserId}");
                return Ok(new { result = true, messageType="Info", message });
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("UPDATE_PASSWORD_ERROR").Type, message = GetAlert("UPDATE_PASSWORD_ERROR").Message });
            }
        }

        [HttpPost("getUserRoles")]
        [Authorize]
        public IActionResult GetUserRoles([FromBody] UserRoleRequest request)
        {
            _log.Info($"GetUserRoles called. UserId={request.UserId}");
            try
            {
                DataTable dt = _userRepository.GetLoginUserRoles(request);

                if (dt == null || dt.Rows.Count == 0)
                {
                    _log.Warn($"No roles found for this user. UserId={request.UserId}");
                    return NotFound(new { result = false, messageType = GetAlert("NO_ROLES_FOUND").Type, message = GetAlert("NO_ROLES_FOUND").Message });
                }

                var roles = dt.AsEnumerable().Select(row => new
                {
                    RoleId = row["RoleId"],
                    RoleName = row["RoleName"]
                });

                _log.Info($"GetUserRoles successfully for UserId={request.UserId}");
                return Ok(new { result = true, messageType = GetAlert("ROLES_RETRIEVED").Type, message = GetAlert("ROLES_RETRIEVED").Message, roles });
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("GET_ROLES_ERROR").Type, message = GetAlert("GET_ROLES_ERROR").Message });
            }
        }
    }
}