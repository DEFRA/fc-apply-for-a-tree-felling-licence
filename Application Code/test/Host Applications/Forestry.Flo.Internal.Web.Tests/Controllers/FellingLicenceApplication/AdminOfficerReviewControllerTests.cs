using AutoFixture;
using CSharpFunctionalExtensions;
using FluentValidation;
using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.AdminOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.FellingLicenceApplication;

public class AdminOfficerReviewControllerTests
{
    private readonly Fixture _fixture = new();
    private readonly Mock<IAdminOfficerReviewUseCase> _adminOfficerReviewUseCaseMock = new();
    private readonly Mock<IAgentAuthorityFormCheckUseCase> _agentAuthorityFormCheckUseCaseMock = new();
    private readonly Mock<IValidator<AgentAuthorityFormCheckModel>> _agentAuthorityFormCheckValidatorMock = new();
    private readonly Mock<IMappingCheckUseCase> _mappingCheckUseCaseMock = new();
    private readonly Mock<IValidator<MappingCheckModel>> _mappingCheckValidatorMock = new();
    private readonly Mock<IConstraintsCheckUseCase> _constraintsCheckUseCaseMock = new();
    private readonly Mock<IClock> _clockMock = new();
    private readonly Mock<ILarchCheckUseCase> _larchCheckUseCaseMock = new();
    private readonly Mock<IFellingLicenceApplicationUseCase> _fellingLicenceApplicationUseCaseMock = new();
    private readonly Mock<IAmendCaseNotes> _amendCaseNotesMock = new();
    private readonly Mock<IValidator<LarchFlyoverModel>> _larchFlyoverValidatorMock = new();
    private readonly Mock<ICBWCheckUseCase> _cbwCheckUseCaseMock = new();
    private readonly Mock<IEnvironmentalImpactAssessmentAdminOfficerUseCase> _cbwEnvironmentalImpactAssessmentUseCaseMock = new();

    private readonly AdminOfficerReviewController _controller;

    public AdminOfficerReviewControllerTests()
    {
        _controller = new AdminOfficerReviewController(_cbwEnvironmentalImpactAssessmentUseCaseMock.Object);
        _controller.PrepareControllerForTest(Guid.NewGuid());
    }

    [Fact]
    public async Task Index_ReturnsView_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var model = Result.Success(_fixture.Create<AdminOfficerReviewModel>());
        _adminOfficerReviewUseCaseMock.Setup(x => x.GetAdminOfficerReviewAsync(id, It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.Index(id, _adminOfficerReviewUseCaseMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AdminOfficerReviewModel>(viewResult.Model);
    }

    [Fact]
    public async Task Index_RedirectsToError_WhenFailure()
    {
        var id = Guid.NewGuid();
        _adminOfficerReviewUseCaseMock.Setup(x => x.GetAdminOfficerReviewAsync(id, It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AdminOfficerReviewModel>("fail"));

        var result = await _controller.Index(id, _adminOfficerReviewUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task AssignWoodlandOfficer_RedirectsToConfirmReassign_WhenAlreadyAssigned()
    {
        var id = Guid.NewGuid();
        var model = _fixture.Build<AdminOfficerReviewModel>().With(x => x.AssignedWoodlandOfficer, "wo").Create();
        _adminOfficerReviewUseCaseMock.Setup(x => x.GetAdminOfficerReviewAsync(id, It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.AssignWoodlandOfficer(id, _adminOfficerReviewUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmReassignApplication", redirectResult.ActionName);
        Assert.Equal("AssignFellingLicenceApplication", redirectResult.ControllerName);
    }

    [Fact]
    public async Task AssignWoodlandOfficer_RedirectsToSelectUser_WhenNotAssigned()
    {
        var id = Guid.NewGuid();
        var model = _fixture.Build<AdminOfficerReviewModel>().With(x => x.AssignedWoodlandOfficer, "").Create();
        _adminOfficerReviewUseCaseMock.Setup(x => x.GetAdminOfficerReviewAsync(id, It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.AssignWoodlandOfficer(id, _adminOfficerReviewUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("SelectUser", redirectResult.ActionName);
        Assert.Equal("AssignFellingLicenceApplication", redirectResult.ControllerName);
    }

    [Fact]
    public async Task AssignWoodlandOfficer_RedirectsToError_WhenFailure()
    {
        var id = Guid.NewGuid();
        _adminOfficerReviewUseCaseMock.Setup(x => x.GetAdminOfficerReviewAsync(id, It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AdminOfficerReviewModel>("fail"));

        var result = await _controller.AssignWoodlandOfficer(id, _adminOfficerReviewUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task ConfirmAdminOfficerReview_Redirects_WhenWoodlandOfficerNotAssigned()
    {
        var model = _fixture.Build<AdminOfficerReviewModel>().With(x => x.AssignedWoodlandOfficer, "").Create();
        var useCase = _adminOfficerReviewUseCaseMock.Object;

        var result = await _controller.ConfirmAdminOfficerReview(model, useCase, _clockMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task ConfirmAdminOfficerReview_Redirects_WhenDateReceivedNotPopulated()
    {
        var model = _fixture.Build<AdminOfficerReviewModel>()
            .With(x => x.AssignedWoodlandOfficer, "wo")
            .With(x => x.ApplicationSource, FellingLicenceApplicationSource.PaperBasedSubmission)
            .Without(x => x.DateReceived)
            .Create();

        var useCase = _adminOfficerReviewUseCaseMock.Object;

        var result = await _controller.ConfirmAdminOfficerReview(model, useCase, _clockMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task ConfirmAdminOfficerReview_Redirects_WhenDateReceivedInFuture()
    {
        var now = DateTime.UtcNow;
        var model = _fixture.Build<AdminOfficerReviewModel>()
            .With(x => x.AssignedWoodlandOfficer, "wo")
            .With(x => x.ApplicationSource, FellingLicenceApplicationSource.PaperBasedSubmission)
            .With(x => x.DateReceived, new DatePart { Day = now.Day, Month = now.Month, Year = now.Year + 1 })
            .Create();

        _clockMock.Setup(x => x.GetCurrentInstant()).Returns(NodaTime.Instant.FromDateTimeUtc(now));
        var useCase = _adminOfficerReviewUseCaseMock.Object;

        var result = await _controller.ConfirmAdminOfficerReview(model, useCase, _clockMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task ConfirmAdminOfficerReview_Redirects_WhenUseCaseFailure()
    {
        var now = DateTime.UtcNow;
        var model = _fixture.Build<AdminOfficerReviewModel>()
            .With(x => x.AssignedWoodlandOfficer, "wo")
            .With(x => x.ApplicationSource, FellingLicenceApplicationSource.ApplicantUser)
            .With(x => x.DateReceived, new DatePart { Day = now.Day, Month = now.Month, Year = now.Year })
            .Create();

        _clockMock.Setup(x => x.GetCurrentInstant()).Returns(NodaTime.Instant.FromDateTimeUtc(now));
        _adminOfficerReviewUseCaseMock.Setup(x => x.ConfirmAdminOfficerReview(
            It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.ConfirmAdminOfficerReview(model, _adminOfficerReviewUseCaseMock.Object, _clockMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task ConfirmAdminOfficerReview_RedirectsToSummary_WhenSuccess()
    {
        var now = DateTime.UtcNow;
        var model = _fixture.Build<AdminOfficerReviewModel>()
            .With(x => x.AssignedWoodlandOfficer, "wo")
            .With(x => x.ApplicationSource, FellingLicenceApplicationSource.ApplicantUser)
            .With(x => x.DateReceived, new DatePart { Day = now.Day, Month = now.Month, Year = now.Year })
            .Create();

        _clockMock.Setup(x => x.GetCurrentInstant()).Returns(NodaTime.Instant.FromDateTimeUtc(now));
        _adminOfficerReviewUseCaseMock.Setup(x => x.ConfirmAdminOfficerReview(
            It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.ConfirmAdminOfficerReview(model, _adminOfficerReviewUseCaseMock.Object, _clockMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ApplicationSummary", redirectResult.ActionName);
        Assert.Equal("FellingLicenceApplication", redirectResult.ControllerName);
    }


    [Fact]
    public async Task ConfirmAdminOfficerReview_RedirectsToSummary_WhenSuccessAndPaperBased()
    {
        var now = DateTime.UtcNow;
        var model = _fixture.Build<AdminOfficerReviewModel>()
            .With(x => x.AssignedWoodlandOfficer, "wo")
            .With(x => x.ApplicationSource, FellingLicenceApplicationSource.PaperBasedSubmission)
            .With(x => x.DateReceived, new DatePart { Day = now.Day, Month = now.Month, Year = now.Year })
            .Create();

        _clockMock.Setup(x => x.GetCurrentInstant()).Returns(NodaTime.Instant.FromDateTimeUtc(now));
        _adminOfficerReviewUseCaseMock.Setup(x => x.ConfirmAdminOfficerReview(
                It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.ConfirmAdminOfficerReview(model, _adminOfficerReviewUseCaseMock.Object, _clockMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ApplicationSummary", redirectResult.ActionName);
        Assert.Equal("FellingLicenceApplication", redirectResult.ControllerName);
    }

    [Fact]
    public async Task AgentAuthorityFormCheck_Get_ReturnsView_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var model = Result.Success(_fixture.Create<AgentAuthorityFormCheckModel>());
        _agentAuthorityFormCheckUseCaseMock.Setup(x => x.GetAgentAuthorityFormCheckModelAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.AgentAuthorityFormCheck(id, _agentAuthorityFormCheckUseCaseMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AgentAuthorityFormCheckModel>(viewResult.Model);
    }

    [Fact]
    public async Task AgentAuthorityFormCheck_Get_Redirects_WhenFailure()
    {
        var id = Guid.NewGuid();
        _agentAuthorityFormCheckUseCaseMock.Setup(x => x.GetAgentAuthorityFormCheckModelAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AgentAuthorityFormCheckModel>("fail"));

        var result = await _controller.AgentAuthorityFormCheck(id, _agentAuthorityFormCheckUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task AgentAuthorityFormCheck_Post_ReturnsView_WhenModelStateInvalid()
    {
        var model = _fixture.Create<AgentAuthorityFormCheckModel>();
        _agentAuthorityFormCheckValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult(new List<FluentValidation.Results.ValidationFailure> { new("CheckPassed", "Required") }));
        _agentAuthorityFormCheckUseCaseMock.Setup(x => x.GetAgentAuthorityFormCheckModelAsync(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.AgentAuthorityFormCheck(model, _agentAuthorityFormCheckUseCaseMock.Object, _agentAuthorityFormCheckValidatorMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AgentAuthorityFormCheckModel>(viewResult.Model);
    }

    [Fact]
    public async Task AgentAuthorityFormCheck_Post_RedirectsToError_WhenNewModelRetrievalFails()
    {
        var model = _fixture.Create<AgentAuthorityFormCheckModel>();
        _agentAuthorityFormCheckValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult(new List<FluentValidation.Results.ValidationFailure> { new("CheckPassed", "Required") }));
        _agentAuthorityFormCheckUseCaseMock.Setup(x => x.GetAgentAuthorityFormCheckModelAsync(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AgentAuthorityFormCheckModel>("fail"));

        var result = await _controller.AgentAuthorityFormCheck(model, _agentAuthorityFormCheckUseCaseMock.Object, _agentAuthorityFormCheckValidatorMock.Object, CancellationToken.None);

        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectToActionResult.ActionName);
        Assert.Equal("Home", redirectToActionResult.ControllerName);
    }

    [Fact]
    public async Task AgentAuthorityFormCheck_Post_Redirects_WhenUseCaseFailure()
    {
        var model = _fixture.Create<AgentAuthorityFormCheckModel>();
        _agentAuthorityFormCheckValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        _agentAuthorityFormCheckUseCaseMock.Setup(x => x.CompleteAgentAuthorityCheckAsync(
            model.ApplicationId, It.IsAny<Guid>(), true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.AgentAuthorityFormCheck(model, _agentAuthorityFormCheckUseCaseMock.Object, _agentAuthorityFormCheckValidatorMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AgentAuthorityFormCheck", redirectResult.ActionName);
    }

    [Fact]
    public async Task AgentAuthorityFormCheck_Post_RedirectsToIndex_WhenSuccess()
    {
        var model = _fixture.Build<AgentAuthorityFormCheckModel>().With(x => x.CheckPassed, true).Create();
        _agentAuthorityFormCheckValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        _agentAuthorityFormCheckUseCaseMock.Setup(x => x.CompleteAgentAuthorityCheckAsync(
            model.ApplicationId, It.IsAny<Guid>(), true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.AgentAuthorityFormCheck(model, _agentAuthorityFormCheckUseCaseMock.Object, _agentAuthorityFormCheckValidatorMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task MappingCheck_Get_ReturnsView_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var model = Result.Success(_fixture.Create<MappingCheckModel>());
        _mappingCheckUseCaseMock.Setup(x => x.GetMappingCheckModelAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.MappingCheck(id, _mappingCheckUseCaseMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<MappingCheckModel>(viewResult.Model);
    }

    [Fact]
    public async Task MappingCheck_Get_Redirects_WhenFailure()
    {
        var id = Guid.NewGuid();
        _mappingCheckUseCaseMock.Setup(x => x.GetMappingCheckModelAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<MappingCheckModel>("fail"));

        var result = await _controller.MappingCheck(id, _mappingCheckUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task MappingCheck_Post_ReturnsView_WhenModelStateInvalid()
    {
        var model = _fixture.Create<MappingCheckModel>();
        _mappingCheckValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult(new List<FluentValidation.Results.ValidationFailure> { new("CheckPassed", "Required") }));
        _mappingCheckUseCaseMock.Setup(x => x.GetMappingCheckModelAsync(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.MappingCheck(model, _mappingCheckUseCaseMock.Object, _mappingCheckValidatorMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<MappingCheckModel>(viewResult.Model);
    }

    [Fact]
    public async Task MappingCheck_Post_Redirects_WhenUseCaseFailure()
    {
        var model = _fixture.Build<MappingCheckModel>().With(x => x.CheckPassed, true).Create();
        _mappingCheckValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        _mappingCheckUseCaseMock.Setup(x => x.CompleteMappingCheckAsync(
            model.ApplicationId, It.IsAny<Guid>(), true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.MappingCheck(model, _mappingCheckUseCaseMock.Object, _mappingCheckValidatorMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("MappingCheck", redirectResult.ActionName);
    }


    [Fact]
    public async Task MappingCheck_Post_Redirects_WhenUseCaseFailureOnNewModelRetrieval()
    {
        var model = _fixture.Build<MappingCheckModel>().With(x => x.CheckPassed, true).Create();
        _mappingCheckValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult(new List<FluentValidation.Results.ValidationFailure> { new("CheckPassed", "Required") }));
        _mappingCheckUseCaseMock.Setup(x => x.GetMappingCheckModelAsync(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<MappingCheckModel>("fail"));

        var result = await _controller.MappingCheck(model, _mappingCheckUseCaseMock.Object, _mappingCheckValidatorMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
    }

    [Fact]
    public async Task MappingCheck_Post_RedirectsToIndex_WhenSuccess()
    {
        var model = _fixture.Build<MappingCheckModel>().With(x => x.CheckPassed, true).Create();
        _mappingCheckValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        _mappingCheckUseCaseMock.Setup(x => x.CompleteMappingCheckAsync(
            model.ApplicationId, It.IsAny<Guid>(), true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.MappingCheck(model, _mappingCheckUseCaseMock.Object, _mappingCheckValidatorMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task ConstraintsCheck_Get_Redirects_WhenCannotStartYet()
    {
        var id = Guid.NewGuid();
        var adminOfficerReviewModel = _fixture.Build<AdminOfficerReviewModel>()
            .With(x => x.AdminOfficerReviewTaskListStates, 
                new AdminOfficerReviewTaskListStates(
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.CannotStartYet,
                    InternalReviewStepStatus.NotStarted, 
                    InternalReviewStepStatus.NotRequired,
                    InternalReviewStepStatus.NotRequired, 
                    InternalReviewStepStatus.NotRequired, 
                    InternalReviewStepStatus.NotRequired))
            .Create();
        _adminOfficerReviewUseCaseMock.Setup(x => x.GetAdminOfficerReviewAsync(id, It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(adminOfficerReviewModel));

        var result = await _controller.ConstraintsCheck(id, _constraintsCheckUseCaseMock.Object, _adminOfficerReviewUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task ConstraintsCheck_Get_Redirects_WhenAdminOfficerReviewFailure()
    {
        var id = Guid.NewGuid();
        _adminOfficerReviewUseCaseMock.Setup(x => x.GetAdminOfficerReviewAsync(id, It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AdminOfficerReviewModel>("fail"));

        var result = await _controller.ConstraintsCheck(id, _constraintsCheckUseCaseMock.Object, _adminOfficerReviewUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task ConstraintsCheck_Get_Redirects_WhenModelFailure()
    {
        var id = Guid.NewGuid();
        var adminOfficerReviewModel = _fixture.Build<AdminOfficerReviewModel>()
            .With(x => x.AdminOfficerReviewTaskListStates,
                new AdminOfficerReviewTaskListStates(
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.NotStarted,
                    InternalReviewStepStatus.NotStarted,
                    InternalReviewStepStatus.NotRequired,
                    InternalReviewStepStatus.NotRequired,
                    InternalReviewStepStatus.NotRequired,
                    InternalReviewStepStatus.NotRequired))
            .Create();
        _adminOfficerReviewUseCaseMock.Setup(x => x.GetAdminOfficerReviewAsync(id, It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(adminOfficerReviewModel));
        _constraintsCheckUseCaseMock.Setup(x => x.GetConstraintsCheckModel(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ConstraintsCheckModel>("fail"));

        var result = await _controller.ConstraintsCheck(id, _constraintsCheckUseCaseMock.Object, _adminOfficerReviewUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task ConstraintsCheck_Get_ReturnsView_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var adminOfficerReviewModel = _fixture.Build<AdminOfficerReviewModel>()
            .With(x => x.AdminOfficerReviewTaskListStates,
                new AdminOfficerReviewTaskListStates(
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.Completed,
                    InternalReviewStepStatus.NotStarted,
                    InternalReviewStepStatus.NotStarted,
                    InternalReviewStepStatus.NotRequired,
                    InternalReviewStepStatus.NotRequired,
                    InternalReviewStepStatus.NotRequired,
                    InternalReviewStepStatus.NotRequired))
            .Create();
        _adminOfficerReviewUseCaseMock.Setup(x => x.GetAdminOfficerReviewAsync(id, It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(adminOfficerReviewModel));
        _constraintsCheckUseCaseMock.Setup(x => x.GetConstraintsCheckModel(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(_fixture.Create<ConstraintsCheckModel>()));

        var result = await _controller.ConstraintsCheck(id, _constraintsCheckUseCaseMock.Object, _adminOfficerReviewUseCaseMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ConstraintsCheckModel>(viewResult.Model);
    }

    [Fact]
    public async Task ConstraintsCheck_Post_Redirects_WhenModelFailure()
    {
        var model = _fixture.Create<ConstraintsCheckModel>();
        _constraintsCheckUseCaseMock.Setup(x => x.GetConstraintsCheckModel(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ConstraintsCheckModel>("fail"));

        var result = await _controller.ConstraintsCheck(model, _constraintsCheckUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task ConstraintsCheck_Post_ReturnsView_WhenModelStateInvalid()
    {
        var model = _fixture.Create<ConstraintsCheckModel>();
        _controller.ModelState.AddModelError("Test", "Error");
        _constraintsCheckUseCaseMock.Setup(x => x.GetConstraintsCheckModel(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.ConstraintsCheck(model, _constraintsCheckUseCaseMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ConstraintsCheckModel>(viewResult.Model);
    }

    [Fact]
    public async Task ConstraintsCheck_Post_ReturnsView_WhenNoLisReport()
    {
        var summaryNoLis = _fixture
            .Build<FellingLicenceApplicationSummaryModel>()
            .With(x => x.MostRecentFcLisReport, Maybe.None)
            .Create();

        var model = _fixture.Build<ConstraintsCheckModel>()
            .With(x => x.FellingLicenceApplicationSummary, summaryNoLis)
            .Create();
        var newModel = _fixture.Build<ConstraintsCheckModel>()
            .With(x => x.FellingLicenceApplicationSummary, summaryNoLis)
            .Create();
        _constraintsCheckUseCaseMock.Setup(x => x.GetConstraintsCheckModel(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(newModel));

        var result = await _controller.ConstraintsCheck(model, _constraintsCheckUseCaseMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ConstraintsCheckModel>(viewResult.Model);
    }

    [Fact]
    public async Task ConstraintsCheck_Post_Redirects_WhenUseCaseFailure()
    {
        var lisReport = _fixture
            .Build<DocumentModel>()
            .With(x => x.DocumentPurpose, DocumentPurpose.FcLisConstraintReport)
            .Create();

        var summaryWithLis = _fixture
            .Build<FellingLicenceApplicationSummaryModel>()
            .With(x => x.MostRecentFcLisReport, lisReport)
            .Create();

        var model = _fixture.Build<ConstraintsCheckModel>()
            .With(x => x.FellingLicenceApplicationSummary, summaryWithLis)
            .With(x => x.IsComplete, true)
            .Create();
        var newModel = _fixture.Build<ConstraintsCheckModel>()
            .With(x => x.FellingLicenceApplicationSummary, summaryWithLis)
            .Create();
        _constraintsCheckUseCaseMock.Setup(x => x.GetConstraintsCheckModel(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(newModel));
        _constraintsCheckUseCaseMock.Setup(x => x.CompleteConstraintsCheckAsync(
            model.ApplicationId, It.IsAny<bool>(), It.IsAny<Guid>(), model.IsComplete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.ConstraintsCheck(model, _constraintsCheckUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConstraintsCheck", redirectResult.ActionName);
    }

    [Fact]
    public async Task ConstraintsCheck_Post_RedirectsToIndex_WhenSuccess()
    {
        var lisReport = _fixture
            .Build<DocumentModel>()
            .With(x => x.DocumentPurpose, DocumentPurpose.FcLisConstraintReport)
            .Create();

        var summaryWithLis = _fixture
            .Build<FellingLicenceApplicationSummaryModel>()
            .With(x => x.MostRecentFcLisReport, lisReport)
            .Create();

        var model = _fixture.Build<ConstraintsCheckModel>()
            .With(x => x.FellingLicenceApplicationSummary, summaryWithLis)
            .With(x => x.IsComplete, true)
            .Create();
        var newModel = _fixture.Build<ConstraintsCheckModel>()
            .With(x => x.FellingLicenceApplicationSummary, summaryWithLis)
            .Create();
        _constraintsCheckUseCaseMock.Setup(x => x.GetConstraintsCheckModel(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(newModel));
        _constraintsCheckUseCaseMock.Setup(x => x.CompleteConstraintsCheckAsync(
            model.ApplicationId, true, It.IsAny<Guid>(), model.IsComplete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.ConstraintsCheck(model, _constraintsCheckUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task LarchCheck_Get_ReturnsView_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var model = Result.Success(_fixture.Create<LarchCheckModel>());
        _larchCheckUseCaseMock.Setup(x => x.GetLarchCheckModelAsync(id, It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.LarchCheck(id, _larchCheckUseCaseMock.Object, _fellingLicenceApplicationUseCaseMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<LarchCheckModel>(viewResult.Model);
    }

    [Fact]
    public async Task LarchCheck_Get_Redirects_WhenFailure()
    {
        var id = Guid.NewGuid();
        _larchCheckUseCaseMock.Setup(x => x.GetLarchCheckModelAsync(id, It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LarchCheckModel>("fail"));

        var result = await _controller.LarchCheck(id, _larchCheckUseCaseMock.Object, _fellingLicenceApplicationUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task LarchCheck_Post_ReturnsView_WhenModelStateInvalid()
    {
        var id = Guid.NewGuid();
        var model = _fixture.Create<LarchCheckModel>();
        _controller.ModelState.AddModelError("Test", "Error");
        _larchCheckUseCaseMock.Setup(x => x.GetLarchCheckModelAsync(id, It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.LarchCheck(id, model, _larchCheckUseCaseMock.Object, _adminOfficerReviewUseCaseMock.Object, _amendCaseNotesMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<LarchCheckModel>(viewResult.Model);
    }


    [Fact]
    public async Task LarchCheck_Post_RedirectsToError_WhenNewModelRetrievalFails()
    {
        var id = Guid.NewGuid();
        var model = _fixture.Create<LarchCheckModel>();
        _controller.ModelState.AddModelError("Test", "Error");
        _larchCheckUseCaseMock.Setup(x => x.GetLarchCheckModelAsync(id, It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LarchCheckModel>("fail"));

        var result = await _controller.LarchCheck(id, model, _larchCheckUseCaseMock.Object, _adminOfficerReviewUseCaseMock.Object, _amendCaseNotesMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task LarchCheck_Post_Redirects_WhenSaveLarchCheckFailure()
    {
        var model = _fixture.Create<LarchCheckModel>();
        _larchCheckUseCaseMock.Setup(x => x.SaveLarchCheckAsync(model, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.LarchCheck(model.ApplicationId, model, _larchCheckUseCaseMock.Object, _adminOfficerReviewUseCaseMock.Object, _amendCaseNotesMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("LarchCheck", redirectResult.ActionName);
    }

    [Fact]
    public async Task LarchCheck_Post_Redirects_WhenCompleteLarchCheckFailure()
    {
        var model = _fixture.Create<LarchCheckModel>();
        _larchCheckUseCaseMock.Setup(x => x.SaveLarchCheckAsync(model, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _adminOfficerReviewUseCaseMock.Setup(x => x.CompleteLarchCheckAsync(model.ApplicationId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.LarchCheck(model.ApplicationId, model, _larchCheckUseCaseMock.Object, _adminOfficerReviewUseCaseMock.Object, _amendCaseNotesMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("LarchCheck", redirectResult.ActionName);
    }

    [Fact]
    public async Task LarchCheck_Post_AddsCaseNote_WhenCaseNoteProvided()
    {
        var caseNote = _fixture.Build<FormLevelCaseNote>()
            .With(x => x.CaseNote, "note")
            .Create();
        var model = _fixture.Build<LarchCheckModel>().With(x => x.FormLevelCaseNote, caseNote).Create();
        _larchCheckUseCaseMock.Setup(x => x.SaveLarchCheckAsync(model, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _adminOfficerReviewUseCaseMock.Setup(x => x.CompleteLarchCheckAsync(model.ApplicationId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _amendCaseNotesMock.Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.LarchCheck(model.ApplicationId, model, _larchCheckUseCaseMock.Object, _adminOfficerReviewUseCaseMock.Object, _amendCaseNotesMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task LarchCheck_Post_RedirectsToLarchFlyover_WhenCaseNoteFailure()
    {
        var caseNote = _fixture.Build<FormLevelCaseNote>()
            .With(x => x.CaseNote, "note")
            .Create();
        var model = _fixture.Build<LarchCheckModel>().With(x => x.FormLevelCaseNote, caseNote).Create();
        _larchCheckUseCaseMock.Setup(x => x.SaveLarchCheckAsync(model, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _adminOfficerReviewUseCaseMock.Setup(x => x.CompleteLarchCheckAsync(model.ApplicationId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _amendCaseNotesMock.Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.LarchCheck(model.ApplicationId, model, _larchCheckUseCaseMock.Object, _adminOfficerReviewUseCaseMock.Object, _amendCaseNotesMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("LarchFlyover", redirectResult.ActionName);
    }

    [Fact]
    public async Task LarchFlyover_Get_ReturnsView_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var model = Result.Success(_fixture.Create<LarchFlyoverModel>());
        _larchCheckUseCaseMock.Setup(x => x.GetLarchFlyoverModelAsync(id, It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.LarchFlyover(id, _larchCheckUseCaseMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<LarchFlyoverModel>(viewResult.Model);
    }

    [Fact]
    public async Task LarchFlyover_Get_Redirects_WhenFailure()
    {
        var id = Guid.NewGuid();
        _larchCheckUseCaseMock.Setup(x => x.GetLarchFlyoverModelAsync(id, It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LarchFlyoverModel>("fail"));

        var result = await _controller.LarchFlyover(id, _larchCheckUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task LarchFlyover_Post_ReturnsView_WhenModelStateInvalid()
    {
        var id = Guid.NewGuid();
        var model = _fixture.Create<LarchFlyoverModel>();
        _larchFlyoverValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult(new List<FluentValidation.Results.ValidationFailure> { new("CaseNote", "Required") }));
        _larchCheckUseCaseMock.Setup(x => x.GetLarchFlyoverModelAsync(id, It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.LarchFlyover(id, model, _larchCheckUseCaseMock.Object, _amendCaseNotesMock.Object, _larchFlyoverValidatorMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<LarchFlyoverModel>(viewResult.Model);
    }


    [Fact]
    public async Task LarchFlyover_Post_RedirectsToError_WhenNewModelRetrievalFails()
    {
        var id = Guid.NewGuid();
        var model = _fixture.Create<LarchFlyoverModel>();
        _larchFlyoverValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult(new List<FluentValidation.Results.ValidationFailure> { new("CaseNote", "Required") }));
        _larchCheckUseCaseMock.Setup(x => x.GetLarchFlyoverModelAsync(id, It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LarchFlyoverModel>("fail"));

        var result = await _controller.LarchFlyover(id, model, _larchCheckUseCaseMock.Object, _amendCaseNotesMock.Object, _larchFlyoverValidatorMock.Object, CancellationToken.None);

        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal("Error", redirectToActionResult.ActionName);
        Assert.Equal("Home", redirectToActionResult.ControllerName);
    }

    [Fact]
    public async Task LarchFlyover_Post_Redirects_WhenSaveLarchFlyoverFailure()
    {
        var model = _fixture.Create<LarchFlyoverModel>();
        _larchFlyoverValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        _larchCheckUseCaseMock.Setup(x => x.SaveLarchFlyoverAsync(model, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.LarchFlyover(model.ApplicationId, model, _larchCheckUseCaseMock.Object, _amendCaseNotesMock.Object, _larchFlyoverValidatorMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("LarchFlyover", redirectResult.ActionName);
    }

    [Fact]
    public async Task LarchFlyover_Post_AddsCaseNote_WhenCaseNoteProvided()
    {
        var caseNote = _fixture.Build<FormLevelCaseNote>()
            .With(x => x.CaseNote, "note")
            .Create();
        var model = _fixture.Build<LarchFlyoverModel>().With(x => x.FormLevelCaseNote, caseNote).Create();
        _larchFlyoverValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        _larchCheckUseCaseMock.Setup(x => x.SaveLarchFlyoverAsync(model, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _amendCaseNotesMock.Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.LarchFlyover(model.ApplicationId, model, _larchCheckUseCaseMock.Object, _amendCaseNotesMock.Object, _larchFlyoverValidatorMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task LarchFlyover_Post_RedirectsToLarchFlyover_WhenCaseNoteFailure()
    {
        var caseNote = _fixture.Build<FormLevelCaseNote>()
            .With(x => x.CaseNote, "note")
            .Create();
        var model = _fixture.Build<LarchFlyoverModel>().With(x => x.FormLevelCaseNote, caseNote).Create();
        _larchFlyoverValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        _larchCheckUseCaseMock.Setup(x => x.SaveLarchFlyoverAsync(model, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _amendCaseNotesMock.Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.LarchFlyover(model.ApplicationId, model, _larchCheckUseCaseMock.Object, _amendCaseNotesMock.Object, _larchFlyoverValidatorMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("LarchFlyover", redirectResult.ActionName);
    }

    [Fact]
    public async Task CBWCheck_Get_ReturnsView_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var model = Result.Success(_fixture.Create<CBWCheckModel>());
        _cbwCheckUseCaseMock.Setup(x => x.GetCBWCheckModelAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.CBWCheck(id, _cbwCheckUseCaseMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<CBWCheckModel>(viewResult.Model);
    }

    [Fact]
    public async Task CBWCheck_Get_Redirects_WhenFailure()
    {
        var id = Guid.NewGuid();
        _cbwCheckUseCaseMock.Setup(x => x.GetCBWCheckModelAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<CBWCheckModel>("fail"));

        var result = await _controller.CBWCheck(id, _cbwCheckUseCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task CBWCheck_Post_ReturnsView_WhenModelStateInvalid()
    {
        var model = _fixture.Create<CBWCheckModel>();
        _controller.ModelState.AddModelError("Test", "Error");
        _cbwCheckUseCaseMock.Setup(x => x.GetCBWCheckModelAsync(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.CBWCheck(model, _cbwCheckUseCaseMock.Object, _amendCaseNotesMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<CBWCheckModel>(viewResult.Model);
    }

    [Fact]
    public async Task CBWCheck_Post_RedirectsToError_WhenNewModelRetrievalFails()
    {
        var model = _fixture.Create<CBWCheckModel>();
        _controller.ModelState.AddModelError("Test", "Error");
        _cbwCheckUseCaseMock.Setup(x => x.GetCBWCheckModelAsync(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<CBWCheckModel>("fail"));

        var result = await _controller.CBWCheck(model, _cbwCheckUseCaseMock.Object, _amendCaseNotesMock.Object, CancellationToken.None);

        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectToActionResult.ActionName);
        Assert.Equal("Home", redirectToActionResult.ControllerName);
    }

    [Fact]
    public async Task CBWCheck_Post_Redirects_WhenUseCaseFailure()
    {
        var model = _fixture.Build<CBWCheckModel>().With(x => x.CheckPassed, true).Create();
        _cbwCheckUseCaseMock.Setup(x => x.CompleteCBWCheckAsync(
            model.ApplicationId, It.IsAny<Guid>(), true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.CBWCheck(model, _cbwCheckUseCaseMock.Object, _amendCaseNotesMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("MappingCheck", redirectResult.ActionName);
    }

    [Fact]
    public async Task CBWCheck_Post_AddsCaseNote_WhenCaseNoteProvided()
    {
        var caseNote = _fixture.Build<FormLevelCaseNote>()
            .With(x => x.CaseNote, "note")
            .Create();
        var model = _fixture.Build<CBWCheckModel>().With(x => x.FormLevelCaseNote, caseNote).With(x => x.CheckPassed, true).Create();
        _cbwCheckUseCaseMock.Setup(x => x.CompleteCBWCheckAsync(
            model.ApplicationId, It.IsAny<Guid>(), true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _amendCaseNotesMock.Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.CBWCheck(model, _cbwCheckUseCaseMock.Object, _amendCaseNotesMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task CBWCheck_Post_RedirectsToIndex_WhenCaseNoteFailure()
    {
        var caseNote = _fixture.Build<FormLevelCaseNote>()
            .With(x => x.CaseNote, "note")
            .Create();
        var model = _fixture.Build<CBWCheckModel>().With(x => x.FormLevelCaseNote, caseNote).With(x => x.CheckPassed, true).Create();
        _cbwCheckUseCaseMock.Setup(x => x.CompleteCBWCheckAsync(
            model.ApplicationId, It.IsAny<Guid>(), true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _amendCaseNotesMock.Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.CBWCheck(model, _cbwCheckUseCaseMock.Object, _amendCaseNotesMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }
}