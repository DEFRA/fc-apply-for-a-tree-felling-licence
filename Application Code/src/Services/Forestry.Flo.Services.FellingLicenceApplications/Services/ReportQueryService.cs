using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.AdminHubs.Model;
using Forestry.Flo.Services.AdminHubs.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.InternalUsers.Models;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// An implementation of <see cref="IReportQueryService"/> for querying the system
/// instance.
/// </summary>
public class ReportQueryService : IReportQueryService
{
    private readonly IFellingLicenceApplicationInternalRepository _internalFlaRepository;
    private readonly IAdminHubService _adminHubService;  
    private readonly IUserAccountService _userAccountService;
    private readonly ICompartmentRepository _compartmentRepositoryService;
    private readonly ILogger<ReportQueryService> _logger;
    private readonly FlaStatusDurationCalculator _flaStatusDurationCalculator;

    public ReportQueryService(
        IFellingLicenceApplicationInternalRepository internalFlaRepository,
        IUserAccountService userAccountService,
        ICompartmentRepository compartmentRepository,
        IAdminHubService adminHubService,
        FlaStatusDurationCalculator flaStatusDurationCalculator,
        ILogger<ReportQueryService> logger)
    {
        _internalFlaRepository = Guard.Against.Null(internalFlaRepository);
        _userAccountService = Guard.Against.Null(userAccountService);
        _compartmentRepositoryService = Guard.Against.Null(compartmentRepository);
        _adminHubService = Guard.Against.Null(adminHubService);
        _flaStatusDurationCalculator = flaStatusDurationCalculator;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task<Result<FellingLicenceApplicationsReportQueryResultModel>> QueryFellingLicenceApplicationsAsync(
        FellingLicenceApplicationsReportQuery query, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Called with query [{query}].", query);
        
        try
        {
            if (query.SelectedAdminHubIds.Any())
            {
                query.AssociatedAdminHubUserIds = await GetFcUsersIdsForSelectedAdminHubsAsync(query.SelectedAdminHubIds, cancellationToken);
            }
            
            var resultModel = new FellingLicenceApplicationsReportQueryResultModel();

            var applicationsData = await _internalFlaRepository.ExecuteFellingLicenceApplicationsReportQueryAsync(query, cancellationToken);

            if (!applicationsData.Any())
            {
                return Result.Success(resultModel);
            }
            
            var fcUsersInDataSet = await GetFcUsersInDataSetAsync(applicationsData, cancellationToken);

            _logger.LogDebug("Query executed. Retrieved [{applicationsDataCount}] records.", applicationsData.Count);
            _logger.LogDebug("Query executed. Total distinct FC users found within data set [{userDataCount}] records.", fcUsersInDataSet.Count);

            var speciesDictionary = TreeSpeciesFactory.SpeciesDictionary;

            foreach (var application in applicationsData.OrderBy(x=>x.CreatedTimestamp))
            {
                var statusDurations = _flaStatusDurationCalculator.CalculateStatusDurations(application);
                var currentStatus = application.StatusHistories.MaxBy(x => x.Created)!.Status.ToString();
                var assignedAdminOfficer = GetCurrentlyAssignedUserForRole(application.AssigneeHistories, fcUsersInDataSet, AssignedUserRole.AdminOfficer);
                var assignedWoodlandOfficer = GetCurrentlyAssignedUserForRole(application.AssigneeHistories, fcUsersInDataSet, AssignedUserRole.WoodlandOfficer);
                var currentAssignee = GetApplicationCurrentUser(application.AssigneeHistories, fcUsersInDataSet);
                var daysAtCurrentState = GetDaysAtCurrentState(application.StatusHistories);

                var propertyCompartments = await _compartmentRepositoryService.ListAsync(
                    application.LinkedPropertyProfile.PropertyProfileId, application.WoodlandOwnerId,
                    cancellationToken);


                resultModel.FellingLicenceApplicationReportEntries.Add(
                    new FellingLicenceApplicationReportEntry
                    {
                        FellingLicenceReference = application.ApplicationReference,
                        Source = application.Source.ToString(),
                        CurrentStatus = currentStatus,
                        DaysAtCurrentStatus = daysAtCurrentState,
                        DateOfSubmission = application.StatusHistories.FirstOrDefault(x => x.Status == FellingLicenceStatus.Submitted)?.Created!,
                        DateOfApproval = application.StatusHistories.FirstOrDefault(x => x.Status == FellingLicenceStatus.Approved)?.Created!,
                        PublicRegisterExempt = application.PublicRegister?.WoodlandOfficerSetAsExemptFromConsultationPublicRegister,
                        PublicRegisterOn = application.PublicRegister?.ConsultationPublicRegisterPublicationTimestamp,
                        PublicRegisterExpires = application.PublicRegister?.ConsultationPublicRegisterExpiryTimestamp,
                        PublicRegisterRemoved = application.PublicRegister?.ConsultationPublicRegisterRemovedTimestamp,
                        FinalActionDate = application.FinalActionDate,
                        FinalActionDateExtended = application.FinalActionDateExtended,
                        CitizensCharterDate = application.CitizensCharterDate,
                        ProposedFellingStart = application.ProposedFellingStart,
                        ProposedFellingEnd = application.ProposedFellingStart,
                        ActualFellingStartDate = application.ActualFellingStart,
                        ActualFellingEndDate = application.ActualFellingEnd,
                        AssignedAO = assignedAdminOfficer,
                        AssignedWO = assignedWoodlandOfficer,
                        CurrentAssignee = currentAssignee,
                        DaysAtDraft = GetDaysAtState(statusDurations, FellingLicenceStatus.Draft),
                        DaysAtSubmitted = GetDaysAtState(statusDurations, FellingLicenceStatus.Submitted),
                        DaysAtReceived = GetDaysAtState(statusDurations, FellingLicenceStatus.Received),
                        DaysAtAdminOfficerReview = GetDaysAtState(statusDurations, FellingLicenceStatus.AdminOfficerReview),
                        DaysAtWoodlandOfficerReview = GetDaysAtState(statusDurations, FellingLicenceStatus.WoodlandOfficerReview),
                        DaysAtSentForApproval = GetDaysAtState(statusDurations, FellingLicenceStatus.SentForApproval),
                        DaysAtApproved = GetDaysAtState(statusDurations, FellingLicenceStatus.Approved),
                        DaysAtWithApplicant = GetDaysAtState(statusDurations, FellingLicenceStatus.WithApplicant),
                        DaysAtReturnedToApplicant = GetDaysAtState(statusDurations, FellingLicenceStatus.ReturnedToApplicant),
                        DaysAtRefused = GetDaysAtState(statusDurations, FellingLicenceStatus.Refused),
                        DaysAtWithdrawn = GetDaysAtState(statusDurations, FellingLicenceStatus.Withdrawn),
                        DaysAtReferredToLocalAuthority = GetDaysAtState(statusDurations, FellingLicenceStatus.ReferredToLocalAuthority)
                    });

                if (application.SubmittedFlaPropertyDetail is not null)
                {
                    resultModel.SubmittedPropertyProfileReportEntries.Add(
                        new SubmittedPropertyProfileReportEntry
                        {
                            FellingLicenceReference = application.ApplicationReference,
                            Name = application.SubmittedFlaPropertyDetail.Name,
                            NearestTown = application.SubmittedFlaPropertyDetail.NearestTown,
                            HasWoodlandManagementPlan = application.SubmittedFlaPropertyDetail.HasWoodlandManagementPlan,
                            WoodlandManagementPlanReference = application.SubmittedFlaPropertyDetail.WoodlandManagementPlanReference,
                            IsWoodlandCertificationScheme = application.SubmittedFlaPropertyDetail.IsWoodlandCertificationScheme,
                            WoodlandCertificationSchemeReference = application.SubmittedFlaPropertyDetail.WoodlandCertificationSchemeReference
                        });

                    foreach (var submittedCompartment in application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments!)
                    {
                        foreach (var confirmedFellingDetail in submittedCompartment.ConfirmedFellingDetails)
                        {
                            foreach (var confirmedFellingSpecies in confirmedFellingDetail.ConfirmedFellingSpecies!)
                            {
                                resultModel.ConfirmedCompartmentDetailReportEntries.Add(
                                new ConfirmedCompartmentDetailReportEntry
                                {
                                    ConfirmedDetailType = ConfirmedDetailType.ConfirmedFelling,
                                    FellingLicenceReference = application.ApplicationReference,
                                    OperationType = confirmedFellingDetail.OperationType,
                                    Area = confirmedFellingDetail.AreaToBeFelled,
                                    CompartmentName = submittedCompartment.DisplayName,
                                    Volume = confirmedFellingDetail.EstimatedTotalFellingVolume,
                                    Species = speciesDictionary[confirmedFellingSpecies.Species].Name,
                                    NumberOfTrees = confirmedFellingDetail.NumberOfTrees
                                });
                            }

                            if (confirmedFellingDetail.ConfirmedRestockingDetails != null &&
                                confirmedFellingDetail.ConfirmedRestockingDetails.Any())
                            {
                                foreach (var confirmedRestockingDetail in confirmedFellingDetail.ConfirmedRestockingDetails)
                                {
                                    if (confirmedRestockingDetail.ConfirmedRestockingSpecies == null || !confirmedRestockingDetail.ConfirmedRestockingSpecies.Any())
                                    {
                                        resultModel.ConfirmedCompartmentDetailReportEntries.Add(
                                            new ConfirmedCompartmentDetailReportEntry
                                            {
                                                ConfirmedDetailType = ConfirmedDetailType.ConfirmedRestocking,
                                                FellingLicenceReference = application.ApplicationReference,
                                                RestockingProposal = confirmedRestockingDetail.RestockingProposal,
                                                Area = confirmedRestockingDetail.Area,
                                                CompartmentName = GetConfirmedCompartmentName(confirmedRestockingDetail.SubmittedFlaPropertyCompartmentId, application, application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments),
                                                ConfirmedFellingReference = $"{submittedCompartment.DisplayName} / {confirmedFellingDetail.OperationType}",
                                                RestockingDensity = confirmedRestockingDetail.RestockingDensity,
                                                NumberOfTrees = confirmedRestockingDetail.NumberOfTrees
                                            });
                                    }
                                    else
                                    {
                                        foreach (var confirmedRestockingSpecies in confirmedRestockingDetail.ConfirmedRestockingSpecies)
                                        {
                                            resultModel.ConfirmedCompartmentDetailReportEntries.Add(
                                                new ConfirmedCompartmentDetailReportEntry
                                                {
                                                    ConfirmedDetailType = ConfirmedDetailType.ConfirmedRestocking,
                                                    FellingLicenceReference = application.ApplicationReference,
                                                    RestockingProposal = confirmedRestockingDetail.RestockingProposal,
                                                    Area = confirmedRestockingDetail.Area,
                                                    CompartmentName = GetConfirmedCompartmentName(confirmedRestockingDetail.SubmittedFlaPropertyCompartmentId, application, application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments),
                                                    Species = speciesDictionary[confirmedRestockingSpecies.Species].Name,
                                                    ConfirmedFellingReference = $"{submittedCompartment.DisplayName} / {confirmedFellingDetail.OperationType}",
                                                    RestockingDensity = confirmedRestockingDetail.RestockingDensity,
                                                    NumberOfTrees = confirmedRestockingDetail.NumberOfTrees
                                                });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (application.LinkedPropertyProfile is not null)
                {
                    var restockingDetailEntries = new List<ProposedCompartmentDetailReportEntry>();

                    foreach (var proposedFellingDetail in application.LinkedPropertyProfile.ProposedFellingDetails!)
                    {
                        if (proposedFellingDetail.FellingSpecies != null)
                        {
                            foreach (var proposedFellingSpecies in proposedFellingDetail.FellingSpecies)
                            {
                                resultModel.ProposedCompartmentDetailReportEntries.Add(
                                    new ProposedCompartmentDetailReportEntry
                                    {
                                        ProposedDetailType = ProposedDetailType.ProposedFelling,
                                        FellingLicenceReference = application.ApplicationReference,
                                        OperationType = proposedFellingDetail.OperationType,
                                        Area = proposedFellingDetail.AreaToBeFelled,
                                        CompartmentName = GetProposedCompartmentName(proposedFellingDetail.PropertyProfileCompartmentId, application, propertyCompartments),
                                        Volume = proposedFellingDetail.EstimatedTotalFellingVolume,
                                        Species = speciesDictionary[proposedFellingSpecies.Species].Name,
                                        NumberOfTrees = proposedFellingDetail.NumberOfTrees,
                                        NoRestockingReason = (proposedFellingDetail.IsRestocking.HasValue && !proposedFellingDetail.IsRestocking.Value) ? proposedFellingDetail.NoRestockingReason : string.Empty
                                    });
                            }
                        }

                        if (proposedFellingDetail.ProposedRestockingDetails != null && proposedFellingDetail.ProposedRestockingDetails.Any())
                        {
                            foreach (var proposedRestockingDetail in proposedFellingDetail.ProposedRestockingDetails)
                            {
                                if (proposedRestockingDetail.RestockingSpecies == null || !proposedRestockingDetail.RestockingSpecies.Any())
                                {
                                    restockingDetailEntries.Add(
                                        new ProposedCompartmentDetailReportEntry
                                        {
                                            ProposedDetailType = ProposedDetailType.ProposedRestocking,
                                            FellingLicenceReference = application.ApplicationReference,
                                            RestockingProposal = proposedRestockingDetail.RestockingProposal,
                                            Area = proposedRestockingDetail.Area,
                                            CompartmentName = GetProposedCompartmentName(proposedRestockingDetail.PropertyProfileCompartmentId, application, propertyCompartments),
                                            PercentageOfRestockArea = proposedRestockingDetail.PercentageOfRestockArea,
                                            Species = string.Empty,
                                            ProposedFellingReference = $"{GetProposedCompartmentName(proposedFellingDetail.PropertyProfileCompartmentId, application, propertyCompartments)} / {proposedFellingDetail.OperationType}",
                                            RestockingDensity = proposedRestockingDetail.RestockingDensity,
                                            NumberOfTrees = proposedRestockingDetail.NumberOfTrees
                                        });
                                }
                                else
                                {
                                    foreach (var proposedRestockingSpecies in proposedRestockingDetail.RestockingSpecies)
                                    {
                                        restockingDetailEntries.Add(
                                            new ProposedCompartmentDetailReportEntry
                                            {
                                                ProposedDetailType = ProposedDetailType.ProposedRestocking,
                                                FellingLicenceReference = application.ApplicationReference,
                                                RestockingProposal = proposedRestockingDetail.RestockingProposal,
                                                Area = proposedRestockingDetail.Area,
                                                CompartmentName = GetProposedCompartmentName(proposedRestockingDetail.PropertyProfileCompartmentId, application, propertyCompartments),
                                                PercentageOfRestockArea = proposedRestockingDetail.PercentageOfRestockArea,
                                                Species = speciesDictionary[proposedRestockingSpecies.Species].Name,
                                                ProposedFellingReference = $"{GetProposedCompartmentName(proposedFellingDetail.PropertyProfileCompartmentId, application, propertyCompartments)} / {proposedFellingDetail.OperationType}",
                                                RestockingDensity = proposedRestockingDetail.RestockingDensity,
                                                NumberOfTrees = proposedRestockingDetail.NumberOfTrees
                                            });
                                    }
                                }
                            }
                        }
                    }

                    resultModel.ProposedCompartmentDetailReportEntries.AddRange(restockingDetailEntries);
                }

                if (application.PublicRegister?.WoodlandOfficerSetAsExemptFromConsultationPublicRegister == true)
                {
                    resultModel.ConsultationPublicRegisterExemptCases.Add(
                        new ConsultationPublicRegisterExemptionEntry
                        {
                            FellingLicenceReference = application.ApplicationReference,
                            ExemptionReason = application.PublicRegister?.WoodlandOfficerConsultationPublicRegisterExemptionReason,
                        });
                }
            }
            return Result.Success(resultModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to successfully execute query for report. Query was: [{queryModel}]", query);
            return Result.Failure<FellingLicenceApplicationsReportQueryResultModel>(
                "Unable to successfully execute query / create result for report.");
        }
    }

    private string GetProposedCompartmentName(Guid compartmentId, FellingLicenceApplication application, IEnumerable<PropertyProfiles.Entities.Compartment> propertyCompartments)
    {
        var comp = propertyCompartments.SingleOrDefault(x => x.Id == compartmentId);

        if (comp != null)
        {
            return comp.CompartmentNumber;
        }

        _logger.LogWarning(
            "Could not find proposed compartment having id {id}, which is linked to Linked Property Profile {linkedPropertyProfileId}",
        compartmentId,
            application.LinkedPropertyProfile.PropertyProfileId);

        return string.Empty;
    }

    private string GetConfirmedCompartmentName(Guid compartmentId, FellingLicenceApplication application, IEnumerable<SubmittedFlaPropertyCompartment> compartments)
    {
        var comp = compartments.SingleOrDefault(x => x.Id == compartmentId);

        if (comp != null)
        {
            return comp.CompartmentNumber;
        }

        _logger.LogWarning($"Could not find confirmed compartment having id {compartmentId}, which is linked to Linked Property Profile {application.SubmittedFlaPropertyDetail.PropertyProfileId}");

        return string.Empty;
    }

    private static int? GetDaysAtState(IEnumerable<FellingLicenceApplicationStateDuration> statusDurations, FellingLicenceStatus state)
    {
        return statusDurations.FirstOrDefault(x => x.Status == state)?.Duration.Days;
    }

    private static int GetDaysAtCurrentState(IEnumerable<StatusHistory> statusHistories)
    {
        var currentStatusStartedDate = statusHistories.MaxBy(x => x.Created)!.Created;
        var elapsedTimeSpan = DateTime.Now.Date - currentStatusStartedDate;
        return elapsedTimeSpan.Days;
    }
    
    private async Task<List<Guid>> GetFcUsersIdsForSelectedAdminHubsAsync(
        List<Guid> adminHubIds,
        CancellationToken cancellationToken)
    {
        var userIds = new List<Guid>();

        var adminHubDataResult = await _adminHubService.RetrieveAdminHubDataAsync(
            new GetAdminHubsDataRequestModel(Guid.Empty, AccountTypeInternal.AdminHubManager), cancellationToken);

        if (adminHubDataResult.IsSuccess)
        {
            foreach (var adminHubId in adminHubIds)
            {
                var hubUsers = adminHubDataResult.Value.SingleOrDefault(x => x.Id == adminHubId);
                if (hubUsers != null)
                {
                    userIds.AddRange(hubUsers.AdminOfficers.Select(adminHubOfficerModel =>
                        adminHubOfficerModel.UserAccountId));
                }
                else
                {
                    _logger.LogWarning("Unable to get userIds for selected Admin Hub with id of [{adminHubId}]",
                        adminHubId);
                }
            }
        }
        else
        {
            _logger.LogWarning("Unable to get admin Hub data.");
        }

        return userIds;
    }

    private async Task<List<UserAccountModel>> GetFcUsersInDataSetAsync(
        IList<FellingLicenceApplication> fellingLicenceApplications, 
        CancellationToken cancellationToken)
    {
        if (!fellingLicenceApplications.Any()) return new List<UserAccountModel>();

        var userIds = new List<Guid>();

        foreach (var fellingLicenceApplication in fellingLicenceApplications)
        {
            userIds.AddRange(fellingLicenceApplication.AssigneeHistories.Select(x => x.AssignedUserId));
        }

        var result = await _userAccountService.RetrieveUserAccountsByIdsAsync(userIds.Distinct().ToList(), cancellationToken);

        return result.IsSuccess ? result.Value : new List<UserAccountModel>();
    }
    
    private static string GetApplicationCurrentUser(
        IEnumerable<AssigneeHistory> assigneeHistories,
        IEnumerable<UserAccountModel> userData)
    {
        var assigneeHistoryEntry = assigneeHistories
            .Where(x => x.TimestampUnassigned == null)
            .MaxBy(x => x.TimestampAssigned); //most recent entry, where not been unassigned.

        return GetUserNameFromAssigneeHistoryEntry(assigneeHistoryEntry, userData);
    }
    private static string GetUserNameFromAssigneeHistoryEntry(
        AssigneeHistory? assigneeHistoryEntry,
        IEnumerable<UserAccountModel> userData)
    {
        if (assigneeHistoryEntry == null) return string.Empty;

        var assignedUser = userData.SingleOrDefault(x => x.UserAccountId == assigneeHistoryEntry.AssignedUserId);

        return assignedUser != null ? assignedUser.FullName : string.Empty;
    }

    private static string GetCurrentlyAssignedUserForRole(
        IEnumerable<AssigneeHistory> assigneeHistories,
        IEnumerable<UserAccountModel> userData,
        AssignedUserRole userRole)
    {
        var assigneeHistoryEntry = assigneeHistories.SingleOrDefault(x => x.Role == userRole && x.TimestampUnassigned == null);
        return GetUserNameFromAssigneeHistoryEntry(assigneeHistoryEntry, userData);
    }
}
