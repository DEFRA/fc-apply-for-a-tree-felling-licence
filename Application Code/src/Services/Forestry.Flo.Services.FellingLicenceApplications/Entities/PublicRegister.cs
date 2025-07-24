using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Public Register status entity class.
/// </summary>
public class PublicRegister
{
    /// <summary>
    /// Gets and sets the Id of this entity.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the felling licence application id.
    /// </summary>
    public Guid FellingLicenceApplicationId { get; set; }

    public FellingLicenceApplication FellingLicenceApplication { get; set; }

    /// <summary>
    /// Gets and sets whether the Woodland Officer has indicated that the FLA is exempt from the consultation public register.
    /// </summary>
    public bool WoodlandOfficerSetAsExemptFromConsultationPublicRegister { get; set; }

    /// <summary>
    /// Gets and sets the reason given by the Woodland Officer why this FLA is exempt from the consultation public register.
    /// </summary>
    public string? WoodlandOfficerConsultationPublicRegisterExemptionReason { get; set; }

    /// <summary>
    /// Gets and sets the date and time that the FLA was published to the consultation public register.
    /// </summary>
    public DateTime? ConsultationPublicRegisterPublicationTimestamp { get; set; }

    /// <summary>
    /// Gets and sets the unique id for this case on the public register provided from the ESRI integration.
    /// </summary>
    public int? EsriId { get; set; }

    /// <summary>
    /// Gets and sets the calculated end date for the FLA to be on the consultation public register.
    /// </summary>
    public DateTime? ConsultationPublicRegisterExpiryTimestamp { get; set; }

    /// <summary>
    /// Gets and sets the date and time that the FLA was removed from the consultation public register.
    /// </summary>
    public DateTime? ConsultationPublicRegisterRemovedTimestamp { get; set; }

    /// <summary>
    /// Gets and sets the date and time that the FLA was published to the decision public register.
    /// </summary>
    /// <remarks>
    /// This can only be set during the approval process.
    /// </remarks>
    public DateTime? DecisionPublicRegisterPublicationTimestamp { get; set; }

    /// <summary>
    /// Gets and sets the calculated end date for the FLA to be on the decision public register.
    /// </summary>
    /// <remarks>
    /// This can only be set during the approval process.
    /// </remarks>
    public DateTime? DecisionPublicRegisterExpiryTimestamp { get; set; }

    /// <summary>
    /// Gets and sets the date and time that the FLA was removed from the decision public register.
    /// </summary>
    public DateTime? DecisionPublicRegisterRemovedTimestamp { get; set; }
}