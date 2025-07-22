using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

public class ApplicationDetailsForPublicRegisterModel
{
    public string CaseReference { get; set; }

    public string PropertyName { get; set; }

    public string GridReference { get; set; }

    public string NearestTown { get; set; }

    public string LocalAuthority { get; set; }

    /// <summary>
    /// The Name of the administrative region, for example Buller's Hill.
    /// </summary>
    public string AdminRegion { get; set; }

    public double? BroadleafArea { get; set; }

    public double? ConiferousArea { get; set; }

    public double? OpenGroundArea { get; set; }

    public double? TotalArea { get; set; }

    public List<InternalCompartmentDetails<Polygon>> Compartments { get; set; }

    /// <summary>
    /// The centre-point, used in order to calculate the Local Authority
    /// </summary>
    public Point? CentrePoint { get; set; }

    /// <summary>
    /// Gets and sets the list of user IDs for users currently assigned to the application.
    /// </summary>
    public List<Guid> AssignedInternalUserIds { get; set; }
}