using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeReview;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

public class ExternalConsulteeEmailTextModel: FellingLicenceApplicationPageViewModel, IExternalConsulteeInvite
{
    /// <summary>
    /// Gets and sets the name of the consultee being invited.
    /// </summary>
    public string ConsulteeName { get; set; } = null!;
    
    /// <summary>
    /// Gets and sets the email address of the consultee being invited. 
    /// </summary>
    public string Email { get; set; } = null!;
    
    /// <summary>
    /// Gets and sets the consultee email text.
    /// </summary>
    [Required(ErrorMessage = "Consultee email text must be provided")]
    [DisplayName("Consultee email text")]
    [StringLength(DataValueConstants.ConsultationEmailTextLength)]
    public string ConsulteeEmailText { get; set; } = null!;
    
    /// <summary>
    /// Gets and inits the invitation id.
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Gets and inits the invitation application id.
    /// </summary>
    public Guid ApplicationId { get; init; }
    
    /// <summary>
    /// Gets and inits the url to return after sending the invite.
    /// </summary>
    public string ReturnUrl { get; init; } = null!;
    
    /// <summary>
    /// Gets and inits a number of all application supporting documents .
    /// </summary>
    public int ApplicationDocumentsCount { get; init; }
}