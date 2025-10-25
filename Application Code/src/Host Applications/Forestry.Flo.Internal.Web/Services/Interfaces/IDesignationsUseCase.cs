using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for operations related to the designations task in the woodland officer review.
/// </summary>
public interface IDesignationsUseCase
{
    /// <summary>
    /// Retrieves the designations view model for the specified application id.
    /// </summary>
    /// <param name="applicationId">The identifier of the application to retrieve the view model for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated view model.</returns>
    Task<Result<DesignationsViewModel>> GetApplicationDesignationsAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the designations view model for a specific compartment within the specified application.
    /// </summary>
    /// <param name="applicationId">The identifier of the application to retrieve the view model for.</param>
    /// <param name="submittedCompartmentId">The identifier of the submitted compartment to retrieve the view model for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated view model.</returns>
    Task<Result<UpdateDesignationsViewModel>> GetUpdateDesignationsModelAsync(
        Guid applicationId,
        Guid submittedCompartmentId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the compartment designations for the specified application.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="model">A model of the designations to save.</param>
    /// <param name="user">The performing user.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating success or failure.</returns>
    Task<Result> UpdateCompartmentDesignationsAsync(
        Guid applicationId,
        SubmittedCompartmentDesignationsModel model,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the completion status of the compartment designations task for the specified application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to be updated.</param>
    /// <param name="user">The user performing the update.</param>
    /// <param name="isComplete">A flag to indicate if the task is completed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating success or failure.</returns>
    Task<Result> UpdateCompartmentDesignationsCompletionAsync(
        Guid applicationId,
        InternalUser user,
        bool isComplete,
        CancellationToken cancellationToken);
}