using AutoFixture;
using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.FellingLicenceApplication;

public class AssignFellingLicenceApplicationControllerTests
{
    private readonly Mock<IAssignToUserUseCase> _assignToUserUseCaseMock;
    private readonly Mock<IAssignToApplicantUseCase> _assignToApplicantUseCaseMock;
    private readonly Mock<IValidator<AssignBackToApplicantModel>> _validatorMock;
    private readonly Mock<IUrlHelper> _mockUrlHelper;
    private readonly AssignFellingLicenceApplicationController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Fixture _fixture = new();

    public AssignFellingLicenceApplicationControllerTests()
    {
        _assignToUserUseCaseMock = new Mock<IAssignToUserUseCase>();
        _assignToApplicantUseCaseMock = new Mock<IAssignToApplicantUseCase>();
        _validatorMock = new Mock<IValidator<AssignBackToApplicantModel>>();
        _mockUrlHelper = new Mock<IUrlHelper>();
        _controller = new AssignFellingLicenceApplicationController();
        _controller.PrepareControllerForTest(_userId);
    }

    [Fact]
    public async Task ConfirmReassignApplication_ReturnsView_WhenSuccess()
    {
        var model = Result.Success(_fixture.Create<ConfirmReassignApplicationModel>());
        _assignToUserUseCaseMock.Setup(x => x.ConfirmReassignApplicationForRole(
            It.IsAny<Guid>(), It.IsAny<AssignedUserRole>(), It.IsAny<string>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.ConfirmReassignApplication(
            _applicationId, AssignedUserRole.AdminOfficer, "returnUrl", _assignToUserUseCaseMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ConfirmReassignApplicationModel>(viewResult.Model);
    }

    [Fact]
    public async Task ConfirmReassignApplication_RedirectsToError_WhenFailure()
    {
        _assignToUserUseCaseMock.Setup(x => x.ConfirmReassignApplicationForRole(
            It.IsAny<Guid>(), It.IsAny<AssignedUserRole>(), It.IsAny<string>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ConfirmReassignApplicationModel>("fail"));

        var result = await _controller.ConfirmReassignApplication(
            _applicationId, AssignedUserRole.AdminOfficer, "returnUrl", _assignToUserUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task SelectUser_Get_ReturnsView_WhenSuccess()
    {
        var model = Result.Success(_fixture.Create<AssignToUserModel>());
        _assignToUserUseCaseMock.Setup(x => x.RetrieveDetailsToAssignFlaToUserAsync(
            It.IsAny<Guid>(), It.IsAny<AssignedUserRole>(), It.IsAny<string>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.SelectUser(
            _applicationId, AssignedUserRole.AdminOfficer, "returnUrl", _assignToUserUseCaseMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AssignToUserModel>(viewResult.Model);
    }

    [Fact]
    public async Task SelectUser_Get_RedirectsToError_WhenFailure()
    {
        _assignToUserUseCaseMock.Setup(x => x.RetrieveDetailsToAssignFlaToUserAsync(
            It.IsAny<Guid>(), It.IsAny<AssignedUserRole>(), It.IsAny<string>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AssignToUserModel>("fail"));

        var result = await _controller.SelectUser(
            _applicationId, AssignedUserRole.AdminOfficer, "returnUrl", _assignToUserUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task SelectUser_Post_ReturnsView_WhenModelStateInvalid()
    {
        var assignModel = _fixture.Build<AssignToUserModel>()
            .With(x => x.SelectedRole, AssignedUserRole.AdminOfficer)
            .With(x => x.SelectedFcAreaCostCode, "costCode")
            .With(x => x.ReturnUrl, "returnUrl")
            .Create();

        _controller.ModelState.AddModelError("Test", "Error");

        var reloadModel = Result.Success(_fixture.Create<AssignToUserModel>());
        _assignToUserUseCaseMock.Setup(x => x.RetrieveDetailsToAssignFlaToUserAsync(
            It.IsAny<Guid>(), It.IsAny<AssignedUserRole>(), It.IsAny<string>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reloadModel);

        var result = await _controller.SelectUser(
            _applicationId, assignModel, _assignToUserUseCaseMock.Object, _mockUrlHelper.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AssignToUserModel>(viewResult.Model);
    }

    [Fact]
    public async Task SelectUser_Post_Redirects_WhenSuccess()
    {
        var caseNote = _fixture.Create<FormLevelCaseNote>();
        var assignModel = _fixture.Build<AssignToUserModel>()
            .With(x => x.SelectedUserId, Guid.NewGuid())
            .With(x => x.SelectedRole, AssignedUserRole.AdminOfficer)
            .With(x => x.SelectedFcAreaCostCode, "costCode")
            .With(x => x.ReturnUrl, "returnUrl")
            .With(x => x.FormLevelCaseNote, caseNote)
            .Create();

        _mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string?>())).Returns(true);

        _assignToUserUseCaseMock.Setup(x => x.AssignToUserAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AssignedUserRole>(), It.IsAny<string>(), It.IsAny<InternalUser>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.SelectUser(
            _applicationId, assignModel, _assignToUserUseCaseMock.Object, _mockUrlHelper.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal(assignModel.ReturnUrl, redirectResult.Url);
    }

    [Fact]
    public async Task SelectUser_Post_Redirects_WhenSuccessWithInvalidReturnUrl()
    {
        var caseNote = _fixture.Create<FormLevelCaseNote>();
        var assignModel = _fixture.Build<AssignToUserModel>()
            .With(x => x.SelectedUserId, Guid.NewGuid())
            .With(x => x.SelectedRole, AssignedUserRole.AdminOfficer)
            .With(x => x.SelectedFcAreaCostCode, "costCode")
            .With(x => x.ReturnUrl, "returnUrl")
            .With(x => x.FormLevelCaseNote, caseNote)
            .Create();

        _mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string?>())).Returns(false);

        _assignToUserUseCaseMock.Setup(x => x.AssignToUserAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AssignedUserRole>(), It.IsAny<string>(), It.IsAny<InternalUser>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.SelectUser(
            _applicationId, assignModel, _assignToUserUseCaseMock.Object, _mockUrlHelper.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ApplicationSummary", redirectResult.ActionName);
        Assert.Equal("FellingLicenceApplication", redirectResult.ControllerName);
    }

    [Fact]
    public async Task SelectUser_Post_RedirectsToError_WhenFailure()
    {
        var caseNote = _fixture.Create<FormLevelCaseNote>();
        var assignModel = _fixture.Build<AssignToUserModel>()
            .With(x => x.SelectedUserId, Guid.NewGuid())
            .With(x => x.SelectedRole, AssignedUserRole.AdminOfficer)
            .With(x => x.SelectedFcAreaCostCode, "costCode")
            .With(x => x.ReturnUrl, "returnUrl")
            .With(x => x.FormLevelCaseNote, caseNote)
            .Create();

        _assignToUserUseCaseMock.Setup(x => x.AssignToUserAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AssignedUserRole>(), It.IsAny<string>(), It.IsAny<InternalUser>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.SelectUser(
            _applicationId, assignModel, _assignToUserUseCaseMock.Object, _mockUrlHelper.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }


    [Fact]
    public async Task SelectUser_Post_RemovesModelState_WhenSelectedRoleIsNotAdminOrWoodlandOfficer()
    {
        _controller.ModelState.AddModelError(nameof(AssignToUserModel.SelectedFcAreaCostCode), "error");

        var caseNote = _fixture.Create<FormLevelCaseNote>();
        var assignModel = _fixture.Build<AssignToUserModel>()
            .With(x => x.SelectedUserId, Guid.NewGuid())
            .With(x => x.SelectedRole, AssignedUserRole.Applicant)
            .With(x => x.SelectedFcAreaCostCode, "costCode")
            .With(x => x.ReturnUrl, "returnUrl")
            .With(x => x.FormLevelCaseNote, caseNote)
            .Create();

        _mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string?>())).Returns(true);

        _assignToUserUseCaseMock.Setup(x => x.AssignToUserAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AssignedUserRole>(), It.IsAny<string>(), It.IsAny<InternalUser>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.SelectUser(
            _applicationId, assignModel, _assignToUserUseCaseMock.Object, _mockUrlHelper.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal(assignModel.ReturnUrl, redirectResult.Url);
    }

    [Fact]
    public async Task SelectUser_Post_RemovesModelState_WhenSelectedRoleIsNotAdminOrWoodlandOfficer_WithInvalidReturnUrl()
    {
        _controller.ModelState.AddModelError(nameof(AssignToUserModel.SelectedFcAreaCostCode), "error");

        var caseNote = _fixture.Create<FormLevelCaseNote>();
        var assignModel = _fixture.Build<AssignToUserModel>()
            .With(x => x.SelectedUserId, Guid.NewGuid())
            .With(x => x.SelectedRole, AssignedUserRole.Applicant)
            .With(x => x.SelectedFcAreaCostCode, "costCode")
            .With(x => x.ReturnUrl, "returnUrl")
            .With(x => x.FormLevelCaseNote, caseNote)
            .Create();

        _mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string?>())).Returns(false);

        _assignToUserUseCaseMock.Setup(x => x.AssignToUserAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AssignedUserRole>(), It.IsAny<string>(), It.IsAny<InternalUser>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.SelectUser(
            _applicationId, assignModel, _assignToUserUseCaseMock.Object, _mockUrlHelper.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ApplicationSummary", redirectResult.ActionName);
        Assert.Equal("FellingLicenceApplication", redirectResult.ControllerName);
    }

    [Fact]
    public async Task AssignBackToApplicant_Get_ReturnsView_WhenSuccess()
    {
        var model = Result.Success(_fixture.Create<AssignBackToApplicantModel>());
        _assignToApplicantUseCaseMock.Setup(x => x.GetValidExternalApplicantsForAssignmentAsync(
            It.IsAny<InternalUser>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.AssignBackToApplicant(
            _applicationId, "returnUrl", _assignToApplicantUseCaseMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AssignBackToApplicantModel>(viewResult.Model);
    }


    [Theory]
    [InlineData("AdminOfficerReview", "Operations Admin Officer Review", "AdminOfficerReview")]
    [InlineData("WoodlandOfficerReview", "Woodland Officer Review", "WoodlandOfficerReview")]
    public async Task AssignBackToApplicant_Get_AddsRelevantBreadcrumbs(string returnUrl, string text, string controller)
    {
        var model = Result.Success(_fixture.Create<AssignBackToApplicantModel>());
        _assignToApplicantUseCaseMock.Setup(x => x.GetValidExternalApplicantsForAssignmentAsync(
                It.IsAny<InternalUser>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.AssignBackToApplicant(
            _applicationId, returnUrl, _assignToApplicantUseCaseMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        var resultModel = Assert.IsType<AssignBackToApplicantModel>(viewResult.Model);

        Assert.NotNull(resultModel.Breadcrumbs);
        var breadcrumb = resultModel.Breadcrumbs.Breadcrumbs.FirstOrDefault(x => x.Text == text);

        Assert.NotNull(breadcrumb);
        Assert.Equal(controller, breadcrumb.Controller);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task AssignBackToApplicant_Get_PopulatesReturnUrlWhenNullOrEmpty(string? url)
    {
        var model = Result.Success(_fixture.Create<AssignBackToApplicantModel>());
        _assignToApplicantUseCaseMock.Setup(x => x.GetValidExternalApplicantsForAssignmentAsync(
                It.IsAny<InternalUser>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.AssignBackToApplicant(
            _applicationId, url, _assignToApplicantUseCaseMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AssignBackToApplicantModel>(viewResult.Model);

        _assignToApplicantUseCaseMock.Verify(v => v.GetValidExternalApplicantsForAssignmentAsync(
            It.IsAny<InternalUser>(),
            _applicationId,
            "/fake-url",
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task AssignBackToApplicant_Get_RedirectsToError_WhenFailure()
    {
        _assignToApplicantUseCaseMock.Setup(x => x.GetValidExternalApplicantsForAssignmentAsync(
            It.IsAny<InternalUser>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AssignBackToApplicantModel>("fail"));

        var result = await _controller.AssignBackToApplicant(
            _applicationId, "returnUrl", _assignToApplicantUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task AssignBackToApplicant_Post_ReturnsView_WhenModelStateInvalid()
    {
        var assignModel = _fixture.Build<AssignBackToApplicantModel>()
            .With(x => x.FellingLicenceApplicationId, _applicationId)
            .With(x => x.ReturnUrl, "returnUrl")
            .Create();

        _controller.ModelState.AddModelError("Test", "Error");
        _validatorMock.Setup(x => x.Validate(assignModel)).Returns(new ValidationResult(new List<ValidationFailure> { new("Test", "Error") }));

        var reloadModel = Result.Success(_fixture.Create<AssignBackToApplicantModel>());
        _assignToApplicantUseCaseMock.Setup(x => x.GetValidExternalApplicantsForAssignmentAsync(
            It.IsAny<InternalUser>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reloadModel);

        var result = await _controller.AssignBackToApplicant(
            assignModel, _assignToApplicantUseCaseMock.Object, _validatorMock.Object, _mockUrlHelper.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AssignBackToApplicantModel>(viewResult.Model);
    }


    [Fact]
    public async Task AssignBackToApplicant_Get_RedirectsToError_WhenReloadModelFails()
    {
        var assignModel = _fixture.Build<AssignBackToApplicantModel>()
            .With(x => x.FellingLicenceApplicationId, _applicationId)
            .With(x => x.ReturnUrl, "returnUrl")
            .Create();

        _controller.ModelState.AddModelError("Test", "Error");
        _validatorMock.Setup(x => x.Validate(assignModel)).Returns(new ValidationResult(new List<ValidationFailure> { new("Test", "Error") }));

        var reloadModel = Result.Success(_fixture.Create<AssignBackToApplicantModel>());
        _assignToApplicantUseCaseMock.Setup(x => x.GetValidExternalApplicantsForAssignmentAsync(
                It.IsAny<InternalUser>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AssignBackToApplicantModel>("fail"));

        var result = await _controller.AssignBackToApplicant(
            assignModel, _assignToApplicantUseCaseMock.Object, _validatorMock.Object, _mockUrlHelper.Object, CancellationToken.None);

        var viewResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", viewResult.ActionName);
        Assert.Equal("Home", viewResult.ControllerName);
    }


    [Fact]
    public async Task AssignBackToApplicant_Post_RedirectsToError_WhenAssignFails()
    {
        var assignModel = _fixture.Build<AssignBackToApplicantModel>()
            .With(x => x.FellingLicenceApplicationId, _applicationId)
            .With(x => x.ExternalApplicantId, Guid.NewGuid())
            .With(x => x.ReturnUrl, "returnUrl")
            .Create();

        _validatorMock.Setup(x => x.Validate(assignModel)).Returns(new ValidationResult());
        _assignToApplicantUseCaseMock.Setup(x => x.AssignApplicationToApplicantAsync(
            It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Dictionary<FellingLicenceApplicationSection, bool>>(), It.IsAny<Dictionary<Guid, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await _controller.AssignBackToApplicant(
            assignModel, _assignToApplicantUseCaseMock.Object, _validatorMock.Object, _mockUrlHelper.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task AssignBackToApplicant_Post_Redirects_WhenSuccess()
    {
        var assignModel = _fixture.Build<AssignBackToApplicantModel>()
            .With(x => x.FellingLicenceApplicationId, _applicationId)
            .With(x => x.ExternalApplicantId, Guid.NewGuid())
            .With(x => x.ReturnUrl, "returnUrl")
            .Create();

        _mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string?>())).Returns(true);

        _validatorMock.Setup(x => x.Validate(assignModel)).Returns(new ValidationResult());
        _assignToApplicantUseCaseMock.Setup(x => x.AssignApplicationToApplicantAsync(
                It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<FellingLicenceApplicationSection, bool>>(), It.IsAny<Dictionary<Guid, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.AssignBackToApplicant(
            assignModel, _assignToApplicantUseCaseMock.Object, _validatorMock.Object, _mockUrlHelper.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal(assignModel.ReturnUrl, redirectResult.Url);
    }

    [Fact]
    public async Task AssignBackToApplicant_Post_RedirectsToDefaultUrl_WhenSuccessButInvalidReturnUrl()
    {
        var assignModel = _fixture.Build<AssignBackToApplicantModel>()
            .With(x => x.FellingLicenceApplicationId, _applicationId)
            .With(x => x.ExternalApplicantId, Guid.NewGuid())
            .With(x => x.ReturnUrl, "returnUrl")
            .Create();

        _mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string?>())).Returns(false);

        _validatorMock.Setup(x => x.Validate(assignModel)).Returns(new ValidationResult());
        _assignToApplicantUseCaseMock.Setup(x => x.AssignApplicationToApplicantAsync(
                It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<FellingLicenceApplicationSection, bool>>(), It.IsAny<Dictionary<Guid, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.AssignBackToApplicant(
            assignModel, _assignToApplicantUseCaseMock.Object, _validatorMock.Object, _mockUrlHelper.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ApplicationSummary", redirectResult.ActionName);
        Assert.Equal("FellingLicenceApplication", redirectResult.ControllerName);
    }
}