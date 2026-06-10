using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.External.Web.Services.Interfaces;

/// <summary>
/// Contract for the use case class dealing with withdrawing a felling licence application.
/// </summary>
public interface IWithdrawApplicationsUseCase
{
    /// <summary>
    /// Gets the view model for the confirm withdrawal page of a felling licence application, which includes
    /// details of the application and any relevant breadcrumbs for navigation, along with the available
    /// reason options for the applicant to select from.
    /// </summary>
    /// <param name="applicationId"></param>
    /// <param name="user"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Result<ConfirmWithdrawFellingLicenceApplicationViewModel>> GetConfirmWithdrawalViewModelAsync(
        Guid applicationId,
        ExternalApplicant user,
        CancellationToken cancellationToken);
}