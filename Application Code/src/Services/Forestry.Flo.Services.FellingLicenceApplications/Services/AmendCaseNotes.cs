using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NodaTime;

namespace Forestry.Flo.Internal.Web;

public class AmendCaseNotes : IAmendCaseNotes
{
    private readonly IDbContextFactory<FellingLicenceApplicationsContext> _dbContextFactory;
    private readonly IClock _clock;
    private readonly ILogger<AmendCaseNotes> _logger;

    /// <summary>
    /// Implementation of <see cref="IAmendCaseNotes"/> that amends case notes within applications.
    /// </summary>
    public AmendCaseNotes(
        IDbContextFactory<FellingLicenceApplicationsContext> dbContextFactory,
        IClock clock,
        ILogger<AmendCaseNotes> logger)
    {
        _dbContextFactory = Guard.Against.Null(dbContextFactory);
        _clock = Guard.Against.Null(clock);
        _logger = logger ?? new NullLogger<AmendCaseNotes>();
    }

    /// <inheritdoc />
    public async Task<Result> AddCaseNoteAsync(
    AddCaseNoteRecord addCaseNoteRecord,
    Guid userId,
    CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            CaseNote CaseNote = new CaseNote()
            {
                FellingLicenceApplicationId = addCaseNoteRecord.FellingLicenceApplicationId,
                Type = addCaseNoteRecord.Type,
                Text = addCaseNoteRecord.Text,
                VisibleToApplicant = addCaseNoteRecord.VisibleToApplicant,
                VisibleToConsultee = addCaseNoteRecord.VisibleToConsultee,
                CreatedTimestamp = _clock.GetCurrentInstant().ToDateTimeUtc(),
                CreatedByUserId = userId,
            };

            await dbContext.CaseNote.AddAsync(CaseNote, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception caught saving Case Note");

            return Result.Failure(e.Message);
        }
    }
}
