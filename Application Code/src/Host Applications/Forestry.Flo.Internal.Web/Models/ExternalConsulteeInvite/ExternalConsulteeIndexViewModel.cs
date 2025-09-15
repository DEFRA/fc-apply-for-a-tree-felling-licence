using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

public class ExternalConsulteeIndexViewModel : WoodlandOfficerReviewModelBase, IExternalConsulteeInvite
{
    /// <summary>
    /// Gets and inits the invitation application id.
    /// </summary>
    public Guid ApplicationId { get; init; }

    /// <summary>
    /// Gets and inits whether any consultations are required for this application.
    /// </summary>
    [Required(ErrorMessage = "Select whether consultations are required")]
    public bool? ApplicationNeedsConsultations { get; init; }

    /// <summary>
    /// Gets and inits whether all required consultations have been completed for this application.
    /// </summary>
    public bool ConsultationsComplete { get; init; }

    /// <summary>
    /// A list of external access email links created for the application
    /// </summary>
    public IList<ExternalInviteLink> InviteLinks { get; init; } = new List<ExternalInviteLink>();

    /// <summary>
    /// The current date and time in UTC
    /// </summary>
    public DateTime CurrentDateTimeUtc { get; init; }
}