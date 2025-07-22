namespace Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;

/// <summary>
/// Represents the details of a Crown Land tenant's landlord.
/// </summary>
/// <param name="FirstName">The first name of the landlord.</param>
/// <param name="LastName">The last name of the landlord.</param>
public record LandlordDetails (string FirstName, string LastName);