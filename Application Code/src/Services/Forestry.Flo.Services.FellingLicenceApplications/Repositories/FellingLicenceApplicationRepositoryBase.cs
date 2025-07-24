using System.Data.Common;
using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Forestry.Flo.Services.FellingLicenceApplications.Repositories;

public class FellingLicenceApplicationRepositoryBase : IFellingLicenceApplicationBaseRepository
{
    protected FellingLicenceApplicationsContext Context { get; }

    public FellingLicenceApplicationRepositoryBase(FellingLicenceApplicationsContext context)
    {
        Context = Guard.Against.Null(context);
    }

    public IUnitOfWork UnitOfWork => Context;


    ///<inheritdoc />
    public void Update(FellingLicenceApplication application) => Context.Entry(application).State = EntityState.Modified;

    public async Task AddStatusHistory(Guid userId, Guid applicationId, FellingLicenceStatus fellingLicenceStatus, CancellationToken cancellationToken)
    {
        Context.StatusHistories.Add(new StatusHistory
        {
            Created = DateTime.UtcNow,
            FellingLicenceApplicationId = applicationId,
            Status = fellingLicenceStatus,
            CreatedById = userId
        });

        await Context.SaveEntitiesAsync(cancellationToken);
    }

    public async Task<UnitResult<UserDbErrorReason>> UpdateApplicationStepStatusAsync(
        Guid applicationId,
        ApplicationStepStatusRecord applicationStepStatuses,
        CancellationToken cancellationToken)
    {
        var entity = 
            await Context.FellingLicenceApplicationStepStatus
                .FirstOrDefaultAsync(x => x.FellingLicenceApplicationId == applicationId, cancellationToken)
                .ConfigureAwait(false);

        if (entity is null)
        {
            return UserDbErrorReason.NotFound;
        }

        entity.ConstraintCheckStatus = applicationStepStatuses.ConstraintsCheckComplete ?? entity.ConstraintCheckStatus;
        entity.OperationsStatus = applicationStepStatuses.OperationDetailsComplete ?? entity.OperationsStatus;
        entity.TermsAndConditionsStatus = applicationStepStatuses.TermsAndConditionsComplete ?? entity.TermsAndConditionsStatus;
        entity.SupportingDocumentationStatus = applicationStepStatuses.SupportingDocumentationComplete ?? entity.SupportingDocumentationStatus;
        entity.SelectCompartmentsStatus = applicationStepStatuses.SelectedCompartmentsComplete ?? entity.SelectCompartmentsStatus;

        // only update the statuses for matched compartments

        foreach (var compartment in applicationStepStatuses.FellingAndRestockingDetailsComplete)
        {
            var matchedCompartment =
                entity.CompartmentFellingRestockingStatuses.FirstOrDefault(x =>
                    x.CompartmentId == compartment.CompartmentId);

            if (matchedCompartment is null) continue;

            matchedCompartment.Status = compartment.Status;
            matchedCompartment.FellingStatuses = compartment.FellingStatuses;
        }

        return await Context.SaveEntitiesAsync(cancellationToken);
    }

    public async Task<IList<CaseNote>> GetCaseNotesAsync(
        Guid applicationId, 
        CaseNoteType[] caseNoteTypes, 
        CancellationToken cancellationToken)
    {
        var caseNotes = await Context.CaseNote
            .Where(x => x.FellingLicenceApplicationId == applicationId)
            .ToListAsync(cancellationToken);

        if (caseNoteTypes.Any())
        {
            caseNotes = caseNotes.Where(x => caseNoteTypes.Contains(x.Type)).ToList();
        }

        return caseNotes;
    }

    ///<inheritdoc />
    public async Task<Maybe<Document>> GetDocumentByIdAsync(
        Guid applicationId,
        Guid documentIdentifier,
        CancellationToken cancellationToken)
    {
        var document = await Context.Documents.SingleOrDefaultAsync(x => x.Id == documentIdentifier && x.FellingLicenceApplicationId == applicationId);

        return document is null ? Maybe<Document>.None : Maybe<Document>.From(document);
    }

    public async Task<UnitResult<UserDbErrorReason>> DeleteDocumentAsync(Document document, CancellationToken cancellationToken)
    {
        Context.Documents.Remove(document);

        return await UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }

    public async Task<UnitResult<UserDbErrorReason>> AddDocumentAsync(Document document, CancellationToken cancellationToken)
    {
        Context.Documents.Add(document);

        return await UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }

    public async Task<bool> CheckApplicationExists(Guid applicationId, CancellationToken cancellationToken)
    {
        return await Context.FellingLicenceApplications.AnyAsync(x => x.Id == applicationId, cancellationToken);
    }

    ///<inheritdoc />
    public async Task<Result<bool>> CheckUserCanAccessApplicationAsync(Guid applicationId, UserAccessModel userAccess, CancellationToken cancellationToken)
    {
        var woodlandOwnerId = await Context.FellingLicenceApplications
            .Where(x => x.Id == applicationId)
            .Select(x => x.WoodlandOwnerId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (woodlandOwnerId ==  Guid.Empty)
        {
            return Result.Failure<bool>("Could not locate application with the given id");
        }

        var result = userAccess.CanManageWoodlandOwner(woodlandOwnerId);

        return Result.Success(result);
    }

    ///<inheritdoc />
    public async Task<Result<string>> GetApplicationReferenceAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var applicationReference = await Context.FellingLicenceApplications
            .Where(x => x.Id == applicationId)
            .Select(x => x.ApplicationReference)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(applicationReference)
            ? Result.Failure<string>("Could not locate application with the given id") 
            : Result.Success(applicationReference);
    }

    ///<inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> DeleteAdminOfficerReviewForApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var review = await Context.AdminOfficerReviews
            .SingleOrDefaultAsync(x => x.FellingLicenceApplicationId == applicationId, cancellationToken)
            .ConfigureAwait(false);

        if (review != null)
        {
            Context.AdminOfficerReviews.Remove(review);
            return await Context.SaveEntitiesAsync(cancellationToken).ConfigureAwait(false);
        }

        return UnitResult.Success<UserDbErrorReason>();
    }

    /// <inheritdoc />
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken) => Context.Database.BeginTransactionAsync(cancellationToken);

    /// <inheritdoc />
    public DbConnection GetDbConnection() => Context.Database.GetDbConnection();
}