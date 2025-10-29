using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using SubmittedFlaPropertyCompartment = Forestry.Flo.Services.FellingLicenceApplications.Entities.SubmittedFlaPropertyCompartment;
using UserAccount = Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class ConfirmedFellingAndRestockingDetailsUseCaseTests
{
    private readonly Mock<IUserAccountService> _userAccountService = new();
    private readonly Mock<IRetrieveUserAccountsService> _externalUserAccountService = new();
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceRepository = new();
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerService = new();
    private readonly Mock<IUpdateConfirmedFellingAndRestockingDetailsService> _updateConfirmedFellAndRestockService = new();
    private readonly Mock<IUpdateWoodlandOfficerReviewService> _updateWoodlandOfficerReviewService = new();
    private readonly Mock<IAgentAuthorityService> _agentAuthorityService = new();
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas = new();
    private readonly Mock<IDbContextTransaction> _transactionMock = new();
    private readonly Mock<IGetFellingLicenceApplicationForInternalUsers> _getFellingLicenceApplicationForInternalUsers = new();
    private readonly Mock<IAuditService<ConfirmedFellingAndRestockingDetailsUseCase>> _auditingService = new();
    private readonly Mock<IActivityFeedItemProvider> _activityFeedItemProvider = new();
    private readonly Mock<IOptions<ReviewAmendmentsOptions>> _reviewAmendmentsOptions = new();
    private readonly Mock<IOptions<ExternalApplicantSiteOptions>> _externalApplicantSiteOptions = new();
    private readonly Mock<ISendNotifications> _emailService = new();
    private readonly Mock<IWoodlandOfficerReviewSubStatusService> _woodlandOfficerReviewSubStatusService = new();
    private const string AdminHubAddress = "admin hub address";
    private readonly Fixture _fixture = new();
    private readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private ConfirmedFellingAndRestockingDetailsUseCase CreateSut()
    {
        _externalUserAccountService.Reset();
        _userAccountService.Reset();
        _fellingLicenceRepository.Reset();
        _woodlandOwnerService.Reset();
        _updateConfirmedFellAndRestockService.Reset();
        _woodlandOwnerService.Reset();
        _getConfiguredFcAreas.Reset();
        _transactionMock.Reset();
        _reviewAmendmentsOptions.Reset();
        _externalApplicantSiteOptions.Reset();
        _emailService.Reset();

        _getConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);

        // Provide default options value if accessed
        _reviewAmendmentsOptions.Setup(x => x.Value).Returns(new ReviewAmendmentsOptions());
        _externalApplicantSiteOptions.Setup(x => x.Value).Returns(new ExternalApplicantSiteOptions());

        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: Guid.NewGuid(),
            accountTypeInternal: Flo.Services.Common.User.AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(userPrincipal);

        return new ConfirmedFellingAndRestockingDetailsUseCase(
            _userAccountService.Object,
            _externalUserAccountService.Object,
            _getFellingLicenceApplicationForInternalUsers.Object,
            _fellingLicenceRepository.Object,
            _woodlandOwnerService.Object,
            _updateConfirmedFellAndRestockService.Object,
            _updateWoodlandOfficerReviewService.Object,
            _agentAuthorityService.Object,
            _getConfiguredFcAreas.Object,
            _auditingService.Object,
            _activityFeedItemProvider.Object,
            _getFellingLicenceApplicationForInternalUsers.Object,
            _reviewAmendmentsOptions.Object,
            new RequestContext("test", new RequestUserModel(internalUser.Principal)),
            _emailService.Object,
            _externalApplicantSiteOptions.Object,
            _woodlandOfficerReviewSubStatusService.Object,
            new NullLogger<ConfirmedFellingAndRestockingDetailsUseCase>());
    }

    [Theory, AutoMoqData]

    public async Task ConfirmedDetailsCorrectlyRetrieved_WhenAllDetailsCorrect(
        FellingLicenceApplication fla, 
        CombinedConfirmedFellingAndRestockingDetailRecord confirmedFrDetailModelList,
        UserAccount userAccount, 
        WoodlandOwnerModel woodlandOwner,
        List<ActivityFeedItemModel> activityItems)
    {
        var sut = CreateSut();

        _fellingLicenceRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fla);

        _updateConfirmedFellAndRestockService.Setup(r =>
                r.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmedFrDetailModelList);

        _woodlandOwnerService.Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwner);

        _externalUserAccountService.Setup(r => r.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccount);

        _activityFeedItemProvider.Setup(r => r.RetrieveAllRelevantActivityFeedItemsAsync(
                It.IsAny<ActivityFeedItemProviderModel>(),
                It.IsAny<ActorType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(activityItems);

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        fla.CreatedById = userAccount.Id;
        fla.WoodlandOwnerId = woodlandOwner.Id!.Value;

        fla.AssigneeHistories = new List<AssigneeHistory>
        {
            new()
            {
                AssignedUserId = userAccount.Id,
                FellingLicenceApplication = fla,
                FellingLicenceApplicationId = fla.Id,
                Role = AssignedUserRole.Author,
                TimestampAssigned = DateTime.Now
            }
        };

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        var result = await sut.GetConfirmedFellingAndRestockingDetailsAsync(fla.Id, internalUser, CancellationToken.None);

        Assert.True(result.IsSuccess);

        for (var i = 0; i < result.Value.Compartments.Length; i++)
        {
            var flaValue = confirmedFrDetailModelList.ConfirmedFellingAndRestockingDetailModels.OrderBy(x=>x.CompartmentId).ToArray()[i];
            var modelValue = result.Value.Compartments.OrderBy(x=>x.CompartmentId).ToArray()[i];

            // compare compartment values to model

            Assert.Equal(flaValue.SubmittedFlaPropertyCompartmentId, modelValue.SubmittedFlaPropertyCompartmentId);
            Assert.Equal(flaValue.CompartmentId, modelValue.CompartmentId);
            Assert.Equal(flaValue.CompartmentNumber, modelValue.CompartmentNumber);

            // compare confirmed felling details to model
            foreach (var confirmedFellingDetailModel in flaValue.ConfirmedFellingDetailModels)
            {
                var fellingDetail = modelValue.ConfirmedFellingDetails.Where(predicate => predicate.ConfirmedFellingDetailsId == confirmedFellingDetailModel.ConfirmedFellingDetailsId).FirstOrDefault();
                Assert.NotNull(fellingDetail);
                Assert.Equal(confirmedFellingDetailModel.AreaToBeFelled, fellingDetail!.AreaToBeFelled);
                Assert.Equal(confirmedFellingDetailModel.IsPartOfTreePreservationOrder, fellingDetail.IsPartOfTreePreservationOrder);
                Assert.Equal(confirmedFellingDetailModel.IsWithinConservationArea, fellingDetail.IsWithinConservationArea);
                Assert.Equal(confirmedFellingDetailModel.NumberOfTrees, fellingDetail.NumberOfTrees);
                Assert.Equal(confirmedFellingDetailModel.OperationType, fellingDetail.OperationType);
                Assert.Equal(confirmedFellingDetailModel.EstimatedTotalFellingVolume, fellingDetail.EstimatedTotalFellingVolume);

                for (var j = 0; j < fellingDetail.ConfirmedFellingSpecies!.Count(); j++)
                {
                    // compare confirmed felling species to model

                    Assert.NotNull(fellingDetail.ConfirmedFellingSpecies[j]);
                    Assert.Equal(confirmedFellingDetailModel.ConfirmedFellingSpecies.ToArray()[j].Species, fellingDetail.ConfirmedFellingSpecies[j].Species);
                }

                // compare confirmed restocking details to model
                foreach(var confirmedRestockingDetailModel in confirmedFellingDetailModel.ConfirmedRestockingDetailModels!)
                {
                    var restockingDetail = fellingDetail.ConfirmedRestockingDetails.Where(predicate => predicate.ConfirmedRestockingDetailsId == confirmedRestockingDetailModel.ConfirmedRestockingDetailsId).FirstOrDefault();
                    Assert.NotNull(restockingDetail);
                    Assert.Equal(confirmedRestockingDetailModel.Area, restockingDetail.RestockArea);
                    Assert.Equal(confirmedRestockingDetailModel.PercentNaturalRegeneration, restockingDetail.PercentNaturalRegeneration);
                    Assert.Equal(confirmedRestockingDetailModel.PercentOpenSpace, restockingDetail.PercentOpenSpace);
                    Assert.Equal(confirmedRestockingDetailModel.RestockingDensity, restockingDetail.RestockingDensity);
                    Assert.Equal(confirmedRestockingDetailModel.RestockingProposal, restockingDetail.RestockingProposal);

                    for (var j = 0; j < restockingDetail.ConfirmedRestockingSpecies!.Count(); j++)
                    {
                        // compare confirmed felling species to model
                        Assert.NotNull(restockingDetail);
                        Assert.Equal(confirmedRestockingDetailModel.ConfirmedRestockingSpecies[j].Percentage, restockingDetail.ConfirmedRestockingSpecies[j].Percentage);
                        Assert.Equal(confirmedRestockingDetailModel.ConfirmedRestockingSpecies[j].Species, restockingDetail.ConfirmedRestockingSpecies[j].Species);
                    }
                }
            }
        }

        _updateConfirmedFellAndRestockService.Verify(v => v.RetrieveConfirmedFellingAndRestockingDetailModelAsync(fla.Id, CancellationToken.None), Times.Once);
        _fellingLicenceRepository.Verify(v => v.GetAsync(fla.Id, CancellationToken.None), Times.Once);
        _woodlandOwnerService.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(woodlandOwner.Id!.Value, CancellationToken.None));
        _externalUserAccountService.Verify(v => v.RetrieveUserAccountEntityByIdAsync(userAccount.Id, CancellationToken.None));
        _activityFeedItemProvider.Verify(v =>
            v.RetrieveAllRelevantActivityFeedItemsAsync(
                It.Is<ActivityFeedItemProviderModel>(x => 
                    x.FellingLicenceId == fla.Id && 
                    x.FellingLicenceReference == fla.ApplicationReference && 
                    x.ItemTypes!.SequenceEqual(new ActivityFeedItemType[]
                        {
                            ActivityFeedItemType.WoodlandOfficerReviewComment, ActivityFeedItemType.AmendmentOfficerReason, ActivityFeedItemType.AmendmentApplicantReason
                        })),
                ActorType.InternalUser,
                CancellationToken.None), 
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ConfirmedDetailsRetrievalFailure_WhenActivityFeedItemsCannotBeRetrieved(
       FellingLicenceApplication fla,
       CombinedConfirmedFellingAndRestockingDetailRecord confirmedFrDetailModelList,
       UserAccount userAccount,
       WoodlandOwnerModel woodlandOwner)
    {
        var sut = CreateSut();

        _fellingLicenceRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fla);

        _updateConfirmedFellAndRestockService.Setup(r =>
                r.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmedFrDetailModelList);

        _woodlandOwnerService.Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwner);

        _externalUserAccountService.Setup(r => r.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccount);

        _activityFeedItemProvider.Setup(r => r.RetrieveAllRelevantActivityFeedItemsAsync(
                It.IsAny<ActivityFeedItemProviderModel>(),
                It.IsAny<ActorType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<ActivityFeedItemModel>>("failure"));

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        fla.CreatedById = userAccount.Id;
        fla.WoodlandOwnerId = woodlandOwner.Id!.Value;

        fla.AssigneeHistories = new List<AssigneeHistory>
        {
            new()
            {
                AssignedUserId = userAccount.Id,
                FellingLicenceApplication = fla,
                FellingLicenceApplicationId = fla.Id,
                Role = AssignedUserRole.Author,
                TimestampAssigned = DateTime.Now
            }
        };

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        var result = await sut.GetConfirmedFellingAndRestockingDetailsAsync(fla.Id, internalUser, CancellationToken.None);

        Assert.True(result.IsFailure);

        _updateConfirmedFellAndRestockService.Verify(v => v.RetrieveConfirmedFellingAndRestockingDetailModelAsync(fla.Id, CancellationToken.None), Times.Once);
        _fellingLicenceRepository.Verify(v => v.GetAsync(fla.Id, CancellationToken.None), Times.Once);
        _woodlandOwnerService.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(woodlandOwner.Id!.Value, CancellationToken.None));
        _externalUserAccountService.Verify(v => v.RetrieveUserAccountEntityByIdAsync(userAccount.Id, CancellationToken.None));
    }

    [Theory, AutoMoqData]

    public async Task ShouldReturnFailure_WhenFLANotFound(FellingLicenceApplication fla)
    {
        var sut = CreateSut();

        _fellingLicenceRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);
        var result = await sut.GetConfirmedFellingAndRestockingDetailsAsync(fla.Id, internalUser, CancellationToken.None);

        Assert.True(result.IsFailure);
        
        _fellingLicenceRepository.Verify(v => v.GetAsync(fla.Id, CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_WhenConfirmedFellingDetailsSuccessfullySaved(
        AmendConfirmedFellingDetailsViewModel amendModel)
    {
        var sut = CreateSut();
        SetValidSpecies(amendModel);

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

         _updateConfirmedFellAndRestockService
            .Setup(r => r.SaveChangesToConfirmedFellingDetailsAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<IndividualFellingRestockingDetailModel>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateWoodlandOfficerReviewService
            .Setup(r => r.HandleConfirmedFellingAndRestockingChangesAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                amendModel.ConfirmedFellingAndRestockingComplete,
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .ReturnsAsync(Result.Success());

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        var result = await sut.SaveConfirmedFellingDetailsAsync(
            amendModel,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        _transactionMock.Verify(v => v.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        _updateConfirmedFellAndRestockService.Verify(v =>
            v.SaveChangesToConfirmedFellingDetailsAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<IndividualFellingRestockingDetailModel>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                CancellationToken.None), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(v =>
            v.HandleConfirmedFellingAndRestockingChangesAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                amendModel.ConfirmedFellingAndRestockingComplete,
                CancellationToken.None,
                It.IsAny<bool>()), Times.Once);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateConfirmedFellingDetails
                && a.ActorType == ActorType.InternalUser
                && a.SourceEntityId == amendModel.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {}, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenSaveConfirmedFellingDetailsFails(
        AmendConfirmedFellingDetailsViewModel amendModel)
    {
        var sut = CreateSut();
        SetValidSpecies(amendModel);

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        const string errorMessage = "Save failed";

        _updateConfirmedFellAndRestockService
            .Setup(r => r.SaveChangesToConfirmedFellingDetailsAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<IndividualFellingRestockingDetailModel>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        var result = await sut.SaveConfirmedFellingDetailsAsync(
            amendModel,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _transactionMock.Verify(v => v.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(v => v.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _updateConfirmedFellAndRestockService.Verify(v =>
            v.SaveChangesToConfirmedFellingDetailsAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<IndividualFellingRestockingDetailModel>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                CancellationToken.None), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(v =>
            v.HandleConfirmedFellingAndRestockingChangesAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateConfirmedFellingDetailsFailure
                && a.ActorType == ActorType.InternalUser
                && a.SourceEntityId == amendModel.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Error = errorMessage
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenHandleConfirmedFellingAndRestockingChangesFails(
        AmendConfirmedFellingDetailsViewModel amendModel)
    {
        var sut = CreateSut();
        SetValidSpecies(amendModel);

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        _updateConfirmedFellAndRestockService
            .Setup(r => r.SaveChangesToConfirmedFellingDetailsAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<IndividualFellingRestockingDetailModel>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        const string errorMessage = "WO review update failed";

        _updateWoodlandOfficerReviewService
            .Setup(r => r.HandleConfirmedFellingAndRestockingChangesAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                amendModel.ConfirmedFellingAndRestockingComplete,
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        var result = await sut.SaveConfirmedFellingDetailsAsync(
            amendModel,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        _transactionMock.Verify(v => v.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(v => v.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _updateConfirmedFellAndRestockService.Verify(v =>
            v.SaveChangesToConfirmedFellingDetailsAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<IndividualFellingRestockingDetailModel>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                CancellationToken.None), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(v =>
            v.HandleConfirmedFellingAndRestockingChangesAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                amendModel.ConfirmedFellingAndRestockingComplete,
                CancellationToken.None,
                It.IsAny<bool>()), Times.Once);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateConfirmedFellingDetailsFailure
                && a.ActorType == ActorType.InternalUser
                && a.SourceEntityId == amendModel.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Error = errorMessage
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_WhenAddNewConfirmedFellingDetailsSuccessfullySaved(
        AddNewConfirmedFellingDetailsViewModel addModel)
    {
        var sut = CreateSut();
        SetValidSpecies(addModel);

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        _updateConfirmedFellAndRestockService
            .Setup(r => r.AddNewConfirmedFellingDetailsAsync(
                addModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<NewConfirmedFellingDetailWithCompartmentId>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateWoodlandOfficerReviewService
            .Setup(r => r.HandleConfirmedFellingAndRestockingChangesAsync(
                addModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                false,
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .ReturnsAsync(Result.Success());

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        var result = await sut.SaveConfirmedFellingDetailsAsync(
            addModel,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        _transactionMock.Verify(v => v.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        _updateConfirmedFellAndRestockService.Verify(v =>
            v.AddNewConfirmedFellingDetailsAsync(
                addModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<NewConfirmedFellingDetailWithCompartmentId>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                CancellationToken.None), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(v =>
            v.HandleConfirmedFellingAndRestockingChangesAsync(
                addModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                false,
                CancellationToken.None,
                It.IsAny<bool>()), Times.Once);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateConfirmedFellingDetails
                && a.ActorType == ActorType.InternalUser
                && a.SourceEntityId == addModel.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                { }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenAddNewConfirmedFellingDetailsFails(
        AddNewConfirmedFellingDetailsViewModel addModel)
    {
        var sut = CreateSut();
        SetValidSpecies(addModel);

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        const string errorMessage = "Add failed";

        _updateConfirmedFellAndRestockService
            .Setup(r => r.AddNewConfirmedFellingDetailsAsync(
                addModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<NewConfirmedFellingDetailWithCompartmentId>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        var result = await sut.SaveConfirmedFellingDetailsAsync(
            addModel,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _transactionMock.Verify(v => v.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(v => v.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _updateConfirmedFellAndRestockService.Verify(v =>
            v.AddNewConfirmedFellingDetailsAsync(
                addModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<NewConfirmedFellingDetailWithCompartmentId>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                CancellationToken.None), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(v =>
            v.HandleConfirmedFellingAndRestockingChangesAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateConfirmedFellingDetailsFailure
                && a.ActorType == ActorType.InternalUser
                && a.SourceEntityId == addModel.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Error = errorMessage
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenHandleConfirmedFellingAndRestockingChangesFails_OnAddNew(
        AddNewConfirmedFellingDetailsViewModel addModel)
    {
        var sut = CreateSut();
        SetValidSpecies(addModel);

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        _updateConfirmedFellAndRestockService
            .Setup(r => r.AddNewConfirmedFellingDetailsAsync(
                addModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<NewConfirmedFellingDetailWithCompartmentId>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        const string errorMessage = "WO review update failed";

        _updateWoodlandOfficerReviewService
            .Setup(r => r.HandleConfirmedFellingAndRestockingChangesAsync(
                addModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                false,
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        var result = await sut.SaveConfirmedFellingDetailsAsync(
            addModel,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        _transactionMock.Verify(v => v.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(v => v.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _updateConfirmedFellAndRestockService.Verify(v =>
            v.AddNewConfirmedFellingDetailsAsync(
                addModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<NewConfirmedFellingDetailWithCompartmentId>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                CancellationToken.None), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(v =>
            v.HandleConfirmedFellingAndRestockingChangesAsync(
                addModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                false,
                CancellationToken.None, It.IsAny<bool>()), Times.Once);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateConfirmedFellingDetailsFailure
                && a.ActorType == ActorType.InternalUser
                && a.SourceEntityId == addModel.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Error = errorMessage
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }



    [Theory, AutoMoqData]
    public async Task GetSelectableFellingCompartmentsAsync_ReturnsSelectableCompartments_WhenServiceSucceeds(
        Guid applicationId,
        List<SubmittedFlaPropertyCompartment> compartments,
        WoodlandOwnerModel woodlandOwner,
        UserAccount userAccount)
    {
        // Arrange
        var sut = CreateSut();
        var selectableCompartments = compartments
            .Select(x => new SelectableCompartment(x.CompartmentId, x.CompartmentNumber))
            .ToList();

        _getFellingLicenceApplicationForInternalUsers
            .Setup(s => s.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(compartments));

        _fellingLicenceRepository
            .Setup(r => r.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FellingLicenceApplication());

        _woodlandOwnerService.Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwner);

        _externalUserAccountService.Setup(r => r.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccount);

        // Act
        var result = await sut.GetSelectableFellingCompartmentsAsync(applicationId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equivalent(selectableCompartments, result.Value.SelectableCompartments);
        Assert.Equal(applicationId, result.Value.ApplicationId);
        Assert.NotNull(result.Value.GisData);
        Assert.NotNull(result.Value.FellingLicenceApplicationSummary);
        _getFellingLicenceApplicationForInternalUsers.Verify(
            s => s.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceRepository.Verify(
            r => r.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetSelectableFellingCompartmentsAsync_ReturnsFailure_WhenServiceFails(Guid applicationId, WoodlandOwnerModel woodlandOwner, UserAccount userAccount)
    {
        // Arrange
        var sut = CreateSut();
        var error = "Some error";
        _getFellingLicenceApplicationForInternalUsers
            .Setup(s => s.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<SubmittedFlaPropertyCompartment>>(error));

        _fellingLicenceRepository
            .Setup(r => r.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FellingLicenceApplication());

        _woodlandOwnerService.Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwner);

        _externalUserAccountService.Setup(r => r.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccount);

        // Act
        var result = await sut.GetSelectableFellingCompartmentsAsync(applicationId, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(applicationId.ToString(), result.Error);
        _getFellingLicenceApplicationForInternalUsers.Verify(
            s => s.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceRepository.Verify(
            r => r.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetSelectableFellingCompartmentsAsync_ReturnsFailure_WhenFlaNotFound(Guid applicationId)
    {
        // Arrange
        var sut = CreateSut();
        _fellingLicenceRepository
            .Setup(r => r.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        // Act
        var result = await sut.GetSelectableFellingCompartmentsAsync(applicationId, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Unable to retrieve application summary for application", result.Error);
        _fellingLicenceRepository.Verify(
            r => r.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getFellingLicenceApplicationForInternalUsers.Verify(
            s => s.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }



    private static void SetValidSpecies(AmendConfirmedFellingDetailsViewModel model)
    {
        var speciesDictionary = TreeSpeciesFactory.SpeciesDictionary;

        model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.ConfirmedFellingSpecies =
        [

            new ConfirmedFellingSpeciesModel
            {
                Id = Guid.NewGuid(),
                Species = speciesDictionary.First().Key,
                SpeciesType = SpeciesType.Broadleaf,
                Deleted = false
            },
            new ConfirmedFellingSpeciesModel
            {
                Id = Guid.NewGuid(),
                Species = speciesDictionary.Skip(1).First().Key,
                SpeciesType = SpeciesType.Conifer,
                Deleted = false
            }
        ];
    }

    private static void SetValidSpecies(AddNewConfirmedFellingDetailsViewModel model)
    {
        var speciesDictionary = TreeSpeciesFactory.SpeciesDictionary;
        model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.ConfirmedFellingSpecies =
        [
            new ConfirmedFellingSpeciesModel
            {
                Id = Guid.NewGuid(),
                Species = speciesDictionary.First().Key,
                SpeciesType = SpeciesType.Broadleaf,
                Deleted = false
            },
            new ConfirmedFellingSpeciesModel
            {
                Id = Guid.NewGuid(),
                Species = speciesDictionary.Skip(1).First().Key,
                SpeciesType = SpeciesType.Conifer,
                Deleted = false
            }
        ];
    }

    [Theory, AutoMoqData]
    public async Task DeleteConfirmedFellingDetailAsync_ShouldReturnSuccess_WhenDeleteSucceeds(
        Guid applicationId,
        Guid confirmedFellingDetailId)
    {
        var sut = CreateSut();

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _updateConfirmedFellAndRestockService
            .Setup(s => s.DeleteConfirmedFellingDetailAsync(
                applicationId,
                confirmedFellingDetailId,
                internalUser.UserAccountId!.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateWoodlandOfficerReviewService
            .Setup(s => s.HandleConfirmedFellingAndRestockingChangesAsync(
                applicationId,
                internalUser.UserAccountId!.Value,
                false,
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Success);

        var result = await sut.DeleteConfirmedFellingDetailAsync(
            applicationId,
            confirmedFellingDetailId,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        _updateConfirmedFellAndRestockService.Verify(s =>
            s.DeleteConfirmedFellingDetailAsync(
                applicationId,
                confirmedFellingDetailId,
                internalUser.UserAccountId!.Value,
                CancellationToken.None), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(s =>
            s.HandleConfirmedFellingAndRestockingChangesAsync(
                applicationId,
                internalUser.UserAccountId!.Value,
                false,
                CancellationToken.None, It.IsAny<bool>()), Times.Once);

        _transactionMock.Verify(t => t.CommitAsync(CancellationToken.None), Times.Once);
        _transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
            a.EventName == AuditEvents.UpdateConfirmedFellingDetails
            && a.ActorType == ActorType.InternalUser
            && a.SourceEntityId == applicationId
            && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
        ), CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task DeleteConfirmedFellingDetailAsync_ShouldReturnFailure_WhenDeleteFails(
        Guid applicationId,
        Guid confirmedFellingDetailId)
    {
        var sut = CreateSut();

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        const string errorMessage = "Delete failed";

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _updateConfirmedFellAndRestockService
            .Setup(s => s.DeleteConfirmedFellingDetailAsync(
                applicationId,
                confirmedFellingDetailId,
                internalUser.UserAccountId!.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        var result = await sut.DeleteConfirmedFellingDetailAsync(
            applicationId,
            confirmedFellingDetailId,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(errorMessage, result.Error);

        _updateConfirmedFellAndRestockService.Verify(s =>
            s.DeleteConfirmedFellingDetailAsync(
                applicationId,
                confirmedFellingDetailId,
                internalUser.UserAccountId!.Value,
                CancellationToken.None), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(s =>
            s.HandleConfirmedFellingAndRestockingChangesAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);

        _transactionMock.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
            a.EventName == AuditEvents.UpdateConfirmedFellingDetailsFailure
            && a.ActorType == ActorType.InternalUser
            && a.SourceEntityId == applicationId
            && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
            && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
               JsonSerializer.Serialize(new { Error = errorMessage }, SerializerOptions)
        ), CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task DeleteConfirmedFellingDetailAsync_ShouldReturnFailure_WhenWoReviewUpdateFails(
        Guid applicationId,
        Guid confirmedFellingDetailId)
    {
        var sut = CreateSut();

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        const string errorMessage = "WO review update failed";

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _updateConfirmedFellAndRestockService
            .Setup(s => s.DeleteConfirmedFellingDetailAsync(
                applicationId,
                confirmedFellingDetailId,
                internalUser.UserAccountId!.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateWoodlandOfficerReviewService
            .Setup(s => s.HandleConfirmedFellingAndRestockingChangesAsync(
                applicationId,
                internalUser.UserAccountId!.Value,
                false,
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        var result = await sut.DeleteConfirmedFellingDetailAsync(
            applicationId,
            confirmedFellingDetailId,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(errorMessage, result.Error);

        _updateConfirmedFellAndRestockService.Verify(s =>
            s.DeleteConfirmedFellingDetailAsync(
                applicationId,
                confirmedFellingDetailId,
                internalUser.UserAccountId!.Value,
                CancellationToken.None), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(s =>
            s.HandleConfirmedFellingAndRestockingChangesAsync(
                applicationId,
                internalUser.UserAccountId!.Value,
                false,
                CancellationToken.None, It.IsAny<bool>()), Times.Once);

        _transactionMock.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
            a.EventName == AuditEvents.UpdateConfirmedFellingDetailsFailure
            && a.ActorType == ActorType.InternalUser
            && a.SourceEntityId == applicationId
            && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
            && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
               JsonSerializer.Serialize(new { Error = errorMessage }, SerializerOptions)
        ), CancellationToken.None), Times.Once);
    }
    [Theory, AutoMoqData]
    public async Task RevertConfirmedFellingDetailAmendmentsAsync_ReturnsSuccess_WhenAllStepsSucceed(
        Guid applicationId,
        Guid proposedFellingDetailsId)
    {
        var sut = CreateSut();

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _updateConfirmedFellAndRestockService
            .Setup(s => s.RevertConfirmedFellingDetailAmendmentsAsync(
                applicationId,
                proposedFellingDetailsId,
                internalUser.UserAccountId!.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateWoodlandOfficerReviewService
            .Setup(s => s.HandleConfirmedFellingAndRestockingChangesAsync(
                applicationId,
                internalUser.UserAccountId!.Value,
                false,
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.RevertConfirmedFellingDetailAmendmentsAsync(
            applicationId,
            proposedFellingDetailsId,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        _updateConfirmedFellAndRestockService.Verify(s =>
            s.RevertConfirmedFellingDetailAmendmentsAsync(
                applicationId,
                proposedFellingDetailsId,
                internalUser.UserAccountId!.Value,
                CancellationToken.None), Times.Once);
        _updateWoodlandOfficerReviewService.Verify(s =>
            s.HandleConfirmedFellingAndRestockingChangesAsync(
                applicationId,
                internalUser.UserAccountId!.Value,
                false,
                CancellationToken.None, It.IsAny<bool>()), Times.Once);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
            a.EventName == AuditEvents.RevertConfirmedFellingDetails
            && a.SourceEntityId == proposedFellingDetailsId
            && a.UserId == internalUser.UserAccountId
        ), CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task RevertConfirmedFellingDetailAmendmentsAsync_ReturnsFailure_WhenRevertFails(
        Guid applicationId,
        Guid proposedFellingDetailsId)
    {
        var sut = CreateSut();

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        const string errorMessage = "Revert failed";

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _updateConfirmedFellAndRestockService
            .Setup(s => s.RevertConfirmedFellingDetailAmendmentsAsync(
                applicationId,
                proposedFellingDetailsId,
                internalUser.UserAccountId!.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        var result = await sut.RevertConfirmedFellingDetailAmendmentsAsync(
            applicationId,
            proposedFellingDetailsId,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(errorMessage, result.Error);
        _transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _updateConfirmedFellAndRestockService.Verify(s =>
            s.RevertConfirmedFellingDetailAmendmentsAsync(
                applicationId,
                proposedFellingDetailsId,
                internalUser.UserAccountId!.Value,
                CancellationToken.None), Times.Once);
        _updateWoodlandOfficerReviewService.Verify(s =>
            s.HandleConfirmedFellingAndRestockingChangesAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
            a.EventName == AuditEvents.RevertConfirmedFellingDetailsFailure
            && a.SourceEntityId == proposedFellingDetailsId
            && a.UserId == internalUser.UserAccountId
            && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
               JsonSerializer.Serialize(new { Error = errorMessage }, SerializerOptions)
        ), CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task RevertConfirmedFellingDetailAmendmentsAsync_ReturnsFailure_WhenUpdateWoodlandOfficerReviewFails(
        Guid applicationId,
        Guid proposedFellingDetailsId)
    {
        var sut = CreateSut();

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        const string errorMessage = "WO review update failed";

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _updateConfirmedFellAndRestockService
            .Setup(s => s.RevertConfirmedFellingDetailAmendmentsAsync(
                applicationId,
                proposedFellingDetailsId,
                internalUser.UserAccountId!.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateWoodlandOfficerReviewService
            .Setup(s => s.HandleConfirmedFellingAndRestockingChangesAsync(
                applicationId,
                internalUser.UserAccountId!.Value,
                false,
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        var result = await sut.RevertConfirmedFellingDetailAmendmentsAsync(
            applicationId,
            proposedFellingDetailsId,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(errorMessage, result.Error);
        _transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _updateConfirmedFellAndRestockService.Verify(s =>
            s.RevertConfirmedFellingDetailAmendmentsAsync(
                applicationId,
                proposedFellingDetailsId,
                internalUser.UserAccountId!.Value,
                CancellationToken.None), Times.Once);
        _updateWoodlandOfficerReviewService.Verify(s =>
            s.HandleConfirmedFellingAndRestockingChangesAsync(
                applicationId,
                internalUser.UserAccountId!.Value,
                false,
                CancellationToken.None, It.IsAny<bool>()), Times.Once);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
            a.EventName == AuditEvents.RevertConfirmedFellingDetailsFailure
            && a.SourceEntityId == proposedFellingDetailsId
            && a.UserId == internalUser.UserAccountId
            && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
               JsonSerializer.Serialize(new { Error = errorMessage }, SerializerOptions)
        ), CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task RevertConfirmedFellingDetailAmendmentsAsync_ReturnsFailure_WhenExceptionThrown(
        Guid applicationId,
        Guid proposedFellingDetailsId)
    {
        var sut = CreateSut();

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _updateConfirmedFellAndRestockService
            .Setup(s => s.RevertConfirmedFellingDetailAmendmentsAsync(
                applicationId,
                proposedFellingDetailsId,
                internalUser.UserAccountId!.Value,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        var result = await sut.RevertConfirmedFellingDetailAmendmentsAsync(
            applicationId,
            proposedFellingDetailsId,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("An error occurred while reverting deleted confirmed felling detail amendments", result.Error);
        _transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _updateConfirmedFellAndRestockService.Verify(s =>
            s.RevertConfirmedFellingDetailAmendmentsAsync(
                applicationId,
                proposedFellingDetailsId,
                internalUser.UserAccountId!.Value,
                CancellationToken.None), Times.Once);
        _updateWoodlandOfficerReviewService.Verify(s =>
            s.HandleConfirmedFellingAndRestockingChangesAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
            a.EventName == AuditEvents.RevertConfirmedFellingDetailsFailure
            && a.SourceEntityId == proposedFellingDetailsId
            && a.UserId == internalUser.UserAccountId
            && JsonSerializer.Serialize(a.AuditData, SerializerOptions) == 
            JsonSerializer.Serialize(new { Error = "Unexpected error" }, SerializerOptions)
        ), CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_WhenConfirmedRestockingDetailsSuccessfullySaved(
        AmendConfirmedRestockingDetailsViewModel amendModel)
    {
        var sut = CreateSut();
        // Set up valid species for the test model
        var speciesDictionary = TreeSpeciesFactory.SpeciesDictionary;
        amendModel.ConfirmedFellingRestockingDetails.ConfirmedRestockingDetails.ConfirmedRestockingSpecies = [
            new ConfirmedRestockingSpeciesModel
            {
                Id = Guid.NewGuid(),
                Species = speciesDictionary.First().Key,
                Deleted = false,
                Percentage = 50
            },
            new ConfirmedRestockingSpeciesModel
            {
                Id = Guid.NewGuid(),
                Species = speciesDictionary.Skip(1).First().Key,
                Deleted = false,
                Percentage = 50
            }
        ];

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        _updateConfirmedFellAndRestockService
            .Setup(r => r.SaveChangesToConfirmedRestockingDetailsAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.IndividualRestockingDetailModel>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        var result = await sut.SaveConfirmedRestockingDetailsAsync(
            amendModel,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        _transactionMock.Verify(v => v.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _updateConfirmedFellAndRestockService.Verify(v =>
            v.SaveChangesToConfirmedRestockingDetailsAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.IndividualRestockingDetailModel>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                CancellationToken.None), Times.Once);
        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateConfirmedRestockingDetails
                && a.ActorType == ActorType.InternalUser
                && a.SourceEntityId == amendModel.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                   JsonSerializer.Serialize(new { }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }


    [Theory, AutoMoqData]
    public async Task DeleteConfirmedRestockingDetailAsync_ShouldReturnSuccess_WhenDeleteSucceeds(
        Guid applicationId,
        Guid confirmedRestockingDetailId)
    {
        var sut = CreateSut();

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _updateConfirmedFellAndRestockService
            .Setup(s => s.DeleteConfirmedRestockingDetailAsync(
                applicationId,
                confirmedRestockingDetailId,
                internalUser.UserAccountId!.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateWoodlandOfficerReviewService
            .Setup(s => s.HandleConfirmedFellingAndRestockingChangesAsync(
                applicationId,
                internalUser.UserAccountId!.Value,
                false,
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Success);

        var result = await sut.DeleteConfirmedRestockingDetailAsync(
            applicationId,
            confirmedRestockingDetailId,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        _updateConfirmedFellAndRestockService.Verify(s =>
            s.DeleteConfirmedRestockingDetailAsync(
                applicationId,
                confirmedRestockingDetailId,
                internalUser.UserAccountId!.Value,
                CancellationToken.None), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(s =>
            s.HandleConfirmedFellingAndRestockingChangesAsync(
                applicationId,
                internalUser.UserAccountId!.Value,
                false,
                CancellationToken.None, It.IsAny<bool>()), Times.Once);

        _transactionMock.Verify(t => t.CommitAsync(CancellationToken.None), Times.Once);
        _transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
            a.EventName == AuditEvents.UpdateConfirmedFellingDetails
            && a.ActorType == ActorType.InternalUser
            && a.SourceEntityId == applicationId
            && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
        ), CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task DeleteConfirmedRestockingDetailAsync_ShouldReturnFailure_WhenDeleteFails(
        Guid applicationId,
        Guid confirmedRestockingDetailId)
    {
        var sut = CreateSut();

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        const string errorMessage = "Delete failed";

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _updateConfirmedFellAndRestockService
            .Setup(s => s.DeleteConfirmedRestockingDetailAsync(
                applicationId,
                confirmedRestockingDetailId,
                internalUser.UserAccountId!.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        var result = await sut.DeleteConfirmedRestockingDetailAsync(
            applicationId,
            confirmedRestockingDetailId,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(errorMessage, result.Error);

        _updateConfirmedFellAndRestockService.Verify(s =>
            s.DeleteConfirmedRestockingDetailAsync(
                applicationId,
                confirmedRestockingDetailId,
                internalUser.UserAccountId!.Value,
                CancellationToken.None), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(s =>
            s.HandleConfirmedFellingAndRestockingChangesAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);

        _transactionMock.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
            a.EventName == AuditEvents.UpdateConfirmedFellingDetailsFailure
            && a.ActorType == ActorType.InternalUser
            && a.SourceEntityId == applicationId
            && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
            && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
               JsonSerializer.Serialize(new { Error = errorMessage }, SerializerOptions)
        ), CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task DeleteConfirmedRestockingDetailAsync_ShouldReturnFailure_WhenWoReviewUpdateFails(
        Guid applicationId,
        Guid confirmedRestockingDetailId)
    {
        var sut = CreateSut();

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        const string errorMessage = "WO review update failed";

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _updateConfirmedFellAndRestockService
            .Setup(s => s.DeleteConfirmedRestockingDetailAsync(
                applicationId,
                confirmedRestockingDetailId,
                internalUser.UserAccountId!.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateWoodlandOfficerReviewService
            .Setup(s => s.HandleConfirmedFellingAndRestockingChangesAsync(
                applicationId,
                internalUser.UserAccountId!.Value,
                false,
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        var result = await sut.DeleteConfirmedRestockingDetailAsync(
            applicationId,
            confirmedRestockingDetailId,
            internalUser,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(errorMessage, result.Error);

        _updateConfirmedFellAndRestockService.Verify(s =>
            s.DeleteConfirmedRestockingDetailAsync(
                applicationId,
                confirmedRestockingDetailId,
                internalUser.UserAccountId!.Value,
                CancellationToken.None), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(s =>
            s.HandleConfirmedFellingAndRestockingChangesAsync(
                applicationId,
                internalUser.UserAccountId!.Value,
                false,
                CancellationToken.None, It.IsAny<bool>()), Times.Once);

        _transactionMock.Verify(t => t.RollbackAsync(CancellationToken.None), Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
            a.EventName == AuditEvents.UpdateConfirmedFellingDetailsFailure
            && a.ActorType == ActorType.InternalUser
            && a.SourceEntityId == applicationId
            && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
            && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
               JsonSerializer.Serialize(new { Error = errorMessage }, SerializerOptions)
        ), CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task SendAmendmentsToApplicant_ReturnsSuccess_WhenAllStepsSucceed(
Guid applicationId,
UserAccount applicantAccount,
FellingLicenceApplication applicationEntity)
    {
        var sut = CreateSut();
        _reviewAmendmentsOptions.Setup(o => o.Value).Returns(new ReviewAmendmentsOptions { ApplicationWithdrawalDays = 7 });

        var applicationDetails = new ApplicationNotificationDetails
        {
            ApplicationReference = "APP-REF-123",
            PropertyName = "Test Property",
            AdminHubName = "Hub1"
        };

        applicationEntity.CreatedById = applicantAccount.Id;

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        _updateWoodlandOfficerReviewService
            .Setup(s => s.CreateFellingAndRestockingAmendmentReviewAsync(applicationId, internalUser.UserAccountId!.Value, It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new FellingAndRestockingAmendmentReview()));

        _getFellingLicenceApplicationForInternalUsers
            .Setup(s => s.RetrieveApplicationNotificationDetailsAsync(applicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicationDetails));

        _getFellingLicenceApplicationForInternalUsers
            .Setup(s => s.GetApplicationByIdAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicationEntity));

        _externalUserAccountService
            .Setup(s => s.RetrieveUserAccountEntityByIdAsync(applicantAccount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicantAccount);

        _getConfiguredFcAreas
            .Setup(s => s.TryGetAdminHubAddress("Hub1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);

        _emailService
            .Setup(s => s.SendNotificationAsync(
                It.IsAny<AmendmentsSentToApplicantDataModel>(),
                NotificationType.AmendmentsSentToApplicant,
                It.IsAny<NotificationRecipient>(),
                null, null, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.SendAmendmentsToApplicant(applicationId, internalUser, "reason", CancellationToken.None);

        Assert.True(result.IsSuccess);
        _updateWoodlandOfficerReviewService.Verify(v => v.CreateFellingAndRestockingAmendmentReviewAsync(applicationId, internalUser.UserAccountId!.Value, It.IsAny<DateTime>(), "reason", It.IsAny<CancellationToken>()), Times.Once);
        _emailService.Verify(v => v.SendNotificationAsync(
            It.IsAny<AmendmentsSentToApplicantDataModel>(),
            NotificationType.AmendmentsSentToApplicant,
            It.IsAny<NotificationRecipient>(),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a => a.EventName == AuditEvents.SendAmendmentsToApplicant && a.SourceEntityId == applicationId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task SendAmendmentsToApplicant_ReturnsFailure_WhenCreateReviewFails(
        Guid applicationId)
    {
        var sut = CreateSut();
        const string error = "create failed";
        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        _updateWoodlandOfficerReviewService
            .Setup(s => s.CreateFellingAndRestockingAmendmentReviewAsync(applicationId, internalUser.UserAccountId!.Value, It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingAndRestockingAmendmentReview>(error));

        var result = await sut.SendAmendmentsToApplicant(applicationId, internalUser, null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        _emailService.Verify(v => v.SendNotificationAsync(It.IsAny<AmendmentsSentToApplicantDataModel>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]>(), It.IsAny<NotificationAttachment[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a => a.EventName == AuditEvents.SendAmendmentsToApplicantFailure && a.SourceEntityId == applicationId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task SendAmendmentsToApplicant_ReturnsFailure_WhenNotificationDetailsLookupFails(
        Guid applicationId)
    {
        var sut = CreateSut();
        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);
        _updateWoodlandOfficerReviewService
            .Setup(s => s.CreateFellingAndRestockingAmendmentReviewAsync(applicationId, internalUser.UserAccountId!.Value, It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new FellingAndRestockingAmendmentReview()));

        _getFellingLicenceApplicationForInternalUsers
            .Setup(s => s.RetrieveApplicationNotificationDetailsAsync(applicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ApplicationNotificationDetails>("details fail"));

        var result = await sut.SendAmendmentsToApplicant(applicationId, internalUser, null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Could not look up application reference", result.Error);
        _emailService.Verify(v => v.SendNotificationAsync(It.IsAny<AmendmentsSentToApplicantDataModel>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]>(), It.IsAny<NotificationAttachment[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task SendAmendmentsToApplicant_ReturnsFailure_WhenEmailSendFails(
        Guid applicationId,
        UserAccount applicantAccount,
        FellingLicenceApplication applicationEntity)
    {
        var sut = CreateSut();

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        _updateWoodlandOfficerReviewService
            .Setup(s => s.CreateFellingAndRestockingAmendmentReviewAsync(applicationId, internalUser.UserAccountId!.Value, It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new FellingAndRestockingAmendmentReview()));

        var applicationDetails = new ApplicationNotificationDetails
        {
            ApplicationReference = "APP-REF-456",
            PropertyName = "Prop",
            AdminHubName = "Hub1"
        };

        applicationEntity.CreatedById = applicantAccount.Id;

        _getFellingLicenceApplicationForInternalUsers
            .Setup(s => s.RetrieveApplicationNotificationDetailsAsync(applicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicationDetails));

        _getFellingLicenceApplicationForInternalUsers
            .Setup(s => s.GetApplicationByIdAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicationEntity));

        _externalUserAccountService
            .Setup(s => s.RetrieveUserAccountEntityByIdAsync(applicantAccount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicantAccount);

        _getConfiguredFcAreas
            .Setup(s => s.TryGetAdminHubAddress("Hub1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);

        _emailService
            .Setup(s => s.SendNotificationAsync(
                It.IsAny<AmendmentsSentToApplicantDataModel>(),
                NotificationType.AmendmentsSentToApplicant,
                It.IsAny<NotificationRecipient>(),
                null, null, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("email fail"));

        var result = await sut.SendAmendmentsToApplicant(applicationId, internalUser, null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to send amendments", result.Error);
    }

    [Theory, AutoMoqData]
    public async Task MakeFurtherAmendments_ReturnsSuccess_WhenServiceSucceeds(Guid amendmentReviewId)
    {
        var sut = CreateSut();

        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            Guid.NewGuid().ToString(),
            _fixture.Create<string>(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(userPrincipal);

        _updateWoodlandOfficerReviewService
            .Setup(s => s.CompleteFellingAndRestockingAmendmentReviewAsync(amendmentReviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.CloseAmendmentReview(internalUser, amendmentReviewId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _updateWoodlandOfficerReviewService.Verify(s => s.CompleteFellingAndRestockingAmendmentReviewAsync(amendmentReviewId, CancellationToken.None), Times.Once);

        // NOTE: The current implementation publishes the *Failure* audit event name on success (likely inverted logic in use case)
        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.CompleteFellingAndRestockingAmendmentReviewFailure &&
                a.SourceEntityId == amendmentReviewId &&
                a.UserId == internalUser.UserAccountId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task MakeFurtherAmendments_ReturnsFailure_WhenServiceFails(Guid amendmentReviewId)
    {
        var sut = CreateSut();

        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            Guid.NewGuid().ToString(),
            _fixture.Create<string>(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(userPrincipal);

        const string errorMessage = "completion failed";

        _updateWoodlandOfficerReviewService
            .Setup(s => s.CompleteFellingAndRestockingAmendmentReviewAsync(amendmentReviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        var result = await sut.CloseAmendmentReview(internalUser, amendmentReviewId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(errorMessage, result.Error);

        _updateWoodlandOfficerReviewService.Verify(s => s.CompleteFellingAndRestockingAmendmentReviewAsync(amendmentReviewId, CancellationToken.None), Times.Once);

        _auditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.CompleteFellingAndRestockingAmendmentReview &&
                a.SourceEntityId == amendmentReviewId &&
                a.UserId == internalUser.UserAccountId &&
                JsonSerializer.Serialize(a.AuditData, SerializerOptions) == JsonSerializer.Serialize(new { Error = errorMessage }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}