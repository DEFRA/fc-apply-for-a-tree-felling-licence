using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.Applicants.Entities;

/// <summary>
/// Model class representing an address
/// <remarks>See https://design-system.service.gov.uk/patterns/addresses/</remarks>
/// </summary>
[Owned]
public class Address
{
    /// <summary>
    /// Gets and sets the first line of the address.
    /// </summary>
    public string? Line1 { get; protected set; }

    /// <summary>
    /// Gets and sets the second line of the address.
    /// </summary>
    public string? Line2 { get; protected set; }

    /// <summary>
    /// Gets and sets the third line of the address.
    /// </summary>
    public string? Line3 { get; protected set; }

    /// <summary>
    /// Gets and sets the fourth line of the address.
    /// </summary>
    public string? Line4 { get; protected set; }

    /// <summary>
    /// Gets and sets the postal code of the address.
    /// </summary>
    public string? PostalCode { get; protected set; }

    protected Address()
    {
    }

    public Address(
        string? line1,
        string? line2,
        string? line3,
        string? line4,
        string? postalCode)
    {
        Line1 = line1;
        Line2 = line2;
        Line3 = line3;
        Line4 = line4;
        PostalCode = postalCode;
    }
}