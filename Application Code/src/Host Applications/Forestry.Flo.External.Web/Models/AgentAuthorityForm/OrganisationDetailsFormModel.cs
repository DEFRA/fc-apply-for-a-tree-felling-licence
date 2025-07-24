using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Common.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Models.AgentAuthorityForm;

public class OrganisationDetailsFormModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and sets the contact name of the organisation to act on behalf of.
    /// </summary>
    [Required(ErrorMessage = "Organisation name must be provided")]
    [DisplayName("Organisation name")]
    [StringLength(DataValueConstants.OrganisationNameMaxLength)]
    public string OrganisationName { get; set; } = null!;

    /// <summary>
    /// Gets and Sets the contact address of the organisation to act on behalf of.
    /// </summary>
    [Required]
    public Address? OrganisationAddress { get; set; }
}