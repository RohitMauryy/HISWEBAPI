using Microsoft.AspNetCore.Mvc;
using System;
using System.Reflection;
using log4net;
using HISWEBAPI.DTO.User;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Exceptions;
using System.Text;
using HISWEBAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using HISWEBAPI.Services;
using HISWEBAPI.Utilities;
using System.Data;
using Azure.Core;

namespace HISWEBAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ISmsService _smsService;
        private readonly IJwtService _jwtService;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public UserController(IUserRepository userRepository, ISmsService smsService, IJwtService jwtService)
        {
            _userRepository = userRepository;
            _smsService = smsService;
            _jwtService = jwtService;
        }

        [HttpPost("userLogin")]
        [AllowAnonymous]
        public IActionResult UserLogin([FromBody] LoginRequest request)
        {
            _log.Info($"UserLogin called. BranchId={request.BranchId}, UserName={request.UserName}");
            try
            {
                if (!ModelState.IsValid)
                {
                    _log.Warn("Invalid login request model.");
                    return BadRequest(new { result = false, message = "Invalid request data.", errors = ModelState });
                }

                // Authenticate user
                var userId = _userRepository.UserLogin(request.BranchId, request.UserName, request.Password);

                if (userId > 0)
                {
                    // Extract browser and device info
                    var userAgent = Request.Headers["User-Agent"].ToString();
                    var ipAddress = GetClientIpAddress();
                    var (browser, browserVersion, os, device, deviceType) = BrowserDetectionHelper.ParseUserAgent(userAgent);

                    // Create login session
                    var sessionRequest = new LoginSessionRequest
                    {
                        UserId = userId,
                        BranchId = request.BranchId,
                        IpAddress = ipAddress,
                        UserAgent = userAgent,
                        Browser = browser,
                        BrowserVersion = browserVersion,
                        OperatingSystem = os,
                        Device = device,
                        DeviceType = deviceType,
                        Location = null // Can be populated using IP geolocation service
                    };

                    long sessionId = _userRepository.CreateLoginSession(sessionRequest);

                    if (sessionId == 0)
                    {
                        _log.Error("Failed to create login session.");
                    }

                    // Generate JWT tokens
                    var roles = new List<string> { "User" };
                    var email = $"{request.UserName}@hospital.com";

                    var accessToken = _jwtService.GenerateToken(
                        userId.ToString(),
                        request.UserName,
                        email,
                        roles
                    );

                    var refreshToken = _jwtService.GenerateRefreshToken();

                    // Save refresh token
                    if (sessionId > 0)
                    {
                        bool tokenSaved = _userRepository.SaveRefreshToken(
                            userId,
                            sessionId,
                            refreshToken,
                            DateTime.UtcNow.AddDays(7)
                        );

                        if (!tokenSaved)
                        {
                            _log.Error($"Failed to save refresh token for sessionId={sessionId}");
                        }
                    }

                    _log.Info($"Login successful. UserId={userId}, SessionId={sessionId}");

                    return Ok(new
                    {
                        result = true,
                        message = "Login successful",
                        data = new
                        {
                            userId = userId,
                            userName = request.UserName,
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
                return Unauthorized(new { result = false, message = "Invalid credentials." });
            }
            catch (Exception ex)
            {
                _log.Error($"Error during login: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, message = "Server error occurred." });
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
                    return BadRequest(new { result = false, message = "Access token and refresh token are required." });
                }

                // Validate the expired token and extract claims
                var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
                if (principal == null)
                {
                    _log.Warn("Invalid access token provided for refresh.");
                    return BadRequest(new { result = false, message = "Invalid access token." });
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
                    return Unauthorized(new { result = false, message = "Invalid or expired refresh token." });
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
                    _log.Error($"Failed to save new refresh token for sessionId={sessionId}");
                }

                _log.Info($"Token refreshed successfully for UserId={userId}");

                return Ok(new
                {
                    result = true,
                    message = "Token refreshed successfully",
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
                return StatusCode(500, new { result = false, message = "Server error occurred while refreshing token." });
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
                        _log.Warn($"Failed to complete logout for SessionId={request.SessionId}");
                    }
                }

                _log.Info($"User logged out successfully. UserId={userId}, UserName={username}");

                return Ok(new { result = true, message = "Logged out successfully." });
            }
            catch (Exception ex)
            {
                _log.Error($"Error during logout: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, message = "Server error occurred during logout." });
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
                    return Unauthorized(new { result = false, message = "Invalid user." });
                }

                bool result = _userRepository.InvalidateAllUserSessions(long.Parse(userId));

                if (result)
                {
                    _log.Info($"All sessions logged out for UserId={userId}");
                    return Ok(new { result = true, message = "All sessions logged out successfully." });
                }

                return StatusCode(500, new { result = false, message = "Failed to logout all sessions." });
            }
            catch (Exception ex)
            {
                _log.Error($"Error during logoutAllSessions: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, message = "Server error occurred." });
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
                    return Unauthorized(new { result = false, message = "Invalid user." });
                }

                DataTable dt = _userRepository.GetUserLoginHistory(long.Parse(userId), pageNumber, pageSize);

                if (dt == null || dt.Rows.Count == 0)
                {
                    return Ok(new { result = true, message = "No login history found.", data = new List<LoginHistoryResponse>() });
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
                return Ok(new { result = true, data = history });
            }
            catch (Exception ex)
            {
                _log.Error($"Error retrieving login history: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, message = "Server error occurred." });
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
                    return Unauthorized(new { result = false, message = "Invalid user." });
                }

                DataTable dt = _userRepository.GetActiveUserSessions(long.Parse(userId));

                if (dt == null || dt.Rows.Count == 0)
                {
                    return Ok(new { result = true, message = "No active sessions found.", data = new List<ActiveSessionResponse>() });
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
                    IsCurrentSession = false // You can compare with current session if needed
                }).ToList();

                _log.Info($"Active sessions retrieved for UserId={userId}");
                return Ok(new { result = true, data = sessions });
            }
            catch (Exception ex)
            {
                _log.Error($"Error retrieving active sessions: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, message = "Server error occurred." });
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
                    return Unauthorized(new { result = false, message = "Invalid user." });
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
                    return Ok(new { result = true, message = "Session terminated successfully." });
                }

                return StatusCode(500, new { result = false, message = "Failed to terminate session." });
            }
            catch (Exception ex)
            {
                _log.Error($"Error terminating session: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, message = "Server error occurred." });
            }
        }

        // Helper method to get client IP address
        private string GetClientIpAddress()
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                // Check for X-Forwarded-For header (for proxies/load balancers)
                if (Request.Headers.ContainsKey("X-Forwarded-For"))
                {
                    ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                }
                // Check for X-Real-IP header
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

        [HttpPost("insertUserMaster")]
        public IActionResult InsertUserMaster([FromBody] UserMasterRequest request)
        {
            _log.Info($"InsertUserMaster called. UserName={request.UserName}");

            try
            {
                if (!ModelState.IsValid)
                {
                    _log.Warn("Invalid model state for user insert.");
                    return BadRequest(new { result = false, message = "Invalid input data.", errors = ModelState });
                }

                var result = _userRepository.InsertUserMaster(request);

                if (result == -1)
                {
                    _log.Warn($"Duplicate username attempted: {request.UserName}");
                    return Conflict(new { result = false, message = "Username already exists. Please choose a different username." });
                }

                if (result == -2)
                {
                    _log.Warn("License validation failed for user insert.");
                    return BadRequest(new { result = false, message = "License limit reached. Cannot add more users." });
                }

                if (result > 0)
                {
                    _log.Info($"User inserted/updated successfully. UserId={result}");
                    string message = request.UserId.HasValue && request.UserId.Value > 0
                        ? "User updated successfully."
                        : "User created successfully.";

                    return Ok(new { result = true, userId = result, message });
                }

                _log.Error("Failed to insert/update user. Result=0");
                return StatusCode(500, new { result = false, message = "Failed to save user." });
            }
            catch (Exception ex)
            {
                _log.Error($"Error in InsertUserMaster: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, message = "Server error occurred." });
            }
        }

        [HttpPost("sendPasswordResetOtp")]
        public IActionResult SendPasswordResetOtp([FromBody] ForgotPasswordRequest request)
        {
            _log.Info($"sendPasswordResetOtp called. UserName={request.UserName}");

            try
            {
                if (!ModelState.IsValid)
                {
                    _log.Warn("Invalid model state for forgot password.");
                    return BadRequest(new { result = false, message = "Invalid input data.", errors = ModelState });
                }

                var (userExists, contactMatch, userId, registeredContact) = _userRepository.ValidateUserForPasswordReset(
                    request.UserName,
                    request.Contact
                );

                if (!userExists)
                {
                    _log.Warn($"Username not found: {request.UserName}");
                    return NotFound(new ForgotPasswordResponse
                    {
                        Result = false,
                        Message = "Username does not exist. Please check and try again.",
                        ContactHint = null
                    });
                }

                if (!contactMatch)
                {
                    _log.Warn($"Contact mismatch for username: {request.UserName}");
                    string contactHint = GenerateContactHint(registeredContact);

                    return BadRequest(new ForgotPasswordResponse
                    {
                        Result = false,
                        Message = "Contact number does not match. Please check the registered contact hint.",
                        ContactHint = contactHint
                    });
                }

                string otp = GenerateOtp();
                bool otpStored = _userRepository.StoreOtpForPasswordReset(userId, otp, 5);

                if (!otpStored)
                {
                    _log.Error($"Failed to store OTP for user: {request.UserName}");
                    return StatusCode(500, new { result = false, message = "Failed to generate OTP. Please try again." });
                }

                bool smsSent = _smsService.SendOtp(registeredContact, otp);

                if (!smsSent)
                {
                    _log.Error($"Failed to send SMS to: {registeredContact}");
                    _log.Warn($"OTP stored but SMS failed for user: {request.UserName}");
                }

                _log.Info($"OTP generated for user {request.UserName}: {otp}");

                string contactHintSuccess = GenerateContactHint(registeredContact);

                _log.Info($"OTP sent successfully for username: {request.UserName}, UserId: {userId}");

                return Ok(new ForgotPasswordResponse
                {
                    Result = true,
                    Message = $"OTP sent successfully on contact no. {contactHintSuccess}",
                    ContactHint = contactHintSuccess,
                    UserId = userId
                });
            }
            catch (Exception ex)
            {
                _log.Error($"Error in sendPasswordResetOtp: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, message = "Server error occurred." });
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

        [HttpPost("verifyOtpAndResetPassword")]
        public IActionResult VerifyOtpAndResetPassword([FromBody] VerifyOtpAndResetPasswordRequest request)
        {
            _log.Info($"VerifyOtpAndResetPassword called. UserName={request.UserName}");

            try
            {
                if (!ModelState.IsValid)
                {
                    _log.Warn("Invalid model state for OTP verification.");
                    return BadRequest(new { result = false, message = "Invalid input data.", errors = ModelState });
                }

                string hashedPassword = PasswordHasher.HashPassword(request.NewPassword);

                var (result, message) = _userRepository.VerifyOtpAndResetPassword(
                    request.UserName,
                    request.Otp,
                    hashedPassword
                );

                switch (result)
                {
                    case 1:
                        _log.Info($"Password reset successfully for user: {request.UserName}");
                        return Ok(new VerifyOtpAndResetPasswordResponse
                        {
                            Result = true,
                            Message = message
                        });

                    case -1:
                        _log.Warn($"User not found: {request.UserName}");
                        return NotFound(new VerifyOtpAndResetPasswordResponse
                        {
                            Result = false,
                            Message = message
                        });

                    case -2:
                        _log.Warn($"Inactive user attempted password reset: {request.UserName}");
                        return BadRequest(new VerifyOtpAndResetPasswordResponse
                        {
                            Result = false,
                            Message = message
                        });

                    case -3:
                    case -4:
                    case -5:
                        _log.Warn($"OTP validation failed for user {request.UserName}: {message}");
                        return BadRequest(new VerifyOtpAndResetPasswordResponse
                        {
                            Result = false,
                            Message = message
                        });

                    case -6:
                        _log.Warn($"Invalid OTP entered for user: {request.UserName}");
                        return BadRequest(new VerifyOtpAndResetPasswordResponse
                        {
                            Result = false,
                            Message = message
                        });

                    default:
                        _log.Error($"Unknown error during password reset for user: {request.UserName}. Result code: {result}");
                        return StatusCode(500, new VerifyOtpAndResetPasswordResponse
                        {
                            Result = false,
                            Message = message
                        });
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error in VerifyOtpAndResetPassword: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, message = "Server error occurred." });
            }
        }

        [HttpPost("updatePassword")]
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
                    return BadRequest(new { result = false, message });
                }

                _log.Info($"Password Updated successfully for UserId={model.UserId}");
                return Ok(new { result = true, message });
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, message = "Server error occurred." });
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
                    return NotFound(new { result = false, message = "No roles found for this user." });
                }

                var roles = dt.AsEnumerable().Select(row => new
                {
                    RoleId = row["RoleId"],
                    RoleName = row["RoleName"]
                });
                _log.Info($"GetUserRoles successfully for UserId={request.UserId}");
                return Ok(new { result = true, roles });
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, message = "Server error occurred." });
            }
        }
    }

    

   
}