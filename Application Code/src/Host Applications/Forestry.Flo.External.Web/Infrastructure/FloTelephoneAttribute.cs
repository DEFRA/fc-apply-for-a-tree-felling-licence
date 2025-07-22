using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Forestry.Flo.External.Web.Infrastructure;

/// <summary>
/// Validation for what makes a valid telephone number.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class FloTelephoneAttribute : DataTypeAttribute
{
    /// <inheritdoc />
    public FloTelephoneAttribute() : base(DataType.PhoneNumber)
    {

    }

    /// <inheritdoc />
    public override bool IsValid(object? value)
    {
        return IsValidTelephoneNumber(value as string);
    }

    public static bool IsValidTelephoneNumber(string? value)
    {
        if (value == null) return true; // this should be handled by other validation

        Regex validTelephone = new Regex(@"^(?:(?:\(?(?:0(?:0|11)\)?[\s-]?\(?|\+)44\)?[\s-]?(?:\(?0\)?[\s-]?)?)|(?:\(?0))(?:(?:\d{5}\)?[\s-]?\d{4,5})|(?:\d{4}\)?[\s-]?(?:\d{5}|\d{3}[\s-]?\d{3}))|(?:\d{3}\)?[\s-]?\d{3}[\s-]?\d{3,4})|(?:\d{2}\)?[\s-]?\d{4}[\s-]?\d{4}))(?:[\s-]?(?:x|ext\.?|\#)\d{3,4})?$");

        return validTelephone.IsMatch(value);
    }
}