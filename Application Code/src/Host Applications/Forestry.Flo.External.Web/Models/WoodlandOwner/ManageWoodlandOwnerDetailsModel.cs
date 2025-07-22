using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using EmailValidation;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.Services.Common.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Models.WoodlandOwner
{
    /// <summary>
    /// Model class representing a Woodland Owner's details.
    /// </summary>
    public class ManageWoodlandOwnerDetailsModel : PageWithBreadcrumbsViewModel
    {
        [Required]
        [HiddenInput]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets and Sets the contact address of the Woodland Owner on the system.
        /// </summary>
        [Required]
        public Address? ContactAddress { get; set; }

        /// <summary>
        /// Gets and sets a flag indicating the contact address is the same as the registered address.
        /// </summary>
        public bool ContactAddressMatchesOrganisationAddress { get; set; }

        /// <summary>
        /// Gets and Sets the email address of the woodland owner.
        /// </summary>
        [Required(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
        [DisplayName("Email address")]
        [FloEmailAddress(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
        public string ContactEmail { get; set; }

        /// <summary>
        /// Gets and Sets the contact telephone number of the woodland owner.
        /// </summary>
        [Required(ErrorMessage = "Enter a telephone number in the correct format, like 01234 5678910")]
        [FloTelephone(ErrorMessage = "Enter a telephone number in the correct format, like 01234 5678910")]
        [DisplayName("Telephone number")]
        public string? ContactTelephoneNumber { get; set; }

        /// <summary>
        /// Gets and Sets the full name of the Woodland owner.
        /// </summary>
        [Required(ErrorMessage = "Enter a contact name")]
        [DisplayName("Contact name")]
        [MaxLength(DataValueConstants.NamePartMaxLength)]
        public string? ContactName { get; set; }

        [Required]
        [HiddenInput]
        public bool IsOrganisation { get; set; }

        /// <summary>
        /// Gets and Sets the Organisation name that the woodland owner is part of.
        /// </summary>
        [MaxLength(DataValueConstants.OrganisationNameMaxLength)]
        [DisplayName("Organisation name")]
        public string? OrganisationName { get; set; }

        /// <summary>
        /// Gets and Sets the address of the Organisation that the woodland owner is part of.
        /// </summary>
        public Address? OrganisationAddress { get; set; }
    }
}
