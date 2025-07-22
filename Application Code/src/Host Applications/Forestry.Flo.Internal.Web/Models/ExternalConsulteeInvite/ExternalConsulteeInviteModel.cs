namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

public record ExternalConsulteeInviteModel
{
    /// <summary>
    /// Gets and inits the invitation id.
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Gets and inits the name of the consultee being invited.
    /// </summary>
    public string ConsulteeName { get; init; } = null!;

    /// <summary>
    /// Gets and inits the email address of the consultee being invited. 
    /// </summary>
    public string Email { get; init; } = null!;
    
    /// <summary>
    /// Gets and inits the purpose of the inviting the external party to be consulted on the application must be provided.
    /// </summary>
    public string? Purpose { get; init; }

    /// <summary>
    /// Gets and inits the consultee email text.
    /// </summary>
    public string ConsulteeEmailText { get; init; } = null!;
    
    /// <summary>
    /// Gets and inits the consultee email content of the email body.
    /// </summary>
    public string? ConsulteeEmailContent { get; init; }
    
    /// <summary>
    /// Gets and inits the external link access code.
    /// </summary>
    public Guid ExternalAccessCode { get; init; }
    
    /// <summary>
    /// Gets and inits the external access link.
    /// </summary>
    public string ExternalAccessLink { get; init; } = null!;

    /// <summary>
    /// Gets and sets a list of selected supporting documents ids.
    /// </summary>
    public List<Guid> SelectedDocumentIds { get; set; } = new ();

    /// <summary>
    /// Gets and sets whether this application is exempt from the consultation public register.
    /// </summary>
    public bool ExemptFromConsultationPublicRegister { get; set; }
}