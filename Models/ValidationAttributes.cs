using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SmartFleet.Models
{
    /// <summary>
    /// Validates that a date is in the future
    /// </summary>
    public class FutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success; // Allow null values

            if (value is DateTime date)
            {
                if (date <= DateTime.Now)
                {
                    return new ValidationResult(ErrorMessage ?? "Date must be in the future");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates that trip end date is after start date
    /// </summary>
    public class TripDateRangeAttribute : ValidationAttribute
    {
        private readonly string _startDatePropertyName;

        public TripDateRangeAttribute(string startDatePropertyName)
        {
            _startDatePropertyName = startDatePropertyName;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var startDateProperty = validationContext.ObjectType.GetProperty(_startDatePropertyName);
            if (startDateProperty == null)
            {
                return new ValidationResult($"Unknown property {_startDatePropertyName}");
            }

            var startDateValue = startDateProperty.GetValue(validationContext.ObjectInstance);
            if (startDateValue == null)
            {
                return ValidationResult.Success; // Let Required attribute handle this
            }

            if (value is DateTime endDate && startDateValue is DateTime startDate)
            {
                if (endDate <= startDate)
                {
                    return new ValidationResult(ErrorMessage ?? "End date must be after start date");
                }

                if (startDate <= DateTime.Now)
                {
                    return new ValidationResult(ErrorMessage ?? "Start date must be in the future");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates Egyptian phone number format
    /// </summary>
    public class EgyptianPhoneNumberAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success; // Let Required attribute handle this

            if (value is string phoneNumber)
            {
                var pattern = @"^01[0-2]\d{8}|015\d{8}$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, pattern))
                {
                    return new ValidationResult(ErrorMessage ?? "Invalid Egyptian phone number format. Use format: 01XXXXXXXXX");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates license plate format
    /// </summary>
    public class LicensePlateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success; // Let Required attribute handle this

            if (value is string licensePlate)
            {
                var pattern = @"^[A-Z0-9\s\-]+$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(licensePlate, pattern))
                {
                    return new ValidationResult(ErrorMessage ?? "License plate can only contain uppercase letters, numbers, spaces, and hyphens");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates that a number is positive
    /// </summary>
    public class PositiveNumberAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success; // Let Required attribute handle this

            if (value is int intValue)
            {
                if (intValue <= 0)
                {
                    return new ValidationResult(ErrorMessage ?? "Value must be positive");
                }
            }
            else if (value is decimal decimalValue)
            {
                if (decimalValue <= 0)
                {
                    return new ValidationResult(ErrorMessage ?? "Value must be positive");
                }
            }
            else if (value is double doubleValue)
            {
                if (doubleValue <= 0)
                {
                    return new ValidationResult(ErrorMessage ?? "Value must be positive");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates that coordinates are within valid ranges
    /// </summary>
    public class ValidCoordinatesAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success; // Let Required attribute handle this

            if (value is decimal coordinate)
            {
                // This is a simplified check - in practice you'd check both lat and lng together
                if (coordinate < -180 || coordinate > 180)
                {
                    return new ValidationResult(ErrorMessage ?? "Coordinate must be between -180 and 180 degrees");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates that a string contains only letters and spaces
    /// </summary>
    public class LettersAndSpacesAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success; // Let Required attribute handle this

            if (value is string text)
            {
                var pattern = @"^[a-zA-Z\s]+$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(text, pattern))
                {
                    return new ValidationResult(ErrorMessage ?? "Text can only contain letters and spaces");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates that a string contains only numbers
    /// </summary>
    public class NumbersOnlyAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success; // Let Required attribute handle this

            if (value is string text)
            {
                var pattern = @"^[0-9]+$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(text, pattern))
                {
                    return new ValidationResult(ErrorMessage ?? "Text can only contain numbers");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates that an email is unique across all users
    /// </summary>
    public class UniqueEmailAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success; // Let Required attribute handle this

            if (value is string email)
            {
                // This will be validated at the service level since we need database access
                // For now, we'll just validate the format
                if (!IsValidEmail(email))
                {
                    return new ValidationResult(ErrorMessage ?? "Invalid email format");
                }
            }

            return ValidationResult.Success;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Validates that a driver license number is unique
    /// </summary>
    public class UniqueDriverLicenseAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success; // Let Required attribute handle this

            if (value is string licenseNumber)
            {
                // This will be validated at the service level since we need database access
                // For now, we'll just validate the format
                var pattern = @"^[A-Z0-9\s\-]+$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(licenseNumber, pattern))
                {
                    return new ValidationResult(ErrorMessage ?? "License number can only contain uppercase letters, numbers, spaces, and hyphens");
                }
            }

            return ValidationResult.Success;
        }
    }
} 