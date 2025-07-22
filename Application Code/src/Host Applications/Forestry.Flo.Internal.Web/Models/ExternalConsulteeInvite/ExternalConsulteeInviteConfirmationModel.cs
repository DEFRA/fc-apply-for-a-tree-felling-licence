using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Common.Infrastructure;

namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

public class ExternalConsulteeInviteConfirmationModel: FellingLicenceApplicationPageViewModel, IExternalConsulteeInvite
{
    /// <summary>
    /// Gets and inits the name of the consultee being invited.
    /// </summary>
    public string ConsulteeName { get; init; } = null!;
    
    /// <summary>
    /// Gets and inits the email address of the consultee being invited. 
    /// </summary>
    public string Email { get; init; } = null!;
    
    /// <summary>
    /// Gets and inits the consultee email content of the email body.
    /// </summary>
    public string EmailContent { get; init; } = null!;
    /// <summary>
    /// Gets and inits the preview consultee email content displayed on the page (without hyperlinks).
    /// </summary>
    public string PreviewEmailContent { get; init; } = null!;
    
    /// <summary>
    /// Gets and sets the confirmed email address of the consultee being invited.
    /// </summary>
    [Required(ErrorMessage = "The confirmed email address must be provided")]
    [DisplayName("Confirmed Email address")]
    [FloEmailAddress(ErrorMessage = "Enter the email address in the correct format, like name@example.com")]
    public string? ConfirmedEmail { get; set; }
    
    /// <summary>
    /// Gets and inits the invitation id.
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    ///  Gets and inits the invitation application id.
    /// </summary>
    public Guid ApplicationId { get; init; }
    
    /// <summary>
    /// Gets and inits the url to return after sending the invite.
    /// </summary>
    public string ReturnUrl { get; init; } = null!;
    
    /// <summary>
    /// Gets and inits a list of application supporting documents attached to the invite.
    /// </summary>
    /// 
    public List<SupportingDocument> AttachedDocuments { get; init; } = new List<SupportingDocument>(); 
    
    /// <summary>
    /// Gets and inits a number of all application supporting documents .
    /// </summary>
    public int ApplicationDocumentCount { get; init; } 
}