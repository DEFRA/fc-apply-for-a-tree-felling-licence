using System.Data;
using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Forestry.Flo.Services.FellingLicenceApplications.Repositories;

public class FellingLicenceApplicationReferenceRepository: IFellingLicenceApplicationReferenceRepository
{
    private readonly FellingLicenceApplicationsContext _context;

    public FellingLicenceApplicationReferenceRepository(FellingLicenceApplicationsContext context)
    {
        _context = Guard.Against.Null(context);
    }

    /// <inheritdoc />
    public async Task<long> GetNextApplicationReferenceIdValueAsync(int year, CancellationToken cancellationToken)
    {
        var name = $"\"{FellingLicenceApplicationsContext.SchemaName}\".\"{FellingLicenceApplicationsContext.ApplicationReferenceIdsSequenceName}{year}\"";
        var result = new NpgsqlParameter("result", SqlDbType.BigInt)
        {
            Direction = ParameterDirection.Output
        };

        var sql = $"SELECT nextval('{name}') AS result";

        await _context.Database.ExecuteSqlRawAsync(sql, new object[] { result }, cancellationToken);

        return (long)result.Value!;
    }
}