using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models.AdminOfficerReview;

/// <summary>
/// A record indicating the status of each task in the admin officer review process.
/// </summary>
public record AdminOfficerReviewTaskListStates(
    [Display(Name = "Agent Authority Form")]
    InternalReviewStepStatus AgentAuthorityFormStepStatus,

    [Display(Name = "Mapping Check Form")]
    InternalReviewStepStatus MappingCheckStepStatus,

    InternalReviewStepStatus ConstraintsCheckStepStatus,
    InternalReviewStepStatus AssignWoodlandOfficerStatus,
    InternalReviewStepStatus LarchApplicationStatus,
    InternalReviewStepStatus LarchFlyoverStatus,
    InternalReviewStepStatus CBWStatus,
    InternalReviewStepStatus EiaStatus,
    InternalReviewStepStatus TreeHealthStatus,
    bool AgentApplication = false) : ICompletable
{
    /// <summary>
    /// Gets the agent authority form step status of the admin officer review.
    /// </summary>
    [Display(Name = "Agent authority form")]
    public readonly InternalReviewStepStatus AgentAuthorityFormStepStatus = AgentApplication 
        ? AgentAuthorityFormStepStatus
        : InternalReviewStepStatus.Completed;

    /// <summary>
    /// Gets the mapping check step status of the admin officer review.
    /// </summary>
    [Display(Name = "Mapping check")]
    public readonly InternalReviewStepStatus MappingCheckStepStatus = MappingCheckStepStatus;

    /// <summary>
    /// Gets the constraints check step status of the admin officer review.
    /// </summary>
    [Display(Name = "Constraints check")]
    public readonly InternalReviewStepStatus ConstraintsCheckStepStatus = ConstraintsCheckStepStatus;

    /// <summary>
    /// Gets the Larch Application Check step status of the admin officer review.
    /// </summary>
    [Display(Name = "Larch application check")]
    public InternalReviewStepStatus LarchApplicationStatus = LarchApplicationStatus;

    /// <summary>
    /// Gets the Larch Application Flyover step status of the admin officer review.
    /// </summary>
    [Display(Name = "Larch flyover")]
    public InternalReviewStepStatus LarchFlyoverStatus = LarchFlyoverStatus;

    /// <summary>
    /// Gets the Larch Application Flyover step status of the admin officer review.
    /// </summary>
    [Display(Name = "Cricket bat willow")]
    public InternalReviewStepStatus CBWStatus = CBWStatus;


    /// <summary>
    /// Gets the EIA step status of the admin officer review.
    /// </summary>
    [Display(Name = "EIA check")]
    public InternalReviewStepStatus EiaStatus = EiaStatus;


    /// <summary>
    /// Gets the tree health/public safety step status of the admin officer review.
    /// </summary>
    [Display(Name = "Review tree health or public safety")]
    public InternalReviewStepStatus TreeHealthStatus = TreeHealthStatus;

    /// <summary>
    /// Gets the Assign Woodland Officer step status of the admin officer review.
    /// </summary>
    [Display(Name = "Assign woodland officer")]
    public InternalReviewStepStatus AssignWoodlandOfficerStatus = AssignWoodlandOfficerStatus;

    public bool IsCompletable() =>
        AgentAuthorityFormStepStatus is InternalReviewStepStatus.NotRequired or InternalReviewStepStatus.Completed &&
        MappingCheckStepStatus is InternalReviewStepStatus.Completed &&
        ConstraintsCheckStepStatus is InternalReviewStepStatus.Completed &&
        LarchApplicationStatus is InternalReviewStepStatus.Completed or InternalReviewStepStatus.NotRequired &&
        CBWStatus is InternalReviewStepStatus.Completed or InternalReviewStepStatus.NotRequired &&
        AssignWoodlandOfficerStatus is InternalReviewStepStatus.Completed &&
        EiaStatus is InternalReviewStepStatus.NotRequired or InternalReviewStepStatus.Completed &&
        TreeHealthStatus is InternalReviewStepStatus.NotRequired or InternalReviewStepStatus.Completed;
}