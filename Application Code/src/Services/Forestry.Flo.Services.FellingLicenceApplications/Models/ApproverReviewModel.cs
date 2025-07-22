using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// Represents the model for an approver's review of a felling licence application.
/// </summary>
public class ApproverReviewModel
{
    /// <summary>
    /// Gets or sets the application ID.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the requested status of the felling licence.
    /// </summary>
    public FellingLicenceStatus? RequestedStatus { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the application has been checked.
    /// </summary>
    public bool CheckedApplication { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the documentation has been checked.
    /// </summary>
    public bool CheckedDocumentation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the case notes have been checked.
    /// </summary>
    public bool CheckedCaseNotes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Woodland Officer review has been checked.
    /// </summary>
    public bool CheckedWOReview { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the applicant has been informed.
    /// </summary>
    public bool InformedApplicant { get; set; }

    /// <summary>
    /// Gets or sets the approved licence duration.
    /// </summary>
    public RecommendedLicenceDuration? ApprovedLicenceDuration { get; set; }

    /// <summary>
    /// Gets or sets the reason for changing the duration.
    /// </summary>
    public string? DurationChangeReason { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the application should be published in the public register.
    /// </summary>
    public bool? PublicRegisterPublish { get; set; }

    /// <summary>
    /// Gets or sets the reason for exemption from the public register.
    /// </summary>
    public string? PublicRegisterExemptionReason { get; set; }
}

public static class ApproverReviewExtensions
{
    public static ApproverReviewModel ToModel(this ApproverReview entity)
    {
        return new ApproverReviewModel
        {
            ApplicationId = entity.FellingLicenceApplicationId,
            RequestedStatus = entity.RequestedStatus,
            CheckedApplication = entity.CheckedApplication,
            CheckedDocumentation = entity.CheckedDocumentation,
            CheckedCaseNotes = entity.CheckedCaseNotes,
            CheckedWOReview = entity.CheckedWOReview,
            InformedApplicant = entity.InformedApplicant,
            ApprovedLicenceDuration = entity.ApprovedLicenceDuration,
            DurationChangeReason = entity.DurationChangeReason,
            PublicRegisterPublish = entity.PublicRegisterPublish,
            PublicRegisterExemptionReason = entity.PublicRegisterExemptionReason
        };
    }

    public static ApproverReview ToEntity(this ApproverReviewModel model)
    {
        var entity = new ApproverReview
        {
            FellingLicenceApplicationId = model.ApplicationId,
            RequestedStatus = model.RequestedStatus ?? FellingLicenceStatus.SentForApproval,
            CheckedApplication = model.CheckedApplication,
            CheckedDocumentation = model.CheckedDocumentation,
            CheckedCaseNotes = model.CheckedCaseNotes,
            CheckedWOReview = model.CheckedWOReview,
            InformedApplicant = model.InformedApplicant,
            ApprovedLicenceDuration = model.ApprovedLicenceDuration,
            DurationChangeReason = model.DurationChangeReason,
            PublicRegisterPublish = model.PublicRegisterPublish,
            PublicRegisterExemptionReason = model.PublicRegisterExemptionReason
        };

        return entity;
    }

    public static ApproverReview MapToEntity(this ApproverReviewModel approverReview, ApproverReview entity)
    {
        entity.FellingLicenceApplicationId = approverReview.ApplicationId;
        entity.RequestedStatus = approverReview.RequestedStatus ?? FellingLicenceStatus.SentForApproval;
        entity.CheckedApplication = approverReview.CheckedApplication;
        entity.CheckedDocumentation = approverReview.CheckedDocumentation;
        entity.CheckedCaseNotes = approverReview.CheckedCaseNotes;
        entity.CheckedWOReview = approverReview.CheckedWOReview;
        entity.InformedApplicant = approverReview.InformedApplicant;
        entity.ApprovedLicenceDuration = approverReview.ApprovedLicenceDuration;
        entity.DurationChangeReason = approverReview.DurationChangeReason;
        entity.PublicRegisterPublish = approverReview.PublicRegisterPublish;
        entity.PublicRegisterExemptionReason = approverReview.PublicRegisterExemptionReason;

        return entity;
    }

}

