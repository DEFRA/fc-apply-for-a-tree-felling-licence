using Forestry.Flo.Services.Applicants.Entities;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;

namespace Forestry.Flo.Services.Applicants.Models;

public class UpdateUserAccountModel
{
    /// <summary>
    /// Gets and sets the id of the user account.
    /// </summary>
    public Guid UserAccountId { get; set; }

    /// <summary>
    /// Gets and Sets the title for the external user.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets and Sets the first name of the external user.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets and Sets the last name of the external user.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets and Sets the preferred contact method for the external user.
    /// </summary>
    public PreferredContactMethod? PreferredContactMethod { get; set; }

    /// <summary>
    /// Gets and Sets the contact address of the external user.
    /// </summary>
    public Address? ContactAddress { get; set; }

    /// <summary>
    /// Gets and Sets the contact telephone number of the external user.
    /// </summary>
    public string? ContactTelephoneNumber { get; set; }

    /// <summary>
    /// Gets and Sets the contact mobile number of the external user.
    /// </summary>
    public string? ContactMobileNumber { get; set; }
}