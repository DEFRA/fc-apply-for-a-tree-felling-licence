using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;

namespace Forestry.Flo.External.Web.Models.UserAccount;

public class AccountPersonContactModel
{
    /// <summary>
    /// Gets and Sets the preferred contact method of the external user.
    /// </summary>
    [DisplayName("Preferred method of contact")]
    public PreferredContactMethod? PreferredContactMethod { get; set; }

    /// <summary>
    /// Gets and Sets the contact address of the external user.
    /// </summary>
    [Required]
    public Address? ContactAddress { get; set; }
    
    /// <summary>
    /// Gets and Sets the contact telephone number of the external user.
    /// </summary>
    [FloTelephone(ErrorMessage = "Enter a telephone number in the correct format, like 01234 5678910")]
    [Required(ErrorMessage = "Enter a telephone number in the correct format, like 01234 5678910")]
    [DisplayName("Phone number")]
    public string? ContactTelephoneNumber { get; set; }

    /// <summary>
    /// Gets and Sets the contact mobile telephone number of the external user.
    /// </summary>
    [FloTelephone(ErrorMessage = "Enter a mobile telephone number in the correct format, like 07712 345678")]
    [DisplayName("Mobile number")]
    public string? ContactMobileNumber { get; set; }

}