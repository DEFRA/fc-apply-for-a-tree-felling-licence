using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;

namespace Forestry.Flo.Services.InternalUsers.Models;

public record UpdateRegistrationDetailsModel
{
    /// <summary>
    /// Gets and sets the id of the user account.
    /// </summary>
    public Guid UserAccountId { get; set; }

    /// <summary>
    /// Gets and Sets the Title for the internal user.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets and Sets the first name of the internal user.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets and Sets the last name of the internal user.
    /// </summary>
    public string? LastName { get; set; }

    public string FullName => $"{Title} {FirstName} {LastName}".Trim().Replace("  ", " ");

    /// <summary>
    /// Gets and sets the account type of the user account.
    /// </summary>
    public AccountTypeInternal AccountType { get; set; }

    /// <summary>
    /// Gets and sets the other account type of the user account.
    /// </summary>
    public AccountTypeInternalOther? AccountTypeOther { get; set; }

    /// <summary>
    /// Gets and sets the role list for the user account.
    /// </summary>
    public List<Roles> Roles { get; set; } = null!;

    /// <summary>
    /// Gets and sets whether the user can approve applications.
    /// </summary>
    public bool CanApproveApplications { get; set; }
}