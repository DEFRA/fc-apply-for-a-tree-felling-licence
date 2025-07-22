using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Forestry.Flo.Internal.Web.Infrastructure;

namespace Forestry.Flo.Internal.Web.Models;

/// <summary>
/// Model class representing an address
/// <remarks>See https://design-system.service.gov.uk/patterns/addresses/</remarks>
/// </summary>
[Owned]
public class Address
{
    [DisplayName("Address line 1")]
    [Required(ErrorMessage = "Enter address line 1, typically the building and street")]
    [StringLength(DataValueConstants.AddressLineMaxLength)]
    public string? Line1 { get; set; }

    [DisplayName("Address line 2")]
    [DisplayAsOptional]
    [StringLength(DataValueConstants.AddressLineMaxLength)]
    public string? Line2 { get; set; }

    [DisplayName("Town or city")]
    [Required(ErrorMessage = "Enter town or city")]
    [StringLength(DataValueConstants.AddressLineMaxLength)]
    [RegularExpression("^[-a-zA-Z0-9'\\s]*$", ErrorMessage = "Town or city must only include letters a to z, numbers, and special characters such as hyphens, spaces and apostrophes")]
    public string? Line3 { get; set; }

    [DisplayName("County")]
    [StringLength(DataValueConstants.AddressLineMaxLength)]
    [DisplayAsOptional]
    [RegularExpression("^[-a-zA-Z0-9'\\s]*$", ErrorMessage = "County must only include letters a to z, numbers, and special characters such as hyphens, spaces and apostrophes")]
    public string? Line4 { get; set; }

    [DisplayName("Postcode")]
    [Required(ErrorMessage = "Enter a full UK postcode in the correct format, like SW1A 2AA")]
    [FloPostalCode(ErrorMessage = "Enter a full UK postcode in the correct format, like SW1A 2AA")]
    [StringLength(DataValueConstants.PostalCodeMaxLength)]
    public string? PostalCode { get; set; }

    public override bool Equals(object? obj)
    {
        var other = obj as Address;
        if (other == null) return false;

        return Line1 == other.Line1
               && Line2 == other.Line2
               && Line3 == other.Line3
               && Line4 == other.Line4
               && PostalCode == other.PostalCode;
    }
}