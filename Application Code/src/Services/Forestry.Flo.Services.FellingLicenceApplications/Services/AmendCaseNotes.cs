using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class AmendCaseNotes : IAmendCaseNotes
{
    private readonly IFellingLicenceApplicationInternalRepository _repository;
    private readonly IClock _clock;
    private readonly ILogger<AmendCaseNotes> _logger;

    /// <summary>
    /// Implementation of <see cref="IAmendCaseNotes"/> that amends case notes within applications.
    /// </summary>
    public AmendCaseNotes(
        IFellingLicenceApplicationInternalRepository repository,
        IClock clock,
        ILogger<AmendCaseNotes> logger)
    {
        _repository = Guard.Against.Null(repository);
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
            var caseNote = new CaseNote
            {
                FellingLicenceApplicationId = addCaseNoteRecord.FellingLicenceApplicationId,
                Type = addCaseNoteRecord.Type,
                Text = addCaseNoteRecord.Text,
                VisibleToApplicant = addCaseNoteRecord.VisibleToApplicant,
                VisibleToConsultee = addCaseNoteRecord.VisibleToConsultee,
                CreatedTimestamp = _clock.GetCurrentInstant().ToDateTimeUtc(),
                CreatedByUserId = userId,
            };

            await _repository.AddCaseNoteAsync(caseNote, cancellationToken);
            var saveResult = await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            if (saveResult.IsFailure)
            {
                _logger.LogError("Failed to save Case Note: {Error}", saveResult.Error);
                return Result.Failure(saveResult.Error.GetDescription());
            }

            return Result.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception caught saving Case Note");

            return Result.Failure(e.Message);
        }
    }
}
