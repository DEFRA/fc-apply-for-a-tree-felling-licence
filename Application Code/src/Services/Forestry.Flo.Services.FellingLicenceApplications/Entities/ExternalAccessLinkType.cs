using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Enumeration of types of external access links.
/// </summary>
public enum ExternalAccessLinkType
{
    [Display(Name = "External consultee invite")]
    ConsulteeInvite
}