﻿using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Extensions;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

public abstract class FellingLicenceApplicationUseCaseBase
{
    protected readonly IGetConfiguredFcAreas GetConfiguredFcAreasService;
    protected readonly IUserAccountService InternalUserAccountService;
    protected readonly IRetrieveUserAccountsService ExternalUserAccountService;
    protected readonly IFellingLicenceApplicationInternalRepository FellingLicenceRepository;
    protected readonly IRetrieveWoodlandOwners WoodlandOwnerService;
    protected readonly IAgentAuthorityService AgentAuthorityService;

    protected FellingLicenceApplicationUseCaseBase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IAgentAuthorityService agentAuthorityService,
        IGetConfiguredFcAreas getConfiguredFcAreasService)
    {
        GetConfiguredFcAreasService = Guard.Against.Null(getConfiguredFcAreasService);
        InternalUserAccountService = Guard.Against.Null(internalUserAccountService);
        ExternalUserAccountService = Guard.Against.Null(externalUserAccountService);
        FellingLicenceRepository = Guard.Against.Null(fellingLicenceApplicationInternalRepository);
        WoodlandOwnerService = Guard.Against.Null(woodlandOwnerService);
        AgentAuthorityService = Guard.Against.Null(agentAuthorityService);
    }

    protected async Task<string> GetAdminHubAddressDetailsAsync(string? adminHubName, CancellationToken cancellationToken)
    {
        return string.IsNullOrWhiteSpace(adminHubName)
            ? string.Empty
            : await GetConfiguredFcAreasService
                .TryGetAdminHubAddress(adminHubName, cancellationToken)
                .ConfigureAwait(false);
    }

    protected async Task<Result<FellingLicenceApplicationSummaryModel>> GetFellingLicenceDetailsAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var fla = await FellingLicenceRepository.GetAsync(applicationId, cancellationToken);
        if (fla.HasNoValue)
        {
            return Result.Failure<FellingLicenceApplicationSummaryModel>(
                "Could not locate Felling Licence Application with the given id");
        }

        return await ExtractApplicationSummaryAsync(fla.Value, cancellationToken);
    }

    protected async Task<Result<FellingLicenceApplicationSummaryModel>> ExtractApplicationSummaryAsync(
        Flo.Services.FellingLicenceApplications.Entities.FellingLicenceApplication fla,
        CancellationToken cancellationToken)
    {
        var woodlandOwner = await WoodlandOwnerService.RetrieveWoodlandOwnerByIdAsync(fla.WoodlandOwnerId, cancellationToken);
        if (woodlandOwner.IsFailure)
        {
            return Result.Failure<FellingLicenceApplicationSummaryModel>(
                "Could not locate Felling Licence Application with the given id");
        }

        var agency = await AgentAuthorityService
            .GetAgencyForWoodlandOwnerAsync(fla.WoodlandOwnerId, cancellationToken)
            .ConfigureAwait(false);
        var agentOrAgencyName = agency.HasValue
            ? agency.Value.OrganisationName ?? agency.Value.ContactName
            : null;

        var assigneeHistories = await GetAssigneeHistory(fla.AssigneeHistories, cancellationToken);
        if (assigneeHistories.IsFailure)
        {
            return Result.Failure<FellingLicenceApplicationSummaryModel>(
                "Could not locate Felling Licence Application with the given id");
        }

        var woodlandOwnerName = woodlandOwner.Value.GetContactNameForDisplay;

        var applicationSummary = new FellingLicenceApplicationSummaryModel
        {
            Id = fla.Id,
            ApplicationReference = fla.ApplicationReference,
            CitizensCharterDate = fla.CitizensCharterDate,
            FinalActionDate = fla.FinalActionDate,
            Status = fla.StatusHistories.Any()
                ? fla.StatusHistories.OrderByDescending(x => x.Created).First().Status
                : FellingLicenceStatus.Draft,
            PropertyName = fla.SubmittedFlaPropertyDetail?.Name,
            NameOfWood = fla.SubmittedFlaPropertyDetail?.NameOfWood,
            WoodlandOwnerName = woodlandOwnerName,
            WoodlandOwnerId = woodlandOwner.Value.Id,
            StatusHistories = ModelMapping.ToStatusHistoryModelList(fla.StatusHistories).ToList(),
            AssigneeHistories = assigneeHistories.Value,
            AgentOrAgencyName = agentOrAgencyName,
            MostRecentFcLisReport = GetMostRecentDocumentOfType(fla.Documents, DocumentPurpose.FcLisConstraintReport),
            MostRecentApplicationDocument = GetMostRecentDocumentOfType(fla.Documents, DocumentPurpose.ApplicationDocument),
            DetailsList = CreateFellingAndRestockingDetails(fla),
            DateReceived = fla.DateReceived,
            Source = fla.Source,
            AreaCode = fla.AreaCode,
            AdministrativeRegion = fla.AdministrativeRegion,
            CreatedById = fla.CreatedById
        };
        return Result.Success(applicationSummary);
    }
    
    protected static string CreateViewFLAUrl(Guid applicationId, string viewFLABase) =>
        $"{viewFLABase}/{applicationId}";

    protected async Task<Result<List<AssigneeHistoryModel>>> GetAssigneeHistory(
        IList<AssigneeHistory> historyEntities,
        CancellationToken cancellationToken)
    {
        var result = new List<AssigneeHistoryModel>(historyEntities.Count);

        foreach (var historyEntity in historyEntities)
        {
            // Note switch between internal and external user repositories.
            // Authors are always external users but otherwise assigned users 
            // are expected to be internal users, which reside in the internal
            // users table

            if (historyEntity.Role is AssignedUserRole.Author || historyEntity.Role is AssignedUserRole.Applicant)
            {
                var externalApplicant = await ExternalUserAccountService.RetrieveUserAccountEntityByIdAsync(
                    historyEntity.AssignedUserId,
                    cancellationToken);
                if (externalApplicant.IsFailure)
                {
                    return Result.Failure<List<AssigneeHistoryModel>>(
                        "Could not retrieve external applicant for assignee history entry");
                }

                result.Add(new AssigneeHistoryModel
                {
                    Role = historyEntity.Role,
                    TimestampAssigned = historyEntity.TimestampAssigned,
                    TimestampUnassigned = historyEntity.TimestampUnassigned,
                    ExternalApplicant = new ExternalApplicantModel
                    {
                        ApplicantType = externalApplicant.Value.GetExternalApplicantType(),
                        Email = externalApplicant.Value.Email,
                        FirstName = externalApplicant.Value.FirstName,
                        LastName = externalApplicant.Value.LastName,
                        Id = externalApplicant.Value.Id,
                        IsActiveAccount = externalApplicant.Value.Status is UserAccountStatus.Active
                    }
                });
            }
            else
            {
                var user = await InternalUserAccountService.GetUserAccountAsync(
                    historyEntity.AssignedUserId,
                    cancellationToken);
                if (user.HasNoValue)
                {
                    return Result.Failure<List<AssigneeHistoryModel>>(
                        "Could not retrieve internal user for assignee history entry");
                }

                result.Add(new AssigneeHistoryModel
                {
                    Role = historyEntity.Role,
                    TimestampAssigned = historyEntity.TimestampAssigned,
                    TimestampUnassigned = historyEntity.TimestampUnassigned,
                    UserAccount = ModelMapping.ToUserAccountModel(user.Value)
                });
            }
        }

        return Result.Success(result);
    }

    public async Task<Result<List<BriefAssigneeHistoryModel>>> GetBriefAssigneeHistory(
        IEnumerable<AssigneeHistory> historyEntities,
        CancellationToken cancellationToken)
    {
        var result = new List<BriefAssigneeHistoryModel>();

        foreach (var historyEntity in historyEntities)
        {
            // Note switch between internal and external user repositories.
            // Authors are always external users but otherwise assigned users 
            // are expected to be internal users, which reside in the internal
            // users table

            if (historyEntity.Role is AssignedUserRole.Author || historyEntity.Role is AssignedUserRole.Applicant)
            {
                var externalApplicant = await ExternalUserAccountService.RetrieveUserAccountEntityByIdAsync(
                    historyEntity.AssignedUserId,
                    cancellationToken);
                if (externalApplicant.IsFailure)
                {
                    return Result.Failure<List<BriefAssigneeHistoryModel>>(
                        "Could not retrieve external applicant for assignee history entry");
                }

                result.Add(new BriefAssigneeHistoryModel
                {
                    FellingLicenceApplicationId = historyEntity.FellingLicenceApplicationId,
                    UserName = externalApplicant.Value.FullName(),
                    UserId = externalApplicant.Value.Id
                });
            }
            else
            {
                var user = await InternalUserAccountService.GetUserAccountAsync(
                    historyEntity.AssignedUserId,
                    cancellationToken);
                if (user.HasNoValue)
                {
                    return Result.Failure<List<BriefAssigneeHistoryModel>>(
                        "Could not retrieve internal user for assignee history entry");
                }

                result.Add(new BriefAssigneeHistoryModel
                {
                    FellingLicenceApplicationId = historyEntity.FellingLicenceApplicationId,
                    UserName = user.Value.FullName(),
                    UserId = user.Value.Id
                });
            }
        }

        return Result.Success(result);
    }

    private List<FellingAndRestockingDetail> CreateFellingAndRestockingDetails(Flo.Services.FellingLicenceApplications.Entities.FellingLicenceApplication application)
    {
        if (application.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments is null)
        {
            return new List<FellingAndRestockingDetail>();
        }

        var compartmentsDictionary = application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.ToDictionary(c => c.CompartmentId, c => c);
        
        var fellingAndRestockingDetails =
            (application.LinkedPropertyProfile?.ProposedFellingDetails ?? new List<ProposedFellingDetail>())
            .Select(proposedFellingDetail =>
                new FellingAndRestockingDetail
                {
                    CompartmentId = proposedFellingDetail.PropertyProfileCompartmentId,
                    CompartmentName = compartmentsDictionary[proposedFellingDetail.PropertyProfileCompartmentId].DisplayName,
                    GISData = compartmentsDictionary[proposedFellingDetail.PropertyProfileCompartmentId].GISData,
                    WoodlandId = proposedFellingDetail.LinkedPropertyProfileId,
                    FellingDetail = ModelMapping.ToProposedFellingDetailModel(proposedFellingDetail),
                    Zone1 = compartmentsDictionary[proposedFellingDetail.PropertyProfileCompartmentId].Zone1,
                    Zone2 = compartmentsDictionary[proposedFellingDetail.PropertyProfileCompartmentId].Zone2,
                    Zone3 = compartmentsDictionary[proposedFellingDetail.PropertyProfileCompartmentId].Zone3,
                }).ToList();
        var fellingAndRestockingDetailsDictionary =
            fellingAndRestockingDetails.ToDictionary(d => d.FellingDetail.Id, d => d);

        var proposedRestockingDetails = application.LinkedPropertyProfile?.ProposedFellingDetails?.SelectMany(p => p.ProposedRestockingDetails);

        proposedRestockingDetails?.ToList().ForEach(r =>
            {
                if (!fellingAndRestockingDetailsDictionary.ContainsKey(r.ProposedFellingDetailsId))
                {
                    return;
                }
                fellingAndRestockingDetailsDictionary[r.ProposedFellingDetailsId].RestockingDetail =
                    ModelMapping.ToProposedRestockingDetailModel(r);
            });
        return fellingAndRestockingDetails;
    }

    private static Maybe<DocumentModel> GetMostRecentDocumentOfType(IList<Document>? documents, DocumentPurpose purpose)
    {
        if (documents == null) return Maybe<DocumentModel>.None;

        var lisDocument = documents.OrderByDescending(x=>x.CreatedTimestamp)
            .FirstOrDefault(x => x.Purpose == purpose);

        if (lisDocument == null) return Maybe<DocumentModel>.None;

        var documentModel = ModelMapping.ToDocumentModel(lisDocument);
        return Maybe<DocumentModel>.From(documentModel);
    }

    protected async Task<Result<ExternalUserAccountModel>> GetSubmittingUserAsync(
        Guid externalUserId,
        CancellationToken cancellationToken)
    {
        var user = await ExternalUserAccountService.RetrieveUserAccountEntityByIdAsync(externalUserId, cancellationToken);
        if (user.IsFailure)
        {
            return Result.Failure<ExternalUserAccountModel>(
                "Could not locate external user with the given id");
        }
        var userReturn = ModelMapping.ToExternalUserAccountModel(user.Value);

        return Result.Success(userReturn);
    }

    protected static List<FellingLicenceStatus> ApplicationCompletedStatuses =>
        new()
        {
            FellingLicenceStatus.Withdrawn,
            FellingLicenceStatus.Approved,
            FellingLicenceStatus.Refused
        };

    public class BriefAssigneeHistoryModel
    {
        public Guid FellingLicenceApplicationId { get; set; }

        public string UserName { get; set; }

        public Guid UserId { get; set; }
    }
}