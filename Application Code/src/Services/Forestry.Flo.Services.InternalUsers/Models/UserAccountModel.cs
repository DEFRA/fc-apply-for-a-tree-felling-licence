using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;

namespace Forestry.Flo.Services.InternalUsers.Models;

public class UserAccountModel
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
    /// Gets and Sets the email address of the internal user.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Gets and sets the account type of the user account.
    /// </summary>
    public AccountTypeInternal AccountType { get; set; }

    /// <summary>
    /// Gets and sets the status of the user account.
    /// </summary>
    public Status Status { get; set; }
}