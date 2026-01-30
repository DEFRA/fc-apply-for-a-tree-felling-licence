using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.FellingLicenceApplications.Repositories;

public class ExternalUserContextFlaRepository : FellingLicenceApplicationRepositoryBase, IFellingLicenceApplicationExternalRepository
{
    private readonly IApplicationReferenceHelper _generateApplicationReference;
    private readonly IFellingLicenceApplicationReferenceRepository _fellingLicenceApplicationReferenceRepository;

    /// <summary>
    /// Creates a new instance of the <see cref="ExternalUserContextFlaRepository"/> class.
    /// </summary>
    /// <param name="context">A database context.</param>
    /// <param name="generateApplicationReference">A service to generate application reference numbers.</param>
    /// <param name="fellingLicenceApplicationReferenceRepository">A repository class to interact with the database.</param>
    public ExternalUserContextFlaRepository(
        FellingLicenceApplicationsContext context,
        IApplicationReferenceHelper generateApplicationReference,
        IFellingLicenceApplicationReferenceRepository fellingLicenceApplicationReferenceRepository)
        : base(context)
    {
        _generateApplicationReference = generateApplicationReference ?? throw new ArgumentNullException(nameof(generateApplicationReference));
        _fellingLicenceApplicationReferenceRepository = Guard.Against.Null(fellingLicenceApplicationReferenceRepository);
    }

    ///<inheritdoc />
    public async Task<Result<FellingLicenceApplication, UserDbErrorReason>> CreateAndSaveAsync(
        FellingLicenceApplication application,
        string? postFix,
        int? startingOffset,
        CancellationToken cancellationToken)
    {
        var referenceId = await _fellingLicenceApplicationReferenceRepository.GetNextApplicationReferenceIdValueAsync(application.CreatedTimestamp.Year, cancellationToken);

        application.ApplicationReference = _generateApplicationReference.GenerateReferenceNumber(application, referenceId, postFix, startingOffset);
        var entity = Context.FellingLicenceApplications.Add(application).Entity;

        return await Context.SaveEntitiesAsync(cancellationToken).Map(() => entity);
    }

    ///<inheritdoc />
    public async Task<FellingLicenceApplication> AddAsync(
        FellingLicenceApplication application, 
        string? postFix,
        int? startingOffset, 
        CancellationToken cancellationToken)
    {
        var referenceId = await _fellingLicenceApplicationReferenceRepository.GetNextApplicationReferenceIdValueAsync(application.CreatedTimestamp.Year, cancellationToken);

        application.ApplicationReference = _generateApplicationReference.GenerateReferenceNumber(application, referenceId, postFix, startingOffset);

        return Context.FellingLicenceApplications.Add(application).Entity;
    }

    ///<inheritdoc />
    public async Task<IEnumerable<FellingLicenceApplication>> ListAsync(Guid woodlandOwnerId,
        CancellationToken cancellationToken) => await Context.FellingLicenceApplications
        .Include(a => a.StatusHistories)
        .Include(a => a.LinkedPropertyProfile)
        .Include(a => a.ApprovedInError)
        .Where(a => a.WoodlandOwnerId == woodlandOwnerId)
        .ToListAsync(cancellationToken);

    ///<inheritdoc />
    public async Task<Maybe<FellingLicenceApplication>> GetAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var application = await Context.FellingLicenceApplications
            .Include(a => a.StatusHistories)
            .Include(a => a.Documents)
            .Include(a => a.AssigneeHistories)
            .Include(a => a.LinkedPropertyProfile)
            .ThenInclude(p => p!.ProposedFellingDetails)!
            .ThenInclude(d => d.FellingSpecies)
            .Include(a => a.LinkedPropertyProfile)
            .ThenInclude(p => p!.ProposedFellingDetails)!
            .ThenInclude(f => f.ProposedRestockingDetails)!
            .ThenInclude(r => r.RestockingSpecies)
            .Include(x => x.LinkedPropertyProfile)
            .ThenInclude(x => x.ProposedCompartmentDesignations)
            .Include(p => p.FellingLicenceApplicationStepStatus)
            .Include(x => x.PublicRegister)
            .Include(x => x.EnvironmentalImpactAssessment)
            .Include(x => x.SubmittedFlaPropertyDetail)
            .ThenInclude(x => x.SubmittedFlaPropertyCompartments)
            .Where(a => a.Id == applicationId)
            .FirstOrDefaultAsync(cancellationToken);
        return application is null ? Maybe<FellingLicenceApplication>.None : Maybe<FellingLicenceApplication>.From(application);
    }

    ///<inheritdoc />
    public async Task<Maybe<HabitatRestoration>> GetHabitatRestorationAsync(
        Guid applicationId,
        Guid compartmentId,
        CancellationToken cancellationToken)
    {
        var restoration = await Context.HabitatRestorations
            .AsNoTracking()
            .Include(hr => hr.LinkedPropertyProfile)
            .Where(hr => hr.PropertyProfileCompartmentId == compartmentId)
            .Where(hr => Context.LinkedPropertyProfiles
                .Any(lpp => lpp.Id == hr.LinkedPropertyProfileId && lpp.FellingLicenceApplicationId == applicationId))
            .FirstOrDefaultAsync(cancellationToken);

        return restoration is null
            ? Maybe<HabitatRestoration>.None
            : Maybe<HabitatRestoration>.From(restoration);
    }

    ///<inheritdoc />
    public async Task<Maybe<List<Guid>>> GetApplicationComparmentIdsAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await Context.ProposedFellingDetails
            .Include(a => a.LinkedPropertyProfile)
            .Where(p => p.LinkedPropertyProfile.FellingLicenceApplicationId == applicationId)
            .Select(p => p.PropertyProfileCompartmentId).ToListAsync(cancellationToken);

        return !result.Any()
            ? Maybe<List<Guid>>.None
            : Maybe<List<Guid>>.From(result);
    }

    ///<inheritdoc />
    public async Task<Maybe<ApplicationCompartmentDetail>> GetApplicationCompartmentDetailAsync(
        Guid applicationId, 
        Guid woodlandOwnerId, 
        Guid compartmentId,
        CancellationToken cancellationToken)
    {
        var result = await Context.FellingLicenceApplications
            .Include(a => a.StatusHistories)
            .Include(a => a.LinkedPropertyProfile)
            .ThenInclude(p => p!.ProposedFellingDetails)!
            .ThenInclude(d => d.FellingSpecies)
            .Include(a => a.LinkedPropertyProfile)
            .ThenInclude(p => p!.ProposedFellingDetails)!
            .ThenInclude(f => f.ProposedRestockingDetails)!
            .ThenInclude(r => r.RestockingSpecies)
            .Where(a => a.WoodlandOwnerId == woodlandOwnerId && a.Id == applicationId)
            .Select(a => new ApplicationCompartmentDetail {
                ApplicationId = a.Id,
                StatusHistories = a.StatusHistories,
                ApplicationReference = a.ApplicationReference,
                PropertyProfileId = a.LinkedPropertyProfile!.PropertyProfileId,
                ProposedFellingDetails = a.LinkedPropertyProfile!.ProposedFellingDetails!
                    .Where(d => d.PropertyProfileCompartmentId == compartmentId).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
        return result is null ? Maybe<ApplicationCompartmentDetail>.None : Maybe<ApplicationCompartmentDetail>.From(result);
    }

    ///<inheritdoc />
    public async Task<bool> GetIsEditable(Guid fellingLicenceApplicationId, CancellationToken cancellationToken)
    {
        var currentStatus = Context.StatusHistories
            .Where(x => x.FellingLicenceApplicationId == fellingLicenceApplicationId)
            .OrderByDescending(s => s.Created).FirstOrDefault()?.Status ?? FellingLicenceStatus.Draft;

        // Draft, ReturnedToApplicant and WithApplicant are statuses that are editable in the external interface

        return currentStatus is FellingLicenceStatus.Draft or FellingLicenceStatus.WithApplicant or FellingLicenceStatus.ReturnedToApplicant;
    }

    ///<inheritdoc />
    public async Task<LinkedPropertyProfile> GetLinkedPropertyProfileAsync(
        Guid applicationId, 
        CancellationToken cancellationToken)
    {
        var linkedPropertyProfile = await Context.LinkedPropertyProfiles.SingleAsync(x => x.FellingLicenceApplicationId == applicationId, cancellationToken);

        return linkedPropertyProfile;
    }

    /// <inheritdoc />
    public async Task<Maybe<SubmittedFlaPropertyDetail>> GetExistingSubmittedFlaPropertyDetailAsync(
        Guid applicationId, 
        CancellationToken cancellationToken)
    {
        var existingSubmittedFlaPropertyDetail = await Context.SubmittedFlaPropertyDetails
            .Include(x => x.SubmittedFlaPropertyCompartments)
            .SingleOrDefaultAsync(x => x.FellingLicenceApplicationId == applicationId, cancellationToken: cancellationToken);

        return existingSubmittedFlaPropertyDetail is not null 
            ? Maybe<SubmittedFlaPropertyDetail>.From(existingSubmittedFlaPropertyDetail) 
            : Maybe<SubmittedFlaPropertyDetail>.None;
    }

    ///<inheritdoc />
    public async Task AddSubmittedFlaPropertyDetailAsync(
        SubmittedFlaPropertyDetail submittedFlaPropertyDetail, 
        CancellationToken cancellationToken)
    {
        Context.SubmittedFlaPropertyDetails.Add(submittedFlaPropertyDetail);

        await Context.SaveEntitiesAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task DeleteSubmittedFlaPropertyDetailAsync(
        SubmittedFlaPropertyDetail submittedFlaPropertyDetail, 
        CancellationToken cancellationToken)
    {
        Context.SubmittedFlaPropertyDetails.Remove(submittedFlaPropertyDetail);

        await Context.SaveEntitiesAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> DeleteSubmittedFlaPropertyDetailForApplicationAsync(
        Guid applicationId, 
        CancellationToken cancellationToken)
    {
        var existingRecord = await Context.SubmittedFlaPropertyDetails
            .SingleOrDefaultAsync(x => x.FellingLicenceApplicationId == applicationId, cancellationToken)
            .ConfigureAwait(false);

        if (existingRecord != null) {
            Context.SubmittedFlaPropertyDetails.Remove(existingRecord);
            return await Context.SaveEntitiesAsync(cancellationToken);
        }

        return UnitResult.Success<UserDbErrorReason>();
    }

    ///<inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> DeleteFlaAsync(
        FellingLicenceApplication fellingLicenceApplication, 
        CancellationToken cancellationToken)
    {
        var linkedPropertyProfile = Context.LinkedPropertyProfiles.First(x => x.FellingLicenceApplicationId == fellingLicenceApplication.Id);

        Context.ProposedFellingDetails.RemoveRange(Context.ProposedFellingDetails.Where(x => x.LinkedPropertyProfileId == linkedPropertyProfile.Id));
        Context.LinkedPropertyProfiles.Remove(linkedPropertyProfile);

        Context.AssigneeHistories.RemoveRange(Context.AssigneeHistories.Where(x => x.FellingLicenceApplicationId == fellingLicenceApplication.Id));

        Context.Documents.RemoveRange(Context.Documents.Where(x => x.FellingLicenceApplicationId == fellingLicenceApplication.Id));

        Context.StatusHistories.RemoveRange(Context.StatusHistories.Where(x => x.FellingLicenceApplicationId == fellingLicenceApplication.Id));

        Context.FellingLicenceApplications.Remove(Context.FellingLicenceApplications.First(x => x.Id == fellingLicenceApplication.Id));

        return await Context.SaveEntitiesAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<IList<CaseNote>> GetCaseNotesAsync(
        Guid applicationId, 
        bool visibleToApplicantOnly, 
        CancellationToken cancellationToken)
    {
        var caseNotes = await Context.CaseNote
            .Where(x => x.FellingLicenceApplicationId == applicationId && !visibleToApplicantOnly || x.VisibleToApplicant)
            .OrderBy(x => x.CreatedTimestamp)
            .ToListAsync(cancellationToken);

        return caseNotes;

    }

    ///<inheritdoc />
    public async Task<bool> VerifyWoodlandOwnerIdForApplicationAsync(
        Guid woodlandOwnerId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        return await Context.FellingLicenceApplications
            .AnyAsync(a => a.WoodlandOwnerId == woodlandOwnerId && a.Id == applicationId, cancellationToken);
    }

    ///<inheritdoc />
    public async Task<FellingLicenceApplicationStepStatus> GetApplicationStepStatus(
        Guid applicationId, 
        CancellationToken cancellationToken)
    {
        return await Context.FellingLicenceApplicationStepStatus
            .FirstAsync(a => a.FellingLicenceApplicationId == applicationId, cancellationToken);
    }

    ///<inheritdoc />
    public async Task<Maybe<SubmittedFlaPropertyCompartment>> GetSubmittedFlaPropertyCompartmentByIdAsync(
        Guid compartmentId, 
        CancellationToken cancellationToken)
    {
        var compartment = await Context.SubmittedFlaPropertyCompartments
            .FirstOrDefaultAsync(c => c.Id == compartmentId, cancellationToken);

        return compartment is null
            ? Maybe<SubmittedFlaPropertyCompartment>.None
            : Maybe<SubmittedFlaPropertyCompartment>.From(compartment);
    }

    ///<inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> UpdateSubmittedFlaPropertyCompartmentZonesAsync(
        Guid compartmentId,
        bool zone1,
        bool zone2,
        bool zone3,
        CancellationToken cancellationToken)
    {
        var compartment = await Context.SubmittedFlaPropertyCompartments
            .FirstOrDefaultAsync(c => c.Id == compartmentId, cancellationToken);

        if (compartment == null)
        {
            return UnitResult.Failure(UserDbErrorReason.NotFound);
        }

        compartment.Zone1 = zone1;
        compartment.Zone2 = zone2;
        compartment.Zone3 = zone3;

        Context.SubmittedFlaPropertyCompartments.Update(compartment);
        return await Context.SaveEntitiesAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> UpdateExistingWoodlandOfficerReviewFlagsForResubmission(
        Guid applicationId,
        DateTime updatedDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var review = await Context.WoodlandOfficerReviews.SingleOrDefaultAsync(
                x => x.FellingLicenceApplicationId == applicationId, cancellationToken);

            if (review == null)
            {
                // no review exists, so exit early
                return UnitResult.Success<UserDbErrorReason>();
            }

            review.DesignationsComplete = false;
            review.ConfirmedFellingAndRestockingComplete = false;
            review.WoodlandOfficerReviewComplete = false;

            // if the conditions were sent to the applicant, record this date in the OldConditionsSentToApplicantDate field
            // then clear the ConditionsToApplicantDate field
            if (review.ConditionsToApplicantDate.HasValue)
            {
                review.OldConditionsSentToApplicantDate = review.ConditionsToApplicantDate;
                review.ConditionsToApplicantDate = null;
            }

            review.LastUpdatedDate = updatedDate;

            return await Context.SaveEntitiesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return UnitResult.Failure(UserDbErrorReason.General);
        }
    }

    ///<inheritdoc />
    public async Task<IReadOnlyList<HabitatRestoration>> GetHabitatRestorationsAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var restorations = await Context.HabitatRestorations
            .AsNoTracking()
            .Include(hr => hr.LinkedPropertyProfile)
            .Where(hr => Context.LinkedPropertyProfiles
                .Any(lpp => lpp.Id == hr.LinkedPropertyProfileId && lpp.FellingLicenceApplicationId == applicationId))
            .ToListAsync(cancellationToken);

        return restorations;
    }

    ///<inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> UpdateHabitatRestorationAsync(HabitatRestoration habitatRestoration, CancellationToken cancellationToken)
    {
        habitatRestoration.LinkedPropertyProfile = null;

        var tracked = Context.HabitatRestorations.Local.FirstOrDefault(hr => hr.Id == habitatRestoration.Id);
        if (tracked is not null)
        {
            tracked.HabitatType = habitatRestoration.HabitatType;
            tracked.WoodlandSpeciesType = habitatRestoration.WoodlandSpeciesType;
            tracked.NativeBroadleaf = habitatRestoration.NativeBroadleaf;
            tracked.ProductiveWoodland = habitatRestoration.ProductiveWoodland;
            tracked.FelledEarly = habitatRestoration.FelledEarly;
            tracked.Completed = habitatRestoration.Completed;
            tracked.OtherHabitatDescription = habitatRestoration.OtherHabitatDescription;
        }
        else
        {
            Context.HabitatRestorations.Attach(habitatRestoration);
            Context.Entry(habitatRestoration).Property(r => r.HabitatType).IsModified = true;
            Context.Entry(habitatRestoration).Property(r => r.WoodlandSpeciesType).IsModified = true;
            Context.Entry(habitatRestoration).Property(r => r.NativeBroadleaf).IsModified = true;
            Context.Entry(habitatRestoration).Property(r => r.ProductiveWoodland).IsModified = true;
            Context.Entry(habitatRestoration).Property(r => r.FelledEarly).IsModified = true;
            Context.Entry(habitatRestoration).Property(r => r.Completed).IsModified = true;
            Context.Entry(habitatRestoration).Property(r => r.OtherHabitatDescription).IsModified = true;
        }

        return await Context.SaveEntitiesAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> AddHabitatRestorationAsync(
        Guid applicationId,
        Guid compartmentId,
        CancellationToken cancellationToken)
    {
        var matchingLinkedProfileId = await Context.LinkedPropertyProfiles
            .Where(lpp => lpp.FellingLicenceApplicationId == applicationId)
            .Where(lpp => Context.ProposedFellingDetails
                .Any(pfd => pfd.LinkedPropertyProfileId == lpp.Id && pfd.PropertyProfileCompartmentId == compartmentId))
            .Select(lpp => lpp.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (matchingLinkedProfileId == Guid.Empty)
        {
            return UnitResult.Failure(UserDbErrorReason.NotFound);
        }

        var exists = await Context.HabitatRestorations
            .AnyAsync(hr => hr.LinkedPropertyProfileId == matchingLinkedProfileId
                             && hr.PropertyProfileCompartmentId == compartmentId, cancellationToken);

        if (exists)
        {
            return UnitResult.Failure(UserDbErrorReason.NotUnique);
        }

        var toAdd = new HabitatRestoration
        {
            Id = Guid.NewGuid(),
            LinkedPropertyProfileId = matchingLinkedProfileId,
            PropertyProfileCompartmentId = compartmentId
        };

        Context.HabitatRestorations.Add(toAdd);
        return await Context.SaveEntitiesAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> DeleteHabitatRestorationAsync(
        Guid applicationId,
        Guid compartmentId,
        CancellationToken cancellationToken)
    {
        var matchingLinkedProfileId = await Context.LinkedPropertyProfiles
            .Where(lpp => lpp.FellingLicenceApplicationId == applicationId)
            .Select(lpp => lpp.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (matchingLinkedProfileId == Guid.Empty)
        {
            return UnitResult.Success<UserDbErrorReason>();
        }

        var existing = await Context.HabitatRestorations
            .FirstOrDefaultAsync(hr => hr.LinkedPropertyProfileId == matchingLinkedProfileId
                                       && hr.PropertyProfileCompartmentId == compartmentId, cancellationToken);

        if (existing is null)
        {
            return UnitResult.Success<UserDbErrorReason>();
        }

        Context.HabitatRestorations.Remove(existing);
        return await Context.SaveEntitiesAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> DeleteHabitatRestorationsAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var linkedIds = await Context.LinkedPropertyProfiles
            .Where(lpp => lpp.FellingLicenceApplicationId == applicationId)
            .Select(lpp => lpp.Id)
            .ToListAsync(cancellationToken);

        if (linkedIds.Count == 0)
        {
            return UnitResult.Success<UserDbErrorReason>();
        }

        var toDelete = await Context.HabitatRestorations
            .Where(hr => linkedIds.Contains(hr.LinkedPropertyProfileId))
            .ToListAsync(cancellationToken);

        if (toDelete.Count == 0)
        {
            return UnitResult.Success<UserDbErrorReason>();
        }

        Context.HabitatRestorations.RemoveRange(toDelete);
        return await Context.SaveEntitiesAsync(cancellationToken);
    }
}