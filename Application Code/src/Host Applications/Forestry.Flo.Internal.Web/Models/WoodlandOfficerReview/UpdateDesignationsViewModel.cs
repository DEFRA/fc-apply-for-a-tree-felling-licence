using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// View model class for the Update Designations page of the woodland officer review.
/// </summary>
public class UpdateDesignationsViewModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the designations data for the compartment being reviewed.
    /// </summary>
    public SubmittedCompartmentDesignationsModel CompartmentDesignations { get; set; }

    /// <summary>
    /// Gets and sets the proposed compartment designations data for the compartment being reviewed.
    /// </summary>
    public PawsCompartmentDesignationsModel? ProposedCompartmentDesignations { get; set; }

    /// <summary>
    /// Gets and sets the submitted compartment identifier for the next compartment to be reviewed.
    /// </summary>
    [HiddenInput]
    public Guid? NextCompartmentId { get; set; }

    /// <summary>
    /// Gets and sets the number of compartments that have been reviewed so far.
    /// </summary>
    public int CompartmentsReviewed { get; set; }

    /// <summary>
    /// Gets and sets the total number of compartments that need to be reviewed.
    /// </summary>
    public int TotalCompartments { get; set; }
}