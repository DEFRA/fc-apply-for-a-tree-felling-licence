using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.Agency;

public class AgencyModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and Sets the address of the Agency on the system.
    /// </summary>
    public Address? Address { get; set; }

    /// <summary>
    /// Gets and Sets the email address of the agency.
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Gets and Sets the full name of the agency contact.
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// Gets and Sets the Organisation name of the agency.
    /// </summary>
    [Required(ErrorMessage = "Enter an organisation name")]
    [MaxLength(DataValueConstants.OrganisationNameMaxLength)]
    [DisplayName("Organisation name")]
    public string? OrganisationName { get; set; }

    /// <summary>
    /// Get and sets a flag indicating whether the agent should auto approve thinning applications
    /// </summary>
    public bool ShouldAutoApproveThinningApplications { get; set; }
}