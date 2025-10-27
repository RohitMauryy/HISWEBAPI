using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using HISWEBAPI.DTO.Configuration;

namespace HISWEBAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PasswordPolicyAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return new ValidationResult("Password is required");

            string password = value.ToString();

            // Access appsettings.json via DI
            var configuration = (IConfiguration)validationContext
                .GetService(typeof(IConfiguration));

            var policy = configuration.GetSection("PasswordPolicy").Get<PasswordPolicy>();

            if (policy == null)
                return new ValidationResult("Password policy not configured properly.");

            // Check length
            if (password.Length < policy.MinLength || password.Length > policy.MaxLength)
                return new ValidationResult($"Password must be between {policy.MinLength} and {policy.MaxLength} characters long.");

            // Check pattern
            if (!Regex.IsMatch(password, policy.Regex))
                return new ValidationResult(policy.ErrorMessage);

            return ValidationResult.Success;
        }
    }
}
