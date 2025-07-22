using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Extensions;

public static class FellingLicenceApplicationStepStatusExtensions
{
    /// <summary>
    /// Determines the overall felling and restocking completion status for a given <see cref="CompartmentFellingRestockingStatus"/>.
    /// </summary>
    /// <param name="stepStatus">A populated <see cref="CompartmentFellingRestockingStatus"/></param>
    /// <returns>A nullable flag indicating the step is complete, or has not been started if it is null.</returns>
    public static bool? OverallCompletion(this CompartmentFellingRestockingStatus stepStatus)
    {
        if (stepStatus.Status.HasNoValue() && stepStatus.FellingStatuses.All(fs => fs.Status.HasNoValue()))
        {
            return null;
        }

        return stepStatus.Status.HasValue && stepStatus.Status.Value
            && stepStatus.FellingStatuses.All(fs => fs.OverallCompletion().HasValue && fs.OverallCompletion().Value);
    }

    public static bool? OverallCompletion(this FellingStatus fellingStatus)
    {
        if (fellingStatus.Status.HasNoValue() && fellingStatus.RestockingCompartmentStatuses.All(rcs => rcs.Status.HasNoValue()))
        {
            return null;
        }

        return fellingStatus.Status.HasValue && fellingStatus.Status.Value 
            && fellingStatus.RestockingCompartmentStatuses.All(rcs => rcs.OverallCompletion().HasValue && rcs.OverallCompletion().Value);
    }

    public static bool? OverallCompletion(this RestockingCompartmentStatus restockingCompartmentStatus)
    {
        if (restockingCompartmentStatus.Status.HasNoValue() && restockingCompartmentStatus.RestockingStatuses.All(rs => rs.Status.HasNoValue()))
        {
            return null;
        }

        return restockingCompartmentStatus.Status.HasValue && restockingCompartmentStatus.Status.Value 
            && restockingCompartmentStatus.RestockingStatuses.All(rs => rs.Status.HasValue && rs.Status.Value);
    }
}