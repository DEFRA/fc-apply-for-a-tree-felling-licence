namespace Forestry.Flo.Services.Applicants.Entities.UserAccount;

/// <summary>
/// Defines a set of possible values for the account status that may be applied to a <see cref="UserAccount"/> instance.
/// </summary>
public enum UserAccountStatus
{
    /// <summary>
    /// A user who is active on the system, has completed the sign-up/registration process.
    /// </summary>
    Active,
    /// <summary>
    /// A Deactivated user.
    /// </summary>
    Deactivated,
    /// <summary>
    /// A user who has been invited to use the system, but who has not yet actioned the invite.
    /// </summary>
    Invited,
    /// <summary>
    /// Only ever used during migration tool usage.
    /// </summary>
    Migrated
}