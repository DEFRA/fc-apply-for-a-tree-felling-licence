using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications;

/// <summary>
/// Implementation of <see cref="IApplicationReferenceHelper"/>.
/// </summary>
public class ApplicationReferenceHelper : IApplicationReferenceHelper
{
    ///<inheritdoc />
    public string GenerateReferenceNumber(FellingLicenceApplication application, long counter, string? postFix, int? startingOffset = 0)
    {
        var offset = startingOffset ?? 0;
        return $"---/{(offset + counter ) :D3}/{application.CreatedTimestamp.Year}{(string.IsNullOrEmpty(postFix) ? string.Empty : $"/{postFix}")}";
    }

    ///<inheritdoc />
    public string UpdateReferenceNumber(string reference, string prefix)
    {
        return $"{prefix}/{string.Join("/", reference.Split('/').Skip(1))}";
    }
}