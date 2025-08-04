namespace Domain.V2;

public record Agency(
    string? AddressLine1,
    string? AddressLine2,
    string? AddressLine3,
    string? AddressLine4,
    string? PostalCode,
    string? Email,
    string? ContactName,
    bool IsOrganisation,
    string? OrganisationName);