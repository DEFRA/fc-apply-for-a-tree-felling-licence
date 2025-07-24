using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.WoodlandOwner;

/// <summary>
/// View model for the page that gathers the organisation name of a woodland owner.
/// </summary>
public class WoodlandOwnerOrganisationNameViewModel : PageWithBreadcrumbsViewModel
{
    [DisplayName("Are you registering for an organisation?")]
    public bool IsOrganisation { get; set; }

    /// <summary>
    /// Gets and Sets the Organisation name that the woodland owner is part of.
    /// </summary>
    [Required(ErrorMessage = "Enter an organisation name")]
    [MaxLength(DataValueConstants.OrganisationNameMaxLength)]
    [DisplayName("Organisation name")]
    public string? OrganisationName { get; set; }
}