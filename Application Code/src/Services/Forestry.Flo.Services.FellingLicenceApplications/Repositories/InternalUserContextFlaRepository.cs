using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.FellingLicenceApplications.Repositories;

/// <summary>
/// Defines a repository of <see cref="FellingLicenceApplication"/> entities
/// that is intended only for use by the Internal user web app.
/// </summary>
public class InternalUserContextFlaRepository : FellingLicenceApplicationRepositoryBase, IFellingLicenceApplicationInternalRepository
{
    public InternalUserContextFlaRepository(
        FellingLicenceApplicationsContext context) 
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Maybe<FellingLicenceApplication>> GetAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var application = await Context.FellingLicenceApplications
            .Include(a => a.StatusHistories)
            .Include(a => a.Documents)
            .Include(a => a.CaseNotes)
            .Include(a => a.ExternalAccessLinks)
            .Include(a => a.AssigneeHistories)
            .Include(a => a.SubmittedFlaPropertyDetail)
                .ThenInclude(c => c!.SubmittedFlaPropertyCompartments)!
                .ThenInclude(f => f.ConfirmedFellingDetails)
                .ThenInclude(s => s!.ConfirmedFellingSpecies)
            .Include(a => a.SubmittedFlaPropertyDetail)
                .ThenInclude(c => c!.SubmittedFlaPropertyCompartments)!
                .ThenInclude(f => f.ConfirmedFellingDetails)!
                .ThenInclude(r => r!.ConfirmedRestockingDetails)!
                .ThenInclude(s => s!.ConfirmedRestockingSpecies)
            .Include(a => a.LinkedPropertyProfile)
                .ThenInclude(p => p!.ProposedFellingDetails)!
                .ThenInclude(d => d.FellingSpecies)
            .Include(a => a.LinkedPropertyProfile)
                .ThenInclude(p => p!.ProposedFellingDetails)!
                .ThenInclude(f => f!.ProposedRestockingDetails)!
                .ThenInclude(r => r.RestockingSpecies)
            .Include(a => a.AdminOfficerReview)
            .Include(a => a.PublicRegister)
            .Include(a => a.LarchCheckDetails)
            .Include(x => x.WoodlandOfficerReview)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == applicationId, cancellationToken);
        return application is null ? Maybe<FellingLicenceApplication>.None : Maybe<FellingLicenceApplication>.From(application);
    }

    /// <inheritdoc />
    public async Task<(bool UserAlreadyAssigned, Maybe<Guid> UserUnassigned)> AssignFellingLicenceApplicationToStaffMemberAsync(
        Guid applicationId,
        Guid assignedUserId,
        AssignedUserRole assignedRole,
        DateTime timestamp,
        CancellationToken cancellationToken)
    {
        var existingEntry = await Context.AssigneeHistories.SingleOrDefaultAsync(
            x => x.Role == assignedRole && x.FellingLicenceApplicationId == applicationId && x.TimestampUnassigned.HasValue == false);
        var existingAssigneeId = existingEntry?.AssignedUserId;

        if (existingAssigneeId == assignedUserId)
        {
            return (true, Maybe<Guid>.None);
        }

        if (existingEntry != null)
        {
            existingEntry.TimestampUnassigned = timestamp;
        }

        await Context.AssigneeHistories.AddAsync(new AssigneeHistory
        {
            Role = assignedRole,
            AssignedUserId = assignedUserId,
            FellingLicenceApplicationId = applicationId,
            TimestampAssigned = timestamp
        }, cancellationToken);

        await Context.SaveEntitiesAsync(cancellationToken);

        return (false, existingAssigneeId.HasValue ? Maybe<Guid>.From(existingAssigneeId.Value) : Maybe<Guid>.None);
    }

    /// <inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> RemoveAssignedFellingLicenceApplicationStaffMemberAsync(
        Guid applicationId,
        Guid assignedUserId,
        DateTime timestamp,
        CancellationToken cancellationToken)
    {
        var assigneeHistories = await Context.AssigneeHistories.Where(x =>
                x.FellingLicenceApplicationId == applicationId &&
                x.AssignedUserId == assignedUserId &&
                x.TimestampUnassigned.HasValue == false)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (assigneeHistories.Count < 1)
        {
            return UnitResult.Failure(UserDbErrorReason.NotFound);
        }

        assigneeHistories.ForEach(x => x.TimestampUnassigned = timestamp);
        return await Context.SaveEntitiesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> RemoveAssignedRolesFromApplicationAsync(
        Guid applicationId, 
        AssignedUserRole[] roles,
        DateTime timestamp,
        CancellationToken cancellationToken)
    {
        var assignees = Context.AssigneeHistories.Where(x => 
            x.FellingLicenceApplicationId == applicationId
            && x.TimestampUnassigned == null
            && roles.Contains(x.Role));

        foreach (var assigneeHistory in assignees)
        {
            assigneeHistory.TimestampUnassigned = timestamp;
        }

        return await Context.SaveEntitiesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<AssigneeHistory>> GetAssigneeHistoryForApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken) 
        => await Context
            .AssigneeHistories
            .Where(x => x.FellingLicenceApplicationId == applicationId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IList<StatusHistory>> GetStatusHistoryForApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
        => await Context
            .StatusHistories
            .Where(x => x.FellingLicenceApplicationId == applicationId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IList<FellingLicenceApplication>> ListByIncludedStatus(
        bool assignedToUserAccountIdOnly,
        Guid userId,
        IList<FellingLicenceStatus> userFellingLicenceSelectionOptions,
        CancellationToken cancellationToken)
    {
        IQueryable<FellingLicenceApplication> conditionalActiveFellingLicenceApplicationsQuery = Context.FellingLicenceApplications
            .Where(x => userFellingLicenceSelectionOptions.Contains(x.StatusHistories.OrderBy(y => y.Created).Last().Status)

                        // This condition will filter to the assigned user IF the assignedToUserAccountIdOnly is non null

                        && (!assignedToUserAccountIdOnly || x.AssigneeHistories.Any(y => y.AssignedUserId == userId && !y.TimestampUnassigned.HasValue)))
            .Include(a => a.StatusHistories)
            .Include(a => a.AssigneeHistories)
            .Include(a => a.LinkedPropertyProfile);

        return await conditionalActiveFellingLicenceApplicationsQuery.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<FellingLicenceApplication>> ListByIncludedStatus(
        bool assignedToUserAccountIdOnly,
        Guid userId,
        IList<FellingLicenceStatus> userFellingLicenceSelectionOptions,
        CancellationToken cancellationToken,
        int pageNumber,
        int pageSize,
        string sortColumn,
        string sortDirection)
    {
        IQueryable<FellingLicenceApplication> query = Context.FellingLicenceApplications
            .Include(a => a.StatusHistories)
            .Include(a => a.AssigneeHistories)
            .Include(a => a.LinkedPropertyProfile)
            .Include(a => a.SubmittedFlaPropertyDetail)
            .Where(app =>
                userFellingLicenceSelectionOptions.Contains(
                    app.StatusHistories
                        .OrderByDescending(sh => sh.Created)
                        .Select(sh => sh.Status)
                        .FirstOrDefault()
                )
                && (!assignedToUserAccountIdOnly ||
                    app.AssigneeHistories.Any(y => y.AssignedUserId == userId && !y.TimestampUnassigned.HasValue))
            );

        bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        // Dynamic ordering
        switch (sortColumn)
        {
            case "Reference":
                query = descending ? query.OrderByDescending(x => x.ApplicationReference) : query.OrderBy(x => x.ApplicationReference);
                break;
            case "Property":
                query = descending ? query.OrderByDescending(x => x.SubmittedFlaPropertyDetail.Name) : query.OrderBy(x => x.SubmittedFlaPropertyDetail.Name);
                break;
            case "SubmittedDate":
                query = descending
                    ? query.OrderByDescending(x => x.StatusHistories
                        .Where(sh => sh.Status == FellingLicenceStatus.Submitted)
                        .Select(sh => sh.Created)
                        .Max())
                    : query.OrderBy(x => x.StatusHistories
                        .Where(sh => sh.Status == FellingLicenceStatus.Submitted)
                        .Select(sh => sh.Created)
                        .Max());
                break;
            case "CitizensCharterDate":
                query = descending ? query.OrderByDescending(x => x.CitizensCharterDate) : query.OrderBy(x => x.CitizensCharterDate);
                break;
            case "Status":
                query = descending
                    ? query.OrderByDescending(x => x.StatusHistories
                        .OrderByDescending(sh => sh.Created)
                        .Select(sh => sh.Status)
                        .FirstOrDefault())
                    : query.OrderBy(x => x.StatusHistories
                        .OrderByDescending(sh => sh.Created)
                        .Select(sh => sh.Status)
                        .FirstOrDefault());
                break;
            default:
                query = descending ? query.OrderByDescending(x => x.FinalActionDate) : query.OrderBy(x => x.FinalActionDate);
                break;
        }

        if (pageNumber > 0 && pageSize > 0)
        {
            int skip = (pageNumber - 1) * pageSize;
            query = query.Skip(skip).Take(pageSize);
        }
        var results = await query.ToListAsync(cancellationToken);
        return results;
    }

    /// <inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> UpdateExternalAccessLinkAsync(ExternalAccessLink accessLink, CancellationToken cancellationToken)
    {
        var existingLink = await Context.ExternalAccessLinks.FindAsync(new object?[] { accessLink.Id }, cancellationToken: cancellationToken);
        if (existingLink == null)
        {
            return UnitResult.Failure(UserDbErrorReason.NotFound);
        }
        Context.Entry(existingLink).CurrentValues.SetValues(accessLink);
        return await Context.SaveEntitiesAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> AddExternalAccessLinkAsync(ExternalAccessLink accessLink, CancellationToken cancellationToken)
    {
        Context.ExternalAccessLinks.Add(accessLink);
        return await Context.SaveEntitiesAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<IList<ExternalAccessLink>> GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(
        Guid applicationId, 
        string name,
        string userEmail, 
        string purpose, 
        CancellationToken cancellationToken)
    {
        var accessLinksForApplication = await Context.ExternalAccessLinks
            .Where(x => x.FellingLicenceApplicationId == applicationId)
            .ToListAsync(cancellationToken);
        
        return accessLinksForApplication
            .Where(l => string.Equals(l.ContactEmail, userEmail, StringComparison.CurrentCultureIgnoreCase)
                        && l.Purpose == purpose 
                        && l.Name == name)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<Result<(List<ProposedFellingDetail>, List<ProposedRestockingDetail>)>> GetProposedFellingAndRestockingDetailsForApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var fla = await Context.FellingLicenceApplications.Where(x => x.Id == applicationId)
            .Include(s => s.LinkedPropertyProfile)
                .ThenInclude(c => c.ProposedFellingDetails)
                .ThenInclude(f => f.FellingSpecies)
            .Include(s => s.LinkedPropertyProfile)
                .ThenInclude(c => c.ProposedFellingDetails)
                    .ThenInclude(f => f.ProposedRestockingDetails)
                    .ThenInclude(r => r.RestockingSpecies)!
            .FirstOrDefaultAsync(cancellationToken);

        if (fla == null)
        {
            return Result.Failure<(List<ProposedFellingDetail>, List<ProposedRestockingDetail>)>(
                "Could not locate application with given id");
        }

        if (!(fla.LinkedPropertyProfile?.ProposedFellingDetails?.Any() ?? false))
        {
            return Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0)));
        }

        var proposedFellingDetails = fla.LinkedPropertyProfile!.ProposedFellingDetails
            .ToList();
        var proposedRestockingDetails = fla.LinkedPropertyProfile!.ProposedFellingDetails
            .SelectMany(x => x.ProposedRestockingDetails)
            .ToList();

        return Result.Success((proposedFellingDetails, proposedRestockingDetails));
    }

    /// <inheritdoc />
    public async Task<Result<(List<ConfirmedFellingDetail>, List<ConfirmedRestockingDetail>)>> GetConfirmedFellingAndRestockingDetailsForApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var fla = await Context.FellingLicenceApplications.Where(x => x.Id == applicationId)
            .Include(s => s.SubmittedFlaPropertyDetail)
                .ThenInclude(c => c.SubmittedFlaPropertyCompartments)!
                .ThenInclude(f => f.ConfirmedFellingDetails)
                .ThenInclude(fs => fs.ConfirmedFellingSpecies)
            .Include(s => s.SubmittedFlaPropertyDetail)
                .ThenInclude(c => c.SubmittedFlaPropertyCompartments)!
                .ThenInclude(f => f.ConfirmedFellingDetails)!
                .ThenInclude(r => r.ConfirmedRestockingDetails)!
                .ThenInclude(s => s.ConfirmedRestockingSpecies)
            .FirstOrDefaultAsync(cancellationToken);

        if (fla == null)
        {
            return Result.Failure<(List<ConfirmedFellingDetail>, List<ConfirmedRestockingDetail>)>("Could not locate application with given id");
        }

        if (!(fla.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments?.Any() ?? false))
        {
            return Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0)));
        }

        var confirmedFellingDetails = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
            .Where(x => x.ConfirmedFellingDetails != null)
            .SelectMany(x => x.ConfirmedFellingDetails);
        var confirmedRestockingDetails = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
            .SelectMany(x => x.ConfirmedFellingDetails)
            .SelectMany(x => x.ConfirmedRestockingDetails);

        return Result.Success((confirmedFellingDetails.ToList(), confirmedRestockingDetails.ToList()));
    }

    /// <inheritdoc />
    public async Task<Maybe<WoodlandOfficerReview>> GetWoodlandOfficerReviewAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await Context.WoodlandOfficerReviews
           .Include(x => x.FellingLicenceApplication)
           .ThenInclude(x => x.LarchCheckDetails)  
           .SingleOrDefaultAsync(x => x.FellingLicenceApplicationId == applicationId);

        return result != null ? Maybe.From(result) : Maybe<WoodlandOfficerReview>.None;
    }

    /// <inheritdoc />
    public async Task<Maybe<ApproverReview>> GetApproverReviewAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await Context.ApproverReviews.SingleOrDefaultAsync(x =>
            x.FellingLicenceApplicationId == applicationId);

        return result != null ? Maybe.From(result) : Maybe<ApproverReview>.None;
    }

    /// <inheritdoc />
    public async Task<Maybe<AdminOfficerReview>> GetAdminOfficerReviewAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = 
            await Context.AdminOfficerReviews
                .Include(x => x.FellingLicenceApplication)
                .ThenInclude(a => a.LarchCheckDetails)
                .SingleOrDefaultAsync(
                x => x.FellingLicenceApplicationId == applicationId,
                cancellationToken: cancellationToken);

        return result != null ? Maybe.From(result) : Maybe<AdminOfficerReview>.None;
    }

    /// <inheritdoc />
    public async Task<Maybe<LarchCheckDetails>> GetLarchCheckDetailsAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await Context.LarchCheckDetails
            .Include(x => x.FellingLicenceApplication)
            .SingleOrDefaultAsync(
                x => x.FellingLicenceApplication.Id == applicationId,
                cancellationToken: cancellationToken);

        return result != null ? Maybe.From(result) : Maybe<LarchCheckDetails>.None;
    }

    /// <inheritdoc />
    public async Task<Maybe<PublicRegister>> GetPublicRegisterAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await Context.PublicRegister.SingleOrDefaultAsync(x =>
            x.FellingLicenceApplicationId == applicationId, cancellationToken: cancellationToken);

        return result != null ? Maybe.From(result) : Maybe<PublicRegister>.None;
    }

    /// <inheritdoc />
    public async Task AddPublicRegisterAsync(PublicRegister publicRegister, CancellationToken cancellationToken)
    {
        await Context.PublicRegister.AddAsync(publicRegister, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> AddDecisionPublicRegisterDetailsAsync(
        Guid applicationId,
        DateTime publishedDateTime,
        DateTime expiryDateTime,
        CancellationToken cancellationToken)
    {
        var publicRegister = await GetPublicRegisterAsync(applicationId, cancellationToken);

        if (publicRegister.HasNoValue)
        {
            return UnitResult.Failure(UserDbErrorReason.NotFound);
        }

        publicRegister.Value.DecisionPublicRegisterExpiryTimestamp = expiryDateTime;
        publicRegister.Value.DecisionPublicRegisterPublicationTimestamp = publishedDateTime;

        return await Context.SaveEntitiesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> ExpireDecisionPublicRegisterEntryAsync(
        Guid applicationId, 
        DateTime removedDateTime,
        CancellationToken cancellationToken)
    {
        var publicRegister = await GetPublicRegisterAsync(applicationId, cancellationToken);

        if (publicRegister.HasNoValue)
        {
            return UnitResult.Failure(UserDbErrorReason.NotFound);
        }

        publicRegister.Value.DecisionPublicRegisterRemovedTimestamp = removedDateTime;

        return await Context.SaveEntitiesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> ExpireConsultationPublicRegisterEntryAsync(Guid applicationId, DateTime removedDateTime,
        CancellationToken cancellationToken)
    {
        var publicRegister = await GetPublicRegisterAsync(applicationId, cancellationToken);

        if (publicRegister.HasNoValue)
        {
            return UnitResult.Failure(UserDbErrorReason.NotFound);
        }

        publicRegister.Value.ConsultationPublicRegisterRemovedTimestamp = removedDateTime;

        return await Context.SaveEntitiesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddWoodlandOfficerReviewAsync(WoodlandOfficerReview woodlandOfficerReview, CancellationToken cancellationToken)
    {
        await Context.WoodlandOfficerReviews.AddAsync(woodlandOfficerReview, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> AddOrUpdateApproverReviewAsync(ApproverReview approverReview, CancellationToken cancellationToken)
    {
        var existingReview = await Context.ApproverReviews.SingleOrDefaultAsync(x => x.FellingLicenceApplicationId == approverReview.FellingLicenceApplicationId, cancellationToken);
        if (existingReview == null)
        {
            await Context.ApproverReviews.AddAsync(approverReview, cancellationToken);
        }
        else
        {
            Context.Entry(existingReview).CurrentValues.SetValues(approverReview);
        }
        return await Context.SaveEntitiesAsync(cancellationToken);
    }

    public async Task<UnitResult<UserDbErrorReason>> DeleteApproverReviewAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var existingReview = await Context.ApproverReviews
            .SingleOrDefaultAsync(x => x.FellingLicenceApplicationId == applicationId, cancellationToken);

        if (existingReview == null)
        {
            return UnitResult.Failure(UserDbErrorReason.NotFound);
        }

        Context.ApproverReviews.Remove(existingReview);
        return await Context.SaveEntitiesAsync(cancellationToken);
    }

    public async Task<UnitResult<UserDbErrorReason>> SetApplicationApproverAsync(Guid applicationId, Guid? approverId, CancellationToken cancellationToken)
    {
        var application = await Context.FellingLicenceApplications
            .SingleOrDefaultAsync(x => x.Id == applicationId, cancellationToken);

        if (application == null)
        {
            return UnitResult.Failure(UserDbErrorReason.NotFound);
        }

        application.ApproverId = approverId;

        return await Context.SaveEntitiesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> AddOrUpdateLarchCheckDetailsAsync(LarchCheckDetails larchCheck, CancellationToken cancellationToken)
    {
        var existingCheck = await Context.LarchCheckDetails
            .Include(x => x.FellingLicenceApplication)
            .SingleOrDefaultAsync(
                x => x.FellingLicenceApplication.Id == larchCheck.FellingLicenceApplicationId,
                cancellationToken: cancellationToken);

        if (existingCheck == null)
        {
            await Context.LarchCheckDetails.AddAsync(larchCheck, cancellationToken);
        }
        else
        {
            Context.Entry(existingCheck).CurrentValues.SetValues(larchCheck);
        }
        return await Context.SaveEntitiesAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task AddCaseNoteAsync(CaseNote caseNote, CancellationToken cancellationToken)
    {
        await Context.CaseNote.AddAsync(caseNote, cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<Maybe<ExternalAccessLink>> GetValidExternalAccessLinkAsync(
        Guid applicationId, 
        Guid accessCode, 
        string emailAddress, 
        DateTime now,
        CancellationToken cancellationToken)
    {
        var link = await Context.ExternalAccessLinks.SingleOrDefaultAsync(
            x => x.FellingLicenceApplicationId == applicationId
                 && x.AccessCode == accessCode
                 && x.ContactEmail == emailAddress
                 && x.ExpiresTimeStamp > now, cancellationToken);

        return link != null 
            ? Maybe<ExternalAccessLink>.From(link) 
            : Maybe<ExternalAccessLink>.None;
    }

    /// <inheritdoc />
    public async Task<IList<ConsulteeComment>> GetConsulteeCommentsAsync(Guid applicationId, string? emailAddress, CancellationToken cancellationToken)
    {
        var commentsForApplication = await Context.ConsulteeComments
            .Where(x => x.FellingLicenceApplicationId == applicationId)
            .ToListAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(emailAddress) == false)
        {
            commentsForApplication = commentsForApplication
                .Where(x => string.Equals(x.AuthorContactEmail, emailAddress, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
        }

        return commentsForApplication;
    }

    /// <inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> AddConsulteeCommentAsync(ConsulteeComment consulteeComment, CancellationToken cancellationToken)
    {
        await Context.ConsulteeComments.AddAsync(consulteeComment, cancellationToken);
        return await Context.SaveEntitiesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<FellingLicenceApplication>> GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(
        DateTime currentTime,
        TimeSpan thresholdBeforeDate,
        CancellationToken cancellationToken)
    {
        var inReviewStatuses = new[]
        {
            FellingLicenceStatus.Submitted,
            FellingLicenceStatus.AdminOfficerReview,
            FellingLicenceStatus.WoodlandOfficerReview,
            FellingLicenceStatus.SentForApproval
        };

        var result = await Context.FellingLicenceApplications
            .Include(s => s.AssigneeHistories)
            .Include(s => s.StatusHistories)
            .Where(x =>
                x.FinalActionDate <= currentTime.Add(thresholdBeforeDate)
                && inReviewStatuses.Contains(x.StatusHistories.OrderByDescending(y => y.Created)!.FirstOrDefault()!.Status))
            .ToListAsync(cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<IList<FellingLicenceApplication>> GetApplicationsThatAreWithinThresholdOfWithdrawalNotificationDateAsync(
        DateTime currentTime,
        TimeSpan thresholdAfterStatusCreatedDate,
        CancellationToken cancellationToken)
    {
        var result = await Context.FellingLicenceApplications
            .Include(s => s.AssigneeHistories)
            .Include(s => s.StatusHistories)
            .Include(s => s.SubmittedFlaPropertyDetail)
            .Where(x =>
                x.StatusHistories.OrderByDescending(a => a.Created)!.FirstOrDefault()!.Created <= currentTime.Subtract(thresholdAfterStatusCreatedDate)
                && (x.StatusHistories.OrderByDescending(y => y.Created)!.FirstOrDefault()!.Status == FellingLicenceStatus.WithApplicant
                    || x.StatusHistories.OrderByDescending(y => y.Created)!.FirstOrDefault()!.Status == FellingLicenceStatus.ReturnedToApplicant)
                && (x.VoluntaryWithdrawalNotificationTimeStamp == null || x.VoluntaryWithdrawalNotificationTimeStamp < x.StatusHistories.OrderByDescending(y => y.Created)!.FirstOrDefault()!.Created))
            .ToListAsync(cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<IList<FellingLicenceApplication>> GetApplicationsThatAreWithinThresholdAutomaticWithdrawalDateAsync(
        DateTime currentTime,
        TimeSpan thresholdAfterStatusCreatedDate,
        CancellationToken cancellationToken)
    {
        var result = await Context.FellingLicenceApplications
            .Include(s => s.AssigneeHistories)
            .Include(s => s.StatusHistories)
            .Include(s => s.SubmittedFlaPropertyDetail)
            .Where(x =>
                x.StatusHistories.OrderByDescending(a => a.Created)!.FirstOrDefault()!.Created <= currentTime.Subtract(thresholdAfterStatusCreatedDate)
                && (x.StatusHistories.OrderByDescending(y => y.Created)!.FirstOrDefault()!.Status == FellingLicenceStatus.WithApplicant
                    || x.StatusHistories.OrderByDescending(y => y.Created)!.FirstOrDefault()!.Status == FellingLicenceStatus.ReturnedToApplicant))
            .ToListAsync(cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<IList<FellingLicenceApplication>> GetApplicationsWithExpiredConsultationPublicRegisterPeriodsAsync(DateTime currentTime,
        CancellationToken cancellationToken)
    {
        return await Context.FellingLicenceApplications
            .Include(s => s.AssigneeHistories)
            .Include(s => s.SubmittedFlaPropertyDetail)
            .Include(s => s.PublicRegister)
            .Where(x =>
                x.PublicRegister != null
                && x.PublicRegister.ConsultationPublicRegisterRemovedTimestamp == null
                && x.PublicRegister.ConsultationPublicRegisterExpiryTimestamp <= currentTime
            )
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<FellingLicenceApplication>> GetFinalisedApplicationsWithExpiredDecisionPublicRegisterPeriodsAsync(
        DateTime currentTime,
        CancellationToken cancellationToken)
    {
        return await Context.FellingLicenceApplications
            .Include(s => s.AssigneeHistories)
            .Include(s => s.SubmittedFlaPropertyDetail)
            .Include(s => s.PublicRegister)
            .Where(x =>
                x.PublicRegister != null
                && x.PublicRegister.DecisionPublicRegisterRemovedTimestamp == null
                && x.PublicRegister.DecisionPublicRegisterExpiryTimestamp <= currentTime
                )
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<FellingLicenceApplication>> ExecuteFellingLicenceApplicationsReportQueryAsync(
        FellingLicenceApplicationsReportQuery query,
        CancellationToken cancellationToken)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        var baseQueryStatement = Context.FellingLicenceApplications
            .Include(s => s.StatusHistories)
            .Include(s => s.LinkedPropertyProfile)
            .Include(s => s.PublicRegister)
            
            .Include(s => s.SubmittedFlaPropertyDetail)
                .ThenInclude(p => p.SubmittedFlaPropertyCompartments)
                    .ThenInclude(c => c.ConfirmedFellingDetails)
                        .ThenInclude(f => f.ConfirmedFellingSpecies)

            .Include(s => s.SubmittedFlaPropertyDetail)
                .ThenInclude(p => p.SubmittedFlaPropertyCompartments)
                .ThenInclude(c => c.ConfirmedFellingDetails)
                .ThenInclude(r => r.ConfirmedRestockingDetails)
                .ThenInclude(r => r.ConfirmedRestockingSpecies)

            .Include(s => s.LinkedPropertyProfile)
            .ThenInclude(p => p.ProposedFellingDetails)
            .ThenInclude(p => p.FellingSpecies)
            .Include(s => s.LinkedPropertyProfile)
            .ThenInclude(p => p.ProposedFellingDetails)
            .ThenInclude(f => f.ProposedRestockingDetails)
            .ThenInclude(p => p.RestockingSpecies)
            .Include(s => s.AssigneeHistories)
            .Include(s => s.LinkedPropertyProfile)
            .Include(s => s.WoodlandOfficerReview)
            .AsNoTracking();

        var queryToApply = query.Apply(baseQueryStatement);

        return await queryToApply.ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> UpdateAreaCodeAsync(
        Guid applicationId, 
        string newAreaCode,
        string? adminHubName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(newAreaCode))
            throw new ArgumentException("Value cannot be null or empty.", nameof(newAreaCode));

        var existingFla = await Context.FellingLicenceApplications.SingleOrDefaultAsync(x => x.Id == applicationId, cancellationToken);

        if (existingFla != null)
        {
            var segments = existingFla.ApplicationReference.Split('/');
            if (segments.Length < 3)
            {
                throw new ArgumentException("Invalid application reference format");
            }
            segments[0] = newAreaCode;
            var applicationReferenceWithNewAreaCode = string.Join("/", segments);

            existingFla.ApplicationReference = applicationReferenceWithNewAreaCode;
            existingFla.AreaCode = newAreaCode;
            existingFla.AdministrativeRegion = adminHubName;
            return await Context.SaveEntitiesAsync(cancellationToken);
        }

        return UnitResult.Failure(UserDbErrorReason.NotFound);
    }

    /// <inheritdoc />
    public async Task<Result<List<SubmittedFlaPropertyCompartment>>> GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var propertyDetail = await Context.SubmittedFlaPropertyDetails
            .Include(d => d.SubmittedFlaPropertyCompartments)
            .FirstOrDefaultAsync(d => d.FellingLicenceApplicationId == applicationId, cancellationToken);

        return propertyDetail is null
            ? Result.Failure<List<SubmittedFlaPropertyCompartment>>("SubmittedFlaPropertyDetail not found for applicationId.")
            : Result.Success(propertyDetail.SubmittedFlaPropertyCompartments?.ToList() ?? []);
    }

    /// <inheritdoc />
    public async Task<List<Document>> GetApplicationDocumentsAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        return await Context.Documents
            .Where(d => d.FellingLicenceApplicationId == applicationId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IncludedApplicationsSummary> TotalIncludedApplicationsAsync(
        bool assignedToUserAccountIdOnly,
        Guid userId,
        IList<FellingLicenceStatus> userFellingLicenceSelectionOptions,
        CancellationToken cancellationToken)
    {
        var baseQuery = Context.FellingLicenceApplications
            .Include(a => a.StatusHistories)
            .Include(a => a.AssigneeHistories)
            .Where(app =>
                userFellingLicenceSelectionOptions.Contains(
                    app.StatusHistories
                        .OrderByDescending(sh => sh.Created)
                        .Select(sh => sh.Status)
                        .FirstOrDefault()
                )
            );

        var assignedFilterQuery = assignedToUserAccountIdOnly
            ? baseQuery.Where(app => app.AssigneeHistories.Any(y => y.AssignedUserId == userId && !y.TimestampUnassigned.HasValue))
            : baseQuery;

        var totalCount = await assignedFilterQuery.CountAsync(cancellationToken);

        // Always compute assigned to user count from the unfiltered base query (within status scope)
        var assignedToUserCount = await baseQuery
            .Where(app => app.AssigneeHistories.Any(y => y.AssignedUserId == userId && !y.TimestampUnassigned.HasValue))
            .CountAsync(cancellationToken);

        var statusCounts = await assignedFilterQuery
            .Select(app => app.StatusHistories
                .OrderByDescending(sh => sh.Created)
                .Select(sh => sh.Status)
                .FirstOrDefault())
            .GroupBy(status => status)
            .Select(g => new IncludedStatusCount { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new IncludedApplicationsSummary
        {
            TotalCount = totalCount,
            AssignedToUserCount = assignedToUserCount,
            StatusCounts = statusCounts
        };
    }
}