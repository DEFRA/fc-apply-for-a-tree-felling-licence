namespace Domain.V1;

public record FloUser(
    long UserId, 
    string UserName, 
    string Email, 
    string RoleName,
    string? FirstName,
    string? LastName,
    string AddressLine1,
    string? AddressLine2,
    string AddressLine3,
    string? AddressLine4,
    string? AddressLine5,
    string? OrganisationName,
    string? PostalCode,
    string? TelephoneNumber,
    string? MobileTelephoneNumber,
    string? CompanyName);
