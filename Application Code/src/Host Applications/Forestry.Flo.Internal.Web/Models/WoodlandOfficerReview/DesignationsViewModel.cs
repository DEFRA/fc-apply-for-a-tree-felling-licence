using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// View model class for the Designations page of the woodland officer review.
/// </summary>
public class DesignationsViewModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the current state of the designations task for the woodland officer.
    /// </summary>
    public ApplicationSubmittedCompartmentDesignations CompartmentDesignations { get; set; }
}