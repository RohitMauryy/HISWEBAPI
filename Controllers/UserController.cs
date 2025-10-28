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
                // Validate model
                if (!ModelState.IsValid)
                {
                    _log.Warn("Invalid login request model.");
                    return BadRequest(new { result = false, message = "Invalid request data.", errors = ModelState });
                }

                // Authenticate user
                var userId = _userRepository.UserLogin(request.BranchId, request.UserName, request.Password);

                if (userId > 0)
                {
                    // Generate JWT tokens
                    var roles = new List<string> { "User" }; // You can fetch actual roles from database
                    var email = $"{request.UserName}@hospital.com"; // Replace with actual email from database if available

                    var accessToken = _jwtService.GenerateToken(
                        userId.ToString(),
                        request.UserName,
                        email,
                        roles
                    );

                    var refreshToken = _jwtService.GenerateRefreshToken();

                    // TODO: Save refresh token to database for future use
                    // Example: _userRepository.SaveRefreshToken(userId, refreshToken, DateTime.UtcNow.AddDays(7));

                    _log.Info($"Login successful. UserId={userId}, Token generated.");

                    return Ok(new
                    {
                        result = true,
                        message = "Login successful",
                        data = new
                        {
                            userId = userId,
                            userName = request.UserName,
                            branchId = request.BranchId,
                            accessToken = accessToken,
                            refreshToken = refreshToken,
                            tokenType = "Bearer",
                            expiresIn = 3600 // 60 minutes in seconds
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

                // TODO: Validate refresh token from database
                // Example: var isValidRefreshToken = _userRepository.ValidateRefreshToken(userId, request.RefreshToken);
                // if (!isValidRefreshToken) return Unauthorized();

                // Generate new tokens
                var newAccessToken = _jwtService.GenerateToken(userId ?? "", username ?? "", email ?? "", roles);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                // TODO: Update refresh token in database
                // Example: _userRepository.UpdateRefreshToken(userId, newRefreshToken, DateTime.UtcNow.AddDays(7));

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
        public IActionResult Logout()
        {
            _log.Info("Logout called.");
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

                // TODO: Invalidate refresh token in database
                // Example: _userRepository.InvalidateRefreshToken(userId);

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

                // Handle duplicate username
                if (result == -1)
                {
                    _log.Warn($"Duplicate username attempted: {request.UserName}");
                    return Conflict(new { result = false, message = "Username already exists. Please choose a different username." });
                }

                // Handle license limit
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

                // Username doesn't exist
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

                // Username exists but contact doesn't match
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

                // Generate OTP
                string otp = GenerateOtp();

                // Store OTP in database with 5 minute expiry
                bool otpStored = _userRepository.StoreOtpForPasswordReset(userId, otp, 5);

                if (!otpStored)
                {
                    _log.Error($"Failed to store OTP for user: {request.UserName}");
                    return StatusCode(500, new { result = false, message = "Failed to generate OTP. Please try again." });
                }

                // Send OTP via SMS
                bool smsSent = _smsService.SendOtp(registeredContact, otp);

                if (!smsSent)
                {
                    _log.Error($"Failed to send SMS to: {registeredContact}");
                    // Note: OTP is already stored in DB, so we still return success but log the SMS failure
                    _log.Warn($"OTP stored but SMS failed for user: {request.UserName}");
                }

                // Log OTP for development/testing
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

        // OTP Generation method (6-digit OTP)
        private string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString(); // 6-digit OTP
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

                // Hash the new password before storing
                string hashedPassword = PasswordHasher.HashPassword(request.NewPassword);
                //string hashedPassword = request.NewPassword;

                var (result, message) = _userRepository.VerifyOtpAndResetPassword(
                    request.UserName,
                    request.Otp,
                    hashedPassword
                );

                // Handle different result codes
                switch (result)
                {
                    case 1: // Success
                        _log.Info($"Password reset successfully for user: {request.UserName}");
                        return Ok(new VerifyOtpAndResetPasswordResponse
                        {
                            Result = true,
                            Message = message
                        });

                    case -1: // User not found
                        _log.Warn($"User not found: {request.UserName}");
                        return NotFound(new VerifyOtpAndResetPasswordResponse
                        {
                            Result = false,
                            Message = message
                        });

                    case -2: // User inactive
                        _log.Warn($"Inactive user attempted password reset: {request.UserName}");
                        return BadRequest(new VerifyOtpAndResetPasswordResponse
                        {
                            Result = false,
                            Message = message
                        });

                    case -3: // No OTP found
                    case -4: // OTP expired
                    case -5: // Max attempts exceeded
                        _log.Warn($"OTP validation failed for user {request.UserName}: {message}");
                        return BadRequest(new VerifyOtpAndResetPasswordResponse
                        {
                            Result = false,
                            Message = message
                        });

                    case -6: // Invalid OTP
                        _log.Warn($"Invalid OTP entered for user: {request.UserName}");
                        return BadRequest(new VerifyOtpAndResetPasswordResponse
                        {
                            Result = false,
                            Message = message
                        });

                    default: // Unknown error
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

    }
}