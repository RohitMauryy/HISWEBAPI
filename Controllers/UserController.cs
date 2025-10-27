using Microsoft.AspNetCore.Mvc;
using System;
using System.Reflection;
using log4net;
using HISWEBAPI.DTO.User;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Exceptions;
using System.Text;

namespace HISWEBAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
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
        public IActionResult sendPasswordResetOtp([FromBody] ForgotPasswordRequest request)
        {
            _log.Info($"sendPasswordResetOtp called. UserName={request.UserName}");

            try
            {
                if (!ModelState.IsValid)
                {
                    _log.Warn("Invalid model state for forgot password.");
                    return BadRequest(new { result = false, message = "Invalid input data.", errors = ModelState });
                }

                var (userExists, contactMatch, userId,registeredContact) = _userRepository.ValidateUserForPasswordReset(
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

                // Both username and contact match - validation successful
                _log.Info($"User validation successful for username: {request.UserName}");
                // Generate OTP
                //string otp = GenerateOtp();
                string otp = "123456";

                // Store OTP in database with 5 minute expiry
                bool otpStored = _userRepository.StoreOtpForPasswordReset(userId, otp, 5);

                if (!otpStored)
                {
                    _log.Error($"Failed to store OTP for user: {request.UserName}");
                    return StatusCode(500, new { result = false, message = "Failed to generate OTP. Please try again." });
                }

                // SendOtpViaSms(registeredContact, otp);

                // For development/testing, you can log the OTP
                _log.Info($"OTP generated for user {request.UserName}: {otp}");

                string contactHintSuccess = GenerateContactHint(registeredContact);

                _log.Info($"OTP sent successfully for username: {request.UserName}, UserId: {userId}");

                return Ok(new ForgotPasswordResponse
                {
                    Result = true,
                    Message = $"OTP sent successfully on contact no. {registeredContact}",
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
            return random.Next(100000, 999999).ToString(); // 6-digit OTP
        }

        private string GenerateContactHint(string contact)
        {
            if (string.IsNullOrEmpty(contact))
                return string.Empty;

            int length = contact.Length;

            // If contact is too short (less than 4 characters)
            if (length < 4)
            {
                return new string('*', length);
            }

            // Get first 2 and last 2 digits
            string first2 = contact.Substring(0, 2);
            string last2 = contact.Substring(length - 2, 2);

            // Create middle asterisks
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
                //string hashedPassword = HashPassword(request.NewPassword);
                string hashedPassword = request.NewPassword;

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

        // Password hashing helper method (if not already present)
        //private string HashPassword(string password)
        //{
        //    using (var sha256 = SHA256.Create())
        //    {
        //        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        //        return Convert.ToBase64String(hashedBytes);
        //    }
        //}

    }
}