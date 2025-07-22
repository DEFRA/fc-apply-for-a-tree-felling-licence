using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public record GetDocumentRequest
{
    public Guid ApplicationId { get; set; }

    public Guid DocumentId { get; set; }

    public Guid UserId { get; set; }
}

public record GetDocumentExternalRequest : GetDocumentRequest
{
    public UserAccessModel UserAccessModel { get; set; }
}