namespace Domain.V2;

public record WoodlandOwner(
    string? AddressLine1,
    string? AddressLine2,
    string? AddressLine3,
    string? AddressLine4,
    string? PostalCode,
    string? Email,
    string? ContactName,
    string? ContactTelephone,
    bool IsOrganisation,
    string? OrganisationName);