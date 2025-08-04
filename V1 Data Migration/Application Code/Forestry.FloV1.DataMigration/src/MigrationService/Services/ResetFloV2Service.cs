using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace MigrationService.Services;

public class ResetFloV2Service
{
    private readonly IDatabaseServiceV2 _v2DatabaseService;
    private readonly ILogger<ResetFloV2Service> _logger;

    public ResetFloV2Service(
        IDatabaseServiceV2 v2DatabaseService,
        ILogger<ResetFloV2Service> logger)
    {
        _v2DatabaseService = Guard.Against.Null(v2DatabaseService);
        _logger = logger;
    }

    public async Task<Result> ResetDatabaseAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing user request to Clear down FloV2 Db");
        return await _v2DatabaseService.ResetDatabaseAsync(cancellationToken);
    }

    //todo Azure Blob storage service....if poss.
}