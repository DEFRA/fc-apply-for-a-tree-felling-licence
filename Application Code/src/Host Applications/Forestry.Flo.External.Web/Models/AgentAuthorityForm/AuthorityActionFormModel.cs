namespace Forestry.Flo.External.Web.Models.AgentAuthorityForm;

public class AuthorityActionFormModel
{
    public Guid AccessCode { get; set; }
    public string EmailAddress { get; set; } = null!;
    public string? SignatureImageData { get; set; }
}