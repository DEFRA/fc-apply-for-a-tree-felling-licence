using AutoFixture;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using IndividualConfirmedFellingRestockingDetailModel = Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.IndividualConfirmedFellingRestockingDetailModel;

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
    private readonly Mock<IDbContextTransaction> _transactionMock = new ();
    private readonly Mock<IAuditService<ConfirmedFellingAndRestockingDetailsUseCase>> _auditingService = new();
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

        _getConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);

        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: Guid.NewGuid(),
            accountTypeInternal: Flo.Services.Common.User.AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(userPrincipal);

        return new ConfirmedFellingAndRestockingDetailsUseCase(
            _userAccountService.Object,
            _externalUserAccountService.Object,
            _fellingLicenceRepository.Object,
            _woodlandOwnerService.Object,
            _updateConfirmedFellAndRestockService.Object,
            _updateWoodlandOfficerReviewService.Object,
            _agentAuthorityService.Object,
            _getConfiguredFcAreas.Object,
            _auditingService.Object,
            new RequestContext("test", new RequestUserModel(internalUser.Principal)),
            new NullLogger<ConfirmedFellingAndRestockingDetailsUseCase>());
    }

    [Theory, AutoMoqData]

    public async Task ConfirmedDetailsCorrectlyRetrieved_WhenAllDetailsCorrect(FellingLicenceApplication fla, CombinedConfirmedFellingAndRestockingDetailRecord confirmedFrDetailModelList, UserAccount userAccount, WoodlandOwnerModel woodlandOwner)
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

        result.IsFailure.Should().BeTrue();
        
        _fellingLicenceRepository.Verify(v => v.GetAsync(fla.Id, CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]

    public async Task ShouldReturnSuccessAndUpdatedDetails_WhenDetailsSuccessfullySaved(
        FellingLicenceApplication fla,
        CompartmentConfirmedFellingRestockingDetailsModel compartmentConfirmedFellingRestockingDetailsModel)
    {
        var sut = CreateSut();

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        var internalUser = new InternalUser(user);

        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = fla.Id,
            Breadcrumbs = new BreadcrumbsModel(),
            ConfirmedFellingAndRestockingComplete = true,
            FellingLicenceApplicationSummary = new FellingLicenceApplicationSummaryModel(),
            Compartments = new CompartmentConfirmedFellingRestockingDetailsModel[fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.Count]
        };

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        for (var i = 0; i < model.Compartments.Length; i++)
        {
            var compartment = compartmentConfirmedFellingRestockingDetailsModel;
            compartment.CompartmentId =
                fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments![i].CompartmentId;
            compartment.CompartmentNumber =
                fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments![i].CompartmentNumber;
            compartment.SubCompartmentName =
                fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments![i].SubCompartmentName;

            model.Compartments[i] = compartment;
        }

        _fellingLicenceRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fla);

        _updateConfirmedFellAndRestockService.Setup(r =>
                r.SaveChangesToConfirmedFellingAndRestockingAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<IList<ConfirmedFellingAndRestockingDetailModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateWoodlandOfficerReviewService.Setup(r =>
                r.HandleConfirmedFellingAndRestockingChangesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _updateConfirmedFellAndRestockService.Setup(r =>
                r.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CombinedConfirmedFellingAndRestockingDetailRecord(new List<ConfirmedFellingAndRestockingDetailModel>(), false));

        _externalUserAccountService.Setup(r => r.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<UserAccount>());

        _woodlandOwnerService.Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<WoodlandOwnerModel>());

        _userAccountService.Setup(r => r.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>());


        var result = await sut.SaveConfirmedFellingAndRestockingDetailsAsync(
            model,
            internalUser,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ApplicationId.Should().Be(fla.Id);

        _fellingLicenceRepository.Verify(v => v.GetAsync(fla.Id, CancellationToken.None), Times.Exactly(1));
        _updateConfirmedFellAndRestockService.Verify(v => v.SaveChangesToConfirmedFellingAndRestockingAsync(fla.Id, internalUser.UserAccountId!.Value,
            It.Is<List<ConfirmedFellingAndRestockingDetailModel>>(x => 
                x.First().CompartmentId == compartmentConfirmedFellingRestockingDetailsModel.CompartmentId
                && x.First().CompartmentNumber == compartmentConfirmedFellingRestockingDetailsModel.CompartmentNumber
                && x.First().TotalHectares == compartmentConfirmedFellingRestockingDetailsModel.TotalHectares
                && x.First().ConfirmedTotalHectares == compartmentConfirmedFellingRestockingDetailsModel.ConfirmedTotalHectares!
                && x.First().Designation == compartmentConfirmedFellingRestockingDetailsModel.Designation
                && x.First().SubCompartmentName == compartmentConfirmedFellingRestockingDetailsModel.SubCompartmentName
            ), CancellationToken.None), Times.Once);
        _updateWoodlandOfficerReviewService.Verify(v => v.HandleConfirmedFellingAndRestockingChangesAsync(fla.Id, internalUser.UserAccountId!.Value, model.ConfirmedFellingAndRestockingComplete, CancellationToken.None), Times.Once);
        _updateConfirmedFellAndRestockService.Verify(v => v.RetrieveConfirmedFellingAndRestockingDetailModelAsync(fla.Id, CancellationToken.None), Times.Once);
        foreach (var assigneeHistory in fla.AssigneeHistories)
        {
            if (assigneeHistory.Role == AssignedUserRole.Author)
            {
                _externalUserAccountService.Verify(v => v.RetrieveUserAccountEntityByIdAsync(assigneeHistory.AssignedUserId, CancellationToken.None), Times.Once);
            }
            else
            {
                _userAccountService.Verify(v => v.GetUserAccountAsync(assigneeHistory.AssignedUserId, CancellationToken.None), Times.Once);
            }
        }
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
                It.IsAny<IndividualConfirmedFellingRestockingDetailModel>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateWoodlandOfficerReviewService
            .Setup(r => r.HandleConfirmedFellingAndRestockingChangesAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                amendModel.ConfirmedFellingAndRestockingComplete,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        var result = await sut.SaveConfirmedFellingDetailsAsync(
            amendModel,
            internalUser,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _transactionMock.Verify(v => v.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        _updateConfirmedFellAndRestockService.Verify(v =>
            v.SaveChangesToConfirmedFellingDetailsAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<IndividualConfirmedFellingRestockingDetailModel>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                CancellationToken.None), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(v =>
            v.HandleConfirmedFellingAndRestockingChangesAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                amendModel.ConfirmedFellingAndRestockingComplete,
                CancellationToken.None), Times.Once);

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
                It.IsAny<IndividualConfirmedFellingRestockingDetailModel>(),
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

        result.IsFailure.Should().BeTrue();

        _transactionMock.Verify(v => v.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(v => v.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _updateConfirmedFellAndRestockService.Verify(v =>
            v.SaveChangesToConfirmedFellingDetailsAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<IndividualConfirmedFellingRestockingDetailModel>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                CancellationToken.None), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(v =>
            v.HandleConfirmedFellingAndRestockingChangesAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Never);

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
                It.IsAny<IndividualConfirmedFellingRestockingDetailModel>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        const string errorMessage = "WO review update failed";

        _updateWoodlandOfficerReviewService
            .Setup(r => r.HandleConfirmedFellingAndRestockingChangesAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                amendModel.ConfirmedFellingAndRestockingComplete,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        var result = await sut.SaveConfirmedFellingDetailsAsync(
            amendModel,
            internalUser,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _transactionMock.Verify(v => v.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(v => v.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _updateConfirmedFellAndRestockService.Verify(v =>
            v.SaveChangesToConfirmedFellingDetailsAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                It.IsAny<IndividualConfirmedFellingRestockingDetailModel>(),
                It.IsAny<Dictionary<string, SpeciesModel>>(),
                CancellationToken.None), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(v =>
            v.HandleConfirmedFellingAndRestockingChangesAsync(
                amendModel.ApplicationId,
                internalUser.UserAccountId!.Value,
                amendModel.ConfirmedFellingAndRestockingComplete,
                CancellationToken.None), Times.Once);

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
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        var result = await sut.SaveConfirmedFellingDetailsAsync(
            addModel,
            internalUser,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

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
                CancellationToken.None), Times.Once);

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

        result.IsFailure.Should().BeTrue();

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
                It.IsAny<CancellationToken>()), Times.Never);

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
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        _updateConfirmedFellAndRestockService
            .Setup(s => s.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        var result = await sut.SaveConfirmedFellingDetailsAsync(
            addModel,
            internalUser,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
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
                CancellationToken.None), Times.Once);

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
}