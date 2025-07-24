using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Common.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

public class ExternalConsulteeInviteFormModel: FellingLicenceApplicationPageViewModel, IExternalConsulteeInvite
{
    /// <summary>
    /// Gets and sets the name of the consultee being invited.
    /// </summary>
    [Required(ErrorMessage = "Consultee name must be provided")]
    [DisplayName("Consultee name")]
    [StringLength(DataValueConstants.NamePartMaxLength)]
    public string ConsulteeName { get; set; } = null!;
    
    /// <summary>
    /// Gets and sets the email address of the consultee being invited. 
    /// </summary>
    [Required(ErrorMessage = "An email address must be provided")]
    [DisplayName("Email address")]
    [FloEmailAddress(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
    public string Email { get; set; } = null!;
    
    /// <summary>
    /// Gets and sets the purpose of the inviting the external party to be consulted on the application must be provided.
    /// </summary>
    [Required(ErrorMessage = "A purpose for inviting the external party to be consulted on the application must be provided")]
    [DisplayName("Purpose")]
    [StringLength(DataValueConstants.ConsultationPurposeLength)]
    public string? Purpose { get; set; }
    
    /// <summary>
    /// Gets and inits the url to return after sending the invite.
    /// </summary>
    public string ReturnUrl { get; init; } = null!;
    
    /// <summary>
    /// Gets and inits the invitation id.
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Gets and inits the invitation application id.
    /// </summary>
    public Guid ApplicationId { get; init; }

    /// <summary>
    /// A list of external access email links created for the application
    /// </summary>
    public IList<ExternalInviteLink> InviteLinks { get; init; } = new List<ExternalInviteLink>();

    /// <summary>
    /// Gets and sets whether this application is exempt from the consultation public register.
    /// </summary>
    [DisplayName("Is the application exempt from the consultation public register")]
    public bool ExemptFromConsultationPublicRegister { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether the woodland officer has already completed the
    /// public register stage for this application i.e. decided it is exempt, or published it.
    /// </summary>
    public bool PublicRegisterAlreadyCompleted { get; set; }
}