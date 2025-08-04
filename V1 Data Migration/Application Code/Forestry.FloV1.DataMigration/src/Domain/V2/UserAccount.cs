namespace Domain.V2;

public record UserAccount(
    AccountType AccountType,
    string Email,
    string? FirstName,
    string? LastName,
    string? AddressLine1,
    string? AddressLine2,
    string? AddressLine3,
    string? AddressLine4,
    string? AddressPostcode,
    string? Telephone,
    string? MobileTelephone
);