using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.WoodlandOwner;

public class OrganisationDetailsFormModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and sets the contact name of the woodland owner organisation.
    /// </summary>
    [Required(ErrorMessage = "Enter an organisation name")]
    [DisplayName("Organisation name")]
    [StringLength(DataValueConstants.NamePartMaxLength)]
    public string OrganisationName { get; set; } = null!;

    /// <summary>
    /// Gets and Sets the contact address of the woodland owner organisation.
    /// </summary>
    [Required]
    public Address? OrganisationAddress { get; set; }
}