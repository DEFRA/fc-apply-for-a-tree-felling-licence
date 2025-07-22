using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models
{
    public record RemoveDocumentRequest
    {
        public Guid ApplicationId { get; set; }
        public Guid DocumentId { get; set; }
        public Guid UserId { get; set; }
    }
    public record RemoveDocumentExternalRequest : RemoveDocumentRequest
    {
        public UserAccessModel UserAccessModel { get; set; }
    }
}
