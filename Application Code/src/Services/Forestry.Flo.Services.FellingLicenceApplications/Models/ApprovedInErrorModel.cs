namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// Represents the Approved In Error details for a felling licence application.
/// This model is used to transfer approved-in-error data between application layers
/// and captures why an application was marked as approved in error along with any supporting notes.
/// </summary>
public class ApprovedInErrorModel
{
    /// <summary>
    /// Gets or sets the unique identifier for this Approved In Error record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the application ID that this record belongs to.
    /// This is the ID of the duplicated/corrected application, not the original application.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the application's old reference number prior to the Approved In Error process.
    /// This preserves the original application reference for audit and traceability purposes.
    /// </summary>
    /// <example>FLO/2024/123</example>
    public string PreviousReference { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a flag indicating whether the expiry date was a reason for marking this application as approved in error.
    /// When true, indicates that the licence expiry date in the approved licence was incorrect.
    /// </summary>
    public bool ReasonExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets the textual explanation for why the expiry date was incorrect.
    /// This provides additional context when <see cref="ReasonExpiryDate"/> is true.
    /// </summary>
    public string? ReasonExpiryDateText { get; set; }

    /// <summary>
    /// Gets and sets the corrected licence expiry date that should be used when re-approving the application.
    /// This is populated when <see cref="ReasonExpiryDate"/> is true and represents the correct expiry date.
    /// </summary>
    public DateTime? LicenceExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether supplementary points were a reason for marking this application as approved in error.
    /// When true, indicates that the supplementary points (Parameter 6) in the conditions were incorrect or incomplete.
    /// </summary>
    public bool ReasonSupplementaryPoints { get; set; }

    /// <summary>
    /// Gets or sets the textual explanation or corrected content for the supplementary points.
    /// This provides the corrected supplementary points text when <see cref="ReasonSupplementaryPoints"/> is true.
    /// Corresponds to Parameter 6 in the conditions builder system.
    /// </summary>
    public string? SupplementaryPointsText { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether other reasons exist for marking this application as approved in error.
    /// When true, indicates that there are additional reasons not covered by the expiry date or supplementary points flags.
    /// </summary>
    public bool ReasonOther { get; set; }

    /// <summary>
    /// Gets or sets the textual explanation for other reasons the application was marked as approved in error.
    /// This is required when <see cref="ReasonOther"/> is true and provides details of issues not covered
    /// by the specific reason flags.
    /// </summary>
    public string? ReasonOtherText { get; set; }

    /// <summary>
    /// Gets and sets the unique identifier of the Field Manager (approver) who last approved the application
    /// after it was marked as approved in error.
    /// This is used to identify and notify the approver who made the decision that is being corrected.
    /// </summary>
    public Guid? ApproverId { get; set; }
}

/// <summary>
/// Provides extension methods for mapping between <see cref="ApprovedInErrorModel"/> 
/// and <see cref="Entities.ApprovedInError"/> entity instances.
/// </summary>
public static class ApprovedInErrorExtensions
{
    /// <summary>
    /// Converts an <see cref="Entities.ApprovedInError"/> entity instance to an <see cref="ApprovedInErrorModel"/> model instance.
    /// </summary>
    /// <param name="entity">The entity to convert. Must not be null.</param>
    /// <returns>
    /// A populated <see cref="ApprovedInErrorModel"/> containing all the data from the entity,
    /// excluding audit fields (LastUpdatedDate, LastUpdatedById) and navigation properties.
    /// </returns>
    public static ApprovedInErrorModel ToModel(this Entities.ApprovedInError entity)
    {
        return new ApprovedInErrorModel
        {
            Id = entity.Id,
            ApplicationId = entity.FellingLicenceApplicationId,
            PreviousReference = entity.PreviousReference,
            ReasonExpiryDate = entity.ReasonExpiryDate,
            LicenceExpiryDate = entity.LicenceExpiryDate,
            ReasonSupplementaryPoints = entity.ReasonSupplementaryPoints,
            ReasonOther = entity.ReasonOther,
            ReasonExpiryDateText = entity.ReasonExpiryDateText,
            ReasonOtherText = entity.ReasonOtherText,
            SupplementaryPointsText = entity.SupplementaryPointsText,
        };
    }

    /// <summary>
    /// Maps values from an <see cref="ApprovedInErrorModel"/> model instance onto an existing 
    /// <see cref="Entities.ApprovedInError"/> entity instance.
    /// </summary>
    /// <param name="model">The source model containing the updated values. Must not be null.</param>
    /// <param name="entity">The target entity to update with values from the model. Must not be null.</param>
    /// <returns>
    /// The updated <see cref="Entities.ApprovedInError"/> entity with all properties from the model applied.
    /// The same instance that was passed in is returned to support method chaining.
    /// </returns>
    public static Entities.ApprovedInError MapToEntity(this ApprovedInErrorModel model, Entities.ApprovedInError entity)
    {
        entity.FellingLicenceApplicationId = model.ApplicationId;
        entity.PreviousReference = model.PreviousReference;
        entity.ReasonExpiryDate = model.ReasonExpiryDate;
        entity.LicenceExpiryDate = model.LicenceExpiryDate;
        entity.ReasonSupplementaryPoints = model.ReasonSupplementaryPoints;
        entity.ReasonOther = model.ReasonOther;
        entity.ReasonExpiryDateText = model.ReasonExpiryDateText;
        entity.ReasonOtherText = model.ReasonOtherText;
        entity.SupplementaryPointsText = model.SupplementaryPointsText;
        entity.ApproverId = model.ApproverId;

        return entity;
    }
}
