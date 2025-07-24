using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Common.Infrastructure;

namespace Forestry.Flo.External.Web.Models.UserAccount;

public class AgencyUserModel: PageWithBreadcrumbsViewModel, IInvitedUser
{
    public Guid AgencyId { get; set; }
    public string AgencyName { get; set; } = null!;
   
    public string OrganisationName => AgencyName;
    public Guid OrganisationId => AgencyId;

    /// <summary>
    /// Gets and sets the name of the individual being invited.
    /// </summary>
    [Required(ErrorMessage = "Enter a name")]
    [DisplayName("Name")]
    [StringLength(DataValueConstants.NamePartMaxLength)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets and sets the email address of the individual being invited. 
    /// </summary>
    [Required(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
    [DisplayName("Email address")]
    [FloEmailAddress(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
    public string Email { get; set; } = null!;

    /// <summary>
    /// Gets and sets the user role of the individual being invited.
    /// </summary>
    [DisplayName("Agency user role")]
    [Required(ErrorMessage = "Select a role")]
    public AgencyUserRole AgencyUserRole{ get; set; }
}