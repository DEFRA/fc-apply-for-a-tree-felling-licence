namespace Forestry.Flo.Services.Common.Infrastructure;

public static class OneLoginPrincipalClaimTypes
{
    /// <summary>
    /// The claim type for the user's unique identifier. 
    /// </summary>
    public const string NameIdentifier = "sub";

    /// <summary>
    /// The claim type for the user's email address.
    /// </summary>
    public const string EmailAddress = "email";
}