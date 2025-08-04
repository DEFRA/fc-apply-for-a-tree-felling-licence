using CSharpFunctionalExtensions;
using Npgsql;

namespace MigrationService.Services;

public interface IDatabaseServiceV2
{
    /// <summary>
    /// Retrieves a mapping of FLOv2 Agency Id to FLOv1 User Account Id for the users migrated to
    /// the FLOv2 database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="IdMap"/> records containing the FLOv2 Agency Id and FLOv1 user account id for
    /// all agent users already migrated to FLOv2, or <see cref="Result.Failure"/> if the operation fails.</returns>
    Task<Result<List<IdMap>>> GetAgentUserMappedIds(CancellationToken cancellationToken);

    /// <summary>
    /// Checks the FLOv2 database to see if user accounts already exist within it.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct representing the outcome.</returns>
    Task<Result> EnsureNoFlo2UsersExistAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Checks the FLOv2 database to see if agent authority entries already exist within it.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct representing the outcome.</returns>
    Task<Result> EnsureNoAgentAuthoritiesExistAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Ensures that a column to hold the FLOv1 user account id has been added to the user account table
    /// in FLOv2.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct representing the outcome.</returns>
    Task<Result> EnsureFlov1IdOnUserAccountTableAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Ensures that a column to hold the FLOv1 managed owner id has been added to the woodland owner table
    /// in FLOv2.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct representing the outcome.</returns>
    Task<Result> EnsureFlov1ManagedOwnerIdOnWoodlandOwnerTableAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Adds a user account into the FLOv2 database.
    /// </summary>
    /// <param name="userAccount">A <see cref="Domain.V2.UserAccount"/> record representing the user account to add.</param>
    /// <param name="flov1UserId">The id of this user in FLOv1.</param>
    /// <param name="woodlandOwnerId">The id of the woodland owner entity linked to this user, if it is a woodland owner user.</param>
    /// <param name="agencyId">The id of the agency entity linked to this user, if it is an agent user.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The id of the newly-inserted user account entry, or <see cref="Result.Failure"/> if the insert failed.</returns>
    Task<Result<Guid>> AddUserAccountAsync(
        Domain.V2.UserAccount userAccount,
        long flov1UserId,
        Guid? woodlandOwnerId,
        Guid? agencyId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a woodland owner entity into the FLOv2 database.
    /// </summary>
    /// <param name="woodlandOwner">A <see cref="Domain.V2.WoodlandOwner"/> record representing the woodland owner to add.</param>
    /// <param name="flov1ManagedOwnerId">The managed owner id of this woodland owner in FLOv1, if it is a managed owner.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The id of the newly-inserted woodland owner entry, or <see cref="Result.Failure"/> if the insert failed.</returns>
    Task<Result<Guid>> AddWoodlandOwnerAsync(
        Domain.V2.WoodlandOwner woodlandOwner,
        long? flov1ManagedOwnerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing woodland owner entity within the FLOv2 database linked to the user account with the given FLOv1 user id,
    /// to add the FLOv1 managed owner id.
    /// </summary>
    /// <param name="flov1OwnerId">The FLOv1 user account id of the user linked to the woodland owner entity that needs to be updated.</param>
    /// <param name="flov1ManagedOwnerId">The FLOv1 managed owner id to add to the woodland owner entity in the FLOv2 database.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct representing the outcome.</returns>
    Task<Result> UpdateWoodlandOwnerManagedOwnerIdAsync(
        long flov1OwnerId,
        long flov1ManagedOwnerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds an agency into the FLOv2 database.
    /// </summary>
    /// <param name="agency">An <see cref="Domain.V2.Agency"/> record representing the agency to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The id of the newly-inserted agency entry, or <see cref="Result.Failure"/> if the insert failed.</returns>
    Task<Result<Guid>> AddAgencyAsync(Domain.V2.Agency agency, CancellationToken cancellationToken);

    /// <summary>
    /// Adds an agent authority entry into the FLOv2 database.
    /// </summary>
    /// <param name="woodlandOwnerId">The FLOv2 woodland owner id for the agent authority entry.</param>
    /// <param name="agencyId">The FLOv2 agency id for the agent authority entry.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The id of the newly-inserted agent authority entry, or <see cref="Result.Failure"/> if the insert failed.</returns>
    Task<Result<Guid>> AddAgentAuthorityAsync(
        Guid woodlandOwnerId,
        Guid agencyId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resets the FLOv2 database back to the state from which a migration can be run from scratch.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct representing the outcome.</returns>
    Task<Result> ResetDatabaseAsync(CancellationToken cancellationToken);
}