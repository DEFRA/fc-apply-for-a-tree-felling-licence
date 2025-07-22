using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public class FellingLicenceApplicationStateDuration
{
    public FellingLicenceStatus Status { get; set; }
    public TimeSpan Duration { get; set; }

    public FellingLicenceApplicationStateDuration(FellingLicenceStatus status, TimeSpan duration)
    {
        Status = status;
        Duration = duration;
    }
}