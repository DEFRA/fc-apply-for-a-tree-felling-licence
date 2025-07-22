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
}