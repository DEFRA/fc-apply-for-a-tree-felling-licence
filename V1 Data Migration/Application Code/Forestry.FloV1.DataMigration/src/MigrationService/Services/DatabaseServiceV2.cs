using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MigrationService.Configuration;
using Npgsql;
using NpgsqlTypes;
using Guid = System.Guid;

namespace MigrationService.Services;

/// <summary>
/// Service class encapsulating all work against the FLOv2 database.
/// </summary>
public class DatabaseServiceV2 : IDatabaseServiceV2
{
    private readonly ILogger<DatabaseServiceV2> _logger;
    private readonly NpgsqlDataSource _dataSource;

    private const string Flov1IdColumnName = "flov1_id";
    private const string Flov1ManagedOwnerIdColumn = "flov1_managedowner_id";

    private readonly Dictionary<string, string> _replacers = new()
    {
        { $"[{nameof(Flov1IdColumnName)}]", Flov1IdColumnName },
        { $"[{nameof(Flov1ManagedOwnerIdColumn)}]", Flov1ManagedOwnerIdColumn }
    };
    
    public DatabaseServiceV2(
        IOptions<DataMigrationConfiguration> options,
        ILogger<DatabaseServiceV2> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger;
        _dataSource = NpgsqlDataSource.Create(options.Value.DatabaseStorage.V2DefaultConnection);
    }

    /// <summary>
    /// Gets a mapping of Agency Id to Flov1 User Id from the UserAccount table.
    /// </summary>
    /// <remarks>This will only return values for user accounts where the Agency Id is present, i.e.
    /// only for Agent users.</remarks>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="IdMap"/> records, mapping Agency Ids on user accounts to Flov1 User Ids.</returns>
    public async Task<Result<List<IdMap>>> GetAgentUserMappedIds(CancellationToken cancellationToken)
    {
        var idMaps = await GetIdMapsAsync(
            "Applicants",
            "UserAccount",
            "AgencyId",
            Flov1IdColumnName,
            cancellationToken);

        if (idMaps.IsFailure)
        {
            return idMaps;
        }

        var withAgencyId = idMaps.Value.Where(x => x.Flov2Id.HasValue).ToList();
        return Result.Success(withAgencyId);
    }

    public async Task<Result> EnsureNoFlo2UsersExistAsync(
        CancellationToken cancellationToken)
    {
        // TODO should this also check WoodlandOwner and Agency tables given that the same Migrator populates all three?
        var userCount = await GetTableCountAsync("Applicants","UserAccount", cancellationToken);
        return userCount switch
        {
            { IsSuccess: true, Value: > 0 } => Result.Failure(
                $"There are already [{userCount.Value}] external applicant user accounts in this database"),
            { IsFailure: true } => Result.Failure(
                $"Could not query for external applicant users in this database [{userCount.Error}]"),
            _ => Result.Success()
        };
    }

    public async Task<Result> EnsureNoAgentAuthoritiesExistAsync(CancellationToken cancellationToken)
    {
        var aaCount = await GetTableCountAsync("Applicants", "AgentAuthority", cancellationToken);
        return aaCount switch
        {
            { IsSuccess: true, Value: > 0 } => Result.Failure(
                $"There are already [{aaCount.Value}] agent authority entries in this database"),
            { IsFailure: true } => Result.Failure(
                $"Could not query for agent authority entries in this database [{aaCount.Error}]"),
            _ => Result.Success()
        };
    }

    public async Task<Result> EnsureFlov1IdOnUserAccountTableAsync(CancellationToken cancellationToken)
    {
        var check = await CheckColumnExistsAsync("Applicants", "UserAccount", Flov1IdColumnName, cancellationToken);

        if (check.IsFailure)
        {
            return Result.Failure("Could not ensure the existence of the FLOv1 ID column on the UserAccount table");
        }

        if (check.Value)
        {
            return Result.Success();
        }

        return await AddColumnToTableAsync("Applicants", "UserAccount", Flov1IdColumnName, "NUMERIC", null, cancellationToken);
    }

    public async Task<Result> EnsureFlov1ManagedOwnerIdOnWoodlandOwnerTableAsync(CancellationToken cancellationToken)
    {
        var check = await CheckColumnExistsAsync("Applicants", "WoodlandOwner", Flov1ManagedOwnerIdColumn, cancellationToken);

        if (check.IsFailure)
        {
            return Result.Failure("Could not ensure the existence of the FLOv1 Managed Owner ID column on the WoodlandOwner table");
        }

        if (check.Value)
        {
            return Result.Success();
        }

        return await AddColumnToTableAsync("Applicants", "WoodlandOwner", Flov1ManagedOwnerIdColumn, "NUMERIC", null, cancellationToken);
    }

    public async Task<Result<Guid>> AddUserAccountAsync(
        Domain.V2.UserAccount userAccount,
        long flov1UserId,
        Guid? woodlandOwnerId,
        Guid? agencyId,
        CancellationToken cancellationToken)
    {
        object woodlandOwnerIdParam = woodlandOwnerId.HasValue
            ? woodlandOwnerId.Value
            : DBNull.Value;
        object agencyIdParam = agencyId.HasValue
            ? agencyId.Value
            : DBNull.Value;

        var cmd = ResourcesService.GetResourceFileStringWithReplacers("InsertUserAccount.sql", _replacers);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(cmd, connection);
        command.Parameters.Add(new NpgsqlParameter {ParameterName = "@accountType", NpgsqlDbType = NpgsqlDbType.Integer, Value = (int)userAccount.AccountType });
        command.Parameters.Add(new NpgsqlParameter {ParameterName = "@firstName", NpgsqlDbType = NpgsqlDbType.Text, Value = (object) userAccount.FirstName ?? DBNull.Value});
        command.Parameters.Add(new NpgsqlParameter {ParameterName = "@lastName", NpgsqlDbType = NpgsqlDbType.Text, Value = (object) userAccount.LastName ?? DBNull.Value});
        command.Parameters.Add(new NpgsqlParameter {ParameterName = "@email", NpgsqlDbType = NpgsqlDbType.Text, Value = userAccount.Email});
        command.Parameters.Add(new NpgsqlParameter {ParameterName = "@addressLine1", NpgsqlDbType = NpgsqlDbType.Text, Value =(object) userAccount.AddressLine1 ?? DBNull.Value});
        command.Parameters.Add(new NpgsqlParameter {ParameterName = "@addressLine2", NpgsqlDbType = NpgsqlDbType.Text, Value = (object) userAccount.AddressLine2 ?? DBNull.Value});
        command.Parameters.Add(new NpgsqlParameter {ParameterName = "@addressLine3", NpgsqlDbType = NpgsqlDbType.Text, Value = (object) userAccount.AddressLine3 ?? DBNull.Value});
        command.Parameters.Add(new NpgsqlParameter {ParameterName = "@addressLine4", NpgsqlDbType = NpgsqlDbType.Text, Value = (object) userAccount.AddressLine4 ?? DBNull.Value});
        command.Parameters.Add(new NpgsqlParameter {ParameterName = "@postalCode", NpgsqlDbType = NpgsqlDbType.Text, Value = (object) userAccount.AddressPostcode ?? DBNull.Value});
        command.Parameters.Add(new NpgsqlParameter {ParameterName = "@telephone", NpgsqlDbType = NpgsqlDbType.Text, Value = (object) userAccount.Telephone ?? DBNull.Value});
        command.Parameters.Add(new NpgsqlParameter {ParameterName = "@mobile", NpgsqlDbType = NpgsqlDbType.Text, Value = (object) userAccount.MobileTelephone ?? DBNull.Value});
        command.Parameters.Add(new NpgsqlParameter {ParameterName = "@woodlandOwnerId", NpgsqlDbType = NpgsqlDbType.Uuid, Value = woodlandOwnerIdParam});
        command.Parameters.Add(new NpgsqlParameter {ParameterName = "@agencyId", NpgsqlDbType = NpgsqlDbType.Uuid, Value = agencyIdParam});
        command.Parameters.Add(new NpgsqlParameter {ParameterName = "@flov1UserId", NpgsqlDbType = NpgsqlDbType.Numeric, Value = flov1UserId});

        try
        {
            var insertedId = await command.ExecuteScalarAsync(cancellationToken);

            return Result.Success((Guid)insertedId);
        }
        catch (Exception ex)
        {
            OutputCommandDetails(command);
            _logger.LogError(ex, "Exception thrown adding UserAccount to database");
            return Result.Failure<Guid>("Failed to insert user account");
        }
    }

    public async Task<Result<Guid>> AddWoodlandOwnerAsync(
        Domain.V2.WoodlandOwner woodlandOwner, 
        long? flov1ManagedOwnerId,
        CancellationToken cancellationToken)
    {
        var cmd = ResourcesService.GetResourceFileStringWithReplacers("InsertWoodlandOwner.sql", _replacers);
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(cmd, connection);
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@addressLine1", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)woodlandOwner.AddressLine1 ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@addressLine2", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)woodlandOwner.AddressLine2 ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@addressLine3", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)woodlandOwner.AddressLine3 ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@addressLine4", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)woodlandOwner.AddressLine4 ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@postalCode", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)woodlandOwner.PostalCode ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@email", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)woodlandOwner.Email ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@contactName", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)woodlandOwner.ContactName ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@isOrganisation", NpgsqlDbType = NpgsqlDbType.Boolean, Value = woodlandOwner.IsOrganisation });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@organisationName", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)woodlandOwner.OrganisationName ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@telephone", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)woodlandOwner.ContactTelephone ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@managedOwnerId", NpgsqlDbType = NpgsqlDbType.Numeric, Value = (object)flov1ManagedOwnerId ?? DBNull.Value });

        try
        {
            var insertedId = await command.ExecuteScalarAsync(cancellationToken);

            return Result.Success((Guid)insertedId);
        }
        catch (Exception ex)
        {
            OutputCommandDetails(command);
            _logger.LogError(ex, "Exception thrown adding Woodland Owner to database");
            return Result.Failure<Guid>("Failed to insert woodland owner");
        }
    }

    public async Task<Result> UpdateWoodlandOwnerManagedOwnerIdAsync(
        long flov1OwnerId,
        long flov1ManagedOwnerId,
        CancellationToken cancellationToken)
    {
        var cmd = ResourcesService.GetResourceFileStringWithReplacers("UpdateWoodlandOwnerManagedOwnerId.sql", _replacers);
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(cmd, connection);
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@managedOwnerId", NpgsqlDbType = NpgsqlDbType.Numeric, Value = flov1ManagedOwnerId });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@ownerId", NpgsqlDbType = NpgsqlDbType.Numeric, Value = flov1OwnerId });

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            OutputCommandDetails(command);
            _logger.LogError(ex, "Exception thrown updating Woodland Owner Managed Owner Id in database");
            return Result.Failure("Failed to update woodland owner");
        }
    }

    public async Task<Result<Guid>> AddAgencyAsync(Domain.V2.Agency agency, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(ResourcesService.GetResourceFileString("InsertAgency.sql"), connection);
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@addressLine1", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)agency.AddressLine1 ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@addressLine2", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)agency.AddressLine2 ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@addressLine3", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)agency.AddressLine3 ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@addressLine4", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)agency.AddressLine4 ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@postalCode", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)agency.PostalCode ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@email", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)agency.Email ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@contactName", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)agency.ContactName ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@isOrganisation", NpgsqlDbType = NpgsqlDbType.Boolean, Value = agency.IsOrganisation });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@organisationName", NpgsqlDbType = NpgsqlDbType.Text, Value = (object)agency.OrganisationName ?? DBNull.Value });

        try
        {
            var insertedId = await command.ExecuteScalarAsync(cancellationToken);

            return Result.Success((Guid)insertedId);
        }
        catch (Exception ex)
        {
            OutputCommandDetails(command);
            _logger.LogError(ex, "Exception thrown adding Agency to database");
            return Result.Failure<Guid>("Failed to insert agency");
        }
    }

    public async Task<Result<Guid>> AddAgentAuthorityAsync(
        Guid woodlandOwnerId,
        Guid agencyId,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(ResourcesService.GetResourceFileString("InsertAgentAuthority.sql"), connection);
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@woodlandOwnerId", NpgsqlDbType = NpgsqlDbType.Uuid, Value = woodlandOwnerId });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "@agencyId", NpgsqlDbType = NpgsqlDbType.Uuid, Value = agencyId });

        try
        {
            var insertedId = await command.ExecuteScalarAsync(cancellationToken);

            return Result.Success((Guid)insertedId);
        }
        catch (Exception ex)
        {
            OutputCommandDetails(command);
            _logger.LogError(ex, "Exception thrown adding Agent Authority to database");
            return Result.Failure<Guid>("Failed to insert agent authority");
        }
    }

    public async Task<Result> ResetDatabaseAsync(
        CancellationToken cancellationToken)
    {
        var schemaAndTables = new List<Tuple<string, string>> {
                new ("Applicants", "UserAccount"),
                new ("Applicants", "WoodlandOwner"),
                new ("Applicants", "Agency"),
                new ("Applicants", "AgentAuthority")
            };

        try
        {
            foreach (var schemaAndTable in schemaAndTables)
            {
                var sql = $"truncate table \"{schemaAndTable.Item1}\".\"{schemaAndTable.Item2}\" cascade";
                await using var command = _dataSource.CreateCommand(sql);
                await command.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogInformation("Successfully ran statement {sql}", sql);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not complete request to clear down FLOv2 db");
        }

        return Result.Failure("Could not complete request to clear down FLOv2 db");
    }

    private void OutputCommandDetails(NpgsqlCommand command)
    {
        _logger.LogDebug("Command Text: [{commandText}]", command.CommandText);

        for (var index = 0; index < command.Parameters.Count; index++)
        {
            var parameter = command.Parameters[index];
            _logger.LogDebug("Parameter [{index}] = [{parameterValue}]", index + 1, parameter.Value);
        }
    }

    private async Task<Result<List<IdMap>>> GetIdMapsAsync(
    string schema,
    string tableName,
    string flov2ColumnName,
    string flov1IdColumnName,
    CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
                $"SELECT \"{flov2ColumnName}\", \"{flov1IdColumnName}\" FROM \"{schema}\".\"{tableName}\"", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<IdMap>();

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new IdMap(
                !reader.IsDBNull(0) ? reader.GetGuid(0) : null,
                !reader.IsDBNull(1) ? reader.GetInt64(1) : null));
        }

        _logger.LogInformation("Read {idMapCount} FLOv1 <> FLOv2 Id maps from FLOv2", result.Count);
        return Result.Success(result);
    }

    private async Task<Result<long>> GetTableCountAsync(
        string schema,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand($"SELECT count(*) from \"{schema}\".\"{tableName}\";");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            return reader.GetInt32(0);
        }

        _logger.LogError("Could not establish the count of records in table {schema}.{tableName}", schema, tableName);
        return Result.Failure<long>($"Could not establish the count of records in table {schema}.{tableName}");
    }

    private async Task<Result<bool>> CheckColumnExistsAsync(
        string schema,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var sql =
            $"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema='{schema}' and table_name='{tableName}' and column_name='{columnName}';";
        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            return reader.GetInt32(0) != 0;
        }

        _logger.LogError("Could not establish whether the FLOv1 ID column exists in table {schema}.{tableName}", schema, tableName);
        return Result.Failure<bool>($"Could not establish whether the FLOv1 ID column exists in table {schema}.{tableName}");
    }

    private async Task<Result> AddColumnToTableAsync(
        string schema,
        string tableName,
        string columnName,
        string type,
        string? modifiers,
        CancellationToken cancellationToken)
    {
        var sql = $"ALTER TABLE \"{schema}\".\"{tableName}\" ADD COLUMN {columnName} {type} {modifiers ?? string.Empty};";

        await using var command = _dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return Result.Success();
    }
}
