using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.PawsDesignations;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.External.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for a usecase dealing with the collection of PAWS
/// designations data for an application.
/// </summary>
public interface ICollectPawsDataUseCase
{
    /// <summary>
    /// Gets the PAWS designation view model for the specified application.
    /// </summary>
    /// <param name="user">The user viewing the application.</param>
    /// <param name="applicationId">The id of the application being viewed.</param>
    /// <param name="currentId">The id of the compartment designations currently being viewed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="PawsDesignationsViewModel"/> view model for the PAWS
    /// designations data in the application.</returns>
    Task<Result<PawsDesignationsViewModel>> GetPawsDesignationsViewModelAsync(
        ExternalApplicant user,
        Guid applicationId,
        Guid? currentId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the PAWS designations for a compartment in the specified application
    /// with the supplied data.
    /// </summary>
    /// <param name="user">The user updating the application.</param>
    /// <param name="applicationId">The id of the application being updated.</param>
    /// <param name="pawsModel">A <see cref="PawsCompartmentDesignationsModel"/> model containing the data
    /// input by the user.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating success of the operation.</returns>
    Task<Result> UpdatePawsDesignationsForCompartmentAsync(
        ExternalApplicant user,
        Guid applicationId,
        PawsCompartmentDesignationsModel pawsModel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the identifier of the next compartment designation to be edited, if there is one.
    /// </summary>
    /// <param name="user">The user viewing the application.</param>
    /// <param name="applicationId">The id of the application being viewed.</param>
    /// <param name="currentId">The id of the current compartment designations being viewed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="PawsRedirectResult"/> containing the id of the next compartment designations to be edited
    /// if there is one, and whether the application requires an EIA check.</returns>
    Task<Result<PawsRedirectResult>> GetNextCompartmentDesignationsIdAsync(
        ExternalApplicant user,
        Guid applicationId,
        Guid currentId,
        CancellationToken cancellationToken);
}