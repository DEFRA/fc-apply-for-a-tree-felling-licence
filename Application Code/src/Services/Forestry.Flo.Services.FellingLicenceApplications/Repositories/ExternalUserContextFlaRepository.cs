using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.Gis.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.FellingLicenceApplications.Repositories;

public class ExternalUserContextFlaRepository : FellingLicenceApplicationRepositoryBase, IFellingLicenceApplicationExternalRepository
{
    private readonly IApplicationReferenceHelper _generateApplicationReference;
    private readonly IFellingLicenceApplicationReferenceRepository _fellingLicenceApplicationReferenceRepository;
    
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
        int? startingOffest, 
        CancellationToken cancellationToken)
    {
        var referenceId = await _fellingLicenceApplicationReferenceRepository.GetNextApplicationReferenceIdValueAsync(application.CreatedTimestamp.Year, cancellationToken);

        application.ApplicationReference = _generateApplicationReference.GenerateReferenceNumber(application, referenceId, postFix, startingOffest);

        return Context.FellingLicenceApplications.Add(application).Entity;
    }

    ///<inheritdoc />
    public async Task<IEnumerable<FellingLicenceApplication>> ListAsync(Guid woodlandOwnerId,
        CancellationToken cancellationToken) => await Context.FellingLicenceApplications
        .Include(a => a.StatusHistories)
        .Include(a => a.LinkedPropertyProfile)
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
            .Include(p => p.FellingLicenceApplicationStepStatus)
            .Include(x => x.PublicRegister)
            .Where(a => a.Id == applicationId)
            .FirstOrDefaultAsync(cancellationToken);
        return application is null ? Maybe<FellingLicenceApplication>.None : Maybe<FellingLicenceApplication>.From(application);
    }

    ///<inheritdoc />
    public async Task<Maybe<List<Guid>>> GetApplicationComparmentIdsAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await Context.ProposedFellingDetails
            .Include(a => a.LinkedPropertyProfile)
            .Where(p => p.LinkedPropertyProfile.FellingLicenceApplicationId == applicationId)
            .Select(p => p.PropertyProfileCompartmentId).ToListAsync(cancellationToken);

        return result is null || !result.Any()
            ? Maybe<List<Guid>>.None
            : Maybe<List<Guid>>.From(result);
    }

    ///<inheritdoc />
    public async Task<Maybe<ApplicationCompartmentDetail>> GetApplicationCompartmentDetailAsync(Guid applicationId, Guid woodlandOwnerId, Guid compartmentId,
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

    public async Task<bool> GetIsEditable(Guid fellingLicenceApplicationId, CancellationToken cancellationToken)
    {
        var currentStatus = Context.StatusHistories.Where(x => x.FellingLicenceApplicationId == fellingLicenceApplicationId).OrderByDescending(s => s.Created).FirstOrDefault()?.Status ?? FellingLicenceStatus.Draft;

        // Draft or WithApplicant are statuses that are editable

        return currentStatus is FellingLicenceStatus.Draft or FellingLicenceStatus.WithApplicant or FellingLicenceStatus.ReturnedToApplicant;
    }

    public async Task<LinkedPropertyProfile> GetLinkedPropertyProfileAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var linkedPropertyProfile = await Context.LinkedPropertyProfiles.SingleAsync(x => x.FellingLicenceApplicationId == applicationId, cancellationToken);

        return linkedPropertyProfile;
    }

    public async Task<SubmittedFlaPropertyDetail?> GetExistingSubmittedFlaPropertyDetailAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var existingSubmittedFlaPropertyDetail = await Context.SubmittedFlaPropertyDetails
                                                               .Include(x => x.SubmittedFlaPropertyCompartments)
                                                               .SingleOrDefaultAsync(x => x.FellingLicenceApplicationId == applicationId);

        return existingSubmittedFlaPropertyDetail;
    }

    public async Task AddSubmittedFlaPropertyDetailAsync(SubmittedFlaPropertyDetail submittedFlaPropertyDetail, CancellationToken cancellationToken)
    {
        Context.SubmittedFlaPropertyDetails.Add(submittedFlaPropertyDetail);

        await Context.SaveEntitiesAsync(cancellationToken);
    }

    public async Task DeleteSubmittedFlaPropertyDetailAsync(SubmittedFlaPropertyDetail submittedFlaPropertyDetail, CancellationToken cancellationToken)
    {
        Context.SubmittedFlaPropertyDetails.Remove(submittedFlaPropertyDetail);

        await Context.SaveEntitiesAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> DeleteSubmittedFlaPropertyDetailForApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
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

    public async Task<UnitResult<UserDbErrorReason>> DeleteFlaAsync(FellingLicenceApplication felingLicenceApplication, CancellationToken cancellationToken)
    {
        var linkedPropertyProfile = Context.LinkedPropertyProfiles.First(x => x.FellingLicenceApplicationId == felingLicenceApplication.Id);

        Context.ProposedFellingDetails.RemoveRange(Context.ProposedFellingDetails.Where(x => x.LinkedPropertyProfileId == linkedPropertyProfile.Id));
        Context.LinkedPropertyProfiles.Remove(linkedPropertyProfile);

        Context.AssigneeHistories.RemoveRange(Context.AssigneeHistories.Where(x => x.FellingLicenceApplicationId == felingLicenceApplication.Id));

        Context.Documents.RemoveRange(Context.Documents.Where(x => x.FellingLicenceApplicationId == felingLicenceApplication.Id));

        Context.StatusHistories.RemoveRange(Context.StatusHistories.Where(x => x.FellingLicenceApplicationId == felingLicenceApplication.Id));

        Context.FellingLicenceApplications.Remove(Context.FellingLicenceApplications.First(x => x.Id == felingLicenceApplication.Id));

        return await Context.SaveEntitiesAsync(cancellationToken);
    }

    public async Task<IList<AssigneeHistory>> GetCurrentlyAssignedAssigneeHistoryAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var currentlyAssignedAssigneeHistories = await Context.AssigneeHistories.Where(x => x.FellingLicenceApplicationId == applicationId && x.TimestampUnassigned == null).ToListAsync(cancellationToken: cancellationToken);

        return currentlyAssignedAssigneeHistories;
    }

    public async Task<IList<CaseNote>> GetCaseNotesAsync(Guid applicationId, bool visibleToApplicantOnly, CancellationToken cancellationToken)
    {
        var caseNotes = await Context.CaseNote
            .Where(x => x.FellingLicenceApplicationId == applicationId && !visibleToApplicantOnly || x.VisibleToApplicant)
            .OrderBy(x => x.CreatedTimestamp)
            .ToListAsync(cancellationToken);

        return caseNotes;

    }

    public async Task<bool> VerifyWoodlandOwnerIdForApplicationAsync(
        Guid woodlandOwnerId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        return await Context
            .FellingLicenceApplications
            .AnyAsync(a => a.WoodlandOwnerId == woodlandOwnerId && a.Id == applicationId, cancellationToken);
    }

    public async Task<FellingLicenceApplicationStepStatus> GetApplicationStepStatus(Guid applicationId)
    {
        return await Context.FellingLicenceApplicationStepStatus.FirstAsync(flas => flas.FellingLicenceApplicationId == applicationId);
    }

    public async Task<Maybe<SubmittedFlaPropertyCompartment>> GetSubmittedFlaPropertyCompartmentByIdAsync(Guid compartmentId, CancellationToken cancellationToken)
    {
        var compartment = await Context.SubmittedFlaPropertyCompartments
            .FirstOrDefaultAsync(c => c.Id == compartmentId, cancellationToken);

        return compartment is null
            ? Maybe<SubmittedFlaPropertyCompartment>.None
            : Maybe<SubmittedFlaPropertyCompartment>.From(compartment);
    }
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

}