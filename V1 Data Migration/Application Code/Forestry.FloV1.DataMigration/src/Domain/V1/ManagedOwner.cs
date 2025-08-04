namespace Domain.V1;

public record ManagedOwner(
    long ManagedOwnerId,
    bool IsSelfManagedOwner,
    string? FirstName,
    string? LastName,
    string? Email,
    string? CompanyName,
    string? TelephoneNumber,
    string? MobileTelephoneNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? AddressLine3,
    string? AddressLine4,
    string? AddressLine5,
    string? PostalCode,
    string? OrganisationName,
    long AgentUserId,
    string AgentRoleName);