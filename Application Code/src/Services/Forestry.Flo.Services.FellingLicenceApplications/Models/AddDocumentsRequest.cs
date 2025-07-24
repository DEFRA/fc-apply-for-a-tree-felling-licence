using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FileStorage.Model;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models
{
    public record AddDocumentsRequest
    {
        public Guid? UserAccountId { get; set; }
        public ActorType ActorType { get; set; }
        public Guid FellingApplicationId { get; set; }
        public IReadOnlyCollection<FileToStoreModel> FileToStoreModels { get; set; } = null!;
        public DocumentPurpose DocumentPurpose { get; set; }
        public int ApplicationDocumentCount { get; set; }
        public bool ReceivedByApi { get; set; }
        public bool VisibleToApplicant { get; set; }
        public bool VisibleToConsultee { get; set; }
    }

    public record AddDocumentsExternalRequest : AddDocumentsRequest
    {
        public Guid WoodlandOwnerId { get; set; }
    }
}
