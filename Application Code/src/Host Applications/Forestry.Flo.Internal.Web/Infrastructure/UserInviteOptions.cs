namespace Forestry.Flo.Internal.Web.Infrastructure;

public class UserInviteOptions
{
    /// <summary>
    /// A number of days when a user invite link is valid
    /// </summary>
    public int InviteLinkExpiryDays { get; set; } = 28;
}