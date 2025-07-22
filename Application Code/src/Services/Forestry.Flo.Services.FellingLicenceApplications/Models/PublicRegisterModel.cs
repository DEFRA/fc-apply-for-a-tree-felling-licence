namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// Model class for the public register details of an application.
/// </summary>
public class PublicRegisterModel
{
    /// <summary>
    /// Gets and sets a flag indicating whether the woodland officer has decided that the application does
    /// not need to go on the consultation public register.
    /// </summary>
    public bool WoodlandOfficerSetAsExemptFromConsultationPublicRegister { get; set; }

    /// <summary>
    /// Gets and sets a reason for the application not to need to be published to the consultation
    /// public register.
    /// </summary>
    public string? WoodlandOfficerConsultationPublicRegisterExemptionReason { get; set; }

    /// <summary>
    /// Gets and sets the number of days the application should be on the consultation public register for.
    /// </summary>
    public int? ConsultationPublicRegisterPeriodDays { get; set; }

    /// <summary>
    /// Gets and sets the date and time that the application was published to the consultation
    /// public register.
    /// </summary>
    public DateTime? ConsultationPublicRegisterPublicationTimestamp { get; set; }

    /// <summary>
    /// Gets and sets the id of the application on the consultation public register.
    /// </summary>
    public int? EsriId { get; set; }

    /// <summary>
    /// Gets and sets the date and time that the applications entry on the consultation
    /// public register will expire.
    /// </summary>
    public DateTime? ConsultationPublicRegisterExpiryTimestamp { get; set; }

    /// <summary>
    /// Gets and sets the date and time that the application was removed from the consultation
    /// public register.
    /// </summary>
    public DateTime? ConsultationPublicRegisterRemovedTimestamp { get; set; }
}