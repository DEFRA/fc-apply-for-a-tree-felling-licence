using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

public record SupportingDocument(Guid Id, string FileName, DocumentPurpose Purpose, DateTime CreatedTimestamp, string Location, string MimeType);


