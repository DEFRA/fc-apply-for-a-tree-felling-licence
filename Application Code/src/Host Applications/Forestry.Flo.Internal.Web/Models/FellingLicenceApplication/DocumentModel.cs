using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class DocumentModel
{
    public Guid Id { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public string MimeType { get; set; }

    public string FileName { get; set; }

    public long FileSize { get; set; }

    public string FileType { get; set; }

    public ActorType AttachedByType { get; set; }

    public DocumentPurpose DocumentPurpose { get; set; }
}