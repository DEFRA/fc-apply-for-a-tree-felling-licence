using AutoFixture;
using AutoFixture.AutoMoq;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using StatusHistoryModel = Forestry.Flo.Internal.Web.Models.FellingLicenceApplication.StatusHistoryModel;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.FellingLicenceApplication;

public class ApproverReviewControllerTests
{
    private readonly Mock<ILogger<ApproverReviewController>> _loggerMock;
    private readonly Mock<IApproverReviewUseCase> _approverReviewUseCaseMock = new();
    private readonly ApproverReviewController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly IFixture _fixture = new Fixture().CustomiseFixtureForFellingLicenceApplications();

    public ApproverReviewControllerTests()
    {
        _loggerMock = new Mock<ILogger<ApproverReviewController>>();
        _controller = new ApproverReviewController(_approverReviewUseCaseMock.Object, _loggerMock.Object);
        _controller.PrepareControllerForTest(Guid.NewGuid(), role: AccountTypeInternal.FieldManager);
    }

    [Fact]
    public async Task Index_ReturnsRedirectToError_WhenModelHasNoValue()
    {
        var useCase = new Mock<IApproverReviewUseCase>();
        useCase.Setup(x => x.RetrieveApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApproverReviewSummaryModel>.None);

        var result = await _controller.Index(_applicationId, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task Index_RedirectsToApplicationSummary_WhenStatusNotSentForApproval()
    {
        var summary = new FellingLicenceApplicationSummaryModel
        {
            StatusHistories = [_fixture.Build<StatusHistoryModel>().With(x => x.Status, FellingLicenceStatus.AdminOfficerReview).Create()]
        };
        var model = _fixture.Build<ApproverReviewSummaryModel>().With(x => x.FellingLicenceApplicationSummary, summary).Create();
        _approverReviewUseCaseMock.Setup(x => x.RetrieveApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.Index(_applicationId, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ApplicationSummary", redirect.ActionName);
        Assert.Equal("FellingLicenceApplication", redirect.ControllerName);
    }

    [Fact]
    public async Task Index_ReturnsView_WhenSuccess()
    {
        var summary = new FellingLicenceApplicationSummaryModel
        {
            StatusHistories = [_fixture.Build<StatusHistoryModel>().With(x => x.Status, FellingLicenceStatus.SentForApproval).Create()],
            ApplicationReference = "REF"
        };
        var model = _fixture.Build<ApproverReviewSummaryModel>().With(x => x.FellingLicenceApplicationSummary, summary).Create();
        _approverReviewUseCaseMock.Setup(x => x.RetrieveApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.Index(_applicationId, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, view.Model);
    }

    [Fact]
    public async Task Index_RestoresDecisionFromTempData()
    {
        var summary = new FellingLicenceApplicationSummaryModel
        {
            StatusHistories = [_fixture.Build<StatusHistoryModel>().With(x => x.Status, FellingLicenceStatus.SentForApproval).Create()]
        };
        var model = _fixture.Build<ApproverReviewSummaryModel>().With(x => x.FellingLicenceApplicationSummary, summary).Create();
        _approverReviewUseCaseMock.Setup(x => x.RetrieveApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);
        _controller.TempData["Decision"] = "True";

        var result = await _controller.Index(_applicationId, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.True(((ApproverReviewSummaryModel)view.Model).Decision);
    }

    [Fact]
    public async Task SaveApproverReview_RedirectsToConfirmation_WhenSuccess()
    {
        var useCase = new Mock<IApproverReviewUseCase>();
        var model = new ApproverReviewSummaryModel
        {
            Id = _applicationId,
            ApproverReview = new ApproverReviewModel
            {
                RequestedStatus = FellingLicenceStatus.Approved,
                ApprovedLicenceDuration = RecommendedLicenceDuration.SevenYear,
                CheckedApplication = true,
                CheckedDocumentation = true,
                CheckedCaseNotes = true,
                CheckedWOReview = true,
                PublicRegisterPublish = true,
                InformedApplicant = true
            },
            RecommendedLicenceDuration = RecommendedLicenceDuration.SevenYear,
            IsWOReviewed = true
        };
        useCase.Setup(x => x.SaveApproverReviewAsync(It.IsAny<ApproverReviewModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.SaveApproverReview(model, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ApproveRefuseApplicationConfirmation", redirect.ActionName);
    }

    [Fact]
    public async Task SaveApproverReview_ReturnsView_WhenValidationFails()
    {
        var useCase = new Mock<IApproverReviewUseCase>();
        var model = new ApproverReviewSummaryModel
        {
            Id = _applicationId,
            ApproverReview = new ApproverReviewModel(),
            RecommendedLicenceDuration = RecommendedLicenceDuration.SevenYear,
            IsWOReviewed = true
        };

        var summary = new FellingLicenceApplicationSummaryModel
        {
            StatusHistories = [_fixture.Build<StatusHistoryModel>().With(x => x.Status, FellingLicenceStatus.AdminOfficerReview).Create()]
        };
        useCase.Setup(x => x.SaveApproverReviewAsync(It.IsAny<ApproverReviewModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));
        useCase.Setup(x => x.RetrieveApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApproverReviewSummaryModel
            {
                FellingLicenceApplicationSummary = summary,
                ApproverReview = new ApproverReviewModel()
            });

        var result = await _controller.SaveApproverReview(model, useCase.Object, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
    }

    [Fact]
    public async Task SaveApproverReview_RedirectsToError_WhenReloadModelFails()
    {
        var useCase = new Mock<IApproverReviewUseCase>();
        var model = new ApproverReviewSummaryModel
        {
            Id = _applicationId,
            ApproverReview = new ApproverReviewModel(),
            RecommendedLicenceDuration = RecommendedLicenceDuration.SevenYear,
            IsWOReviewed = true
        };

        var summary = new FellingLicenceApplicationSummaryModel
        {
            StatusHistories = [_fixture.Build<StatusHistoryModel>().With(x => x.Status, FellingLicenceStatus.AdminOfficerReview).Create()]
        };
        useCase.Setup(x => x.SaveApproverReviewAsync(It.IsAny<ApproverReviewModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));
        useCase.Setup(x => x.RetrieveApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApproverReviewSummaryModel>.None);

        var result = await _controller.SaveApproverReview(model, useCase.Object, CancellationToken.None);

        var view = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", view.ActionName);
        Assert.Equal("Home", view.ControllerName);
    }

    [Fact]
    public async Task SaveApproverReview_ReturnsView_WhenRequestedStatusMissing()
    {
        var useCase = new Mock<IApproverReviewUseCase>();
        var model = new ApproverReviewSummaryModel
        {
            Id = _applicationId,
            ApproverReview = new ApproverReviewModel
            {
                // RequestedStatus is missing
                ApprovedLicenceDuration = RecommendedLicenceDuration.SevenYear,
                CheckedApplication = true,
                CheckedDocumentation = true,
                CheckedCaseNotes = true,
                CheckedWOReview = true,
                PublicRegisterPublish = true,
                InformedApplicant = true
            },
            RecommendedLicenceDuration = RecommendedLicenceDuration.SevenYear,
            IsWOReviewed = true
        };

        useCase.Setup(x => x.SaveApproverReviewAsync(It.IsAny<ApproverReviewModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        useCase.Setup(x => x.RetrieveApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApproverReviewSummaryModel
            {
                FellingLicenceApplicationSummary = new FellingLicenceApplicationSummaryModel(),
                ApproverReview = new ApproverReviewModel()
            });

        var result = await _controller.SaveApproverReview(model, useCase.Object, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        Assert.True(_controller.ModelState.ContainsKey("ApproverReview.RequestedStatus"));
    }

    [Fact]
    public async Task SaveApproverReview_ReturnsView_WhenDurationChangeReasonMissing()
    {
        var useCase = new Mock<IApproverReviewUseCase>();
        var model = new ApproverReviewSummaryModel
        {
            Id = _applicationId,
            ApproverReview = new ApproverReviewModel
            {
                RequestedStatus = FellingLicenceStatus.Approved,
                ApprovedLicenceDuration = RecommendedLicenceDuration.FiveYear, // Different from recommended
                CheckedApplication = true,
                CheckedDocumentation = true,
                CheckedCaseNotes = true,
                CheckedWOReview = true,
                PublicRegisterPublish = true,
                InformedApplicant = true,
                DurationChangeReason = null // Missing
            },
            RecommendedLicenceDuration = RecommendedLicenceDuration.SevenYear,
            IsWOReviewed = true
        };

        useCase.Setup(x => x.SaveApproverReviewAsync(It.IsAny<ApproverReviewModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        useCase.Setup(x => x.RetrieveApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApproverReviewSummaryModel
            {
                FellingLicenceApplicationSummary = new FellingLicenceApplicationSummaryModel(),
                ApproverReview = new ApproverReviewModel()
            });

        var result = await _controller.SaveApproverReview(model, useCase.Object, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        Assert.True(_controller.ModelState.ContainsKey("ApproverReview.DurationChangeReason"));
    }

    [Fact]
    public async Task SaveApproverReview_ReturnsView_WhenPublicRegisterExemptionReasonMissing()
    {
        var useCase = new Mock<IApproverReviewUseCase>();
        var model = new ApproverReviewSummaryModel
        {
            Id = _applicationId,
            ApproverReview = new ApproverReviewModel
            {
                RequestedStatus = FellingLicenceStatus.Approved,
                ApprovedLicenceDuration = RecommendedLicenceDuration.SevenYear,
                CheckedApplication = true,
                CheckedDocumentation = true,
                CheckedCaseNotes = true,
                CheckedWOReview = true,
                PublicRegisterPublish = false, // Not publishing
                PublicRegisterExemptionReason = null, // Missing
                InformedApplicant = true
            },
            RecommendedLicenceDuration = RecommendedLicenceDuration.SevenYear,
            IsWOReviewed = true
        };

        useCase.Setup(x => x.SaveApproverReviewAsync(It.IsAny<ApproverReviewModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        useCase.Setup(x => x.RetrieveApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApproverReviewSummaryModel
            {
                FellingLicenceApplicationSummary = new FellingLicenceApplicationSummaryModel(),
                ApproverReview = new ApproverReviewModel()
            });

        var result = await _controller.SaveApproverReview(model, useCase.Object, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        Assert.True(_controller.ModelState.ContainsKey("ApproverReview.PublicRegisterExemptionReason"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(RecommendedLicenceDuration.None)]
    public async Task SaveApproverReview_ReturnsView_WhenRecommendedDurationIsNone(RecommendedLicenceDuration? duration)
    {
        var useCase = new Mock<IApproverReviewUseCase>();
        var model = new ApproverReviewSummaryModel
        {
            Id = _applicationId,
            ApproverReview = new ApproverReviewModel
            {
                RequestedStatus = FellingLicenceStatus.Approved,
                ApprovedLicenceDuration = duration,
                CheckedApplication = true,
                CheckedDocumentation = true,
                CheckedCaseNotes = true,
                CheckedWOReview = true,
                PublicRegisterPublish = true,
                InformedApplicant = true
            },
            RecommendedLicenceDuration = duration,
            IsWOReviewed = true
        };
        useCase.Setup(x => x.SaveApproverReviewAsync(It.IsAny<ApproverReviewModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        useCase.Setup(x => x.RetrieveApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApproverReviewSummaryModel
            {
                FellingLicenceApplicationSummary = new FellingLicenceApplicationSummaryModel(),
                ApproverReview = new ApproverReviewModel()
            });

        var result = await _controller.SaveApproverReview(model, useCase.Object, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        Assert.True(_controller.ModelState.ContainsKey("ApproverReview.ApprovedLicenceDuration"));
    }

    [Fact]
    public async Task SaveApproverReview_ReturnsView_WhenCheckedApplicationIsFalse()
    {
        var useCase = new Mock<IApproverReviewUseCase>();
        var model = new ApproverReviewSummaryModel
        {
            Id = _applicationId,
            ApproverReview = new ApproverReviewModel
            {
                RequestedStatus = FellingLicenceStatus.Approved,
                ApprovedLicenceDuration = RecommendedLicenceDuration.SevenYear,
                CheckedApplication = false, // Not checked
                CheckedDocumentation = true,
                CheckedCaseNotes = true,
                CheckedWOReview = true,
                PublicRegisterPublish = true,
                InformedApplicant = true
            },
            RecommendedLicenceDuration = RecommendedLicenceDuration.SevenYear,
            IsWOReviewed = true
        };

        useCase.Setup(x => x.SaveApproverReviewAsync(It.IsAny<ApproverReviewModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        useCase.Setup(x => x.RetrieveApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApproverReviewSummaryModel
            {
                FellingLicenceApplicationSummary = new FellingLicenceApplicationSummaryModel(),
                ApproverReview = new ApproverReviewModel()
            });

        var result = await _controller.SaveApproverReview(model, useCase.Object, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        Assert.True(_controller.ModelState.ContainsKey("ApproverReview.CheckedApplication"));
    }

    [Fact]
    public async Task SaveApproverReview_ReturnsView_WhenSaveApproverReviewAsyncFails()
    {
        var useCase = new Mock<IApproverReviewUseCase>();
        var model = new ApproverReviewSummaryModel
        {
            Id = _applicationId,
            ApproverReview = new ApproverReviewModel
            {
                RequestedStatus = FellingLicenceStatus.Approved,
                ApprovedLicenceDuration = RecommendedLicenceDuration.SevenYear,
                CheckedApplication = true,
                CheckedDocumentation = true,
                CheckedCaseNotes = true,
                CheckedWOReview = true,
                PublicRegisterPublish = true,
                InformedApplicant = true
            },
            RecommendedLicenceDuration = RecommendedLicenceDuration.SevenYear,
            IsWOReviewed = true
        };

        useCase.Setup(x => x.SaveApproverReviewAsync(It.IsAny<ApproverReviewModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));
        useCase.Setup(x => x.RetrieveApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApproverReviewSummaryModel
            {
                FellingLicenceApplicationSummary = new FellingLicenceApplicationSummaryModel(),
                ApproverReview = new ApproverReviewModel()
            });

        var result = await _controller.SaveApproverReview(model, useCase.Object, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
    }

    [Fact]
    public void ReturnApplication_RedirectsToReturnApplication()
    {
        var model = new ApproverReviewSummaryModel
        {
            Id = _applicationId,
            ApproverReview = new ApproverReviewModel
            {
                RequestedStatus = FellingLicenceStatus.Refused
            }
        };

        var result = _controller.ReturnApplication(model, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("ReturnApplication", redirect.ControllerName);
    }

    [Fact]
    public async Task SaveGeneratePdfPreview_RedirectsToIndex_WhenSuccess()
    {
        var useCase = new Mock<IApproverReviewUseCase>();
        var pdfUseCase = new Mock<IGeneratePdfApplicationUseCase>();
        var doc = _fixture .Create<Document>();
        pdfUseCase.Setup(x => x.GeneratePdfApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(doc));

        var model = _fixture .Build<ApproverReviewSummaryModel>()
            .With(x => x.Id, _applicationId)
            .With(x => x.FellingLicenceApplicationSummary, new FellingLicenceApplicationSummaryModel
            {
                ApplicationReference = "REF",
                StatusHistories = [_fixture.Build<StatusHistoryModel>().With(x => x.Status, FellingLicenceStatus.SentForApproval).Create()]
            })
            .Create();

        var result = await _controller.SaveGeneratePdfPreview(model, useCase.Object, pdfUseCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("ApproverReview", redirect.ControllerName);
    }

    [Fact]
    public async Task SaveGeneratePdfPreview_RedirectsToIndex_WhenFailure()
    {
        var useCase = new Mock<IApproverReviewUseCase>();
        var pdfUseCase = new Mock<IGeneratePdfApplicationUseCase>();
        pdfUseCase.Setup(x => x.GeneratePdfApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Document>("fail"));

        var model = _fixture.Build<ApproverReviewSummaryModel>()
            .With(x => x.Id, _applicationId)
            .With(x => x.FellingLicenceApplicationSummary, new FellingLicenceApplicationSummaryModel
            {
                ApplicationReference = "REF",
                StatusHistories = [_fixture.Build<StatusHistoryModel>().With(x => x.Status, FellingLicenceStatus.SentForApproval).Create()]
            })
            .Create();

        var result = await _controller.SaveGeneratePdfPreview(model, useCase.Object, pdfUseCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task ApproveRefuseApplicationConfirmation_RedirectsToIndex_WhenUserCannotApprove()
    {
        var useCase = new Mock<IFellingLicenceApplicationUseCase>();
        _controller.PrepareControllerForTest(Guid.NewGuid());
        var user = new InternalUser (_controller.User);
        var summary = new FellingLicenceApplicationReviewSummaryModel
        {
            ViewingUser = user,
            FellingLicenceApplicationSummary = new FellingLicenceApplicationSummaryModel
            {
                Status = FellingLicenceStatus.SentForApproval,
                AssigneeHistories = []
            }
        };
        useCase.Setup(x => x.RetrieveFellingLicenceApplicationReviewSummaryAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var result = await _controller.ApproveRefuseApplicationConfirmation(_applicationId, FellingLicenceStatus.Approved, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task ApproveRefuseApplicationConfirmation_RedirectsToError_WhenSummaryHasNoValue()
    {
        var useCase = new Mock<IFellingLicenceApplicationUseCase>();
        useCase.Setup(x => x.RetrieveFellingLicenceApplicationReviewSummaryAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplicationReviewSummaryModel>.None);

        var result = await _controller.ApproveRefuseApplicationConfirmation(_applicationId, FellingLicenceStatus.Approved, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task ApproveRefuseApplicationConfirmation_RedirectsToIndex_WhenStatusNotSentForApprovalOrRoleNotFieldManager()
    {
        var useCase = new Mock<IFellingLicenceApplicationUseCase>();
        var summary = new FellingLicenceApplicationReviewSummaryModel
        {
            ViewingUser = new InternalUser(new System.Security.Claims.ClaimsPrincipal()),
            FellingLicenceApplicationSummary = new FellingLicenceApplicationSummaryModel
            {
                Status = FellingLicenceStatus.Approved,
                AssigneeHistories = [
                    _fixture. Build<AssigneeHistoryModel>()
                        .With(x => x.Role, AssignedUserRole.WoodlandOfficer)
                        .Create()
                ]
            }
        };
        useCase.Setup(x => x.RetrieveFellingLicenceApplicationReviewSummaryAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var result = await _controller.ApproveRefuseApplicationConfirmation(_applicationId, FellingLicenceStatus.Approved, useCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task ApproveRefuseApplicationConfirmation_ReturnsView_WhenSuccess()
    {
        var user = new InternalUser(_controller.User);

        var useCase = new Mock<IFellingLicenceApplicationUseCase>();

        var userAccountModel = _fixture.Build<UserAccountModel>().With(x => x.Id, user.UserAccountId).Create();

        var summary = new FellingLicenceApplicationReviewSummaryModel
        {
            ViewingUser = user,
            FellingLicenceApplicationSummary = new FellingLicenceApplicationSummaryModel
            {
                Status = FellingLicenceStatus.SentForApproval,
                ApplicationReference = "REF",
                AssigneeHistories = new List<AssigneeHistoryModel>
                {
                    _fixture.Build<AssigneeHistoryModel>()
                        .With(x => x.Role, AssignedUserRole.FieldManager)
                        .With(x => x.UserAccount, userAccountModel)
                        .Create()
                }
            }
        };

        useCase.Setup(x => x.RetrieveFellingLicenceApplicationReviewSummaryAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var result = await _controller.ApproveRefuseApplicationConfirmation(_applicationId, FellingLicenceStatus.Approved, useCase.Object, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task RefuseApplication_CallsChangeApplicationStatus()
    {
        var approvalRefusalUseCase = new Mock<IApproveRefuseOrReferApplicationUseCase>();
        var pdfUseCase = new Mock<IGeneratePdfApplicationUseCase>();
        var removeDocUseCase = new Mock<IRemoveSupportingDocumentUseCase>();
        var transactionMock = new Mock<IDbContextTransaction>();
        _approverReviewUseCaseMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);
        approvalRefusalUseCase.Setup(x => x.ApproveOrRefuseOrReferApplicationAsync(It.IsAny<InternalUser>(), It.IsAny<Guid>(), FellingLicenceStatus.Refused, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FinaliseFellingLicenceApplicationResult.CreateSuccess([]));
        var result = await _controller.RefuseApplication(_applicationId, approvalRefusalUseCase.Object, pdfUseCase.Object, removeDocUseCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task ApproveApplication_CallsChangeApplicationStatus()
    {
        var approvalRefusalUseCase = new Mock<IApproveRefuseOrReferApplicationUseCase>();
        var pdfUseCase = new Mock<IGeneratePdfApplicationUseCase>();
        var doc = _fixture.Create<Document>();
        var removeDocUseCase = new Mock<IRemoveSupportingDocumentUseCase>();
        var transactionMock = new Mock<IDbContextTransaction>();
        _approverReviewUseCaseMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);
        pdfUseCase.Setup(x => x.CalculateLicenceExpiryDateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTime.UtcNow);
        approvalRefusalUseCase.Setup(x => x.UpdateApplicationApproverAndExpiryDateAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        pdfUseCase.Setup(x => x.GeneratePdfApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);
        approvalRefusalUseCase.Setup(x => x.ApproveOrRefuseOrReferApplicationAsync(It.IsAny<InternalUser>(), It.IsAny<Guid>(), FellingLicenceStatus.Approved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FinaliseFellingLicenceApplicationResult.CreateSuccess([]));
        var result = await _controller.ApproveApplication(_applicationId, approvalRefusalUseCase.Object, pdfUseCase.Object, removeDocUseCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task ReferApplicationToLocalAuthority_CallsChangeApplicationStatus()
    {
        var approvalRefusalUseCase = new Mock<IApproveRefuseOrReferApplicationUseCase>();
        var pdfUseCase = new Mock<IGeneratePdfApplicationUseCase>();
        var removeDocUseCase = new Mock<IRemoveSupportingDocumentUseCase>();
        var transactionMock = new Mock<IDbContextTransaction>();
        _approverReviewUseCaseMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);
        approvalRefusalUseCase.Setup(x => x.ApproveOrRefuseOrReferApplicationAsync(It.IsAny<InternalUser>(), It.IsAny<Guid>(), FellingLicenceStatus.ReferredToLocalAuthority, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FinaliseFellingLicenceApplicationResult.CreateSuccess([]));
        var result = await _controller.ReferApplicationToLocalAuthority(_applicationId, approvalRefusalUseCase.Object, pdfUseCase.Object, removeDocUseCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task ChangeApplicationStatus_RedirectsToIndex_WhenUserCannotApprove()
    {
        var approvalRefusalUseCase = new Mock<IApproveRefuseOrReferApplicationUseCase>();
        var pdfUseCase = new Mock<IGeneratePdfApplicationUseCase>();
        var removeDocUseCase = new Mock<IRemoveSupportingDocumentUseCase>();
        var transactionMock = new Mock<IDbContextTransaction>();
        _approverReviewUseCaseMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);
        _controller.PrepareControllerForTest(Guid.NewGuid(), true);

        var result = await _controller.RefuseApplication(_applicationId, approvalRefusalUseCase.Object, pdfUseCase.Object, removeDocUseCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task ChangeApplicationStatus_RedirectsToIndex_WhenUpdateApproverIdFails()
    {
        var approvalRefusalUseCase = new Mock<IApproveRefuseOrReferApplicationUseCase>();
        var pdfUseCase = new Mock<IGeneratePdfApplicationUseCase>();
        var removeDocUseCase = new Mock<IRemoveSupportingDocumentUseCase>();
        var transactionMock = new Mock<IDbContextTransaction>();
        _approverReviewUseCaseMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);
        pdfUseCase.Setup(x => x.CalculateLicenceExpiryDateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTime.UtcNow);
        approvalRefusalUseCase.Setup(x => x.ApproveOrRefuseOrReferApplicationAsync(It.IsAny<InternalUser>(), It.IsAny<Guid>(), It.IsAny<FellingLicenceStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FinaliseFellingLicenceApplicationResult.CreateSuccess([]));
        approvalRefusalUseCase.Setup(x => x.UpdateApplicationApproverAndExpiryDateAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await _controller.ApproveApplication(_applicationId, approvalRefusalUseCase.Object, pdfUseCase.Object, removeDocUseCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        transactionMock.Verify(v => v.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        transactionMock.Verify(v => v.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeApplicationStatus_RedirectsToIndex_WhenPdfGenerationFails()
    {
        var approvalRefusalUseCase = new Mock<IApproveRefuseOrReferApplicationUseCase>();
        var pdfUseCase = new Mock<IGeneratePdfApplicationUseCase>();
        var removeDocUseCase = new Mock<IRemoveSupportingDocumentUseCase>();
        var transactionMock = new Mock<IDbContextTransaction>();
        _approverReviewUseCaseMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);
        approvalRefusalUseCase.Setup(x => x.UpdateApplicationApproverAndExpiryDateAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        approvalRefusalUseCase.Setup(x => x.ApproveOrRefuseOrReferApplicationAsync(It.IsAny<InternalUser>(), It.IsAny<Guid>(), It.IsAny<FellingLicenceStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FinaliseFellingLicenceApplicationResult.CreateSuccess([]));
        pdfUseCase.Setup(x => x.GeneratePdfApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Document>("fail"));

        var result = await _controller.ApproveApplication(_applicationId, approvalRefusalUseCase.Object, pdfUseCase.Object, removeDocUseCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        transactionMock.Verify(v => v.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        transactionMock.Verify(v => v.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeApplicationStatus_RedirectsToIndex_WhenApproveOrRefuseFails()
    {
        var approvalRefusalUseCase = new Mock<IApproveRefuseOrReferApplicationUseCase>();
        var pdfUseCase = new Mock<IGeneratePdfApplicationUseCase>();
        var removeDocUseCase = new Mock<IRemoveSupportingDocumentUseCase>();
        var transactionMock = new Mock<IDbContextTransaction>();
        _approverReviewUseCaseMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);
        approvalRefusalUseCase.Setup(x => x.ApproveOrRefuseOrReferApplicationAsync(It.IsAny<InternalUser>(), It.IsAny<Guid>(), It.IsAny<FellingLicenceStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FinaliseFellingLicenceApplicationResult.CreateFailure("fail", FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotPublishToDecisionPublicRegister));

        pdfUseCase.Setup(x =>
                x.GeneratePdfApplicationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<Document>());

        removeDocUseCase.Setup(x => x.RemoveFellingLicenceDocument(It.IsAny<InternalUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        pdfUseCase.Setup(x => x.CalculateLicenceExpiryDateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTime.UtcNow);
        approvalRefusalUseCase.Setup(x => x.UpdateApplicationApproverAndExpiryDateAsync(It.IsAny<Guid>(), null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.ApproveApplication(_applicationId, approvalRefusalUseCase.Object, pdfUseCase.Object, removeDocUseCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }


    [Fact]
    public async Task ChangeApplicationStatus_AddsErrorWhenApprovedIdCannotBeUpdated()
    {
        var internalUser = new InternalUser(_controller.User);
        var approvalRefusalUseCase = new Mock<IApproveRefuseOrReferApplicationUseCase>();
        var pdfUseCase = new Mock<IGeneratePdfApplicationUseCase>();
        var removeDocUseCase = new Mock<IRemoveSupportingDocumentUseCase>();
        var transactionMock = new Mock<IDbContextTransaction>();
        _approverReviewUseCaseMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);
        approvalRefusalUseCase.Setup(x => x.ApproveOrRefuseOrReferApplicationAsync(It.IsAny<InternalUser>(), It.IsAny<Guid>(), It.IsAny<FellingLicenceStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FinaliseFellingLicenceApplicationResult.CreateSuccess([]));

        pdfUseCase.Setup(x =>
                x.GeneratePdfApplicationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<Document>());

        removeDocUseCase.Setup(x => x.RemoveFellingLicenceDocument(It.IsAny<InternalUser>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        pdfUseCase.Setup(x => x.CalculateLicenceExpiryDateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTime.UtcNow);
        approvalRefusalUseCase.Setup(x => x.UpdateApplicationApproverAndExpiryDateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await _controller.ApproveApplication(_applicationId, approvalRefusalUseCase.Object, pdfUseCase.Object, removeDocUseCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        Assert.Equal("Unable to update the approver id for the application",
            _controller.TempData[Infrastructure.ControllerExtensions.ErrorMessageKey]);
    }


    [Fact]
    public async Task ChangeApplicationStatus_AddsUserGuide_WhenSubProcessFailures()
    {
        var approvalRefusalUseCase = new Mock<IApproveRefuseOrReferApplicationUseCase>();
        var pdfUseCase = new Mock<IGeneratePdfApplicationUseCase>();
        var removeDocUseCase = new Mock<IRemoveSupportingDocumentUseCase>();
        var transactionMock = new Mock<IDbContextTransaction>();
        _approverReviewUseCaseMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);
        approvalRefusalUseCase.Setup(x => x.ApproveOrRefuseOrReferApplicationAsync(It.IsAny<InternalUser>(), It.IsAny<Guid>(), It.IsAny<FellingLicenceStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FinaliseFellingLicenceApplicationResult.CreateSuccess([
                FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotPublishToDecisionPublicRegister,
                FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotSendNotificationToApplicant,
                FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotStoreDecisionDetailsLocally
            ]));

        var result = await _controller.RefuseApplication(_applicationId, approvalRefusalUseCase.Object, pdfUseCase.Object, removeDocUseCase.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }
}