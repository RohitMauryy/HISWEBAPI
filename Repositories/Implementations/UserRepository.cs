using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using log4net;
using Microsoft.Data.SqlClient;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Data.Helpers;
using HISWEBAPI.Models;
using HISWEBAPI.DTO;
using HISWEBAPI.Services;
using HISWEBAPI.Utilities;
using HISWEBAPI.Exceptions;
using HISWEBAPI.Services.Interfaces;

namespace HISWEBAPI.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly ICustomSqlHelper _sqlHelper;
        private readonly IResponseMessageService _messageService;
        private readonly ISmsService _smsService;
        private readonly IEmailService _emailService;
        private readonly IJwtService _jwtService;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public UserRepository(
            ICustomSqlHelper sqlHelper,
            IResponseMessageService messageService,
            ISmsService smsService,
            IEmailService emailService,
            IJwtService jwtService)
        {
            _sqlHelper = sqlHelper;
            _messageService = messageService;
            _smsService = smsService;
            _emailService = emailService;
            _jwtService = jwtService;
        }

        public ServiceResult<UserLoginResponseData> UserLogin(UserLoginRequest request)
        {
            try
            {
                var dataTable = _sqlHelper.GetDataTable("sp_S_Login", CommandType.StoredProcedure, new
                {
                    BranchId = request.BranchId,
                    UserName = request.UserName
                });

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_CREDENTIALS");
                    _log.Warn($"Invalid credentials for UserName={request.UserName}");
                    return ServiceResult<UserLoginResponseData>.Failure(
                        alert.Type,
                        alert.Message,
                        401
                    );
                }

                DataRow userRow = dataTable.Rows[0];
                string storedHash = Convert.ToString(userRow["Password"]);
                bool isPasswordValid = PasswordHasher.VerifyPassword(request.Password, storedHash);

                if (!isPasswordValid)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_CREDENTIALS");
                    _log.Warn($"Invalid password for UserName={request.UserName}");
                    return ServiceResult<UserLoginResponseData>.Failure(
                        alert.Type,
                        alert.Message,
                        401
                    );
                }

                int userId = (int)Convert.ToInt64(userRow["Id"]);
                string accessToken = _jwtService.GenerateToken(userId.ToString(), request.UserName);
                string refreshToken = _jwtService.GenerateRefreshToken();

                var responseData = new UserLoginResponseData
                {
                    userId = userId,
                    userName = request.UserName,
                    email = Convert.ToString(userRow["Email"]),
                    contact = Convert.ToString(userRow["Contact"]),
                    isContactVerified = Convert.ToBoolean(userRow["IsContactVerified"]),
                    isEmailVerified = Convert.ToBoolean(userRow["IsEmailVerified"]),
                    branchId = request.BranchId,
                    accessToken = accessToken,
                    refreshToken = refreshToken,
                    tokenType = "Bearer",
                    expiresIn = 3600
                };

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("LOGIN_SUCCESS");
                _log.Info($"Login successful for UserId={userId}");

                return ServiceResult<UserLoginResponseData>.Success(
                    responseData,
                    alert1.Type,
                    alert1.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<UserLoginResponseData>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<TokenResponseData> RefreshToken(RefreshTokenRequest request)
        {
            try
            {
                var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
                if (principal == null)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_ACCESS_TOKEN");
                    _log.Warn("Invalid access token provided for refresh");
                    return ServiceResult<TokenResponseData>.Failure(
                        alert.Type,
                        alert.Message,
                        400
                    );
                }

                var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var username = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

                var (isValid, sessionId, userIdFromToken) = ValidateRefreshTokenInternal(request.RefreshToken);

                if (!isValid || userIdFromToken.ToString() != userId)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_REFRESH_TOKEN");
                    _log.Warn($"Invalid refresh token for UserId={userId}");
                    return ServiceResult<TokenResponseData>.Failure(
                        alert.Type,
                        alert.Message,
                        401
                    );
                }

                var newAccessToken = _jwtService.GenerateToken(userId ?? "", username ?? "");
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                bool tokenSaved = SaveRefreshTokenInternal(userIdFromToken, sessionId, newRefreshToken, DateTime.UtcNow.AddDays(7));

                if (!tokenSaved)
                {
                    _log.Error($"Failed to save refresh token for sessionId={sessionId}");
                }

                var responseData = new TokenResponseData
                {
                    accessToken = newAccessToken,
                    refreshToken = newRefreshToken,
                    tokenType = "Bearer",
                    expiresIn = 3600
                };

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("TOKEN_REFRESHED");
                _log.Info($"Token refreshed successfully for UserId={userId}");

                return ServiceResult<TokenResponseData>.Success(
                    responseData,
                    alert1.Type,
                    alert1.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("TOKEN_REFRESH_ERROR");
                return ServiceResult<TokenResponseData>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<string> Logout(LogoutRequest request)
        {
            try
            {
                if (request?.SessionId > 0)
                {
                    bool sessionUpdated = UpdateLoginSessionInternal(request.SessionId, "Logged Out", request.LogoutReason ?? "User Logout");
                    bool tokenInvalidated = InvalidateRefreshTokenInternal(request.SessionId);

                    if (!sessionUpdated || !tokenInvalidated)
                    {
                        _log.Warn($"Logout partially failed for SessionId={request.SessionId}");
                    }
                }

                var alert = _messageService.GetMessageAndTypeByAlertCode("LOGOUT_SUCCESS");
                _log.Info($"User logged out successfully. SessionId={request.SessionId}");

                return ServiceResult<string>.Success(
                    "Logged out successfully",
                    alert.Type,
                    alert.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("LOGOUT_ERROR");
                return ServiceResult<string>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<UserSignupResponseData> NewUserSignUp(UserSignupRequest request)
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@Address", request.Address ?? (object)DBNull.Value),
                    new SqlParameter("@Contact", request.Contact ?? (object)DBNull.Value),
                    new SqlParameter("@DOB", request.DOB != default(DateTime) ? (object)request.DOB : DBNull.Value),
                    new SqlParameter("@Email", request.Email ?? (object)DBNull.Value),
                    new SqlParameter("@FirstName", request.FirstName),
                    new SqlParameter("@MidelName", request.MiddleName ?? (object)DBNull.Value),
                    new SqlParameter("@LastName", request.LastName ?? (object)DBNull.Value),
                    new SqlParameter("@Password", PasswordHasher.HashPassword(request.Password)),
                    new SqlParameter("@UserName", request.UserName),
                    new SqlParameter("@Gender", request.Gender ?? (object)DBNull.Value),
                    new SqlParameter("@Result", SqlDbType.BigInt) { Direction = ParameterDirection.Output }
                };

                long result = _sqlHelper.RunProcedureInsert("I_NewUserSignUp", parameters);

                if (result == -1)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("USERNAME_EXISTS");
                    _log.Warn($"Duplicate username attempted: {request.UserName}");
                    return ServiceResult<UserSignupResponseData>.Failure(
                        alert.Type,
                        alert.Message,
                        409
                    );
                }

                if (result == -2)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("LICENSE_LIMIT_REACHED");
                    _log.Warn("License validation failed for user insert");
                    return ServiceResult<UserSignupResponseData>.Failure(
                        alert.Type,
                        alert.Message,
                        400
                    );
                }

                if (result > 0)
                {
                    var responseData = new UserSignupResponseData { userId = result };
                    var alert = _messageService.GetMessageAndTypeByAlertCode("USER_CREATED");
                    _log.Info($"User inserted successfully. UserId={result}");
                    return ServiceResult<UserSignupResponseData>.Success(
                        responseData,
                        alert.Type,
                        alert.Message,
                        201
                    );
                }

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("USER_SAVE_FAILED");
                _log.Error("Failed to insert user. Result=0");
                return ServiceResult<UserSignupResponseData>.Failure(
                    alert1.Type,
                    alert1.Message,
                    500
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SIGNUP_ERROR");
                return ServiceResult<UserSignupResponseData>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<SmsOtpResponseData> SendSmsOtp(SendSmsOtpRequest request)
        {
            try
            {
                var (userExists, contactMatch, userId, registeredContact) = ValidateUserForPasswordResetInternal(request.UserName, request.Contact);

                if (!userExists)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("USERNAME_NOT_FOUND");
                    _log.Warn($"Username not found: {request.UserName}");
                    return ServiceResult<SmsOtpResponseData>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                if (!contactMatch)
                {
                    string contactHint = GenerateContactHint(registeredContact);
                    var alert = _messageService.GetMessageAndTypeByAlertCode("CONTACT_NOT_MATCH");
                    _log.Warn($"Contact mismatch for username: {request.UserName}");

                    var failureData = new SmsOtpResponseData { contactHint = contactHint };
                    return ServiceResult<SmsOtpResponseData>.Failure(
                        alert.Type,
                        $"{alert.Message} to {contactHint}",
                        400
                    );
                }

                string otp = GenerateOtp();
                bool otpStored = StoreOtpForPasswordResetInternal(userId, otp, 5);

                if (!otpStored)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("OTP_GENERATION_FAILED");
                    _log.Error($"Failed to store OTP for user: {request.UserName}");
                    return ServiceResult<SmsOtpResponseData>.Failure(
                        alert.Type,
                        alert.Message,
                        500
                    );
                }

                bool smsSent = _smsService.SendOtp(registeredContact, otp);

                if (!smsSent)
                {
                    _log.Warn($"OTP stored but SMS failed for user: {request.UserName}");
                }

                string contactHintSuccess = GenerateContactHint(registeredContact);
                var responseData = new SmsOtpResponseData
                {
                    userId = userId,
                    contactHint = contactHintSuccess
                };

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OTP_SENT_SMS");
                _log.Info($"OTP sent successfully for username: {request.UserName}, UserId: {userId}");

                return ServiceResult<SmsOtpResponseData>.Success(
                    responseData,
                    alert1.Type,
                    $"{alert1.Message} to {contactHintSuccess}",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SMS_OTP_ERROR");
                return ServiceResult<SmsOtpResponseData>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<OtpVerificationResponseData> VerifySmsOtp(VerifySmsOtpRequest request)
        {
            try
            {
                var (result, message) = VerifySmsOtpInternal(request.UserId, request.Otp);

                var responseData = new OtpVerificationResponseData
                {
                    userId = request.UserId,
                    otp = request.Otp
                };

                switch (result)
                {
                    case 1:
                        var alert = _messageService.GetMessageAndTypeByAlertCode("OTP_VERIFIED");
                        _log.Info($"OTP verified successfully for UserId: {request.UserId}");
                        return ServiceResult<OtpVerificationResponseData>.Success(
                            responseData,
                            alert.Type,
                            alert.Message,
                            200
                        );

                    case -1:
                        var alert1 = _messageService.GetMessageAndTypeByAlertCode("USER_NOT_FOUND_OTP");
                        _log.Warn($"User not found: UserId={request.UserId}");
                        return ServiceResult<OtpVerificationResponseData>.Failure(
                            alert1.Type,
                            alert1.Message,
                            404
                        );

                    case -2:
                        var alert2 = _messageService.GetMessageAndTypeByAlertCode("USER_INACTIVE");
                        _log.Warn($"Inactive user attempted OTP verification: UserId={request.UserId}");
                        return ServiceResult<OtpVerificationResponseData>.Failure(
                            alert2.Type,
                            alert2.Message,
                            400
                        );

                    case -3:
                    case -4:
                    case -5:
                        var alert3 = _messageService.GetMessageAndTypeByAlertCode("OTP_VALIDATION_FAILED");
                        _log.Warn($"OTP validation failed for UserId {request.UserId}: {message}");
                        return ServiceResult<OtpVerificationResponseData>.Failure(
                            alert3.Type,
                            alert3.Message,
                            400
                        );

                    case -6:
                        var alert4 = _messageService.GetMessageAndTypeByAlertCode("INVALID_OTP");
                        _log.Warn($"Invalid OTP entered for UserId: {request.UserId}");
                        return ServiceResult<OtpVerificationResponseData>.Failure(
                            alert4.Type,
                            alert4.Message,
                            400
                        );

                    default:
                        var alert5 = _messageService.GetMessageAndTypeByAlertCode("OTP_VERIFICATION_ERROR");
                        _log.Error($"Unknown error during OTP verification for UserId: {request.UserId}. Result code: {result}");
                        return ServiceResult<OtpVerificationResponseData>.Failure(
                            alert5.Type,
                            alert5.Message,
                            500
                        );
                }
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("VERIFY_OTP_ERROR");
                return ServiceResult<OtpVerificationResponseData>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<EmailOtpResponseData> SendEmailOtp(SendEmailOtpRequest request)
        {
            try
            {
                var (userExists, emailMatch, userId, registeredEmail) = ValidateUserForEmailPasswordResetInternal(request.UserName, request.Email);

                if (!userExists)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("USERNAME_NOT_FOUND");
                    _log.Warn($"Username not found: {request.UserName}");
                    return ServiceResult<EmailOtpResponseData>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                if (!emailMatch)
                {
                    string emailHint = GenerateEmailHint(registeredEmail);
                    var alert = _messageService.GetMessageAndTypeByAlertCode("EMAIL_NOT_MATCH");
                    _log.Warn($"Email mismatch for username: {request.UserName}");

                    var failureData = new EmailOtpResponseData { emailHint = emailHint };
                    return ServiceResult<EmailOtpResponseData>.Failure(
                        alert.Type,
                        $"{alert.Message} to {emailHint}",
                        400
                    );
                }

                string otp = GenerateOtp();
                bool otpStored = StoreEmailOtpForPasswordResetInternal(userId, otp, 5);

                if (!otpStored)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("OTP_GENERATION_FAILED");
                    _log.Error($"Failed to store OTP for user: {request.UserName}");
                    return ServiceResult<EmailOtpResponseData>.Failure(
                        alert.Type,
                        alert.Message,
                        500
                    );
                }

                bool emailSent = _emailService.SendOtpEmail(registeredEmail, otp, "Password Reset").GetAwaiter().GetResult();

                if (!emailSent)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("EMAIL_SEND_FAILED");
                    _log.Error($"Failed to send email to: {registeredEmail}");
                    return ServiceResult<EmailOtpResponseData>.Failure(
                        alert.Type,
                        alert.Message,
                        500
                    );
                }

                string emailHintSuccess = GenerateEmailHint(registeredEmail);
                var responseData = new EmailOtpResponseData
                {
                    userId = userId,
                    emailHint = emailHintSuccess
                };

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OTP_SENT_EMAIL");
                _log.Info($"OTP sent successfully via email for username: {request.UserName}, UserId: {userId}");

                return ServiceResult<EmailOtpResponseData>.Success(
                    responseData,
                    alert1.Type,
                    $"{alert1.Message} to {emailHintSuccess}",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("EMAIL_OTP_ERROR");
                return ServiceResult<EmailOtpResponseData>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<string> VerifyEmailOtp(VerifyEmailOtpRequest request)
        {
            try
            {
                var (result, message) = VerifyEmailOtpInternal(request.UserId, request.Otp);

                switch (result)
                {
                    case 1:
                        var alert = _messageService.GetMessageAndTypeByAlertCode("EMAIL_OTP_VERIFIED");
                        _log.Info($"Email OTP verified successfully for UserId: {request.UserId}");
                        return ServiceResult<string>.Success(
                            "Email OTP verified",
                            alert.Type,
                            alert.Message,
                            200
                        );

                    case -1:
                        var alert1 = _messageService.GetMessageAndTypeByAlertCode("USER_NOT_FOUND_OTP");
                        _log.Warn($"User not found: UserId={request.UserId}");
                        return ServiceResult<string>.Failure(
                            alert1.Type,
                            alert1.Message,
                            404
                        );

                    case -2:
                        var alert2 = _messageService.GetMessageAndTypeByAlertCode("USER_INACTIVE");
                        _log.Warn($"Inactive user attempted OTP verification: UserId={request.UserId}");
                        return ServiceResult<string>.Failure(
                            alert2.Type,
                            alert2.Message,
                            400
                        );

                    case -3:
                    case -4:
                    case -5:
                        var alert3 = _messageService.GetMessageAndTypeByAlertCode("EMAIL_OTP_VALIDATION_FAILED");
                        _log.Warn($"Email OTP validation failed for UserId {request.UserId}: {message}");
                        return ServiceResult<string>.Failure(
                            alert3.Type,
                            alert3.Message,
                            400
                        );

                    case -6:
                        var alert4 = _messageService.GetMessageAndTypeByAlertCode("INVALID_EMAIL_OTP");
                        _log.Warn($"Invalid email OTP entered for UserId: {request.UserId}");
                        return ServiceResult<string>.Failure(
                            alert4.Type,
                            alert4.Message,
                            400
                        );

                    default:
                        var alert5 = _messageService.GetMessageAndTypeByAlertCode("EMAIL_OTP_VERIFICATION_ERROR");
                        _log.Error($"Unknown error during email OTP verification for UserId: {request.UserId}. Result code: {result}");
                        return ServiceResult<string>.Failure(
                            alert5.Type,
                            alert5.Message,
                            500
                        );
                }
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("VERIFY_EMAIL_OTP_ERROR");
                return ServiceResult<string>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<string> ResetPasswordByUserId(ResetPasswordRequest request)
        {
            try
            {
                if (request.NewPassword != request.ConfirmPassword)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("PASSWORD_MISMATCH");
                    _log.Warn($"Password mismatch for UserId: {request.UserId}");
                    return ServiceResult<string>.Failure(
                        alert.Type,
                        alert.Message,
                        400
                    );
                }

                string hashedPassword = PasswordHasher.HashPassword(request.NewPassword);
                var (result, message) = ResetPasswordByUserIdInternal(request.UserId, request.Otp, hashedPassword);

                if (result)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("PASSWORD_RESET_SUCCESS");
                    _log.Info($"Password reset successfully for UserId: {request.UserId}");
                    return ServiceResult<string>.Success(
                        "Password reset successfully",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }
                else
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("PASSWORD_RESET_FAILED");
                    _log.Warn($"Password reset failed for UserId: {request.UserId}. Message: {message}");
                    return ServiceResult<string>.Failure(
                        alert.Type,
                        alert.Message,
                        400
                    );
                }
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("RESET_PASSWORD_ERROR");
                return ServiceResult<string>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<string> UpdatePassword(UpdatePasswordRequest request)
        {
            try
            {
                var dataTable = _sqlHelper.GetDataTable("S_GetUserByUserId", CommandType.StoredProcedure, new
                {
                    @userId = request.UserId
                });

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("USER_NOT_FOUND");
                    _log.Warn($"User not found for UserId: {request.UserId}");
                    return ServiceResult<string>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                string storedHash = dataTable.Rows[0]["Password"].ToString();
                bool isValid = PasswordHasher.VerifyPassword(request.CurrentPassword, storedHash);

                if (!isValid)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("CURRENT_PASSWORD_INCORRECT");
                    _log.Warn($"Current password incorrect for UserId: {request.UserId}");
                    return ServiceResult<string>.Failure(
                        alert.Type,
                        alert.Message,
                        400
                    );
                }

                string newHashedPassword = PasswordHasher.HashPassword(request.NewPassword);
                var result = _sqlHelper.DML("U_UserPasswords", CommandType.StoredProcedure, new
                {
                    @userId = request.UserId,
                    @newPassword = newHashedPassword
                });

                if (result < 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("PASSWORD_UPDATE_FAILED");
                    _log.Error($"Password update failed for UserId: {request.UserId}");
                    return ServiceResult<string>.Failure(
                        alert.Type,
                        alert.Message,
                        500
                    );
                }

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("PASSWORD_UPDATED");
                _log.Info($"Password updated successfully for UserId: {request.UserId}");
                return ServiceResult<string>.Success(
                    "Password updated",
                    alert1.Type,
                    alert1.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("UPDATE_PASSWORD_ERROR");
                return ServiceResult<string>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<UserRoleModel>> GetUserRoles(UserRoleRequest request)
        {
            try
            {
                var dataTable = _sqlHelper.GetDataTable("S_GetUserRoles", CommandType.StoredProcedure, new
                {
                    @branchId = request.BranchId,
                    @userId = request.UserId
                });

                var roles = dataTable?.AsEnumerable().Select(row => new UserRoleModel
                {
                    RoleId = row.Field<int>("RoleId"),
                    RoleName = row.Field<string>("RoleName")
                }).ToList() ?? new List<UserRoleModel>();

                if (!roles.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("NO_ROLES_FOUND");
                    _log.Warn($"No roles found for UserId: {request.UserId}");
                    return ServiceResult<IEnumerable<UserRoleModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("ROLES_RETRIEVED");
                _log.Info($"User roles retrieved successfully for UserId: {request.UserId}");
                return ServiceResult<IEnumerable<UserRoleModel>>.Success(
                    roles,
                    alert1.Type,
                    alert1.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("GET_ROLES_ERROR");
                return ServiceResult<IEnumerable<UserRoleModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        #region Private Helper Methods

        private (bool userExists, bool contactMatch, long userId, string registeredContact) ValidateUserForPasswordResetInternal(string userName, string contact)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserName", userName),
                new SqlParameter("@Contact", contact),
                new SqlParameter("@UserExists", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                new SqlParameter("@ContactMatch", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                new SqlParameter("@UserId", SqlDbType.BigInt) { Direction = ParameterDirection.Output },
                new SqlParameter("@RegisteredContact", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Output }
            };

            _sqlHelper.RunProcedure("sp_ValidateUserForPasswordReset", parameters);

            bool userExists = parameters[2].Value != DBNull.Value && (bool)parameters[2].Value;
            bool contactMatch = parameters[3].Value != DBNull.Value && (bool)parameters[3].Value;
            long userId = parameters[4].Value != DBNull.Value ? Convert.ToInt64(parameters[4].Value) : 0;
            string registeredContact = parameters[5].Value != DBNull.Value ? parameters[5].Value.ToString() : string.Empty;

            return (userExists, contactMatch, userId, registeredContact);
        }

        private bool StoreOtpForPasswordResetInternal(long userId, string otp, int expiryMinutes)
        {
            try
            {
                var result = _sqlHelper.GetDataTable("sp_StoreOtpForPasswordReset", CommandType.StoredProcedure,
                    new { UserId = userId, Otp = otp, ExpiryMinutes = expiryMinutes });

                if (result != null && result.Rows.Count > 0)
                {
                    int resultValue = Convert.ToInt32(result.Rows[0]["Result"]);
                    return resultValue == 1;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private (int result, string message) VerifySmsOtpInternal(long userId, string otp)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Otp", otp),
                new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output },
                new SqlParameter("@Message", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output }
            };

            _sqlHelper.RunProcedure("sp_VerifySmsOtp", parameters);

            int result = parameters[2].Value != DBNull.Value ? Convert.ToInt32(parameters[2].Value) : 0;
            string message = parameters[3].Value != DBNull.Value ? parameters[3].Value.ToString() : "Unknown error";

            return (result, message);
        }

        private (bool userExists, bool emailMatch, long userId, string registeredEmail) ValidateUserForEmailPasswordResetInternal(string userName, string email)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserName", userName),
                new SqlParameter("@Email", email),
                new SqlParameter("@UserExists", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                new SqlParameter("@EmailMatch", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                new SqlParameter("@UserId", SqlDbType.BigInt) { Direction = ParameterDirection.Output },
                new SqlParameter("@RegisteredEmail", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output }
            };

            _sqlHelper.RunProcedure("sp_ValidateUserForEmailPasswordReset", parameters);

            bool userExists = parameters[2].Value != DBNull.Value && (bool)parameters[2].Value;
            bool emailMatch = parameters[3].Value != DBNull.Value && (bool)parameters[3].Value;
            long userId = parameters[4].Value != DBNull.Value ? Convert.ToInt64(parameters[4].Value) : 0;
            string registeredEmail = parameters[5].Value != DBNull.Value ? parameters[5].Value.ToString() : string.Empty;

            return (userExists, emailMatch, userId, registeredEmail);
        }

        private bool StoreEmailOtpForPasswordResetInternal(long userId, string otp, int expiryMinutes)
        {
            try
            {
                var result = _sqlHelper.GetDataTable("sp_StoreEmailOtpForPasswordReset", CommandType.StoredProcedure,
                    new { UserId = userId, Otp = otp, ExpiryMinutes = expiryMinutes });

                if (result != null && result.Rows.Count > 0)
                {
                    int resultValue = Convert.ToInt32(result.Rows[0]["Result"]);
                    return resultValue == 1;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private (int result, string message) VerifyEmailOtpInternal(long userId, string otp)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Otp", otp),
                new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output },
                new SqlParameter("@Message", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output }
            };

            _sqlHelper.RunProcedure("sp_VerifyEmailOtp", parameters);

            int result = parameters[2].Value != DBNull.Value ? Convert.ToInt32(parameters[2].Value) : 0;
            string message = parameters[3].Value != DBNull.Value ? parameters[3].Value.ToString() : "Unknown error";

            return (result, message);
        }

        private (bool result, string message) ResetPasswordByUserIdInternal(long userId, string otp, string hashedPassword)
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@otp", otp),
                    new SqlParameter("@NewPassword", hashedPassword),
                    new SqlParameter("@Result", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                    new SqlParameter("@Message", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output }
                };

                _sqlHelper.RunProcedure("sp_ResetPasswordByUserId", parameters);

                bool result = parameters[3].Value != DBNull.Value && (bool)parameters[3].Value;
                string message = parameters[4].Value != DBNull.Value ? parameters[4].Value.ToString() : "Unknown error";

                return (result, message);
            }
            catch (Exception ex)
            {
                return (false, "Password reset failed");
            }
        }

        private bool UpdateLoginSessionInternal(long sessionId, string status, string logoutReason = null)
        {
            try
            {
                var result = _sqlHelper.DML("sp_U_UserLoginSession", CommandType.StoredProcedure, new
                {
                    @SessionId = sessionId,
                    @Status = status,
                    @LogoutReason = logoutReason ?? (object)DBNull.Value
                });

                return result > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool SaveRefreshTokenInternal(long userId, long sessionId, string refreshToken, DateTime expiryDate)
        {
            try
            {
                var result = _sqlHelper.DML("sp_I_UserRefreshToken", CommandType.StoredProcedure, new
                {
                    @UserId = userId,
                    @SessionId = sessionId,
                    @RefreshToken = refreshToken,
                    @ExpiryDate = expiryDate
                });

                return result > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private (bool isValid, long sessionId, long userId) ValidateRefreshTokenInternal(string refreshToken)
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@RefreshToken", refreshToken),
                    new SqlParameter("@IsValid", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                    new SqlParameter("@SessionId", SqlDbType.BigInt) { Direction = ParameterDirection.Output },
                    new SqlParameter("@UserId", SqlDbType.BigInt) { Direction = ParameterDirection.Output }
                };

                _sqlHelper.RunProcedure("sp_ValidateRefreshToken", parameters);

                bool isValid = parameters[1].Value != DBNull.Value && (bool)parameters[1].Value;
                long sessionId = parameters[2].Value != DBNull.Value ? Convert.ToInt64(parameters[2].Value) : 0;
                long userId = parameters[3].Value != DBNull.Value ? Convert.ToInt64(parameters[3].Value) : 0;

                return (isValid, sessionId, userId);
            }
            catch (Exception ex)
            {
                return (false, 0, 0);
            }
        }

        private bool InvalidateRefreshTokenInternal(long sessionId)
        {
            try
            {
                var result = _sqlHelper.DML("sp_InvalidateRefreshToken", CommandType.StoredProcedure, new
                {
                    @SessionId = sessionId
                });

                return result > 0;
            }
            catch (Exception ex)
            {
                return false;
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
    }
}