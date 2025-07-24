namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

public record ExternalInviteLink
{
    /// <summary>
    /// Gets and Sets the external access link ID.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets and Sets the link created time.
    /// </summary>
    public DateTime CreatedTimeStamp { get; init; }
    
    /// <summary>
    /// Gets and Sets the link expiration time.
    /// </summary>
    public DateTime ExpiresTimeStamp { get; init; }

    /// <summary>
    /// Gets and Sets the link owner name.
    /// </summary>
    public string Name { get; init; } = null!;
    
    /// <summary>
    /// Gets and Sets the link owner email.
    /// </summary>
    public string ContactEmail { get; init; } = null!;

    /// <summary>
    /// Gets and Sets the external access link purpose.
    /// </summary>
    public string Purpose { get; init; } = null!;

    //TODO: The property will be set when the Consultee Comments table is created 
    public bool AreCommentsProvided { get; set; }
}