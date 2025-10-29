using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.InternalUsers.Models;

namespace Forestry.Flo.External.Web.Tests.Services;

public class ReviewFellingAndRestockingAmendmentsUseCaseTests
{
    private readonly Mock<IUpdateConfirmedFellingAndRestockingDetailsService> _updateConfirmedService = new();
    private readonly IFixture _fixture = new Fixture().CustomiseFixtureForFellingLicenceApplications();
    private readonly Mock<IGetWoodlandOfficerReviewService> _getWoodlandOfficerReviewService = new();
    private readonly Mock<IUpdateWoodlandOfficerReviewService> _updateWoodlandOfficerReviewService = new();
    private readonly Mock<IAuditService<ReviewFellingAndRestockingAmendmentsUseCase>> _auditService = new();
    private readonly Mock<IGetFellingLicenceApplicationForInternalUsers> _getFellingLicenceApplicationService = new();
    private readonly Mock<IUserAccountService> _userAccountService = new();
    private readonly Mock<ISendNotifications> _sendNotifications = new();
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas = new();
    private readonly Mock<IOptions<InternalUserSiteOptions>> _internalUserSiteOptions = new();

    private readonly RequestContext _requestContext;
    private ExternalApplicant? _externalApplicant;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly InternalUserSiteOptions _defaultInternalUserSiteOptions = new()
    {
        BaseUrl = "https://test.com/"
    };

    public ReviewFellingAndRestockingAmendmentsUseCaseTests()
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal();
        _requestContext = new RequestContext("test", new RequestUserModel(user));
    }

    [Theory, AutoMoqData]
    public async Task ConfirmedDetailsCorrectlyRetrieved_WhenAllDetailsCorrect(
        FellingLicenceApplication fla,
        CombinedConfirmedFellingAndRestockingDetailRecord confirmedFrDetailModelList,
        UserAccount userAccount,
        FellingAndRestockingAmendmentReviewModel model,
        SubmittedFlaPropertyDetail submittedFlaProperty)
    {
        var sut = CreateSut();

        confirmedFrDetailModelList.SubmittedFlaPropertyCompartments.Clear();

        foreach (var fellingDetail in confirmedFrDetailModelList.ConfirmedFellingAndRestockingDetailModels)
        {
            confirmedFrDetailModelList.SubmittedFlaPropertyCompartments.Add(
                _fixture.Build<SubmittedFlaPropertyCompartment>()
                    .With(x => x.CompartmentId, fellingDetail.CompartmentId)
                    .With(x => x.Id, fellingDetail.SubmittedFlaPropertyCompartmentId)
                    .Create());


            foreach (var restockingDetail in fellingDetail.ProposedFellingDetailModels.SelectMany(y =>
                         y.ProposedRestockingDetails))
            {
                confirmedFrDetailModelList.SubmittedFlaPropertyCompartments.Add(
                    _fixture.Build<SubmittedFlaPropertyCompartment>()
                        .With(x => x.CompartmentId, restockingDetail.CompartmentId)
                        .Create());
            }
        }

        _updateConfirmedService.Setup(r =>
                r.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmedFrDetailModelList);

        _getWoodlandOfficerReviewService.Setup(r =>
                r.GetCurrentFellingAndRestockingAmendmentReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(model));

        fla.CreatedById = userAccount.Id;

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

        submittedFlaProperty.SubmittedFlaPropertyCompartments!.Clear();

        var result = await sut.GetReviewAmendmentsViewModelAsync(fla.Id, _externalApplicant, CancellationToken.None);

        Assert.True(result.IsSuccess);

        for (var i = 0; i < result.Value.AmendedFellingAndRestockingDetails.Compartments.Length; i++)
        {
            var flaValue = confirmedFrDetailModelList.ConfirmedFellingAndRestockingDetailModels.OrderBy(x => x.CompartmentId).ToArray()[i];
            var modelValue = result.Value.AmendedFellingAndRestockingDetails.Compartments.OrderBy(x => x.CompartmentId).ToArray()[i];

            // compare compartment values to model

            Assert.Equal(flaValue.SubmittedFlaPropertyCompartmentId, modelValue.SubmittedFlaPropertyCompartmentId);
            Assert.Equal(flaValue.CompartmentId, modelValue.CompartmentId);
            Assert.Equal(flaValue.CompartmentNumber, modelValue.CompartmentNumber);

            // compare confirmed felling details to model
            foreach (var confirmedFellingDetailModel in flaValue.ConfirmedFellingDetailModels)
            {
                var fellingDetail = modelValue.ConfirmedFellingDetails.FirstOrDefault(predicate => predicate.ConfirmedFellingDetailsId == confirmedFellingDetailModel.ConfirmedFellingDetailsId);
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
                foreach (var confirmedRestockingDetailModel in confirmedFellingDetailModel.ConfirmedRestockingDetailModels!)
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

        var idLookups = result.Value.AmendedFellingAndRestockingDetails.CompartmentIdToSubmittedCompartmentId;
        var gisLookup = result.Value.AmendedFellingAndRestockingDetails.CompartmentGisLookup;

        Assert.Equal(idLookups.Count, gisLookup.Count);

        foreach (var value in idLookups.Values)
        {
            Assert.True(gisLookup.ContainsKey(value));
        }

        Assert.Equal(model.ApplicantAgreed, result.Value.ApplicantAgreed);
        Assert.Equal(model.AmendmentsSentDate, result.Value.AmendmentsSentDate);
        Assert.Equal(model.ResponseReceivedDate, result.Value.ResponseReceivedDate);
        Assert.Equal(model.ApplicantDisagreementReason, result.Value.ApplicantDisagreementReason);

        _updateConfirmedService.Verify(v => v.RetrieveConfirmedFellingAndRestockingDetailModelAsync(fla.Id, CancellationToken.None), Times.Once);
        _getWoodlandOfficerReviewService.Verify(v => v.GetCurrentFellingAndRestockingAmendmentReviewAsync(fla.Id, CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenCurrentReviewRetrievalFailure(
        FellingLicenceApplication fla,
        CombinedConfirmedFellingAndRestockingDetailRecord confirmedFrDetailModelList,
        UserAccount userAccount,
        FellingAndRestockingAmendmentReviewModel model,
        SubmittedFlaPropertyDetail submittedFlaProperty)
    {
        var sut = CreateSut();

        confirmedFrDetailModelList.SubmittedFlaPropertyCompartments.Clear();

        foreach (var fellingDetail in confirmedFrDetailModelList.ConfirmedFellingAndRestockingDetailModels)
        {
            confirmedFrDetailModelList.SubmittedFlaPropertyCompartments.Add(
                _fixture.Build<SubmittedFlaPropertyCompartment>()
                    .With(x => x.CompartmentId, fellingDetail.CompartmentId)
                    .With(x => x.Id, fellingDetail.SubmittedFlaPropertyCompartmentId)
                    .Create());


            foreach (var restockingDetail in fellingDetail.ProposedFellingDetailModels.SelectMany(y =>
                         y.ProposedRestockingDetails))
            {
                confirmedFrDetailModelList.SubmittedFlaPropertyCompartments.Add(
                    _fixture.Build<SubmittedFlaPropertyCompartment>()
                        .With(x => x.CompartmentId, restockingDetail.CompartmentId)
                        .Create());
            }
        }

        _updateConfirmedService.Setup(r =>
                r.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmedFrDetailModelList);

        _getWoodlandOfficerReviewService.Setup(r =>
                r.GetCurrentFellingAndRestockingAmendmentReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Maybe<FellingAndRestockingAmendmentReviewModel>>("fail"));

        fla.CreatedById = userAccount.Id;

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

        submittedFlaProperty.SubmittedFlaPropertyCompartments!.Clear();

        var result = await sut.GetReviewAmendmentsViewModelAsync(fla.Id, _externalApplicant, CancellationToken.None);

        Assert.True(result.IsFailure);

        _updateConfirmedService.Verify(v => v.RetrieveConfirmedFellingAndRestockingDetailModelAsync(fla.Id, CancellationToken.None), Times.Once);
        _getWoodlandOfficerReviewService.Verify(v => v.GetCurrentFellingAndRestockingAmendmentReviewAsync(fla.Id, CancellationToken.None), Times.Once);
    }



    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenCurrentReviewNotFound(
        FellingLicenceApplication fla,
        CombinedConfirmedFellingAndRestockingDetailRecord confirmedFrDetailModelList,
        UserAccount userAccount,
        FellingAndRestockingAmendmentReviewModel model,
        SubmittedFlaPropertyDetail submittedFlaProperty)
    {
        var sut = CreateSut();

        confirmedFrDetailModelList.SubmittedFlaPropertyCompartments.Clear();

        foreach (var fellingDetail in confirmedFrDetailModelList.ConfirmedFellingAndRestockingDetailModels)
        {
            confirmedFrDetailModelList.SubmittedFlaPropertyCompartments.Add(
                _fixture.Build<SubmittedFlaPropertyCompartment>()
                    .With(x => x.CompartmentId, fellingDetail.CompartmentId)
                    .With(x => x.Id, fellingDetail.SubmittedFlaPropertyCompartmentId)
                    .Create());


            foreach (var restockingDetail in fellingDetail.ProposedFellingDetailModels.SelectMany(y =>
                         y.ProposedRestockingDetails))
            {
                confirmedFrDetailModelList.SubmittedFlaPropertyCompartments.Add(
                    _fixture.Build<SubmittedFlaPropertyCompartment>()
                        .With(x => x.CompartmentId, restockingDetail.CompartmentId)
                        .Create());
            }
        }

        _updateConfirmedService.Setup(r =>
                r.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmedFrDetailModelList);

        _getWoodlandOfficerReviewService.Setup(r =>
                r.GetCurrentFellingAndRestockingAmendmentReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingAndRestockingAmendmentReviewModel>.None);

        fla.CreatedById = userAccount.Id;

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

        submittedFlaProperty.SubmittedFlaPropertyCompartments!.Clear();

        var result = await sut.GetReviewAmendmentsViewModelAsync(fla.Id, _externalApplicant, CancellationToken.None);

        Assert.True(result.IsFailure);

        _updateConfirmedService.Verify(v => v.RetrieveConfirmedFellingAndRestockingDetailModelAsync(fla.Id, CancellationToken.None), Times.Once);
        _getWoodlandOfficerReviewService.Verify(v => v.GetCurrentFellingAndRestockingAmendmentReviewAsync(fla.Id, CancellationToken.None), Times.Once);
    }


    private FellingLicenceApplication CreateApplication(Guid appId, Guid woUserId, Guid? aoUserId = null)
    {
        var assignees = new List<AssigneeHistory>
        {
            _fixture.Build< AssigneeHistory>()
                .With(a => a.AssignedUserId, woUserId)
                .With(a => a.FellingLicenceApplicationId, appId)
                .With(a => a.TimestampAssigned, DateTime.Now.AddDays(-5))
                .Without(a => a.TimestampUnassigned)
                .With(a => a.Role, AssignedUserRole.WoodlandOfficer)
                .Create()
        };
        if (aoUserId.HasValue)
        {
            assignees.Add(_fixture.Build<AssigneeHistory>()
                .With(a => a.AssignedUserId, aoUserId.Value)
                .With(a => a.FellingLicenceApplicationId, appId)
                .With(a => a.TimestampAssigned, DateTime.Now.AddDays(-5))
                .Without(a => a.TimestampUnassigned)
                .With(a => a.Role, AssignedUserRole.AdminOfficer)
                .Create());
        }

        var fla = _fixture
            .Build<FellingLicenceApplication>()
            .With(f => f.Id, appId)
            .With(f => f.AssigneeHistories, assignees)
            .With(x => x.AdministrativeRegion, "Region1")
            .Create();

        fla.SubmittedFlaPropertyDetail!.Name = "Property1";

        return fla;
    }

    [Fact]
    public async Task CompleteAmendmentReviewAsync_ApplicationRetrievalFails_NotificationNotSent()
    {
        var record = new FellingAndRestockingAmendmentReviewUpdateRecord
        {
            FellingLicenceApplicationId = Guid.NewGuid(),
            ApplicantAgreed = true,
            ApplicantDisagreementReason = null
        };
        var userId = Guid.NewGuid();
        var sut = CreateSut();

        _updateWoodlandOfficerReviewService
            .Setup(s => s.UpdateFellingAndRestockingAmendmentReviewAsync(record, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getFellingLicenceApplicationService
            .Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>("App not found"));

        var result = await sut.CompleteAmendmentReviewAsync(record, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _sendNotifications.VerifyNoOtherCalls();

        _updateWoodlandOfficerReviewService.Verify(v => v.UpdateFellingAndRestockingAmendmentReviewAsync(record, userId, CancellationToken.None), Times.Once);
        _getFellingLicenceApplicationService.Verify(v => v.GetApplicationByIdAsync(record.FellingLicenceApplicationId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CompleteAmendmentReviewAsync_NoWoodlandOfficerAssigned_NotificationNotSent()
    {
        var record = new FellingAndRestockingAmendmentReviewUpdateRecord
        {
            FellingLicenceApplicationId = Guid.NewGuid(),
            ApplicantAgreed = true,
            ApplicantDisagreementReason = null
        };
        var userId = Guid.NewGuid();
        var app = new FellingLicenceApplication
        {
            Id = record.FellingLicenceApplicationId,
            ApplicationReference = "REF123",
            AssigneeHistories = new List<AssigneeHistory>(), // No woodland officer
            AdministrativeRegion = "Region1",
            SubmittedFlaPropertyDetail = new SubmittedFlaPropertyDetail { Name = "Property1" }
        };
        var sut = CreateSut();

        _updateWoodlandOfficerReviewService
            .Setup(s => s.UpdateFellingAndRestockingAmendmentReviewAsync(record, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getFellingLicenceApplicationService
            .Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(app));

        var result = await sut.CompleteAmendmentReviewAsync(record, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _sendNotifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteAmendmentReviewAsync_UserAccountRetrievalFails_NotificationNotSent()
    {
        var record = new FellingAndRestockingAmendmentReviewUpdateRecord
        {
            FellingLicenceApplicationId = Guid.NewGuid(),
            ApplicantAgreed = true,
            ApplicantDisagreementReason = null
        };
        var userId = Guid.NewGuid();
        var woUserId = Guid.NewGuid();
        var app = CreateApplication(record.FellingLicenceApplicationId, woUserId);

        var sut = CreateSut();

        _updateWoodlandOfficerReviewService
            .Setup(s => s.UpdateFellingAndRestockingAmendmentReviewAsync(record, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getFellingLicenceApplicationService
            .Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(app));

        _userAccountService
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<UserAccountModel>>("User retrieval failed"));

        var result = await sut.CompleteAmendmentReviewAsync(record, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _sendNotifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteAmendmentReviewAsync_NotificationSendFails_StillReturnsSuccess()
    {
        var record = new FellingAndRestockingAmendmentReviewUpdateRecord
        {
            FellingLicenceApplicationId = Guid.NewGuid(),
            ApplicantAgreed = true,
            ApplicantDisagreementReason = null
        };
        var userId = Guid.NewGuid();
        var woUserId = Guid.NewGuid();
        var app = CreateApplication(record.FellingLicenceApplicationId, woUserId);
        var sut = CreateSut();
        var woModel = new UserAccountModel
            {
                UserAccountId = woUserId,
                FirstName = "WO",
                LastName = "Name",
                Email = "wo@email.com",
            };

        _updateWoodlandOfficerReviewService
            .Setup(s => s.UpdateFellingAndRestockingAmendmentReviewAsync(record, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getFellingLicenceApplicationService
            .Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(app));

        _userAccountService
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<UserAccountModel>
            {
                woModel
            }));

        _getConfiguredFcAreas
            .Setup(s => s.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Footer");

        _sendNotifications
            .Setup(s => s.SendNotificationAsync(
                It.IsAny<ApplicantReviewedAmendmentsDataModel>(),
                NotificationType.ApplicantReviewedAmendments,
                It.IsAny<NotificationRecipient>(),
                It.IsAny<NotificationRecipient[]>(),
                null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Notification failed"));


        var result = await sut.CompleteAmendmentReviewAsync(record, userId, CancellationToken.None);
        Assert.True(result.IsSuccess);

        _sendNotifications.Verify(s => s.SendNotificationAsync(
            It.Is<ApplicantReviewedAmendmentsDataModel>(x =>
                x.ApplicationReference == app.ApplicationReference &&
                x.AdminHubFooter == "Footer" &&
                x.Name == woModel.FullName &&
                x.PropertyName == app.SubmittedFlaPropertyDetail!.Name &&
                x.ViewApplicationURL == $"{_defaultInternalUserSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationSummary/{record.FellingLicenceApplicationId}"),
            NotificationType.ApplicantReviewedAmendments,
            It.Is<NotificationRecipient>(x =>
                x.Name == woModel.FullName &&
                x.Address == woModel.Email),
            It.IsAny<NotificationRecipient[]>(),
            null, null, It.IsAny<CancellationToken>()), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(v => v.UpdateFellingAndRestockingAmendmentReviewAsync(record, userId, CancellationToken.None), Times.Once);
        _getFellingLicenceApplicationService.Verify(v => v.GetApplicationByIdAsync(record.FellingLicenceApplicationId, CancellationToken.None), Times.Once);
        _userAccountService.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(x => x.Contains(woUserId)), CancellationToken.None), Times.Once);
        _getConfiguredFcAreas.Verify(v => v.TryGetAdminHubAddress(app.AdministrativeRegion!, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CompleteAmendmentReviewAsync_NotificationSendSucceeds_ReturnsSuccess()
    {
        var record = new FellingAndRestockingAmendmentReviewUpdateRecord
        {
            FellingLicenceApplicationId = Guid.NewGuid(),
            ApplicantAgreed = true,
            ApplicantDisagreementReason = null
        };
        var userId = Guid.NewGuid();
        var woUserId = Guid.NewGuid();
        var app = CreateApplication(record.FellingLicenceApplicationId, woUserId);
        var woModel = new UserAccountModel
        {
            UserAccountId = woUserId,
            FirstName = "WO", 
            LastName = "Name",
            Email = "wo@email.com"
        };
        var sut = CreateSut();

        _updateWoodlandOfficerReviewService
            .Setup(s => s.UpdateFellingAndRestockingAmendmentReviewAsync(record, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getFellingLicenceApplicationService
            .Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(app));

        _userAccountService
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<UserAccountModel>
            {
                woModel
            }));

        _getConfiguredFcAreas
            .Setup(s => s.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Footer");

        _sendNotifications
            .Setup(s => s.SendNotificationAsync(
                It.IsAny<ApplicantReviewedAmendmentsDataModel>(),
                NotificationType.ApplicantReviewedAmendments,
                It.IsAny<NotificationRecipient>(),
                It.IsAny<NotificationRecipient[]>(),
                null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.CompleteAmendmentReviewAsync(record, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _sendNotifications.Verify(s => s.SendNotificationAsync(
            It.Is<ApplicantReviewedAmendmentsDataModel>(x => 
                x.ApplicationReference == app.ApplicationReference &&
                x.AdminHubFooter == "Footer" &&
                x.Name == woModel.FullName &&
                x.PropertyName == app.SubmittedFlaPropertyDetail!.Name &&
                x.ViewApplicationURL == $"{_defaultInternalUserSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationSummary/{record.FellingLicenceApplicationId}"),
            NotificationType.ApplicantReviewedAmendments,
            It.Is<NotificationRecipient>(x => 
                x.Name == woModel.FullName && 
                x.Address == woModel.Email),
            It.Is<NotificationRecipient[]>(x => x.IsNullOrEmpty()),
            null, null, It.IsAny<CancellationToken>()), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(v => v.UpdateFellingAndRestockingAmendmentReviewAsync(record, userId, CancellationToken.None), Times.Once);
        _getFellingLicenceApplicationService.Verify(v => v.GetApplicationByIdAsync(record.FellingLicenceApplicationId, CancellationToken.None), Times.Once);
        _userAccountService.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(x => x.Count == 1 && x.Contains(woUserId)), CancellationToken.None), Times.Once);
        _getConfiguredFcAreas.Verify(v => v.TryGetAdminHubAddress(app.AdministrativeRegion!, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CompleteAmendmentReviewAsync_NotificationSendSucceeds_ReturnsSuccess_AndCCsAdminOfficer()
    {
        var record = new FellingAndRestockingAmendmentReviewUpdateRecord
        {
            FellingLicenceApplicationId = Guid.NewGuid(),
            ApplicantAgreed = true,
            ApplicantDisagreementReason = null
        };
        var userId = Guid.NewGuid();
        var woUserId = Guid.NewGuid();
        var adminOfficerId = Guid.NewGuid();

        var app = CreateApplication(record.FellingLicenceApplicationId, woUserId, adminOfficerId);
        var woModel = new UserAccountModel
        {
            UserAccountId = woUserId,
            FirstName = "WO",
            LastName = "Name",
            Email = "wo@email.com"
        };
        var aoModel = new UserAccountModel
        {
            UserAccountId = adminOfficerId,
            FirstName = "AO",
            LastName = "Name",
            Email = "ao@email.com"
        };
        var sut = CreateSut();

        _updateWoodlandOfficerReviewService
            .Setup(s => s.UpdateFellingAndRestockingAmendmentReviewAsync(record, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getFellingLicenceApplicationService
            .Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(app));

        _userAccountService
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<UserAccountModel>
            {
                woModel,
                aoModel
            }));

        _getConfiguredFcAreas
            .Setup(s => s.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Footer");

        _sendNotifications
            .Setup(s => s.SendNotificationAsync(
                It.IsAny<ApplicantReviewedAmendmentsDataModel>(),
                NotificationType.ApplicantReviewedAmendments,
                It.IsAny<NotificationRecipient>(),
                It.IsAny<NotificationRecipient[]>(),
                null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.CompleteAmendmentReviewAsync(record, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _sendNotifications.Verify(s => s.SendNotificationAsync(
            It.Is<ApplicantReviewedAmendmentsDataModel>(x =>
                x.ApplicationReference == app.ApplicationReference &&
                x.AdminHubFooter == "Footer" &&
                x.Name == woModel.FullName &&
                x.PropertyName == app.SubmittedFlaPropertyDetail!.Name &&
                x.ViewApplicationURL == $"{_defaultInternalUserSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationSummary/{record.FellingLicenceApplicationId}"),
            NotificationType.ApplicantReviewedAmendments,
            It.Is<NotificationRecipient>(x =>
                x.Name == woModel.FullName &&
                x.Address == woModel.Email),
            It.Is<NotificationRecipient[]>(x => 
                x.Length == 1 && 
                x.Any(y => 
                    y.Name == aoModel.FullName && 
                    y.Address == aoModel.Email)),
            null, null, It.IsAny<CancellationToken>()), Times.Once);

        _updateWoodlandOfficerReviewService.Verify(v => v.UpdateFellingAndRestockingAmendmentReviewAsync(record, userId, CancellationToken.None), Times.Once);
        _getFellingLicenceApplicationService.Verify(v => v.GetApplicationByIdAsync(record.FellingLicenceApplicationId, CancellationToken.None), Times.Once);
        _userAccountService.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(x => x.Count == 2 && x.Contains(woUserId) && x.Contains(adminOfficerId)), CancellationToken.None), Times.Once);
        _getConfiguredFcAreas.Verify(v => v.TryGetAdminHubAddress(app.AdministrativeRegion!, CancellationToken.None), Times.Once);
    }

    private ReviewFellingAndRestockingAmendmentsUseCase CreateSut()
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            agencyId: _fixture.Create<Guid>(),
            woodlandOwnerName: _fixture.Create<string>());
        _externalApplicant = new ExternalApplicant(user);

        _updateConfirmedService.Reset();
        _getWoodlandOfficerReviewService.Reset();
        _updateWoodlandOfficerReviewService.Reset();
        _auditService.Reset();
        _getFellingLicenceApplicationService.Reset();
        _userAccountService.Reset();
        _sendNotifications.Reset();
        _getConfiguredFcAreas.Reset();
        _internalUserSiteOptions.Reset();

        _internalUserSiteOptions.Setup(o => o.Value).Returns(_defaultInternalUserSiteOptions);

        return new ReviewFellingAndRestockingAmendmentsUseCase(
            _updateConfirmedService.Object,
            _getWoodlandOfficerReviewService.Object,
            _updateWoodlandOfficerReviewService.Object,
            _getFellingLicenceApplicationService.Object,
            _userAccountService.Object,
            _sendNotifications.Object,
            _getConfiguredFcAreas.Object,
            _internalUserSiteOptions.Object,
            _auditService.Object,
            _requestContext,
            new NullLogger<ReviewFellingAndRestockingAmendmentsUseCase>()
        );
    }
}