using CSharpFunctionalExtensions;
using Domain.V1;

namespace MigrationService.Services;

/// <summary>
/// Contract defining a service that interacts with the FLOv1 database.
/// </summary>
public interface IDatabaseServiceV1
{
    /// <summary>
    /// Retrieves all user accounts from FLOv1.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="FloUser"/> models representing the user accounts in FLOv1, or <see cref="Result.Failure"/>
    /// if the operation failed.</returns>
    Task<Result<List<FloUser>>> GetFloV1UsersAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all managed owners from FLOv1.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="ManagedOwner"/> models representing the managed owners in FLOv1, or <see cref="Result.Failure"/>
    /// if the operation failed.</returns>
    Task<Result<List<ManagedOwner>>> GetFloV1ManagedOwnersAsync(CancellationToken cancellationToken);
}