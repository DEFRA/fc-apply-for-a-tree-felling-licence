using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public class IncludedApplicationsSummary
{
    public int TotalCount { get; set; }
    public int AssignedToUserCount { get; set; }
    public IList<IncludedStatusCount> StatusCounts { get; set; } = new List<IncludedStatusCount>();
}

public class IncludedStatusCount
{
    public FellingLicenceStatus Status { get; set; }
    public int Count { get; set; }
}
