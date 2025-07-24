using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Common.Infrastructure;

namespace Forestry.Flo.External.Web.Models.UserAccount;

public class OrganisationWoodlandOwnerUserModel: PageWithBreadcrumbsViewModel, IInvitedUser
{
    public Guid WoodlandOwnerId { get; set; }
    public string WoodlandOwnerName { get; set; } = null!;
   
    public string OrganisationName => WoodlandOwnerName;
    public Guid OrganisationId => WoodlandOwnerId;

    /// <summary>
    /// Gets and sets the name of the individual being invited.
    /// </summary>
    [Required(ErrorMessage = "Name must be provided")]
    [DisplayName("Name")]
    [StringLength(DataValueConstants.NamePartMaxLength)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets and sets the email address of the individual being invited. 
    /// </summary>
    [Required(ErrorMessage = "An email address must be provided")]
    [DisplayName("Email address")]
    [FloEmailAddress(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
    public string Email { get; set; } = null!;

    /// <summary>
    /// Gets and sets the user role of the individual being invited.
    /// </summary>
    [DisplayName("Woodland owner user role")]
    [Required(ErrorMessage = "Woodland owner user role must be selected")]
    public WoodlandOwnerUserRole WoodlandOwnerUserRole { get; set; }
    
    
}