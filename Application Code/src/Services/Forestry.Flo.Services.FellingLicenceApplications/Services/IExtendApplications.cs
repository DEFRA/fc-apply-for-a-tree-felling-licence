using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Contract for services that extend applications' final action dates.
/// </summary>
public interface IExtendApplications
{
    /// <summary>
    /// Extends final action date for applications still in the submitted state if the date has been reached and hasn't been previously extended.
    /// </summary> 
    /// <param name="extensionLength">The length of the extension.</param>
    /// <param name="periodBeforeThreshold">The time prior to the final action date that notifications should start being sent to assigned FC staff members.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing a list of all applications that have exceeded their final action dates, for notifications.</returns>
    Task<Result<IList<ApplicationExtensionModel>>> ApplyApplicationExtensionsAsync(
        TimeSpan extensionLength,
        TimeSpan periodBeforeThreshold,
        CancellationToken cancellationToken);
}