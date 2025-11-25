namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// Represents the Approved In Error details for a felling licence application.
/// Captures why an application was marked as approved in error and any supporting notes.
/// </summary>
public class ApprovedInErrorModel
{
    /// <summary>
    /// Gets or sets the unique identifier for this Approved In Error record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the application ID that this record belongs to.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the application's old reference number prior to the Approved In Error process.
    /// </summary>
    public string PreviousReference { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a flag indicating the expiry date was a reason for the approval in error.
    /// </summary>
    public bool ReasonExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating supplementary points were a reason for the approval in error.
    /// </summary>
    public bool ReasonSupplementaryPoints { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating another reason (not covered by other flags) applied.
    /// </summary>
    public bool ReasonOther { get; set; }

    /// <summary>
    /// Gets or sets optional case notes providing additional context for the approval in error.
    /// </summary>
    public string? CaseNote { get; set; }
}

/// <summary>
/// Mapping helpers for converting between the Approved In Error entity and model.
/// </summary>
public static class ApprovedInErrorExtensions
{
    /// <summary>
    /// Converts an entity instance to a model instance.
    /// </summary>
    /// <param name="entity">The entity to convert.</param>
    /// <returns>A populated <see cref="ApprovedInErrorModel"/>.</returns>
    public static ApprovedInErrorModel ToModel(this Entities.ApprovedInError entity)
    {
        return new ApprovedInErrorModel
        {
            Id = entity.Id,
            ApplicationId = entity.FellingLicenceApplicationId,
            PreviousReference = entity.PreviousReference,
            ReasonExpiryDate = entity.ReasonExpiryDate,
            ReasonSupplementaryPoints = entity.ReasonSupplementaryPoints,
            ReasonOther = entity.ReasonOther,
            CaseNote = entity.CaseNote
        };
    }

    /// <summary>
    /// Maps values from the provided model onto an existing entity instance.
    /// </summary>
    /// <param name="model">The source model.</param>
    /// <param name="entity">The target entity to update.</param>
    /// <returns>The updated <see cref="Entities.ApprovedInError"/> entity.</returns>
    public static Entities.ApprovedInError MapToEntity(this ApprovedInErrorModel model, Entities.ApprovedInError entity)
    {
        entity.FellingLicenceApplicationId = model.ApplicationId;
        entity.PreviousReference = model.PreviousReference;
        entity.ReasonExpiryDate = model.ReasonExpiryDate;
        entity.ReasonSupplementaryPoints = model.ReasonSupplementaryPoints;
        entity.ReasonOther = model.ReasonOther;
        entity.CaseNote = model.CaseNote;

        return entity;
    }
}
