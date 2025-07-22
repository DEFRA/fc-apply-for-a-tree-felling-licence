using Ardalis.GuardClauses;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class FlaStatusDurationCalculator
{
    private readonly IClock _clock;

    public FlaStatusDurationCalculator(IClock clock)
    {
        _clock = Guard.Against.Null(clock);
    }

    public List<FellingLicenceApplicationStateDuration> CalculateStatusDurations(FellingLicenceApplication fellingLicenceApplication)
    {
        var statusTimes = new List<FellingLicenceApplicationStateDuration>();
        DateTime? previousTimestamp = null;
        FellingLicenceStatus? previousStatus = null;

        foreach (var statusChange in fellingLicenceApplication.StatusHistories.OrderBy(sc => sc.Created))
        {
            var currentTimestamp = statusChange.Created;
            var currentStatus = statusChange.Status;

            if (previousStatus != null)
            {
                var timeSpent = currentTimestamp - previousTimestamp!.Value;
                var statusTime = statusTimes.FirstOrDefault(st => st.Status == previousStatus.Value);
                if (statusTime != null)
                {
                    statusTime.Duration += timeSpent;
                }
                else
                {
                    statusTime = new FellingLicenceApplicationStateDuration(status: previousStatus.Value, duration: timeSpent);
                    statusTimes.Add(statusTime);
                }
            }

            previousTimestamp = currentTimestamp;
            previousStatus = currentStatus;
        }

        if (previousStatus != null)
        {
            // Handle the last status change
            var timeSpent = _clock.GetCurrentInstant().ToDateTimeUtc().ToLocalTime() - previousTimestamp!.Value;
            var statusTime = statusTimes.FirstOrDefault(st => st.Status == previousStatus.Value);
            if (statusTime != null)
            {
                statusTime.Duration += timeSpent;
            }
            else
            {
                statusTime = new FellingLicenceApplicationStateDuration(status: previousStatus.Value, duration: timeSpent);
                statusTimes.Add(statusTime);
            }
        }
        
        return statusTimes;
    }
}
