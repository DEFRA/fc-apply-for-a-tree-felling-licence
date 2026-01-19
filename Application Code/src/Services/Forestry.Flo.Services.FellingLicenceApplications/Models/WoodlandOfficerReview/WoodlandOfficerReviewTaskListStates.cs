namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// A record indicating the status of each task in the woodland officer review process.
/// </summary>
public record WoodlandOfficerReviewTaskListStates(
    InternalReviewStepStatus PublicRegisterStepStatus,
    InternalReviewStepStatus SiteVisitStepStatus,
    InternalReviewStepStatus Pw14ChecksStepStatus,
    InternalReviewStepStatus FellingAndRestockingStepStatus,
    InternalReviewStepStatus ConditionsStepStatus,
    InternalReviewStepStatus ConsultationStepStatus,
    InternalReviewStepStatus CompartmentDesignationsStepStatus,
    InternalReviewStepStatus LarchApplicationStatus,
    InternalReviewStepStatus LarchFlyoverStatus,
    InternalReviewStepStatus EiaScreeningStatus,
    InternalReviewStepStatus TreeHealthStatus,
    InternalReviewStepStatus PriorityOpenHabitatStepStatus,
    bool WoodlandOfficerReviewComplete) : ICompletable
{
    public readonly InternalReviewStepStatus PublicRegisterStepStatus = PublicRegisterStepStatus;
    public readonly InternalReviewStepStatus SiteVisitStepStatus = SiteVisitStepStatus;
    public readonly InternalReviewStepStatus Pw14ChecksStepStatus = Pw14ChecksStepStatus;
    public readonly InternalReviewStepStatus FellingAndRestockingStepStatus = FellingAndRestockingStepStatus;
    public readonly InternalReviewStepStatus ConditionsStepStatus = ConditionsStepStatus;
    public readonly InternalReviewStepStatus ConsultationStepStatus = ConsultationStepStatus;
    public readonly InternalReviewStepStatus CompartmentDesignationsStepStatus = CompartmentDesignationsStepStatus;
    public readonly InternalReviewStepStatus LarchApplicationStatus = LarchApplicationStatus;
    public readonly InternalReviewStepStatus LarchFlyoverStatus = LarchFlyoverStatus;
    public readonly InternalReviewStepStatus EiaScreeningStatus = EiaScreeningStatus;
    public readonly InternalReviewStepStatus TreeHealthStatus = TreeHealthStatus;
    public readonly InternalReviewStepStatus PriorityOpenHabitatStepStatus = PriorityOpenHabitatStepStatus;

    public InternalReviewStepStatus FinalChecksStepStatus =>
        WoodlandOfficerReviewComplete
            ? InternalReviewStepStatus.Completed
            : IsCompletable() 
                ? InternalReviewStepStatus.NotStarted 
                : InternalReviewStepStatus.CannotStartYet;

    public bool IsCompletable() =>
        PublicRegisterStepStatus is InternalReviewStepStatus.Completed &&
        SiteVisitStepStatus is InternalReviewStepStatus.Completed &&
        Pw14ChecksStepStatus is InternalReviewStepStatus.Completed &&
        FellingAndRestockingStepStatus is InternalReviewStepStatus.Completed &&
        CompartmentDesignationsStepStatus is InternalReviewStepStatus.Completed &&
        LarchApplicationStatus is InternalReviewStepStatus.Completed or InternalReviewStepStatus.NotRequired &&
        LarchFlyoverStatus is InternalReviewStepStatus.Completed or InternalReviewStepStatus.NotRequired &&
        ConditionsStepStatus is InternalReviewStepStatus.Completed &&
        EiaScreeningStatus is InternalReviewStepStatus.Completed or InternalReviewStepStatus.NotRequired &&
        TreeHealthStatus is InternalReviewStepStatus.Completed &&
        ConsultationStepStatus is InternalReviewStepStatus.NotRequired or InternalReviewStepStatus.Completed &&  // TODO add the two new steps when those pages are implemented
        PriorityOpenHabitatStepStatus is InternalReviewStepStatus.Completed or InternalReviewStepStatus.NotRequired &&
        ConsultationStepStatus is InternalReviewStepStatus.NotRequired or InternalReviewStepStatus.Completed;  // TODO add the two new steps when those pages are implemented

}