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
    /// Gets and sets whether the application is exempt from the public register.
    /// </summary>
    public bool PublicRegisterExempt { get; set; }

    /// <summary>
    /// Gets and sets the reason for exemption from the public register, if applicable.
    /// </summary>
    public string? PublicRegisterExemptionReason { get; set; }

    /// <summary>
    /// Gets and sets the name of the consultee as was originally invited.
    /// </summary>
    public string ConsulteeName { get; set; }

    /// <summary>
    /// Gets and sets the email address of the consultee as was originally invited.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Gets and sets the notification content of the original invite sent to the consultee.
    /// </summary>
    public string InviteContent { get; set; }

    /// <summary>
    /// Gets and sets the list of documents that were shared with the consultee as part of the invite.
    /// </summary>
    public List<DocumentModel> SharedDocuments { get; set; }

    /// <summary>
    /// Gets and sets the list of comments received from the consultee.
    /// </summary>
    public List<ReceivedConsulteeCommentModel> ReceivedComments { get; set; }

    /// <summary>
    /// Gets and sets the selected purpose for the consultee invitation.
    /// </summary>
    public string InvitationPurpose { get; set; }

    /// <summary>
    /// Gets and sets the date when the original invitation was sent to the consultee.
    /// </summary>
    public DateTime? InvitationDate { get; set; }
}