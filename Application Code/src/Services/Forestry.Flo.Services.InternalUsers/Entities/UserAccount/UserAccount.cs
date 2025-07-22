using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.InternalUsers.Entities.UserAccount;

/// <summary>
/// Model class representing a user's account of the system.
/// </summary>
public class UserAccount
{
    /// <summary>
    /// Gets the unique internal identifier for the user account on the system.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets or Sets the User Identifier as known to the the Identity Provider system.
    /// </summary>
    public string? IdentityProviderId { get; set; }

    /// <summary>
    /// Gets and sets the account type for this local account.
    /// </summary>
    [Required]
    public AccountTypeInternal AccountType { get; set; }

    /// <summary>
    /// Gets and sets the other account type for this local account.
    /// </summary>
    public AccountTypeInternalOther? AccountTypeOther { get; set; }

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


    private string? _email;

    /// <summary>
    /// Gets and Sets the email address of the internal user.
    /// </summary>
    [Required]
    public string Email
    {
        get => _email;
        set => _email = value.ToLower();
    }

    /// <summary>
    /// Gets and sets the status of the user account.
    /// </summary>
    public Status Status { get; set; } = Status.Requested;

    public string Roles { get; set; }

    /// <summary>
    /// Gets and sets whether the user has permission to approve/decline applications.
    /// </summary>
    public bool CanApproveApplications { get; set; }
}