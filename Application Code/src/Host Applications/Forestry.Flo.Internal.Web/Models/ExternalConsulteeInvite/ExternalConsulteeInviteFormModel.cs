using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Common.Infrastructure;

namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

public class ExternalConsulteeInviteFormModel: FellingLicenceApplicationPageViewModel, IExternalConsulteeInvite
{
    /// <summary>
    /// Gets and sets the name of the consultee being invited.
    /// </summary>
    [Required(ErrorMessage = "Enter a consultee name")]
    [DisplayName("Full name")]
    [StringLength(DataValueConstants.NamePartMaxLength)]
    public string ConsulteeName { get; set; } = null!;
    
    /// <summary>
    /// Gets and sets the email address of the consultee being invited. 
    /// </summary>
    [Required(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
    [DisplayName("Email address")]
    [FloEmailAddress(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
    public string Email { get; set; } = null!;
    
    /// <summary>
    /// Gets and sets the purpose of the inviting the external party to be consulted on the application must be provided.
    /// </summary>
    [Required(ErrorMessage = "Select the purpose for inviting the external party to be consulted on the application")]
    [DisplayName("Why are you inviting this consultee?")]
    public ExternalConsulteeInvitePurpose? Purpose { get; set; }

    /// <summary>
    /// Gets and sets the purpose of the inviting the external party to be consulted on the application must be provided.
    /// </summary>
    [Required(ErrorMessage = "Enter the area of focus for the consultation on the application")]
    [DisplayName("Area of focus")]
    public string? AreaOfFocus { get; set; }

    /// <summary>
    /// Gets and sets a list of selected supporting documents ids.
    /// </summary>
    public List<Guid?> SelectedDocumentIds { get; set; } = new();
    
    /// <summary>
    /// Gets and inits the invitation application id.
    /// </summary>
    public Guid ApplicationId { get; init; }

    /// <summary>
    /// Gets and sets whether this application is exempt from the consultation public register.
    /// </summary>
    [DisplayName("Is the application exempt from the consultation public register?")]
    [Required(ErrorMessage = "Select whether the application is exempt from the public register")]
    public bool? ExemptFromConsultationPublicRegister { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether the woodland officer has already completed the
    /// public register stage for this application i.e. decided it is exempt, or published it.
    /// </summary>
    public bool PublicRegisterAlreadyCompleted { get; set; }

    /// <summary>
    /// Gets and sets a list of <see cref="DocumentModel"/> representing the supporting documents available to view by consultees.
    /// </summary>
    public List<DocumentModel>? ConsulteeDocuments { get; init; }
}