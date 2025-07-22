using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.External.Web.Infrastructure;

namespace Forestry.Flo.External.Web.Models.WoodlandOwner
{
    /// <summary>
    /// Model class representing a Woodland Owner's details.
    /// </summary>
    public class WoodlandOwnerModel : PageWithBreadcrumbsViewModel
    {
        /// <summary>
        /// Gets and Sets the contact address of the Woodland Owner on the system.
        /// </summary>
        public Address? ContactAddress { get; set; }

        /// <summary>
        /// Gets and sets a flag indicating the contact address is the same as the registered address.
        /// </summary>
        public bool ContactAddressMatchesOrganisationAddress { get; set; }

        /// <summary>
        /// Gets and Sets the email address of the woodland owner.
        /// </summary>
        public string? ContactEmail { get; set; }

        /// <summary>
        /// Gets and Sets the contact telephone number of the woodland owner.
        /// </summary>
        [FloTelephone(ErrorMessage = "Enter a telephone number in the correct format, like 01234 5678910")]
        public string? ContactTelephoneNumber { get; set; }

        /// <summary>
        /// Gets and Sets the full name of the Woodland owner.
        /// </summary>
        public string? ContactName { get; set; }

        [DisplayName("Are you registering for an organisation?")]
        public bool IsOrganisation { get; set; }

        /// <summary>
        /// Gets and Sets the Organisation name that the woodland owner is part of.
        /// </summary>
        [Required(ErrorMessage = "Enter an organisation name")]
        [MaxLength(DataValueConstants.OrganisationNameMaxLength)]
        [DisplayName("Organisation name")]
        public string? OrganisationName { get; set; }

        /// <summary>
        /// Gets and Sets the address of the Organisation that the woodland owner is part of.
        /// </summary>
        public Address? OrganisationAddress { get; set; }

    }
}
