using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.Services.Common.Infrastructure;

namespace Forestry.Flo.External.Web.Models.WoodlandOwner;

public class ContactDetailsFormModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and sets the contact name of the woodland owner/organisation.
    /// </summary>
    [Required(ErrorMessage = "Enter a contact name")]
    [DisplayName("Contact name")]
    [StringLength(DataValueConstants.NamePartMaxLength)]
    public string ContactName { get; set; } = null!;

    /// <summary>
    /// Gets and sets the email address of the woodland owner/organisation.
    /// </summary>
    [Required(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
    [DisplayName("Contact email address")]
    [FloEmailAddress(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
    public string ContactEmail { get; set; } = null!;

    /// <summary>
    /// Gets and Sets the contact telephone number of the woodland owner/organisation.
    /// </summary>
    [Required(ErrorMessage = "Enter a telephone number in the correct format, like 01234 5678910")]
    [FloTelephone(ErrorMessage = "Enter a telephone number in the correct format, like 01234 5678910")]
    [DisplayName("Telephone number")]
    public string? ContactTelephoneNumber { get; set; }

    /// <summary>
    /// Gets and Sets the contact address of the woodland owner/organisation.
    /// </summary>
    [Required]
    public Address? ContactAddress { get; set; }

    /// <summary>
    /// Gets and Sets a flag indicating whether the woodland owner is an organisation.
    /// </summary>
    [Required(ErrorMessage = "Select if the owner is an organisation")]
    public bool? IsOrganisation { get; set; } = false;

    /// <summary>
    /// Gets and sets a flag indicating whether the contact details are being accessed back from the summary page.
    /// </summary>
    [Required]
    public bool FromSummary { get; set; } = false;
}