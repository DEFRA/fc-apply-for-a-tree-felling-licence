using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for the Larch Check use case, including retrieval and saving of larch check and flyover details.
/// </summary>
public interface ILarchCheckUseCase
{
    /// <summary>
    /// Retrieves the larch check model for a given application and internal user.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="user">The internal user requesting the model.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the <see cref="LarchCheckModel"/> if successful.</returns>
    Task<Result<LarchCheckModel>> GetLarchCheckModelAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves the larch check details for a given application.
    /// </summary>
    /// <param name="viewModel">The larch check model to save.</param>
    /// <param name="performingUserId">The ID of the user performing the save.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SaveLarchCheckAsync(
        LarchCheckModel viewModel,
        Guid performingUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the larch flyover model for a given application and internal user.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="user">The internal user requesting the model.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the <see cref="LarchFlyoverModel"/> if successful.</returns>
    Task<Result<LarchFlyoverModel>> GetLarchFlyoverModelAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves the larch flyover details for a given application.
    /// </summary>
    /// <param name="viewModel">The larch flyover model to save.</param>
    /// <param name="performingUserId">The ID of the user performing the save.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SaveLarchFlyoverAsync(
        LarchFlyoverModel viewModel,
        Guid performingUserId,
        CancellationToken cancellationToken);
}