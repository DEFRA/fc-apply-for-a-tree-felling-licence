using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Infrastructure;

/// <summary>
/// Specifies that a field is required if another specified field is null or empty.
/// </summary>
public class RequiredIfOtherFieldEmptyAttribute : ValidationAttribute
{
    private readonly string _otherPropertyName;
    private readonly string _errorMessage;

    /// <summary>
    /// Initializes a new instance of the RequiredIfOtherFieldEmptyAttribute class.
    /// </summary>
    /// <param name="otherPropertyName">The name of the other property that will be checked.</param>
    /// <param name="errorMessage">The error message to associate with a validation control if validation fails.</param>
    public RequiredIfOtherFieldEmptyAttribute(string otherPropertyName, string errorMessage)
    {
        _otherPropertyName = otherPropertyName;
        _errorMessage = errorMessage;
    }

    /// <summary>
    /// Checks whether the specified value is valid with respect to the current validation attribute.
    /// </summary>
    /// <param name="value">The value of the object to validate.</param>
    /// <param name="validationContext">The context information about the validation operation.</param>
    /// <returns>An instance of the ValidationResult class.</returns>
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        var otherProperty = validationContext.ObjectInstance.GetType().GetProperty(_otherPropertyName);
        var otherPropertyValue = otherProperty?.GetValue(validationContext.ObjectInstance, null)?.ToString();

        if (value == null && string.IsNullOrWhiteSpace(otherPropertyValue))
        {
            return new ValidationResult(_errorMessage);
        }

        return ValidationResult.Success;
    }
}