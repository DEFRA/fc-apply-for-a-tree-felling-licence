namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Contract for use case handling expiry of applications on the Consultation Public Register.
/// </summary>
public interface IPublicRegisterExpiryUseCase
{
    /// <summary>
    /// Removes Felling Licence Applications from the Consultation Public Register when their expiry/end date is reached.
    /// </summary>
    /// <param name="viewApplicationBaseUrl">The base URL for viewing an application summary on the internal app.</param>
    /// <param name="cancellationToken">A cancellation Token</param>
    Task RemoveExpiredApplicationsFromConsultationPublicRegisterAsync(
        string viewApplicationBaseUrl,
        CancellationToken cancellationToken);
}
