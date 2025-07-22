using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Model class for the overall status of the woodland officer review of an application.
/// </summary>
public class WoodlandOfficerReviewStatusModel
{
    /// <summary>
    /// Gets and sets a populated <see cref="WoodlandOfficerReviewTaskListStates"/> indicating the status of each
    /// step of the woodland officer review process.
    /// </summary>
    public WoodlandOfficerReviewTaskListStates WoodlandOfficerReviewTaskListStates { get; set; }

    /// <summary>
    /// Gets and sets a list of <see cref="CaseNoteModel"/> containing the woodland officer review comments.
    /// </summary>
    public IList<CaseNoteModel> WoodlandOfficerReviewComments { get; set; }

    /// <summary>
    /// Gets and sets the recommended licence duration selected by the woodland officer.
    /// </summary>
    public RecommendedLicenceDuration? RecommendedLicenceDuration { get; set; }

    /// <summary>
    /// Gets and sets a bool to indicate whether the Woodland Officer has recommended that the application
    /// is published to the decision public register.
    /// </summary>
    public bool? RecommendationForDecisionPublicRegister { get; set; }
}