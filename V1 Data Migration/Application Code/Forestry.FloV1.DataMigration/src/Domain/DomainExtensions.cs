using Domain.V1;
using Domain.V2;

namespace Domain;

public static class DomainExtensions
{
    public static UserAccount ToUserAccount(this FloUser value, string? customEmailAddressToUse)
    {
        var accountType = value.RoleName switch
        {
            "owner" => AccountType.WoodlandOwnerAdministrator,
            "agent" => AccountType.AgentAdministrator,
            _ => AccountType.FcUser
        };

        var email = string.IsNullOrWhiteSpace(customEmailAddressToUse)
            ? value.Email 
            : ConstructCustomEmailAddress(value, customEmailAddressToUse);

        return new UserAccount(
            accountType,
            email,
            value.FirstName,
            value.LastName,
            value.AddressLine1,
            value.AddressLine2,
            value.AddressLine3,
            $"{value.AddressLine4} {value.AddressLine5}".Trim(),
            value.PostalCode,
            value.TelephoneNumber,
            value.MobileTelephoneNumber);
    }

    public static WoodlandOwner ToWoodlandOwner(this FloUser value)
    {
        bool isOrganisation = string.IsNullOrWhiteSpace(value.CompanyName) is false;

        return new WoodlandOwner(
            value.AddressLine1,
            value.AddressLine2,
            value.AddressLine3,
            $"{value.AddressLine4} {value.AddressLine5}".Trim(),
            value.PostalCode,
            value.Email,
            $"{value.FirstName} {value.LastName}",
            value.TelephoneNumber,
            isOrganisation,
            value.CompanyName);
    }

    public static WoodlandOwner ToWoodlandOwner(this ManagedOwner value)
    {
        bool isOrganisation = string.IsNullOrWhiteSpace(value.CompanyName) is false;

        return new WoodlandOwner(
            value.AddressLine1,
            value.AddressLine2,
            value.AddressLine3,
            $"{value.AddressLine4} {value.AddressLine5}".Trim(),
            value.PostalCode,
            value.Email,
            $"{value.FirstName} {value.LastName}".Trim(),
            value.TelephoneNumber,
            isOrganisation,
            value.CompanyName);
    }

    public static Agency ToAgency(this FloUser value)
    {
        bool isOrganisation = string.IsNullOrWhiteSpace(value.CompanyName) is false;

        return new Agency(
            value.AddressLine1,
            value.AddressLine2,
            value.AddressLine3,
            $"{value.AddressLine4} {value.AddressLine5}".Trim(),
            value.PostalCode,
            value.Email,
            $"{value.FirstName} {value.LastName}",
            isOrganisation,
            value.CompanyName);
    }

    private static string ConstructCustomEmailAddress(FloUser flo1User, string customEmailAddressToUse)
    {
        var emailArray = customEmailAddressToUse.Split('@');
        return emailArray[0] + $"+{flo1User.UserId}@{emailArray[^1]}";
    }
}