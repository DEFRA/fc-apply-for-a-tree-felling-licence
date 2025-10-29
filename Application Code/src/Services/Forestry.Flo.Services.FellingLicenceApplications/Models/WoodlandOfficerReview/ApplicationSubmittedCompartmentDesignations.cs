namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Model class representing the current state of the designations task for the woodland officer
/// review.
/// </summary>
public class ApplicationSubmittedCompartmentDesignations
{
    /// <summary>
    /// Gets and sets a value indicating whether the designations task has been completed.
    /// </summary>
    public bool HasCompletedDesignations { get; set; }

    /// <summary>
    /// Gets and sets the list of compartment designations for the submitted FLA.
    /// </summary>
    public IList<SubmittedCompartmentDesignationsModel> CompartmentDesignations { get; set; }
}