using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Domain.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MigrationService.Configuration;
using Npgsql;

namespace MigrationService.Services;

/// <summary>
/// Service class encapsulating all work against the FLOv1 database.
/// </summary>
public class DatabaseServiceV1 : IDatabaseServiceV1
{
    private readonly DataMigrationConfiguration _options;
    private readonly ILogger<DatabaseServiceV1> _logger;
    private readonly NpgsqlDataSource _dataSource;

    public DatabaseServiceV1(
        IOptions<DataMigrationConfiguration> options,
        ILogger<DatabaseServiceV1> logger)
    {
        _options = Guard.Against.Null(options.Value);
        _logger = logger;
        _dataSource = NpgsqlDataSource.Create(_options.DatabaseStorage.V1DefaultConnection);
    }

    /// <inheritdoc />
    public async Task<Result<List<FloUser>>> GetFloV1UsersAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(ResourcesService.GetResourceFileString("GetAllV1Users.sql"), connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<FloUser>();

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new FloUser(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                !reader.IsDBNull(4) ? reader.GetString(4) : null,
                !reader.IsDBNull(5) ? reader.GetString(5) : null,
                !reader.IsDBNull(6) ? reader.GetString(6) : null,
                !reader.IsDBNull(7) ? reader.GetString(7) : null,
                !reader.IsDBNull(8) ? reader.GetString(8) : null,
                !reader.IsDBNull(9) ? reader.GetString(9) : null,
                !reader.IsDBNull(10) ? reader.GetString(10) : null,
                !reader.IsDBNull(11) ? reader.GetString(11) : null,
                !reader.IsDBNull(12) ? reader.GetString(12) : null,
                !reader.IsDBNull(13) ? reader.GetString(13) : null,
                !reader.IsDBNull(14) ? reader.GetString(14) : null,
                !reader.IsDBNull(15) ? reader.GetString(15) : null
                ));
        }

        _logger.LogInformation("Read {userCount} Users from FLOv1", result.Count);
        return Result.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<List<ManagedOwner>>> GetFloV1ManagedOwnersAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(ResourcesService.GetResourceFileString("GetAllV1ManagedOwners.sql"), connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<ManagedOwner>();

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new ManagedOwner(
                reader.GetInt64(0),
                reader.GetInt32(1) != 0,
                !reader.IsDBNull(2) ? reader.GetString(2) : null,
                !reader.IsDBNull(3) ? reader.GetString(3) : null,
                !reader.IsDBNull(4) ? reader.GetString(4) : null,
                !reader.IsDBNull(5) ? reader.GetString(5) : null,
                !reader.IsDBNull(6) ? reader.GetString(6) : null,
                !reader.IsDBNull(7) ? reader.GetString(7) : null,
                !reader.IsDBNull(8) ? reader.GetString(8) : null,
                !reader.IsDBNull(9) ? reader.GetString(9) : null,
                !reader.IsDBNull(10) ? reader.GetString(10) : null,
                !reader.IsDBNull(11) ? reader.GetString(11) : null,
                !reader.IsDBNull(12) ? reader.GetString(12) : null,
                !reader.IsDBNull(13) ? reader.GetString(13) : null,
                !reader.IsDBNull(14) ? reader.GetString(14) : null,
                reader.GetInt64(15),
                reader.GetString(16)
                ));
        }

        _logger.LogInformation("Read {ManagedOwnerCount} Managed Owners from FLOv1", result.Count);
        return Result.Success(result);
    }
}