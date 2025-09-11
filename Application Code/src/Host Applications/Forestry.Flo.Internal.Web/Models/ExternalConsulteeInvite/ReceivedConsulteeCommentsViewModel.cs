using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

/// <summary>
/// View model for displaying comments received from an external consultee.
/// </summary>
public class ReceivedConsulteeCommentsViewModel : FellingLicenceApplicationPageViewModel
{
    /// <summary>
    /// Gets and sets the application ID for which the comments were received.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the name of the consultee who was originally invited.
    /// </summary>
    public string ConsulteeName { get; set; }

    /// <summary>
    /// Gets and sets the email address of the consultee who was originally invited.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Gets and sets the list of comments received from the consultee.
    /// </summary>
    public List<ReceivedConsulteeCommentModel> ReceivedComments { get; set; }
}