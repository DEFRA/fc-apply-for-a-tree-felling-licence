using System.IO;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications;

public class ApplicationReferenceHelper : IApplicationReferenceHelper
{
    ///<inheritdoc />
    public string GenerateReferenceNumber(FellingLicenceApplication application, long counter, string? postFix, int? startingOffset)
    {
        int offset = startingOffset ?? 0;
        return $"---/{(offset + counter ) :D3}/{application.CreatedTimestamp.Year}{(string.IsNullOrEmpty(postFix) ? string.Empty : $"/{postFix}")}";
    }

    public string UpdateReferenceNumber(string reference, string prefix)
    {
        return $"{prefix}/{string.Join("/", reference.Split('/').Skip(1))}";
    }
}