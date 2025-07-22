using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

public class ExternalConsulteeEmailDocumentsModel: FellingLicenceApplicationPageViewModel, IExternalConsulteeInvite
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
    /// Gets and inits a list of application supporting documents attached to the invite.
    /// </summary>
    public List<SupportingDocument> SupportingDocuments { get; init; } = new ();
    
    /// <summary>
    /// Gets and inits a list of selected supporting documents ids.
    /// </summary>
    public List<Guid> SelectedDocumentIds { get; init; } = new ();
    
    /// <summary>
    /// Gets and inits the invitation id
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Gets and inits the invitation application id
    /// </summary>
    public Guid ApplicationId { get; init; }
    
    /// <summary>
    /// Gets and inits the url to return after sending the invite
    /// </summary>
    public string ReturnUrl { get; init; } = null!;
}