using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.External.Web.Models.Home;

public class WoodlandOwnerHomePageModel
{
    //TODO - entity class for FLAs
    /// <summary>
    /// Gets the collection of felling licence applications for a woodland owner user.
    /// </summary>
    public IReadOnlyCollection<FellingLicenceApplicationSummary> FellingLicenceApplications { get; }

    /// <summary>
    /// The Woodland Owner Id who the applications belong to.
    /// </summary>
    public Guid WoodlandOwnerId { get; set; }

    public WoodlandOwnerHomePageModel(
        List<FellingLicenceApplicationSummary> fellingLicenceApplications, Guid woodlandOwnerId)
    {
        Guard.Against.Null(fellingLicenceApplications);

        FellingLicenceApplications = fellingLicenceApplications.Any()
            ? fellingLicenceApplications
            : new List<FellingLicenceApplicationSummary>(0);

        WoodlandOwnerId = woodlandOwnerId;
    }
}