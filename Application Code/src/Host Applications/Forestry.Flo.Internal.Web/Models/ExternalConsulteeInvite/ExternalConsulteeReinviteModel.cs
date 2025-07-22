using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

public class ExternalConsulteeReInviteModel: FellingLicenceApplicationPageViewModel, IExternalConsulteeInvite
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
    ///  Gets and sets the purpose of the inviting the external party to be consulted on the application must be provided.
    /// </summary>
    public string Purpose { get; set; } = null!;
    
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
}