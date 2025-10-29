using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;

/// <summary>
/// Provides methods for determining the current woodland officer review sub-statuses for a felling licence application.
/// </summary>
public interface IWoodlandOfficerReviewSubStatusService
{
    /// <summary>
    /// Gets the set of current woodland officer review sub-statuses for the specified felling licence application.
    /// </summary>
    /// <param name="application">The felling licence application to evaluate.</param>
    /// <returns>A set of woodland officer review sub-statuses that currently apply to the application.</returns>
    HashSet<WoodlandOfficerReviewSubStatus> GetCurrentSubStatuses(FellingLicenceApplication application);
}