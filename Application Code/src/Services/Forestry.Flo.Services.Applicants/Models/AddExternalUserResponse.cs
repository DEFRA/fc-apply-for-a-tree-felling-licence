using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;

namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Model class representing the resulting response to creating a new <see cref="UserAccount"/> within the system.
/// </summary>
public class AddExternalUserResponse
{
    /// <summary>
    /// Gets and sets the id of created user account.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the Identity Provider Id of created user account.
    /// </summary>
    public string? IdentityProviderId { get; set; }

    /// <summary>
    /// Gets and sets the Email address of created user account.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets and sets the Account Type of created user account.
    /// </summary>
    public AccountTypeExternal AccountType { get; set; }
}
