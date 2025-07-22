using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Contract for a service used by the external app that updates a <see cref="FellingLicenceApplication"/>./>
/// </summary>
public interface IUpdateFellingLicenceApplicationForExternalUsers
{
    /// <summary>
    /// Submits the felling licence application with the provided id.
    /// </summary>
    /// <remarks>This may result in the application being in either <see cref="FellingLicenceStatus.Submitted"/> or
    /// <see cref="FellingLicenceStatus.WoodlandOfficerReview"/> state, depending on the current state of the application.</remarks>
    /// <param name="applicationId">The id of the application to submit.</param>
    /// <param name="userAccessModel">A populated <see cref="UserAccessModel"/> representing the permissions of the performing user.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="SubmitFellingLicenceApplicationResponse"/> model if successful,
    /// otherwise <see cref="Result.Failure"/>.</returns>
    Task<Result<SubmitFellingLicenceApplicationResponse>> SubmitFellingLicenceApplicationAsync(
        Guid applicationId,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds the given submitted FLA property detail to the application with the given id.
    /// TODO this should take a Model as input, not the entity class
    /// </summary>
    /// <param name="propertyDetail">The property details to add to the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating the success or otherwise of adding the details.</returns>
    Task<Result> AddSubmittedFellingLicenceApplicationPropertyDetailAsync(
        SubmittedFlaPropertyDetail propertyDetail,
        CancellationToken cancellationToken);
    Task<Result<SubmittedFlaPropertyCompartment>> GetSubmittedFlaPropertyCompartmentByIdAsync(Guid compartmentId, CancellationToken cancellationToken);
    Task<Result> UpdateSubmittedFlaPropertyCompartmentZonesAsync(Guid compartmentId, bool zone1, bool zone2, bool zone3, CancellationToken cancellationToken);
}