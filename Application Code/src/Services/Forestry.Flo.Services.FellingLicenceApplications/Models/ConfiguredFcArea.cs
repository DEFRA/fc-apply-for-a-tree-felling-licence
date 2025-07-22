using Forestry.Flo.Services.AdminHubs.Model;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// A configured FC area held within the system.
/// </summary>
/// <param name="Area">A <see cref="AreaModel"/> of the Area served</param>
/// <param name="AreaCostCode">The cost code associated to this area</param>
/// <param name="AdminHubName">The name of the Admin hub which this area is associated to</param>
public record ConfiguredFcArea(
    AreaModel Area,
    string AreaCostCode,
    string AdminHubName);
