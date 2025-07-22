using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.Services.Common.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Models.AgentAuthorityForm;

public class ContactDetailsFormModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and sets the contact name of the applicant/organisation to act on behalf of.
    /// </summary>
    [Required(ErrorMessage = "Contact name must be provided")]
    [DisplayName("Contact name")]
    [StringLength(DataValueConstants.FullNameMaxLength)]
    public string ContactName { get; set; } = null!;

    /// <summary>
    /// Gets and sets the email address of the applicant/organisation to act on behalf of.
    /// </summary>
    [Required(ErrorMessage = "An email address must be provided")]
    [DisplayName("Contact email address")]
    [FloEmailAddress(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
    public string ContactEmail { get; set; } = null!;

    /// <summary>
    /// Gets and Sets the contact telephone number of the applicant/organisation to act on behalf of.
    /// </summary>
    [Required(ErrorMessage = "A telephone number must be provided")]
    [FloTelephone(ErrorMessage = "The provided telephone number is invalid.")]
    [DisplayName("Telephone number")]
    public string? ContactTelephoneNumber { get; set; }

    /// <summary>
    /// Gets and Sets the contact address of the applicant/organisation to act on behalf of.
    /// </summary>
    [Required]
    public Address? ContactAddress { get; set; }

    /// <summary>
    /// Gets and Sets a flag indicating whether the applicant is an organisation.
    /// </summary>
    [Required]
    public bool? IsOrganisation { get; set; } = false;

    /// <summary>
    /// Gets and sets a flag indicating whether the contact details are being accessed back from the summary page.
    /// </summary>
    [Required]
    public bool FromSummary { get; set; } = false;
}