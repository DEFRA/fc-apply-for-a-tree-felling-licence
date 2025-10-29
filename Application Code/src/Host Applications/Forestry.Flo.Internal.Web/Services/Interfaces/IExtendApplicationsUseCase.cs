namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Contract for the use case that handles extending applications.
/// </summary>
public interface IExtendApplicationsUseCase
{
    /// <summary>
    /// Extends final action date for applications still in a review state if the date has been reached and hasn't been previously extended.
    /// </summary>
    /// <param name="viewFLABaseURL">The base URL for viewing an application summary on the internal app.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task ExtendApplicationFinalActionDatesAsync(string viewFLABaseURL, CancellationToken cancellationToken);
}
