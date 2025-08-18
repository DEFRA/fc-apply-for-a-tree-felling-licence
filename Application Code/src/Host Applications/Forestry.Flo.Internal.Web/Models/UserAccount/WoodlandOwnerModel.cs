using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Common.Infrastructure;

namespace Forestry.Flo.Internal.Web.Models.UserAccount
{
    /// <summary>
    /// Model class representing a Woodland Owner's details.
    /// </summary>
    public class WoodlandOwnerModel : PageWithBreadcrumbsViewModel
    {
        /// <summary>
        /// Gets and sets the contact address of the Woodland Owner on the system.
        /// </summary>
        public Address? ContactAddress { get; set; }

        /// <summary>
        /// Gets and sets a flag indicating the contact address is the same as the registered address.
        /// </summary>
        public bool ContactAddressMatchesOrganisationAddress { get; set; }

        /// <summary>
        /// Gets and sets the email address of the external user.
        /// </summary>
        public string? ContactEmail { get; set; }

        /// <summary>
        /// Gets and sets the full name of the Woodland owner.
        /// </summary>
        public string? ContactName { get; set; }

        /// <summary>
        /// Gets and sets the contact telephone number of the woodland owner.
        /// </summary>
        public string? ContactTelephone { get; set; }

        /// <summary>
        /// Gets and sets a flag indicating whether the woodland owner is an organisation.
        /// </summary>
        public bool IsOrganisation { get; set; }

        /// <summary>
        /// Gets and sets the Organisation name that the woodland owner is part of.
        /// </summary>
        public string? OrganisationName { get; set; }

        /// <summary>
        /// Gets and sets the address of the Organisation that the woodland owner is part of.
        /// </summary>
        public Address? OrganisationAddress { get; set; }

    }
}
