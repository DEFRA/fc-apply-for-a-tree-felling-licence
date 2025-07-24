namespace Forestry.Flo.External.Web.Services;

/// <summary>
/// Enum to represent the outcome of user account checks prior to permitting a user to
/// proceed with system registration.
/// </summary>
public enum AccountSignupValidityCheckOutcome
{
    IsValidSignUp,
    IsAlreadyInvited,
    IsMigratedUser,
    IsDeactivated
}
