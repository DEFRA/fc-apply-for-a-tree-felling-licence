using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Forestry.Flo.External.Web.Infrastructure;

/// <summary>
/// Validation for what makes a valid Postal Code.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class FloPostalCodeAttribute : DataTypeAttribute
{
    /// <inheritdoc />
    public FloPostalCodeAttribute() : base(DataType.PostalCode)
    {

    }

    /// <inheritdoc />
    public override bool IsValid(object? value)
    {
        return IsValidPostalCode(value as string);
    }

    public static bool IsValidPostalCode(string? value)
    {
        if (value == null) return true; // this should be handled by other validation

        Regex validPostalCode = new Regex(@"^([a-zA-Z]{1,2}\d{1,2})\s*?(\d[a-zA-Z]{2})$");

        return validPostalCode.IsMatch(value);
    }
}