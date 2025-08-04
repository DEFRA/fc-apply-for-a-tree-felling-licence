using System.Collections.Concurrent;
using CSharpFunctionalExtensions;
using Domain;
using Domain.V1;
using Domain.V2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MigrationHostApp.CommandOptions;
using MigrationHostApp.Validation;
using MigrationService.Configuration;
using MigrationService.Services;

namespace MigrationHostApp.MigrationModules;

/// <summary>
/// This class inherits from <see cref="MigratorBase"/> and provides a
/// specific implementation that is responsible for the db migration of
/// Managed owners and Agent Authorities into FLOv2.
/// </summary>
public class ManagedOwnersMigrator : MigratorBase
{
    private readonly IDatabaseServiceV1 _v1DatabaseService;
    private readonly IDatabaseServiceV2 _v2DatabaseService;
    private readonly ILogger<ManagedOwnersMigrator> _logger;
    private readonly ParallelOptions _parallelOptions;

    private ConcurrentBag<ManagedOwner>? _flov1ManagedOwners;
    private ConcurrentDictionary<long, Guid>? _agentIdsMap;

    public ManagedOwnersMigrator(
        IDatabaseServiceV1 v1DatabaseService,
        IDatabaseServiceV2 v2DatabaseService,
        IOptions<DataMigrationConfiguration> dataMigrationConfiguration,
        ILogger<ManagedOwnersMigrator> logger)
    {
        ArgumentNullException.ThrowIfNull(v1DatabaseService);
        ArgumentNullException.ThrowIfNull(v2DatabaseService);
        ArgumentNullException.ThrowIfNull(dataMigrationConfiguration);

        _v1DatabaseService = v1DatabaseService;
        _v2DatabaseService = v2DatabaseService;

        _parallelOptions = new ParallelOptions
            { MaxDegreeOfParallelism = dataMigrationConfiguration.Value.ParallelismSettings.MaxDegreeOfParallelism };

        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> CanBeExecutedAsync(CancellationToken cancellationToken)
    {
        // check no agent authorities already exist
        var ensureNoAgentAuthoritiesResult = await _v2DatabaseService.EnsureNoAgentAuthoritiesExistAsync(cancellationToken);
        if (ensureNoAgentAuthoritiesResult.IsFailure)
        {
            _logger.LogCritical("Could not execute migration - {result}", ensureNoAgentAuthoritiesResult.Error);

            return ensureNoAgentAuthoritiesResult;
        }

        // check the FLOv1 ID column has been added to UserAccount table (for looking up agent -> agency id/woodland owner -> woodland owner id)
        var ensureFlo1IdColumnResult = await _v2DatabaseService.EnsureFlov1IdOnUserAccountTableAsync(cancellationToken);
        if (ensureFlo1IdColumnResult.IsFailure)
        {
            _logger.LogCritical("Could not execute migration - {result}", ensureFlo1IdColumnResult.Error);

            return ensureFlo1IdColumnResult;
        }

        // check the Flov1ManagedOwnerId column has been added to the WoodlandOwner table
        var ensureFlo1ManagedOwnerIdColumnResult = await _v2DatabaseService.EnsureFlov1ManagedOwnerIdOnWoodlandOwnerTableAsync(cancellationToken);
        if (ensureFlo1ManagedOwnerIdColumnResult.IsFailure)
        {
            _logger.LogCritical("Could not execute migration - {result}", ensureFlo1ManagedOwnerIdColumnResult.Error);

            return ensureFlo1ManagedOwnerIdColumnResult;
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public override async Task<Result<PreValidationResultSuccess, PreValidationResultError>> PreValidateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var getV1ManagedOwnersResult = await LoadManagedWoodlandOwnersFromFlov1(cancellationToken);

            if (getV1ManagedOwnersResult.IsFailure)
            {
                return Result.Failure<PreValidationResultSuccess, PreValidationResultError>(new PreValidationResultError
                {
                    ExceptionMessage = getV1ManagedOwnersResult.Error
                });
            }

            var getAgencyFlov1AccountIdMappingResult = await LoadAgencyIdsFromFlov2(cancellationToken);

            if (getAgencyFlov1AccountIdMappingResult.IsFailure)
            {
                return Result.Failure<PreValidationResultSuccess, PreValidationResultError>(new PreValidationResultError
                {
                    ExceptionMessage = getAgencyFlov1AccountIdMappingResult.Error
                });
            }

            _flov1ManagedOwners = getV1ManagedOwnersResult.Value;
            _agentIdsMap = getAgencyFlov1AccountIdMappingResult.Value;

            Parallel.ForEach(_flov1ManagedOwners, _parallelOptions, ValidateFlov1ManagedOwner);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during Validation");
            return Result.Failure<PreValidationResultSuccess, PreValidationResultError>(new PreValidationResultError
            {
                ValidationResultFailures = ValidationResultFailures.ToArray(),
                ExceptionMessage = ex.Message
            });
        }

        if (ValidationResultFailures.Any())
        {
            return Result.Failure<PreValidationResultSuccess, PreValidationResultError>(new PreValidationResultError
            {
                ValidationResultFailures = ValidationResultFailures.ToArray()
            });
        }

        return new Result<PreValidationResultSuccess, PreValidationResultError>();
    }

    /// <inheritdoc />
    public override async Task<Result> MigrateAsync(MigrationOptions migrationOptions, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running Migrate task in {Migrator}", nameof(ManagedOwnersMigrator));

        if (_flov1ManagedOwners is null || _flov1ManagedOwners.IsEmpty)
        {
            _logger.LogInformation("Detected that pre-validate has not been run, loading FLOv1 managed owners from database");

            var getManagedOwnersResult = await LoadManagedWoodlandOwnersFromFlov1(cancellationToken);

            if (getManagedOwnersResult.IsFailure)
            {
                return getManagedOwnersResult.ConvertFailure();
            }

            _flov1ManagedOwners = getManagedOwnersResult.Value;
        }

        if (_agentIdsMap is null || _agentIdsMap.IsEmpty)
        {
            _logger.LogInformation("Detected that pre-validate has not been run, loading v2 agency id/v1 user id mappings from database");

            var getIdMapsResult = await LoadAgencyIdsFromFlov2(cancellationToken);

            if (getIdMapsResult.IsFailure)
            {
                return getIdMapsResult.ConvertFailure();
            }

            _agentIdsMap = getIdMapsResult.Value;
        }

        var failureErrors = new ConcurrentBag<string>();
        await Parallel.ForEachAsync(_flov1ManagedOwners, _parallelOptions, async (managedOwner, ct) =>
        {
            Guid? agencyId = null;
            if (_agentIdsMap.TryGetValue(managedOwner.AgentUserId, out var mappedAgencyId))
            {
                agencyId = mappedAgencyId;
            }
            else
            {
                // no mapped agency id is only an error if the managed owner is managed by a standard agent
                if (managedOwner.AgentRoleName == "agent")
                {
                    failureErrors.Add($"No Agency Id found for agent FLOv1 id {managedOwner.AgentUserId}");
                    return;
                }
            }

            var result = await InsertManagedOwnerEntities(managedOwner, agencyId, ct);
            if (result.IsFailure)
            {
                failureErrors.Add(result.Error);
            }
        });

        foreach (var failureError in failureErrors)
        {
            _logger.LogCritical("Failed to insert managed owner: {Error}", failureError);
        }

        return failureErrors.Any() ? Result.Failure("Failed to insert managed owners") : Result.Success();

    }

    public override Task<Result> VerifyAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override Task<Result> SummariseAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private void ValidateFlov1ManagedOwner(ManagedOwner flov1ManagedOwner)
    {
        CheckForMissingData(flov1ManagedOwner.ManagedOwnerId, nameof(flov1ManagedOwner.FirstName), flov1ManagedOwner.FirstName);
        CheckForMissingData(flov1ManagedOwner.ManagedOwnerId, nameof(flov1ManagedOwner.LastName), flov1ManagedOwner.LastName);
        CheckForMissingData(flov1ManagedOwner.ManagedOwnerId, nameof(flov1ManagedOwner.AddressLine1), flov1ManagedOwner.AddressLine1);
        CheckForMissingData(flov1ManagedOwner.ManagedOwnerId, nameof(flov1ManagedOwner.AddressLine3), flov1ManagedOwner.AddressLine3);
        CheckForMissingData(flov1ManagedOwner.ManagedOwnerId, nameof(flov1ManagedOwner.PostalCode), flov1ManagedOwner.PostalCode);
        CheckForMissingData(flov1ManagedOwner.ManagedOwnerId, nameof(flov1ManagedOwner.Email), flov1ManagedOwner.Email);

        if (flov1ManagedOwner.AgentRoleName == "agent" 
            && !_agentIdsMap.TryGetValue(flov1ManagedOwner.AgentUserId, out _))
        {
            ValidationResultFailures.Add(new ValidationResultFailure
            {
                RowId = flov1ManagedOwner.ManagedOwnerId,
                ItemValidationIssue = DataItemValidationIssue.DataMismatch,
                FieldName = nameof(flov1ManagedOwner.AgentUserId),
                Message = "No mapped FLOv2 agency id was found for the agent ID linked to a Managed Owner"
            });
        }
    }

    private async Task<Result<ConcurrentBag<ManagedOwner>>> LoadManagedWoodlandOwnersFromFlov1(CancellationToken cancellationToken)
    {
        var getManagedWoodlandOwnersResult = await _v1DatabaseService.GetFloV1ManagedOwnersAsync(cancellationToken);

        if (getManagedWoodlandOwnersResult.IsFailure)
        {
            return getManagedWoodlandOwnersResult.ConvertFailure<ConcurrentBag<ManagedOwner>>();
        }

        // check for >0
        var managedOwners = getManagedWoodlandOwnersResult.Value;

        if (!managedOwners.Any())
        {
            return Result.Failure<ConcurrentBag<ManagedOwner>>("No managed owners found in FLOv1 data set");
        }

        // load into concurrentbag to process them in parallel
        var result = new ConcurrentBag<ManagedOwner>();
        foreach (var managedOwner in managedOwners)
        {
            result.Add(managedOwner);
        }

        return Result.Success(result);
    }

    private async Task<Result<ConcurrentDictionary<long, Guid>>> LoadAgencyIdsFromFlov2(CancellationToken cancellationToken)
    {
        var getAgencyUserIdMappingsResult = await _v2DatabaseService.GetAgentUserMappedIds(cancellationToken);

        if (getAgencyUserIdMappingsResult.IsFailure)
        {
            return getAgencyUserIdMappingsResult.ConvertFailure<ConcurrentDictionary<long, Guid>>();
        }

        // check for any with a missing id
        var idMaps = getAgencyUserIdMappingsResult.Value;
        if (idMaps.Any(x => !x.Flov1Id.HasValue || !x.Flov2Id.HasValue))
        {
            return Result.Failure<ConcurrentDictionary<long, Guid>>("Agency or FLOv1 Id was missing from a User Account in FLOv2 data set");
        }

        //load into a concurrentdictionary to use the mappings in parallel threads
        var result = new ConcurrentDictionary<long, Guid>();
        foreach (var idMap in getAgencyUserIdMappingsResult.Value)
        {
            result.TryAdd(idMap.Flov1Id!.Value, idMap.Flov2Id!.Value);
        }

        return Result.Success(result);
    }

    private async Task<Result> InsertManagedOwnerEntities(
        ManagedOwner managedOwner,
        Guid? agencyId,
        CancellationToken cancellationToken)
    {

        // if this is managed by the owner then just update the managed owner id on the existing woodland owner entity already created for the user
        if (managedOwner.AgentRoleName == "owner")
        {
            var updateResult = await _v2DatabaseService.UpdateWoodlandOwnerManagedOwnerIdAsync(
                managedOwner.AgentUserId, managedOwner.ManagedOwnerId, cancellationToken);

            if (updateResult.IsFailure)
            {
                return updateResult;
            }
        }
        // otherwise add the new woodland owner entity
        else
        {
            var woodlandOwner = managedOwner.ToWoodlandOwner();

            var insertWoodlandOwnerResult = await _v2DatabaseService.AddWoodlandOwnerAsync(
                woodlandOwner, managedOwner.ManagedOwnerId, cancellationToken);

            if (insertWoodlandOwnerResult.IsFailure)
            {
                return insertWoodlandOwnerResult.ConvertFailure();
            }

            // don't need an agent authority entry for owners managed by an FC user
            if (agencyId.HasValue)
            {
                var insertAgentAuthorityResult = await _v2DatabaseService.AddAgentAuthorityAsync(
                    insertWoodlandOwnerResult.Value, agencyId.Value, cancellationToken);

                if (insertAgentAuthorityResult.IsFailure)
                {
                    return insertAgentAuthorityResult.ConvertFailure();
                }
            }
        }



        return Result.Success();
    }
}