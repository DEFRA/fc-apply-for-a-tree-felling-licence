using Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;
using Forestry.Flo.Services.Common.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Forestry.Flo.External.Web.Models.Agency;

public class FcUserAgencyCreationModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and Sets the address of the Agency on the system.
    /// </summary>
    public Address? Address { get; set; }

    /// <summary>
    /// Gets and Sets the email address of the agency.
    /// </summary>
    [Required(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
    [DisplayName("Email address")]
    [FloEmailAddress(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Gets and Sets the full name of the agency contact.
    /// </summary>
    [Required(ErrorMessage = "Enter a contact name")]
    [DisplayName("Contact name")]
    [MaxLength(DataValueConstants.NamePartMaxLength)]
    public string? ContactName { get; set; }

    /// <summary>
    /// Gets and Sets the Organisation name of the agency.
    /// </summary>
    [MaxLength(DataValueConstants.OrganisationNameMaxLength)]
    [DisplayName("Organisation name")]
    [Required(ErrorMessage = "Enter an organisation name")]
    public string? OrganisationName { get; set; }

    /// <summary>
    /// Get and sets a flag indicating whether the agent should auto approve thinning applications
    /// </summary>
    public bool ShouldAutoApproveThinningApplications { get; set; }

    public OrganisationStatus? OrganisationStatus { get; set; }
    
    public bool IsOrganisation { get; set; }
 
    public bool IsFcAgency => false;
}