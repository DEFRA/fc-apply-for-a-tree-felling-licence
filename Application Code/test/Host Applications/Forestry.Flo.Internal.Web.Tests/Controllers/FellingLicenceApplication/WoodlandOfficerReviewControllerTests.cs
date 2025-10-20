using AutoFixture;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.ValueTasks;
using FluentValidation;
using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication.EnvironmentalImpactAssessment;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.Notifications.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using UserAccountModel = Forestry.Flo.Services.InternalUsers.Models.UserAccountModel;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.FellingLicenceApplication;

public class WoodlandOfficerReviewControllerTests
{
    private readonly Mock<IValidator<CompartmentConfirmedFellingRestockingDetailsModel>> _validatorMock;
    private readonly Mock<IEnvironmentalImpactAssessmentAdminOfficerUseCase> _eiaUseCaseMock;
    private readonly Mock<IWoodlandOfficerReviewUseCase> _woReviewUseCaseMock;
    private readonly WoodlandOfficerReviewController _controller;
    private readonly Fixture _fixture = new();
    private readonly Guid _applicationId = Guid.NewGuid();

    public WoodlandOfficerReviewControllerTests()
    {
        _validatorMock = new Mock<IValidator<CompartmentConfirmedFellingRestockingDetailsModel>>();
        _eiaUseCaseMock = new Mock<IEnvironmentalImpactAssessmentAdminOfficerUseCase>();
        _woReviewUseCaseMock = new Mock<IWoodlandOfficerReviewUseCase>();
        _controller = new WoodlandOfficerReviewController(
            _validatorMock.Object,
            _eiaUseCaseMock.Object,
            _woReviewUseCaseMock.Object
        );
        _controller.PrepareControllerForTest(Guid.NewGuid());
    }

    [Fact]
    public async Task Index_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var model = Result.Success(_fixture.Create<WoodlandOfficerReviewModel>());
        useCase.Setup(x => x.WoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.Index(_applicationId, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<WoodlandOfficerReviewModel>(viewResult.Model);
    }

    [Fact]
    public async Task Index_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        useCase.Setup(x => x.WoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WoodlandOfficerReviewModel>("fail"));

        var result = await _controller.Index(_applicationId, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task ConfirmWoodlandOfficerReview_ReturnsError_WhenAssignedFieldManagerMissing()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var model = _fixture.Build<WoodlandOfficerReviewModel>()
            .With(x => x.AssignedFieldManager, "")
            .Create();

        var result = await _controller.ConfirmWoodlandOfficerReview(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task ConfirmWoodlandOfficerReview_ReturnsError_WhenRecommendationForDecisionPublicRegisterMissing()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var model = _fixture.Build<WoodlandOfficerReviewModel>()
            .With(x => x.AssignedFieldManager, "Manager")
            .With(x => x.RecommendationForDecisionPublicRegister, (bool?)null)
            .Create();

        var result = await _controller.ConfirmWoodlandOfficerReview(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task ConfirmWoodlandOfficerReview_ReturnsError_WhenRecommendationReasonMissing()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var model = _fixture.Build<WoodlandOfficerReviewModel>()
            .With(x => x.AssignedFieldManager, "Manager")
            .With(x => x.RecommendationForDecisionPublicRegister, true)
            .With(x => x.RecommendationForDecisionPublicRegisterReason, "")
            .Create();

        var result = await _controller.ConfirmWoodlandOfficerReview(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task ConfirmWoodlandOfficerReview_ReturnsError_WhenRecommendedLicenceDurationMissing()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var model = _fixture.Build<WoodlandOfficerReviewModel>()
            .With(x => x.AssignedFieldManager, "Manager")
            .With(x => x.RecommendationForDecisionPublicRegister, true)
            .With(x => x.RecommendationForDecisionPublicRegisterReason, "Reason")
            .Without(x => x.RecommendedLicenceDuration)
            .Create();

        var result = await _controller.ConfirmWoodlandOfficerReview(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task ConfirmWoodlandOfficerReview_ReturnsError_WhenReviewAsyncFails()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var model = _fixture.Build<WoodlandOfficerReviewModel>()
            .With(x => x.AssignedFieldManager, "Manager")
            .With(x => x.RecommendationForDecisionPublicRegister, true)
            .With(x => x.RecommendationForDecisionPublicRegisterReason, "Reason")
            .With(x => x.RecommendedLicenceDuration, RecommendedLicenceDuration.NineYear)
            .Create();

        useCase.Setup(x => x.WoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WoodlandOfficerReviewModel>("fail"));

        var result = await _controller.ConfirmWoodlandOfficerReview(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task ConfirmWoodlandOfficerReview_ReturnsError_WhenTaskListNotComplete()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var reviewModel = _fixture.Build<WoodlandOfficerReviewModel>()
            .With(x => x.WoodlandOfficerReviewTaskListStates,
                new WoodlandOfficerReviewTaskListStates(
                    InternalReviewStepStatus.NotStarted,
                    InternalReviewStepStatus.NotStarted,
                    InternalReviewStepStatus.NotStarted,
                    InternalReviewStepStatus.NotStarted,
                    InternalReviewStepStatus.NotStarted,
                    InternalReviewStepStatus.NotStarted,
                    InternalReviewStepStatus.NotStarted,
                    InternalReviewStepStatus.NotStarted,
                    InternalReviewStepStatus.NotStarted,
                    InternalReviewStepStatus.NotStarted,
                    InternalReviewStepStatus.NotStarted))
            .Create();
        var model = _fixture.Build<WoodlandOfficerReviewModel>()
            .With(x => x.AssignedFieldManager, "Manager")
            .With(x => x.RecommendationForDecisionPublicRegister, true)
            .With(x => x.RecommendationForDecisionPublicRegisterReason, "Reason")
            .With(x => x.RecommendedLicenceDuration, RecommendedLicenceDuration.NineYear)
            .Create();

        useCase.Setup(x => x.WoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(reviewModel));

        var result = await _controller.ConfirmWoodlandOfficerReview(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task ConfirmWoodlandOfficerReview_ReturnsError_WhenCompleteReviewFails()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var reviewModel = _fixture.Build<WoodlandOfficerReviewModel>()
            .With(x => x.WoodlandOfficerReviewTaskListStates,
                new WoodlandOfficerReviewTaskListStates(
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.Completed))
            .Create();
        var model = _fixture.Build<WoodlandOfficerReviewModel>()
            .With(x => x.AssignedFieldManager, "Manager")
            .With(x => x.RecommendationForDecisionPublicRegister, true)
            .With(x => x.RecommendationForDecisionPublicRegisterReason, "Reason")
            .With(x => x.RecommendedLicenceDuration, RecommendedLicenceDuration.NineYear)
            .Create();

        useCase.Setup(x => x.WoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(reviewModel));
        useCase.Setup(x => x.CompleteWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<RecommendedLicenceDuration>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.ConfirmWoodlandOfficerReview(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task ConfirmWoodlandOfficerReview_RedirectsToSummary_WhenSuccess()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var reviewModel = _fixture.Build<WoodlandOfficerReviewModel>()
            .With(x => x.WoodlandOfficerReviewTaskListStates, 
                new WoodlandOfficerReviewTaskListStates(
                    InternalReviewStepStatus.Completed, 
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.Completed, 
                    InternalReviewStepStatus.Completed, 
                    InternalReviewStepStatus.Completed, 
                    InternalReviewStepStatus.Completed, 
                    InternalReviewStepStatus.Completed, 
                    InternalReviewStepStatus.Completed, 
                    InternalReviewStepStatus.Completed, 
                    InternalReviewStepStatus.Completed, 
                    InternalReviewStepStatus.Completed))
            .Create();
        var model = _fixture.Build<WoodlandOfficerReviewModel>()
            .With(x => x.AssignedFieldManager, "Manager")
            .With(x => x.RecommendationForDecisionPublicRegister, true)
            .With(x => x.RecommendationForDecisionPublicRegisterReason, "Reason")
            .With(x => x.RecommendedLicenceDuration, RecommendedLicenceDuration.NineYear)
            .Create();

        useCase.Setup(x => x.WoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(reviewModel));
        useCase.Setup(x => x.CompleteWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<RecommendedLicenceDuration>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.ConfirmWoodlandOfficerReview(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ApplicationSummary", redirect.ActionName);
    }

    [Fact]
    public async Task AssignFieldManager_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        useCase.Setup(x => x.WoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WoodlandOfficerReviewModel>("fail"));

        var result = await _controller.AssignFieldManager(_applicationId, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task AssignFieldManager_RedirectsToConfirmReassign_WhenFieldManagerAssigned()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var model = _fixture.Build<WoodlandOfficerReviewModel>()
            .With(x => x.AssignedFieldManager, "Manager")
            .Create();
        useCase.Setup(x => x.WoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.AssignFieldManager(_applicationId, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmReassignApplication", redirect.ActionName);
        Assert.Equal("AssignFellingLicenceApplication", redirect.ControllerName);
    }

    [Fact]
    public async Task AssignFieldManager_RedirectsToSelectUser_WhenFieldManagerNotAssigned()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var model = _fixture.Build<WoodlandOfficerReviewModel>()
            .With(x => x.AssignedFieldManager, "")
            .Create();
        useCase.Setup(x => x.WoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.AssignFieldManager(_applicationId, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("SelectUser", redirect.ActionName);
        Assert.Equal("AssignFellingLicenceApplication", redirect.ControllerName);
    }

    [Fact]
    public async Task PublicRegister_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<IPublicRegisterUseCase>();
        var model = Result.Success(new PublicRegisterViewModel { ApplicationId = _applicationId });
        useCase.Setup(x => x.GetPublicRegisterDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.PublicRegister(_applicationId, useCase.Object, null, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<PublicRegisterViewModel>(viewResult.Model);
    }


    [Fact]
    public async Task PublicRegister_ReturnsView_WhenExemption()
    {
        var useCase = new Mock<IPublicRegisterUseCase>();
        var model =
            _fixture.Build<PublicRegisterViewModel>()
                .With(x => x.PublicRegister)
                .Create();
        useCase.Setup(x => x.GetPublicRegisterDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.PublicRegister(_applicationId, useCase.Object, true, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        var viewModel = Assert.IsType<PublicRegisterViewModel>(viewResult.Model);

        Assert.True(viewModel.PublicRegister.WoodlandOfficerSetAsExemptFromConsultationPublicRegister);
    }

    [Fact]
    public async Task PublicRegister_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<IPublicRegisterUseCase>();
        useCase.Setup(x => x.GetPublicRegisterDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PublicRegisterViewModel>("fail"));

        var result = await _controller.PublicRegister(_applicationId, useCase.Object, null, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task SaveExemption_ReturnsError_WhenReasonMissing()
    {
        var useCase = new Mock<IPublicRegisterUseCase>();
        var model = new PublicRegisterViewModel
        {
            ApplicationId = _applicationId,
            PublicRegister = new()
            {
                WoodlandOfficerSetAsExemptFromConsultationPublicRegister = true,
                WoodlandOfficerConsultationPublicRegisterExemptionReason = ""
            }
        };

        var result = await _controller.SaveExemption(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("PublicRegister", redirect.ActionName);
    }

    [Fact]
    public async Task SaveExemption_RedirectsToPublicRegister_WhenFailure()
    {
        var useCase = new Mock<IPublicRegisterUseCase>();
        var model = new PublicRegisterViewModel
        {
            ApplicationId = _applicationId,
            PublicRegister = new()
            {
                WoodlandOfficerSetAsExemptFromConsultationPublicRegister = false
            }
        };
        useCase.Setup(x => x.StorePublicRegisterExemptionAsync(
            It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.SaveExemption(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("PublicRegister", redirect.ActionName);
    }

    [Fact]
    public async Task SaveExemption_RedirectsToPublicRegister_WhenSuccess()
    {
        var useCase = new Mock<IPublicRegisterUseCase>();
        var model = new PublicRegisterViewModel
        {
            ApplicationId = _applicationId,
            PublicRegister = new()
            {
                WoodlandOfficerSetAsExemptFromConsultationPublicRegister = false
            }
        };
        useCase.Setup(x => x.StorePublicRegisterExemptionAsync(
            It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.SaveExemption(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("PublicRegister", redirect.ActionName);
    }

    [Fact]
    public async Task PublishToConsultationPublicRegister_ReturnsError_WhenPeriodMissing()
    {
        var useCase = new Mock<IPublicRegisterUseCase>();
        var model = new PublicRegisterViewModel
        {
            ApplicationId = _applicationId,
            PublicRegister = new() { ConsultationPublicRegisterPeriodDays = null }
        };

        var result = await _controller.PublishToConsultationPublicRegister(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("PublicRegister", redirect.ActionName);
    }

    [Fact]
    public async Task PublishToConsultationPublicRegister_ReturnsError_WhenFailure()
    {
        var useCase = new Mock<IPublicRegisterUseCase>();
        var model = new PublicRegisterViewModel
        {
            ApplicationId = _applicationId,
            PublicRegister = new() { ConsultationPublicRegisterPeriodDays = 30 }
        };
        useCase.Setup(x => x.PublishToConsultationPublicRegisterAsync(
            It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.PublishToConsultationPublicRegister(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("PublicRegister", redirect.ActionName);
    }

    [Fact]
    public async Task PublishToConsultationPublicRegister_RedirectsToPublicRegister_WhenSuccess()
    {
        var useCase = new Mock<IPublicRegisterUseCase>();
        var model = new PublicRegisterViewModel
        {
            ApplicationId = _applicationId,
            PublicRegister = new() { ConsultationPublicRegisterPeriodDays = 30 }
        };
        useCase.Setup(x => x.PublishToConsultationPublicRegisterAsync(
            It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.PublishToConsultationPublicRegister(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("PublicRegister", redirect.ActionName);
    }

    [Fact]
    public async Task ReviewComment_Get_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<IPublicRegisterUseCase>();
        var commentModel = Result.Success(new ReviewCommentModel());
        useCase.Setup(x => x.GetPublicRegisterCommentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(commentModel);

        var result = await _controller.ReviewComment(_applicationId, Guid.NewGuid(), useCase.Object, null, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ReviewCommentModel>(viewResult.Model);
    }

    [Fact]
    public async Task ReviewComment_Get_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<IPublicRegisterUseCase>();
        useCase.Setup(x => x.GetPublicRegisterCommentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ReviewCommentModel>("fail"));

        var result = await _controller.ReviewComment(_applicationId, Guid.NewGuid(), useCase.Object, null, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task ReviewComment_Post_UpdatesComment_WhenSuccess()
    {
        var useCase = new Mock<IPublicRegisterUseCase>();
        var model = new ReviewCommentModel { Comment = _fixture.Create<NotificationHistoryModel>() };
        useCase.Setup(x => x.UpdatePublicRegisterDetailsAsync(It.IsAny<Guid>(), It.IsAny<NotificationHistoryModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.ReviewComment(_applicationId, Guid.NewGuid(), model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("PublicRegister", redirect.ActionName);
    }

    [Fact]
    public async Task ReviewComment_Post_ReturnsError_WhenUpdateFails()
    {
        var useCase = new Mock<IPublicRegisterUseCase>();
        var model = new ReviewCommentModel { Comment = _fixture.Create<NotificationHistoryModel>() };
        useCase.Setup(x => x.UpdatePublicRegisterDetailsAsync(It.IsAny<Guid>(), It.IsAny<NotificationHistoryModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.ReviewComment(_applicationId, Guid.NewGuid(), model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ReviewComment", redirect.ActionName);
    }

    [Fact]
    public async Task RemoveFromPublicRegister_ReturnsError_WhenFailure()
    {
        var useCase = new Mock<IPublicRegisterUseCase>();
        var model = new PublicRegisterViewModel
        {
            ApplicationId = _applicationId,
            RemoveFromPublicRegister = new RemoveFromPublicRegisterModel
            {
                EsriId = 123,
                ApplicationReference = "ref"
            }
        };
        useCase.Setup(x => x.RemoveFromPublicRegisterAsync(
            It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.RemoveFromPublicRegister(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("PublicRegister", redirect.ActionName);
    }

    [Fact]
    public async Task RemoveFromPublicRegister_RedirectsToPublicRegister_WhenSuccess()
    {
        var useCase = new Mock<IPublicRegisterUseCase>();
        var model = new PublicRegisterViewModel
        {
            ApplicationId = _applicationId,
            RemoveFromPublicRegister = new RemoveFromPublicRegisterModel
            {
                EsriId = 123,
                ApplicationReference = "ref"
            }
        };
        useCase.Setup(x => x.RemoveFromPublicRegisterAsync(
            It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.RemoveFromPublicRegister(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("PublicRegister", redirect.ActionName);
    }
    [Fact]
    public async Task SiteVisit_Get_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = Result.Success(_fixture.Build<SiteVisitViewModel>().Without(x => x.SiteVisitArrangementsMade).Create());
        useCase.Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.SiteVisit(_applicationId, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<SiteVisitViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task SiteVisit_Get_RedirectsToReviewSiteVisitEvidence_WhenSiteVisitCompleteAndArrangementsMade()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = Result.Success(_fixture
            .Build<SiteVisitViewModel>()
            .With(x => x.SiteVisitArrangementsMade, true)
            .With(x => x.SiteVisitComplete, true)

            .Create());

        useCase.Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.SiteVisit(_applicationId, useCase.Object, CancellationToken.None);

        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ReviewSiteVisitEvidence", redirectToActionResult.ActionName);
        Assert.Equal(_applicationId, redirectToActionResult.RouteValues["id"]);
    }


    [Fact]
    public async Task SiteVisit_Get_RedirectsToAddSiteVisitEvidence_WhenSiteVisitIncompleteAndArrangementsMade()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = Result.Success(_fixture
            .Build<SiteVisitViewModel>()
            .With(x => x.SiteVisitArrangementsMade, true)
            .With(x => x.SiteVisitComplete, false)

            .Create());

        useCase.Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.SiteVisit(_applicationId, useCase.Object, CancellationToken.None);

        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AddSiteVisitEvidence", redirectToActionResult.ActionName);
        Assert.Equal(_applicationId, redirectToActionResult.RouteValues["id"]);
    }

    [Fact]
    public async Task SiteVisit_Get_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        useCase.Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SiteVisitViewModel>("fail"));

        var result = await _controller.SiteVisit(_applicationId, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task SiteVisitSummary_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = Result.Success(_fixture.Create<SiteVisitSummaryModel>());
        useCase.Setup(x => x.GetSiteVisitSummaryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.SiteVisitSummary(_applicationId, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<SiteVisitSummaryModel>(viewResult.Model);
    }

    [Fact]
    public async Task SiteVisitSummary_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        useCase.Setup(x => x.GetSiteVisitSummaryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SiteVisitSummaryModel>("fail"));

        var result = await _controller.SiteVisitSummary(_applicationId, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SiteVisit_Post_SiteVisitNotNeeded_ReturnsError_WhenReasonMissing(string? value)
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var emptyReason = _fixture.Build<FormLevelCaseNote>().With(x => x.CaseNote, value).Create();
        var model = _fixture.Build<SiteVisitViewModel>()
            .With(x => x.SiteVisitNeeded, false)
            .With(x => x.SiteVisitNotNeededReason, emptyReason)
            .Create();
        useCase.Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.SiteVisit(model, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<SiteVisitViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task SiteVisit_Post_SiteVisitNotNeeded_RedirectsToError_WhenGetFails()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var emptyReason = _fixture.Build<FormLevelCaseNote>().With(x => x.CaseNote, string.Empty).Create();
        var model = _fixture.Build<SiteVisitViewModel>()
            .With(x => x.SiteVisitNeeded, false)
            .With(x => x.SiteVisitNotNeededReason, emptyReason)
            .Create();
        useCase.Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SiteVisitViewModel>("fail"));

        var result = await _controller.SiteVisit(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task SiteVisit_Post_SiteVisitNotNeeded_RedirectsToIndex_WhenSuccess()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = _fixture.Build<SiteVisitViewModel>()
            .With(x => x.SiteVisitNeeded, false)
            .With(x => x.SiteVisitNotNeededReason, _fixture.Create<FormLevelCaseNote>())
            .Create();
        useCase.Setup(x => x.SiteVisitIsNotNeededAsync(
            It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<FormLevelCaseNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.SiteVisit(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task SiteVisit_Post_SiteVisitNotNeeded_RedirectsToSiteVisit_WhenFailure()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = _fixture.Build<SiteVisitViewModel>()
            .With(x => x.SiteVisitNeeded, false)
            .With(x => x.SiteVisitNotNeededReason, _fixture.Create<FormLevelCaseNote>())
            .Create();
        useCase.Setup(x => x.SiteVisitIsNotNeededAsync(
            It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<FormLevelCaseNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.SiteVisit(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("SiteVisit", redirect.ActionName);
    }

    [Fact]
    public async Task SiteVisit_Post_RedirectsToSiteVisit_WhenSiteVisitNeededEmpty()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = _fixture.Build<SiteVisitViewModel>()
            .Without(x => x.SiteVisitNeeded)
            .With(x => x.ApplicationId, _applicationId)
            .With(x => x.SiteVisitNotNeededReason, _fixture.Create<FormLevelCaseNote>())
            .Create();

        var result = await _controller.SiteVisit(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("SiteVisit", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task SiteVisit_Post_SiteVisitNeeded_ReturnsError_WhenArrangementsMissing()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = _fixture.Build<SiteVisitViewModel>()
            .With(x => x.SiteVisitNeeded, true)
            .With(x => x.SiteVisitArrangementsMade, (bool?)null)
            .Create();
        useCase.Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.SiteVisit(model, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<SiteVisitViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task SiteVisit_Post_SiteVisitNeeded_RedirectsToError_WhenGetFails()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = _fixture.Build<SiteVisitViewModel>()
            .With(x => x.SiteVisitNeeded, true)
            .With(x => x.SiteVisitArrangementsMade, (bool?)null)
            .Create();
        useCase.Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SiteVisitViewModel>("fail"));

        var result = await _controller.SiteVisit(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task SiteVisit_Post_SiteVisitNeeded_RedirectsToAddSiteVisitEvidence_WhenSuccess()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = _fixture.Build<SiteVisitViewModel>()
            .With(x => x.SiteVisitNeeded, true)
            .With(x => x.SiteVisitArrangementsMade, true)
            .Create();
        useCase.Setup(x => x.SetSiteVisitArrangementsAsync(
            It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<bool?>(), It.IsAny<FormLevelCaseNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.SiteVisit(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AddSiteVisitEvidence", redirect.ActionName);
    }

    [Fact]
    public async Task SiteVisit_Post_SiteVisitNeeded_RedirectsToSiteVisit_WhenFailure()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = _fixture.Build<SiteVisitViewModel>()
            .With(x => x.SiteVisitNeeded, true)
            .With(x => x.SiteVisitArrangementsMade, true)
            .Create();
        useCase.Setup(x => x.SetSiteVisitArrangementsAsync(
            It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<bool?>(), It.IsAny<FormLevelCaseNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.SiteVisit(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("SiteVisit", redirect.ActionName);
    }

    [Fact]
    public async Task AddSiteVisitEvidence_Get_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = Result.Success(_fixture.Create<AddSiteVisitEvidenceModel>());
        useCase.Setup(x => x.GetSiteVisitEvidenceModelAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.AddSiteVisitEvidence(_applicationId, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AddSiteVisitEvidenceModel>(viewResult.Model);
    }

    [Fact]
    public async Task AddSiteVisitEvidence_Get_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        useCase.Setup(x => x.GetSiteVisitEvidenceModelAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AddSiteVisitEvidenceModel>("fail"));

        var result = await _controller.AddSiteVisitEvidence(_applicationId, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task AddSiteVisitEvidence_Get_RedirectsToReview_WhenComplete()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = _fixture.Build<AddSiteVisitEvidenceModel>()
            .With(x => x.SiteVisitComplete, true)
            .Create();
        useCase.Setup(x => x.GetSiteVisitEvidenceModelAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.AddSiteVisitEvidence(_applicationId, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ReviewSiteVisitEvidence", redirect.ActionName);
    }

    [Fact]
    public async Task AddSiteVisitEvidence_Post_ReturnsError_WhenFailure()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = _fixture.Build<AddSiteVisitEvidenceModel>()
            .With(x => x.SiteVisitComplete, false)
            .Create();
        useCase.Setup(x => x.AddSiteVisitEvidenceAsync(
            It.IsAny<AddSiteVisitEvidenceModel>(), It.IsAny<FormFileCollection>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.AddSiteVisitEvidence(model, new FormFileCollection(), useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AddSiteVisitEvidence", redirect.ActionName);
    }

    [Fact]
    public async Task AddSiteVisitEvidence_Post_RedirectsToReview_WhenComplete()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = _fixture.Build<AddSiteVisitEvidenceModel>()
            .With(x => x.SiteVisitComplete, true)
            .Create();
        useCase.Setup(x => x.AddSiteVisitEvidenceAsync(
            It.IsAny<AddSiteVisitEvidenceModel>(), It.IsAny<FormFileCollection>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.AddSiteVisitEvidence(model, new FormFileCollection(), useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ReviewSiteVisitEvidence", redirect.ActionName);
    }

    [Fact]
    public async Task AddSiteVisitEvidence_Post_RedirectsToAdd_WhenNotComplete()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = _fixture.Build<AddSiteVisitEvidenceModel>()
            .With(x => x.SiteVisitComplete, false)
            .Create();
        useCase.Setup(x => x.AddSiteVisitEvidenceAsync(
            It.IsAny<AddSiteVisitEvidenceModel>(), It.IsAny<FormFileCollection>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.AddSiteVisitEvidence(model, new FormFileCollection(), useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AddSiteVisitEvidence", redirect.ActionName);
    }

    [Fact]
    public async Task ReviewSiteVisitEvidence_Get_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        var model = Result.Success(_fixture.Create<AddSiteVisitEvidenceModel>());
        useCase.Setup(x => x.GetSiteVisitEvidenceModelAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.ReviewSiteVisitEvidence(_applicationId, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AddSiteVisitEvidenceModel>(viewResult.Model);
    }

    [Fact]
    public async Task ReviewSiteVisitEvidence_Get_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<ISiteVisitUseCase>();
        useCase.Setup(x => x.GetSiteVisitEvidenceModelAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AddSiteVisitEvidenceModel>("fail"));

        var result = await _controller.ReviewSiteVisitEvidence(_applicationId, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task Pw14Checks_Get_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<IPw14UseCase>();
        var model = Result.Success(_fixture.Create<Pw14ChecksViewModel>());
        useCase.Setup(x => x.GetPw14CheckDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.Pw14Checks(_applicationId, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<Pw14ChecksViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task Pw14Checks_Get_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<IPw14UseCase>();
        useCase.Setup(x => x.GetPw14CheckDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Pw14ChecksViewModel>("fail"));

        var result = await _controller.Pw14Checks(_applicationId, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task Pw14Checks_Post_ReturnsError_WhenSaveFails()
    {
        var useCase = new Mock<IPw14UseCase>();
        var amendCaseNotes = new Mock<IAmendCaseNotes>();
        var model = _fixture.Create<Pw14ChecksViewModel>();
        useCase.Setup(x => x.SavePw14ChecksAsync(It.IsAny<Pw14ChecksViewModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.Pw14Checks(model, useCase.Object, amendCaseNotes.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Pw14Checks", redirect.ActionName);
    }

    [Fact]
    public async Task Pw14Checks_Post_ReturnsError_WhenCaseNoteFails()
    {
        var useCase = new Mock<IPw14UseCase>();
        var amendCaseNotes = new Mock<IAmendCaseNotes>();
        var model = _fixture.Build<Pw14ChecksViewModel>()
            .With(x => x.FormLevelCaseNote, _fixture.Create<FormLevelCaseNote>())
            .Create();
        useCase.Setup(x => x.SavePw14ChecksAsync(It.IsAny<Pw14ChecksViewModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        amendCaseNotes.Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.Pw14Checks(model, useCase.Object, amendCaseNotes.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task Pw14Checks_Post_RedirectsToIndex_WhenSuccess()
    {
        var useCase = new Mock<IPw14UseCase>();
        var amendCaseNotes = new Mock<IAmendCaseNotes>();
        var model = _fixture.Build<Pw14ChecksViewModel>()
            .With(x => x.FormLevelCaseNote, _fixture.Create<FormLevelCaseNote>())
            .Create();
        useCase.Setup(x => x.SavePw14ChecksAsync(It.IsAny<Pw14ChecksViewModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.Pw14Checks(model, useCase.Object, amendCaseNotes.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task Conditions_Get_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<IConditionsUseCase>();
        var model = Result.Success(_fixture.Create<ConditionsViewModel>());
        useCase.Setup(x => x.GetConditionsAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.Conditions(_applicationId, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ConditionsViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task Conditions_Get_ReturnsError_WhenFailure()
    {
        var useCase = new Mock<IConditionsUseCase>();
        useCase.Setup(x => x.GetConditionsAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ConditionsViewModel>("fail"));

        var result = await _controller.Conditions(_applicationId, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task SaveConditionalStatus_ReturnsError_WhenStatusMissing()
    {
        var useCase = new Mock<IConditionsUseCase>();
        var model = _fixture.Build<ConditionsViewModel>()
            .With(x => x.ConditionsStatus, new ConditionsStatusModel { IsConditional = null })
            .Create();

        var result = await _controller.SaveConditionalStatus(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Conditions", redirect.ActionName);
    }

    [Fact]
    public async Task SaveConditionalStatus_RedirectsToConditions_WhenFailure()
    {
        var useCase = new Mock<IConditionsUseCase>();
        var model = _fixture.Build<ConditionsViewModel>()
            .With(x => x.ConditionsStatus, new ConditionsStatusModel { IsConditional = true })
            .Create();
        useCase.Setup(x => x.SaveConditionStatusAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<ConditionsStatusModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.SaveConditionalStatus(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Conditions", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task SaveConditionalStatus_RedirectsToConditions_WhenSuccess()
    {
        var useCase = new Mock<IConditionsUseCase>();
        var model = _fixture.Build<ConditionsViewModel>()
            .With(x => x.ConditionsStatus, new ConditionsStatusModel { IsConditional = true })
            .Create();
        useCase.Setup(x => x.SaveConditionStatusAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<ConditionsStatusModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.SaveConditionalStatus(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Conditions", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task GenerateConditions_RedirectsToConditions_WhenFailure()
    {
        var useCase = new Mock<IConditionsUseCase>();
        var model = _fixture.Build<ConditionsViewModel>()
            .With(x => x.ConditionsStatus, new ConditionsStatusModel { IsConditional = true })
            .Create();
        useCase.Setup(x => x.GenerateConditionsAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ConditionsResponse>("fail"));

        var result = await _controller.GenerateConditions(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Conditions", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task GenerateConditions_RedirectsToConditions_WhenSuccess()
    {
        var useCase = new Mock<IConditionsUseCase>();
        var model = _fixture.Build<ConditionsViewModel>()
            .With(x => x.ConditionsStatus, new ConditionsStatusModel { IsConditional = true })
            .Create();
        useCase.Setup(x => x.GenerateConditionsAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<ConditionsResponse>());

        var result = await _controller.GenerateConditions(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Conditions", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task SaveConditions_RedirectsToConditions_WhenFailure()
    {
        var useCase = new Mock<IConditionsUseCase>();
        var model = _fixture.Create<ConditionsViewModel>();
        useCase.Setup(x => x.SaveConditionsAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<List<CalculatedCondition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ConditionsResponse>("fail"));

        var result = await _controller.SaveConditions(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Conditions", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task SaveConditions_RedirectsToConditions_WhenSuccess()
    {
        var useCase = new Mock<IConditionsUseCase>();
        var model = _fixture.Create<ConditionsViewModel>();
        useCase.Setup(x => x.SaveConditionsAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<List<CalculatedCondition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.SaveConditions(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Conditions", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task SendConditionsNotification_RedirectsToConditions_WhenFailure()
    {
        var useCase = new Mock<IConditionsUseCase>();
        var model = _fixture.Create<ConditionsViewModel>();
        useCase.Setup(x => x.SendConditionsToApplicantAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.SendConditionsNotification(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Conditions", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
    }


    [Fact]
    public async Task SendConditionsNotification_RedirectsToConditions_WhenConfirmedFellingAndRestockingIncomplete()
    {
        var useCase = new Mock<IConditionsUseCase>();
        var model = _fixture.Build<ConditionsViewModel>().With(x => x.ConfirmedFellingAndRestockingComplete, false)
            .Create();

        var result = await _controller.SendConditionsNotification(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Conditions", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
        Assert.Equal(
            "You must complete the confirm felling and restocking details, then generate and complete the conditions, before sending them to the applicant",
            _controller.TempData[Infrastructure.ControllerExtensions.ErrorMessageKey]);
    }

    [Fact]
    public async Task SendConditionsNotification_RedirectsToConditions_WhenSuccess()
    {
        var useCase = new Mock<IConditionsUseCase>();
        var model = _fixture.Create<ConditionsViewModel>();
        useCase.Setup(x => x.SendConditionsToApplicantAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.SendConditionsNotification(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Conditions", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task LarchCheck_Get_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        var model = Result.Success(_fixture.Create<LarchCheckModel>());
        useCase.Setup(x => x.GetLarchCheckModelAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.LarchCheck(_applicationId, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<LarchCheckModel>(viewResult.Model);
    }

    [Fact]
    public async Task LarchCheck_Get_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        useCase.Setup(x => x.GetLarchCheckModelAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LarchCheckModel>("fail"));

        var result = await _controller.LarchCheck(_applicationId, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
        Assert.Equal("Could not retrieve larch check status", _controller.TempData[Infrastructure.ControllerExtensions.ErrorMessageKey]);
    }

    [Fact]
    public async Task LarchCheck_Post_ReturnsError_WhenSaveFails()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        var woReviewUseCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var amendCaseNotes = new Mock<IAmendCaseNotes>();
        var model = _fixture.Create<LarchCheckModel>();
        useCase.Setup(x => x.SaveLarchCheckAsync(It.IsAny<LarchCheckModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.LarchCheck(_applicationId, model, useCase.Object, woReviewUseCase.Object, amendCaseNotes.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("LarchCheck", redirect.ActionName);
    }

    [Fact]
    public async Task LarchCheck_Post_RedirectsToIndex_WhenSuccess()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        var woReviewUseCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var amendCaseNotes = new Mock<IAmendCaseNotes>();
        var model = _fixture.Create<LarchCheckModel>();
        useCase.Setup(x => x.SaveLarchCheckAsync(It.IsAny<LarchCheckModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.LarchCheck(_applicationId, model, useCase.Object, woReviewUseCase.Object, amendCaseNotes.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task LarchCheck_Post_ReturnsView_WhenModelStateIsInvalid_AndUseCaseSucceeds()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        var woReviewUseCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var amendCaseNotes = new Mock<IAmendCaseNotes>();
        var model = _fixture.Create<LarchCheckModel>();

        _controller.ModelState.AddModelError("Test", "Error");
        useCase.Setup(x => x.GetLarchCheckModelAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.LarchCheck(_applicationId, model, useCase.Object, woReviewUseCase.Object, amendCaseNotes.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<LarchCheckModel>(viewResult.Model);
    }

    [Fact]
    public async Task LarchCheck_Post_RedirectsToError_WhenModelStateIsInvalid_AndUseCaseFails()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        var woReviewUseCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var amendCaseNotes = new Mock<IAmendCaseNotes>();
        var model = _fixture.Create<LarchCheckModel>();

        _controller.ModelState.AddModelError("Test", "Error");
        useCase.Setup(x => x.GetLarchCheckModelAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LarchCheckModel>("fail"));

        var result = await _controller.LarchCheck(_applicationId, model, useCase.Object, woReviewUseCase.Object, amendCaseNotes.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task LarchCheck_Post_RedirectsToIndex_WhenCompleteLarchCheckFails()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        var woReviewUseCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var amendCaseNotes = new Mock<IAmendCaseNotes>();
        var model = _fixture.Create<LarchCheckModel>();

        useCase.Setup(x => x.SaveLarchCheckAsync(It.IsAny<LarchCheckModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        woReviewUseCase.Setup(x => x.CompleteLarchCheckAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await _controller.LarchCheck(_applicationId, model, useCase.Object, woReviewUseCase.Object, amendCaseNotes.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task LarchCheck_Post_RedirectsToLarchFlyover_WhenAddCaseNoteFails()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        var woReviewUseCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var amendCaseNotes = new Mock<IAmendCaseNotes>();
        var model = _fixture.Build<LarchCheckModel>()
            .With(x => x.CaseNote, "Some note")
            .With(x => x.VisibleToApplicant, true)
            .With(x => x.VisibleToConsultee, false)
            .Create();

        useCase.Setup(x => x.SaveLarchCheckAsync(It.IsAny<LarchCheckModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        woReviewUseCase.Setup(x => x.CompleteLarchCheckAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        amendCaseNotes.Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await _controller.LarchCheck(_applicationId, model, useCase.Object, woReviewUseCase.Object, amendCaseNotes.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("LarchFlyover", redirect.ActionName);
    }

    [Fact]
    public async Task LarchCheck_Post_RedirectsToIndex_WhenAddCaseNoteSucceeds()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        var woReviewUseCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var amendCaseNotes = new Mock<IAmendCaseNotes>();
        var model = _fixture.Build<LarchCheckModel>()
            .With(x => x.CaseNote, "Some note")
            .With(x => x.VisibleToApplicant, true)
            .With(x => x.VisibleToConsultee, false)
            .Create();

        useCase.Setup(x => x.SaveLarchCheckAsync(It.IsAny<LarchCheckModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        woReviewUseCase.Setup(x => x.CompleteLarchCheckAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        amendCaseNotes.Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.LarchCheck(_applicationId, model, useCase.Object, woReviewUseCase.Object, amendCaseNotes.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task LarchCheck_Post_RedirectsToIndex_WhenNoCaseNote()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        var woReviewUseCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var amendCaseNotes = new Mock<IAmendCaseNotes>();
        var model = _fixture.Build<LarchCheckModel>()
            .With(x => x.CaseNote, "")
            .Create();

        useCase.Setup(x => x.SaveLarchCheckAsync(It.IsAny<LarchCheckModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        woReviewUseCase.Setup(x => x.CompleteLarchCheckAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.LarchCheck(_applicationId, model, useCase.Object, woReviewUseCase.Object, amendCaseNotes.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task LarchFlyover_Get_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        var model = Result.Success(_fixture.Create<LarchFlyoverModel>());
        useCase.Setup(x => x.GetLarchFlyoverModelAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.LarchFlyover(_applicationId, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<LarchFlyoverModel>(viewResult.Model);
    }

    [Fact]
    public async Task LarchFlyover_Get_RedirectsToIndex_WhenFailure()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        useCase.Setup(x => x.GetLarchFlyoverModelAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LarchFlyoverModel>("fail"));

        var result = await _controller.LarchFlyover(_applicationId, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
        Assert.Equal("Could not retrieve larch flyover status", _controller.TempData[Infrastructure.ControllerExtensions.ErrorMessageKey]);
    }

    [Fact]
    public async Task LarchFlyover_Post_ReturnsError_WhenValidationFails()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        var amendCaseNotes = new Mock<IAmendCaseNotes>();
        var validator = new Mock<IValidator<LarchFlyoverModel>>();
        var model = _fixture.Create<LarchFlyoverModel>();

        useCase.Setup(x => x.GetLarchFlyoverModelAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);
        validator.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult(new[] { new FluentValidation.Results.ValidationFailure("Test", "Error") }));

        var result = await _controller.LarchFlyover(_applicationId, model, useCase.Object, amendCaseNotes.Object, validator.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<LarchFlyoverModel>(viewResult.Model);
    }

    [Fact]
    public async Task LarchFlyover_Post_RedirectsToError_WhenReloadFails()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        var amendCaseNotes = new Mock<IAmendCaseNotes>();
        var validator = new Mock<IValidator<LarchFlyoverModel>>();
        var model = _fixture.Create<LarchFlyoverModel>();

        useCase.Setup(x => x.GetLarchFlyoverModelAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LarchFlyoverModel>("fail"));
        validator.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult(new[] { new FluentValidation.Results.ValidationFailure("Test", "Error") }));

        var result = await _controller.LarchFlyover(_applicationId, model, useCase.Object, amendCaseNotes.Object, validator.Object, CancellationToken.None);

        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal("Error", redirectToActionResult.ActionName);
        Assert.Equal("Home", redirectToActionResult.ControllerName);
    }

    [Fact]
    public async Task LarchFlyover_Post_RedirectsToIndex_WhenSuccess()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        var amendCaseNotes = new Mock<IAmendCaseNotes>();
        var validator = new Mock<IValidator<LarchFlyoverModel>>();
        var model = _fixture.Create<LarchFlyoverModel>();
        validator.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        useCase.Setup(x => x.SaveLarchFlyoverAsync(It.IsAny<LarchFlyoverModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.LarchFlyover(_applicationId, model, useCase.Object, amendCaseNotes.Object, validator.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task LarchFlyover_Post_RedirectsToLarchFlyover_WhenSavingFails()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        var amendCaseNotes = new Mock<IAmendCaseNotes>();
        var validator = new Mock<IValidator<LarchFlyoverModel>>();
        var model = _fixture.Create<LarchFlyoverModel>();
        validator.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        useCase.Setup(x => x.SaveLarchFlyoverAsync(It.IsAny<LarchFlyoverModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await _controller.LarchFlyover(_applicationId, model, useCase.Object, amendCaseNotes.Object, validator.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal("LarchFlyover", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task LarchFlyover_Post_RedirectsToLarchFlyover_WhenCaseNoteFails()
    {
        var useCase = new Mock<ILarchCheckUseCase>();
        var amendCaseNotes = new Mock<IAmendCaseNotes>();
        var validator = new Mock<IValidator<LarchFlyoverModel>>();
        var model = _fixture.Create<LarchFlyoverModel>();
        validator.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        useCase.Setup(x => x.SaveLarchFlyoverAsync(It.IsAny<LarchFlyoverModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        amendCaseNotes.Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await _controller.LarchFlyover(_applicationId, model, useCase.Object, amendCaseNotes.Object, validator.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("LarchFlyover", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
        Assert.Equal("fail", _controller.TempData[Infrastructure.ControllerExtensions.ErrorMessageKey]);
    }

    [Fact]
    public async Task EiaScreening_Get_ReturnsView_WhenSuccess()
    {
        var woReview = _fixture.Create<WoodlandOfficerReviewModel>();

        _woReviewUseCaseMock.Setup(s => s.WoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woReview);

        var model = Result.Success(_fixture.Create<EnvironmentalImpactAssessmentModel>());
        _eiaUseCaseMock.Setup(s => s.GetEnvironmentalImpactAssessmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var userAccounts = model.Value.EiaRequests.Select(x => x.RequestingUserId).ToList();

        var userAccountModels = userAccounts.Select(x => _fixture.Build<UserAccountModel>().With(y => y.UserAccountId, x).Create());

        _eiaUseCaseMock.Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountModels.ToList());

        var result = await _controller.EiaScreening(_applicationId, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        var viewModel = Assert.IsType<EiaScreeningViewModel>(viewResult.Model);

        Assert.Equal(model.Value.EiaRequests.Count, viewModel.RequestHistoryItems.Count());
    }

    [Fact]
    public async Task EiaScreening_Get_RedirectsToError_WhenFailure()
    {
        _eiaUseCaseMock.Setup(s => s.GetEnvironmentalImpactAssessmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EnvironmentalImpactAssessmentModel>("fail"));

        var result = await _controller.EiaScreening(_applicationId, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task EiaScreening_Post_ReturnsError_WhenValidationFails()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var validator = new Mock<IValidator<EiaScreeningViewModel>>();
        var screeningModel = _fixture.Create<EiaScreeningViewModel>();


        var woReview = _fixture.Create<WoodlandOfficerReviewModel>();
        _woReviewUseCaseMock.Setup(s => s.WoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woReview);

        var model = Result.Success(_fixture.Create<EnvironmentalImpactAssessmentModel>());
        _eiaUseCaseMock.Setup(s => s.GetEnvironmentalImpactAssessmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var userAccounts = model.Value.EiaRequests.Select(x => x.RequestingUserId).ToList();

        var userAccountModels = userAccounts.Select(x => _fixture.Build<UserAccountModel>().With(y => y.UserAccountId, x).Create());

        _eiaUseCaseMock.Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountModels.ToList());
        validator.Setup(x => x.Validate(screeningModel)).Returns(new FluentValidation.Results.ValidationResult(new[] { new FluentValidation.Results.ValidationFailure("Test", "Error") }));

        var result = await _controller.EiaScreening(screeningModel, useCase.Object, validator.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<EiaScreeningViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task EiaScreening_Post_RedirectsToError_WhenReloadFails()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var validator = new Mock<IValidator<EiaScreeningViewModel>>();
        var screeningModel = _fixture.Create<EiaScreeningViewModel>();


        var woReview = _fixture.Create<WoodlandOfficerReviewModel>();
        _woReviewUseCaseMock.Setup(s => s.WoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woReview);

        var model = Result.Success(_fixture.Create<EnvironmentalImpactAssessmentModel>());
        _eiaUseCaseMock.Setup(s => s.GetEnvironmentalImpactAssessmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EnvironmentalImpactAssessmentModel>("fail"));

        var userAccounts = model.Value.EiaRequests.Select(x => x.RequestingUserId).ToList();

        var userAccountModels = userAccounts.Select(x => _fixture.Build<UserAccountModel>().With(y => y.UserAccountId, x).Create());

        _eiaUseCaseMock.Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountModels.ToList());
        validator.Setup(x => x.Validate(screeningModel)).Returns(new FluentValidation.Results.ValidationResult(new[] { new FluentValidation.Results.ValidationFailure("Test", "Error") }));

        var result = await _controller.EiaScreening(screeningModel, useCase.Object, validator.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }


    [Fact]
    public async Task EiaScreening_Post_RedirectsToError_WhenWoReviewRetrievalFails()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var validator = new Mock<IValidator<EiaScreeningViewModel>>();
        var screeningModel = _fixture.Create<EiaScreeningViewModel>();


        _woReviewUseCaseMock.Setup(s => s.WoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WoodlandOfficerReviewModel>("fail"));

        var model = Result.Success(_fixture.Create<EnvironmentalImpactAssessmentModel>());
        _eiaUseCaseMock.Setup(s => s.GetEnvironmentalImpactAssessmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var userAccounts = model.Value.EiaRequests.Select(x => x.RequestingUserId).ToList();

        var userAccountModels = userAccounts.Select(x => _fixture.Build<UserAccountModel>().With(y => y.UserAccountId, x).Create());

        _eiaUseCaseMock.Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountModels.ToList());
        validator.Setup(x => x.Validate(screeningModel)).Returns(new FluentValidation.Results.ValidationResult(new[] { new FluentValidation.Results.ValidationFailure("Test", "Error") }));

        var result = await _controller.EiaScreening(screeningModel, useCase.Object, validator.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }


    [Fact]
    public async Task EiaScreening_Post_RedirectsToError_WhenAuthorRequestFails()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var validator = new Mock<IValidator<EiaScreeningViewModel>>();
        var screeningModel = _fixture.Create<EiaScreeningViewModel>();

        var woReview = _fixture.Create<WoodlandOfficerReviewModel>();
        _woReviewUseCaseMock.Setup(s => s.WoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woReview);

        var model = Result.Success(_fixture.Create<EnvironmentalImpactAssessmentModel>());
        _eiaUseCaseMock.Setup(s => s.GetEnvironmentalImpactAssessmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var userAccounts = model.Value.EiaRequests.Select(x => x.RequestingUserId).ToList();

        var userAccountModels = userAccounts.Select(x => _fixture.Build<UserAccountModel>().With(y => y.UserAccountId, x).Create());

        _eiaUseCaseMock.Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<UserAccountModel>>("fail"));
        validator.Setup(x => x.Validate(screeningModel)).Returns(new FluentValidation.Results.ValidationResult(new[] { new FluentValidation.Results.ValidationFailure("Test", "Error") }));

        var result = await _controller.EiaScreening(screeningModel, useCase.Object, validator.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task EiaScreening_Post_RedirectsToIndex_WhenSuccess()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var validator = new Mock<IValidator<EiaScreeningViewModel>>();
        var model = _fixture.Create<EiaScreeningViewModel>();
        validator.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        useCase.Setup(x => x.CompleteEiaScreeningAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.EiaScreening(model, useCase.Object, validator.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task EiaScreening_Post_RedirectsToEiaScreening_WhenCompletionFails()
    {
        var useCase = new Mock<IWoodlandOfficerReviewUseCase>();
        var validator = new Mock<IValidator<EiaScreeningViewModel>>();
        var model = _fixture.Create<EiaScreeningViewModel>();
        validator.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        useCase.Setup(x => x.CompleteEiaScreeningAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await _controller.EiaScreening(model, useCase.Object, validator.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal("EiaScreening", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task ViewDesignations_Get_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<IDesignationsUseCase>();
        var model = Result.Success(_fixture.Create<DesignationsViewModel>());
        useCase.Setup(x => x.GetApplicationDesignationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.ViewDesignations(_applicationId, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<DesignationsViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task ViewDesignations_Get_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<IDesignationsUseCase>();
        useCase.Setup(x => x.GetApplicationDesignationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<DesignationsViewModel>("fail"));

        var result = await _controller.ViewDesignations(_applicationId, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task ViewDesignations_Post_ReturnsView_WhenResultFails()
    {
        var useCase = new Mock<IDesignationsUseCase>();
        var model = _fixture.Create<DesignationsViewModel>();

        useCase.Setup(s => s.GetApplicationDesignationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        useCase.Setup(x => x.UpdateCompartmentDesignationsCompletionAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.ViewDesignations(model, useCase.Object, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, view.Model);
    }


    [Fact]
    public async Task ViewDesignations_Post_ReturnsView_WhenNotAllCompartmentsHaveCompletedDesignations()
    {
        var useCase = new Mock<IDesignationsUseCase>();
        var model = _fixture
            .Create<DesignationsViewModel>();

        model.CompartmentDesignations.CompartmentDesignations.Clear();

        model.CompartmentDesignations.CompartmentDesignations.Add(
            _fixture.Build<SubmittedCompartmentDesignationsModel>()
                .Without(x => x.Id)
                .Create());

        useCase.Setup(s => s.GetApplicationDesignationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.ViewDesignations(model, useCase.Object, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, view.Model);
    }

    [Fact]
    public async Task ViewDesignations_Post_RedirectsToError_WhenGetApplicationFails()
    {
        var useCase = new Mock<IDesignationsUseCase>();
        var model = _fixture.Create<DesignationsViewModel>();

        useCase.Setup(s => s.GetApplicationDesignationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<DesignationsViewModel>("fail"));


        var result = await _controller.ViewDesignations(model, useCase.Object, CancellationToken.None);

        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal("Error", redirectToActionResult.ActionName);
    }

    [Fact]
    public async Task ViewDesignations_Post_RedirectsToViewDesignations_WhenSuccess()
    {
        var useCase = new Mock<IDesignationsUseCase>();
        var model = _fixture.Create<DesignationsViewModel>();


        useCase.Setup(s => s.GetApplicationDesignationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));
        useCase.Setup(x => x.UpdateCompartmentDesignationsCompletionAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.ViewDesignations(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task UpdateDesignations_Get_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<IDesignationsUseCase>();
        var model = Result.Success(_fixture.Create<UpdateDesignationsViewModel>());
        useCase.Setup(x => x.GetUpdateDesignationsModelAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.UpdateDesignations(_applicationId, Guid.NewGuid(), useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<UpdateDesignationsViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task UpdateDesignations_Get_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<IDesignationsUseCase>();
        useCase.Setup(x => x.GetUpdateDesignationsModelAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UpdateDesignationsViewModel>("fail"));

        var result = await _controller.UpdateDesignations(_applicationId, Guid.NewGuid(), useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task UpdateDesignations_Post_ReturnsError_WhenValidationFails()
    {
        var useCase = new Mock<IDesignationsUseCase>();
        var validator = new Mock<IValidator<UpdateDesignationsViewModel>>();
        var model = _fixture.Create<UpdateDesignationsViewModel>();
        useCase.Setup(x => x.GetUpdateDesignationsModelAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);
        validator.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult(new[] { new FluentValidation.Results.ValidationFailure("Test", "Error") }));

        var result = await _controller.UpdateDesignations(model, useCase.Object, validator.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<UpdateDesignationsViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task UpdateDesignations_Post_RedirectsToError_WhenReloadFails()
    {
        var useCase = new Mock<IDesignationsUseCase>();
        var validator = new Mock<IValidator<UpdateDesignationsViewModel>>();
        var model = _fixture.Create<UpdateDesignationsViewModel>();
        useCase.Setup(x => x.GetUpdateDesignationsModelAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UpdateDesignationsViewModel>("fail"));
        validator.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult(new[] { new FluentValidation.Results.ValidationFailure("Test", "Error") }));

        var result = await _controller.UpdateDesignations(model, useCase.Object, validator.Object, CancellationToken.None);

        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectToActionResult.ActionName);
        Assert.Equal("Home", redirectToActionResult.ControllerName);
    }

    [Fact]
    public async Task UpdateDesignations_Post_RedirectsToViewDesignations_WhenSuccess()
    {
        var useCase = new Mock<IDesignationsUseCase>();
        var validator = new Mock<IValidator<UpdateDesignationsViewModel>>();
        var model = _fixture.Build<UpdateDesignationsViewModel>().Without(x => x.NextCompartmentId).Create();
        validator.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        useCase.Setup(x => x.UpdateCompartmentDesignationsAsync(
                It.IsAny<Guid>(), 
                It.IsAny<SubmittedCompartmentDesignationsModel>(), 
                It.IsAny<InternalUser>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.UpdateDesignations(model, useCase.Object, validator.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ViewDesignations", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task UpdateDesignations_Post_RedirectsToUpdateDesignations_WhenNextCompartment()
    {
        var useCase = new Mock<IDesignationsUseCase>();
        var validator = new Mock<IValidator<UpdateDesignationsViewModel>>();
        var model = _fixture.Build<UpdateDesignationsViewModel>().Create();
        validator.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        useCase.Setup(x => x.UpdateCompartmentDesignationsAsync(
                It.IsAny<Guid>(),
                It.IsAny<SubmittedCompartmentDesignationsModel>(),
                It.IsAny<InternalUser>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.UpdateDesignations(model, useCase.Object, validator.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal("UpdateDesignations", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
        Assert.Equal(model.NextCompartmentId, redirect.RouteValues["compartmentId"]);
    }

    [Fact]
    public async Task UpdateDesignations_Post_RedirectsToError_WhenGetReloadFails()
    {
        var useCase = new Mock<IDesignationsUseCase>();
        var validator = new Mock<IValidator<UpdateDesignationsViewModel>>();
        var model = _fixture.Build<UpdateDesignationsViewModel>().Without(x => x.NextCompartmentId).Create();
        validator.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        useCase.Setup(x => x.UpdateCompartmentDesignationsAsync(
                It.IsAny<Guid>(),
                It.IsAny<SubmittedCompartmentDesignationsModel>(),
                It.IsAny<InternalUser>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));
        useCase.Setup(x => x.GetUpdateDesignationsModelAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UpdateDesignationsViewModel>("fail"));

        var result = await _controller.UpdateDesignations(model, useCase.Object, validator.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }


    [Fact]
    public async Task UpdateDesignations_Post_ReturnsViewWhenReloadSucceedsAfterUpdateFailure()
    {
        var useCase = new Mock<IDesignationsUseCase>();
        var validator = new Mock<IValidator<UpdateDesignationsViewModel>>();
        var model = _fixture.Build<UpdateDesignationsViewModel>().Without(x => x.NextCompartmentId).Create();
        validator.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        useCase.Setup(x => x.UpdateCompartmentDesignationsAsync(
                It.IsAny<Guid>(),
                It.IsAny<SubmittedCompartmentDesignationsModel>(),
                It.IsAny<InternalUser>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));
        useCase.Setup(x => x.GetUpdateDesignationsModelAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.UpdateDesignations(model, useCase.Object, validator.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<UpdateDesignationsViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task GenerateLicencePreview_RedirectsToIndex_WhenSuccess()
    {
        var generatePdfApplicationUseCase = new Mock<IGeneratePdfApplicationUseCase>();
        generatePdfApplicationUseCase
            .Setup(x => x.GeneratePdfApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new Document()));

        var result = await _controller.GenerateLicencePreview(_applicationId, generatePdfApplicationUseCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task GenerateLicencePreview_AddsErrorMessageAndRedirects_WhenFailure()
    {
        var generatePdfApplicationUseCase = new Mock<IGeneratePdfApplicationUseCase>();
        generatePdfApplicationUseCase
            .Setup(x => x.GeneratePdfApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Document>("fail"));

        var result = await _controller.GenerateLicencePreview(_applicationId, generatePdfApplicationUseCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
        Assert.Equal("Unable to generate the preview licence document for the application", _controller.TempData[Forestry.Flo.Internal.Web.Infrastructure.ControllerExtensions.ErrorMessageKey]);
    }

}