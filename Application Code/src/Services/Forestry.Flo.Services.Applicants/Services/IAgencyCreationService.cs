using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Models;

namespace Forestry.Flo.Services.Applicants.Services;

/// <summary>
/// Contract for a service that orchestrates the creation of an <see cref="Agency"/> entity.
/// </summary>
public interface IAgencyCreationService
{
    /// <summary>
    /// Adds a new <see cref="Agency"/> entity to the system.
    /// </summary>
    /// <param name="request">A populated <see cref="AddAgencyDetailsRequest"/> model containing details of the agency to be added and the performing user who is requesting its addition.</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    Task<Result<AddAgencyDetailsResponse>> AddAgencyAsync(
        AddAgencyDetailsRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing <see cref="Agency"/> entity in the system.
    /// </summary>
    /// <param name="request">A populated <see cref="UpdateAgencyDetailsRequest"/> model containing new details to apply to the
    /// agency with the given ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Task<Result> UpdateAgencyAsync(
        UpdateAgencyDetailsRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets an existing agency's details from the system.
    /// </summary>
    /// <param name="agencyId">The ID of the agency to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A model of the existing agency, if found.</returns>
    Task<Result<AgencyModel>> GetAgencyDetailsAsync(
        Guid agencyId,
        CancellationToken cancellationToken);
}