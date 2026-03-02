using Ardalis.GuardClauses;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.External.Web.Models.WoodlandOwner;

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

    /// <summary>
    /// Details of the woodland owner.
    /// </summary>
    public ManageWoodlandOwnerDetailsModel WoodlandOwnerDetails { get; set; }

    public WoodlandOwnerHomePageModel(
        List<FellingLicenceApplicationSummary> fellingLicenceApplications, 
        Guid woodlandOwnerId,
        ManageWoodlandOwnerDetailsModel woodlandOwnerModel)
    {
        Guard.Against.Null(fellingLicenceApplications);
        Guard.Against.Null(woodlandOwnerModel);

        FellingLicenceApplications = fellingLicenceApplications.Any()
            ? fellingLicenceApplications
            : new List<FellingLicenceApplicationSummary>(0);

        WoodlandOwnerId = woodlandOwnerId;
        WoodlandOwnerDetails = woodlandOwnerModel;
    }
}