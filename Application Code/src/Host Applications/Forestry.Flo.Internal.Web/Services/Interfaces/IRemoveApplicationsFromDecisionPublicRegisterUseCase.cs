namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Contract for the use case that removes applications from the Decision Public Register when their expiry/end date is reached.
/// </summary>
public interface IRemoveApplicationsFromDecisionPublicRegisterUseCase
{
    /// <summary>
    /// Removes Felling Licence Applications from the Decision Public Register when their expiry/end date is reached.
    /// </summary>
    /// <param name="viewApplicationBaseUrl">The base URL for viewing an application summary on the internal app.</param>
    /// <param name="cancellationToken">A cancellation Token</param>
    Task ExecuteAsync(string viewApplicationBaseUrl, CancellationToken cancellationToken);
}
