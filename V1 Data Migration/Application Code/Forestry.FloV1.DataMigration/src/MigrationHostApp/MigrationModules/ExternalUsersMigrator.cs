using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Domain.V1;
using Microsoft.Extensions.Logging;
using MigrationHostApp.Validation;
using MigrationService.Services;
using System.Collections.Concurrent;
using MigrationHostApp.CommandOptions;
using Domain;
using Domain.V2;
using Microsoft.Extensions.Options;
using MigrationService.Configuration;

namespace MigrationHostApp.MigrationModules;

/// <summary>
/// This class inherits from <see cref="MigratorBase"/> and provides a
/// specific implementation that is responsible for the db migration of
/// FLOv1 User accounts, woodland owners and Agencies into FLOv2.
/// </summary>
public class ExternalUsersMigrator : MigratorBase
{
    private readonly IDatabaseServiceV1 _v1DatabaseService;
    private readonly IDatabaseServiceV2 _v2DatabaseService;
    private readonly ILogger<ExternalUsersMigrator> _logger;
    private readonly ParallelOptions _parallelOptions;
    private ConcurrentBag<FloUser>? _loadedFlov1Users;

    public ExternalUsersMigrator(
        IDatabaseServiceV1 v1DatabaseService,
        IDatabaseServiceV2 v2DatabaseService,
        IOptions<DataMigrationConfiguration> dataMigrationConfiguration,
        ILogger<ExternalUsersMigrator> logger)
    {
        _v1DatabaseService = Guard.Against.Null(v1DatabaseService);
        _v2DatabaseService = Guard.Against.Null(v2DatabaseService);
       
        var dataMigrationConfiguration1 = Guard.Against.Null(dataMigrationConfiguration).Value;
        
        _parallelOptions = new ParallelOptions
            { MaxDegreeOfParallelism = dataMigrationConfiguration1.ParallelismSettings.MaxDegreeOfParallelism};

        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> CanBeExecutedAsync(CancellationToken cancellationToken)
    {
        // check no user accounts already exist
        var ensureNoUsersResult = await _v2DatabaseService.EnsureNoFlo2UsersExistAsync(cancellationToken);
        if (ensureNoUsersResult.IsFailure)
        {
            _logger.LogCritical("Could not execute migration - {result}", ensureNoUsersResult.Error);

            return ensureNoUsersResult;
        }

        // check the FLOv1 ID column has been added to UserAccount table
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
            var getV1UsersResult = await LoadUsersFromFlov1(cancellationToken);

            if (getV1UsersResult.IsSuccess)
            {
                _loadedFlov1Users = getV1UsersResult.Value;

                Parallel.ForEach(_loadedFlov1Users, _parallelOptions, ValidateFlov1User);
            }
            else
            {
                return Result.Failure<PreValidationResultSuccess, PreValidationResultError>(new PreValidationResultError
                {
                    ExceptionMessage = getV1UsersResult.Error
                });
            }
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
    public override async Task<Result> MigrateAsync(
        MigrationOptions migrationOptions,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running Migrate task in {Migrator}", nameof(ExternalUsersMigrator));

        if (_loadedFlov1Users is null || _loadedFlov1Users.IsEmpty)
        {
            _logger.LogInformation("Detected that pre-validate has not been run, loading FLOv1 users from database");
            var getV1UsersResult = await LoadUsersFromFlov1(cancellationToken);

            if (getV1UsersResult.IsFailure)
            {
                return getV1UsersResult.ConvertFailure();
            }

            _loadedFlov1Users = getV1UsersResult.Value;
        }

        var failureErrors = new ConcurrentBag<string>();
        await Parallel.ForEachAsync(_loadedFlov1Users, _parallelOptions, async (floV1User, ct) =>
        {
            var result = await InsertAccountEntities(floV1User, migrationOptions.EmailAddressToUse, ct);
            if (result.IsFailure)
            {
                failureErrors.Add(result.Error);
            }
        });

        foreach (var failureError in failureErrors)
        {
            _logger.LogCritical("Failed to insert user account: {Error}", failureError);
        }

        return failureErrors.Any() ? Result.Failure("Failed to insert user accounts") : Result.Success();
    }

    /// <inheritdoc />
    public override Task<Result> VerifyAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override Task<Result> SummariseAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<Result<ConcurrentBag<Domain.V1.FloUser>>> LoadUsersFromFlov1(CancellationToken cancellationToken)
    {
        var getV1UsersResult = await _v1DatabaseService.GetFloV1UsersAsync(cancellationToken);

        if (getV1UsersResult.IsFailure)
        {
            return Result.Failure<ConcurrentBag<Domain.V1.FloUser>>("Could not load users from FLOv1 user data set");
        }

        // check for >0 and email uniqueness
        var users = getV1UsersResult.Value;

        if (!users.Any())
        {
            return Result.Failure<ConcurrentBag<Domain.V1.FloUser>>("No users found in FLOv1 user data set");
        }

        if (users.DistinctBy(x => x.Email).Count() != users.Count)
        {
            return Result.Failure<ConcurrentBag<Domain.V1.FloUser>>("Duplicate email usage was found in FLOv1 user data set");
        }
        _logger.LogInformation("All FLOv1 users have distinct email addresses");

        // load into concurrentbag to process them in parallel
        var loadedFlov1Users = new ConcurrentBag<FloUser>();
        foreach (var loadedFlov1User in getV1UsersResult.Value)
        {
            loadedFlov1Users.Add(loadedFlov1User);
        }

        return Result.Success(loadedFlov1Users);
    }

    private void ValidateFlov1User(Domain.V1.FloUser user)
    {
        CheckForMissingData(user.UserId, nameof(user.FirstName), user.FirstName);
        CheckForMissingData(user.UserId, nameof(user.LastName), user.LastName);
        CheckForMissingData(user.UserId, nameof(user.AddressLine1), user.AddressLine1);

        //todo this may need to be relaxed.. to check for line 4 being empty also, and only fail then
        CheckForMissingData(user.UserId, nameof(user.AddressLine3), user.AddressLine3);
        CheckForMissingData(user.UserId, nameof(user.PostalCode), user.PostalCode);

        if (string.IsNullOrEmpty(user.TelephoneNumber)
            && string.IsNullOrEmpty(user.MobileTelephoneNumber))
        {
            CheckForMissingData(user.UserId, nameof(user.TelephoneNumber), user.TelephoneNumber, "Must be a contact phone number for this user");
            CheckForMissingData(user.UserId, nameof(user.MobileTelephoneNumber), user.MobileTelephoneNumber, "Must be a contact phone number for this user");
        }
    }

    private async Task<Result> InsertAccountEntities(
        FloUser user, 
        string? customEmailAddressToUse,
        CancellationToken cancellationToken)
    {
        Guid? woodlandOwnerId = null;
        Guid? agencyId = null;
        var userAccount = user.ToUserAccount(customEmailAddressToUse);

        if (userAccount.AccountType is AccountType.WoodlandOwner or AccountType.WoodlandOwnerAdministrator)
        {
            var woodlandOwner = user.ToWoodlandOwner();
            var insertWoodlandOwnerResult = await _v2DatabaseService.AddWoodlandOwnerAsync(woodlandOwner, null, cancellationToken); 

            if (insertWoodlandOwnerResult.IsFailure)
            {
                return insertWoodlandOwnerResult.ConvertFailure();
            }

            woodlandOwnerId = insertWoodlandOwnerResult.Value;
        }
        
        if (userAccount.AccountType is AccountType.Agent or AccountType.AgentAdministrator)
        {
            var agency = user.ToAgency();
            var insertAgencyResult = await _v2DatabaseService.AddAgencyAsync(agency, cancellationToken);

            if (insertAgencyResult.IsFailure)
            {
                return insertAgencyResult.ConvertFailure();
            }

            agencyId = insertAgencyResult.Value;
        }

        if (userAccount.AccountType is AccountType.FcUser)
        {
            _logger.LogInformation("Skipping inserting account for FC Internal Agent");
            return Result.Success();
        }

        var insertAccountResult = await _v2DatabaseService.AddUserAccountAsync(
            userAccount, user.UserId, woodlandOwnerId, agencyId, cancellationToken);

        if (insertAccountResult.IsFailure)
        {
            return insertAccountResult.ConvertFailure();
        }

        return Result.Success();
    }
}