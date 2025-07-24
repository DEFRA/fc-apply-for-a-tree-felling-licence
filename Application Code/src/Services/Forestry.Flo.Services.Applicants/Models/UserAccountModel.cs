using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.Applicants.Models;

public class UserAccountModel
{
    /// <summary>
    /// Gets and sets the id of the user account.
    /// </summary>
    public Guid UserAccountId { get; set; }

    /// <summary>
    /// Gets and Sets the first name of the external user.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets and Sets the last name of the external user.
    /// </summary>
    public string? LastName { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim().Replace("  ", " ");

    /// <summary>
    /// Gets and Sets the email address of the external user.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Gets and sets the account type of the user account.
    /// </summary>
    public AccountTypeExternal AccountType { get; set; }

    /// <summary>
    /// Gets and sets the status of the user account.
    /// </summary>
    public UserAccountStatus Status { get; init; }
}