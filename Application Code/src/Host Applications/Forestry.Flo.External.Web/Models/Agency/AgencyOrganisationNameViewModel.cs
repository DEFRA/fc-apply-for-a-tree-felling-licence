using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Forestry.Flo.External.Web.Models.Agency;

/// <summary>
/// View model for the page that gathers the organisation name of an agency.
/// </summary>
public class AgencyOrganisationNameViewModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and Sets the Organisation name of the agency.
    /// </summary>
    [Required(ErrorMessage = "Enter an organisation name")]
    [MaxLength(DataValueConstants.OrganisationNameMaxLength)]
    [DisplayName("Organisation name")]
    public string? OrganisationName { get; set; }
}