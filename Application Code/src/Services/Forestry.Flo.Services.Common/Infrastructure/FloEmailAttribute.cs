using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using EmailValidation;

namespace Forestry.Flo.Services.Common.Infrastructure;

/// <summary>
/// Validation for what makes a valid email address.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class FloEmailAddressAttribute : ValidationAttribute
{
    /// <summary>
    /// Instantiates a new instance of <see cref="T:EmailValidation.EmailAttribute" />.
    /// </summary>
    /// <remarks>
    /// Creates a new <see cref="T:EmailValidation.EmailAttribute" />.
    /// </remarks>
    /// <param name="allowTopLevelDomains"><c>true</c> if the validator should allow addresses at top-level domains; otherwise, <c>false</c>.</param>
    /// <param name="allowInternational"><c>true</c> if the validator should allow international characters; otherwise, <c>false</c>.</param>
    public FloEmailAddressAttribute(bool allowTopLevelDomains = false, bool allowInternational = false)
    {
        this.AllowTopLevelDomains = allowTopLevelDomains;
        this.AllowInternational = allowInternational;
    }

    /// <summary>
    /// Get or set whether or not the validator should allow top-level domains.
    /// </summary>
    /// <remarks>
    /// Gets or sets whether or not the validator should allow top-level domains.
    /// </remarks>
    /// <value><c>true</c> if top-level domains should be allowed; otherwise, <c>false</c>.</value>
    public bool AllowTopLevelDomains { get; set; }

    /// <summary>
    /// Get or set whether or not the validator should allow international characters.
    /// </summary>
    /// <remarks>
    /// Gets or sets whether or not the validator should allow international characters.
    /// </remarks>
    /// <value><c>true</c> if international characters should be allowed; otherwise, <c>false</c>.</value>
    public bool AllowInternational { get; set; }

    /// <summary>Validates the value.</summary>
    /// <remarks>
    /// Checks whether or not the email address provided is syntactically correct.
    /// </remarks>
    /// <returns>The validation result.</returns>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">The validation context.</param>
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        var memberNames = new string[1]
        {
            validationContext?.MemberName ?? nameof (value)
        };
        
        return (value is null || EmailValidator.Validate((string)value, this.AllowTopLevelDomains, this.AllowInternational) ? ValidationResult.Success : new ValidationResult(ErrorMessage, memberNames))!;
    }

    /// <summary>Validates the value.</summary>
    /// <remarks>
    /// Checks whether or not the email address provided is syntactically correct.
    /// </remarks>
    /// <returns><c>true</c> if the value is a valid email address; otherwise, <c>false</c>.</returns>
    /// <param name="value">The value to validate.</param>
    public override bool IsValid(object? value) => value is null || EmailValidator.Validate((string)value, this.AllowTopLevelDomains, this.AllowInternational);
}