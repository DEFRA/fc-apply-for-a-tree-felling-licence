using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.ApproverReview;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Internal.Web.Services; // InternalUser
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication; // FellingLicenceApplicationUseCase
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services; // External users services
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services; // IUserAccountService
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using NodaTime.Testing;
using System.Security.Claims;
using Xunit;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Services.ConditionsBuilder.Services;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class ApprovedInErrorUseCaseTests
{
    private readonly Mock<IAgentAuthorityInternalService> _agentAuthorityInternalService = new();
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _flaRepository = new();
    private readonly Mock<IActivityFeedItemProvider> _activityFeedItemProvider = new();
    private readonly Mock<IGetWoodlandOfficerReviewService> _getWoodlandOfficerReviewService = new();
    private readonly Mock<IApprovedInErrorService> _approvedInErrorService = new();
    private readonly Mock<IAuditService<FellingLicenceApplicationUseCase>> _auditService = new();
    private readonly Mock<IUserAccountService> _internalUserAccountService = new();
    private readonly Mock<IRetrieveUserAccountsService> _externalUserAccountService = new();
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerService = new();
    private readonly Mock<IAgentAuthorityService> _agentAuthorityService = new();
    private readonly Mock<IWoodlandOfficerReviewSubStatusService> _woodlandOfficerReviewSubStatusService = new();
    private readonly Mock<IOptions<FellingLicenceApplicationOptions>> _flaOptions = new();
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas = new();
    private readonly Mock<ILogger<FellingLicenceApplicationUseCase>> _logger = new();
    private readonly Mock<IApproverReviewUseCase> _approverReviewUseCase = new();
    private readonly Mock<IApproveRefuseOrReferApplicationUseCase> _approvalRefusalUseCase = new();
    private readonly Mock<IGeneratePdfApplicationUseCase> _generatePdfUseCase = new();
    private readonly Mock<IUpdateFellingLicenceApplication> _updateFellingLicenceService = new();
    private readonly Mock<ISendNotifications> _notificationsService = new();
    private readonly Mock<IOptions<ExternalApplicantSiteOptions>> _externalApplicantSiteOptions = new();
    private readonly RequestContext _requestContext = new("requestId", new RequestUserModel(new ClaimsPrincipal()));

    private ApprovedInErrorUseCase CreateSut()
    {
        _getConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync("admin hub address");
        _flaOptions.SetupGet(x => x.Value).Returns(new FellingLicenceApplicationOptions());
        _externalApplicantSiteOptions.SetupGet(x => x.Value).Returns(new ExternalApplicantSiteOptions { BaseUrl = "https://external.test/" });
        var clock = new FakeClock(SystemClock.Instance.GetCurrentInstant());

        return new ApprovedInErrorUseCase(
        _internalUserAccountService.Object,
        _externalUserAccountService.Object,
        _flaRepository.Object,
        _woodlandOwnerService.Object,
        _auditService.Object,
        _activityFeedItemProvider.Object,
        _agentAuthorityService.Object,
        _agentAuthorityInternalService.Object,
        _getWoodlandOfficerReviewService.Object,
        _approvedInErrorService.Object,
        _getConfiguredFcAreas.Object,
        _requestContext,
        _flaOptions.Object,
        _woodlandOfficerReviewSubStatusService.Object,
        clock,
        _logger.Object,
        _approverReviewUseCase.Object,
        _approvalRefusalUseCase.Object,
        _generatePdfUseCase.Object,
        _updateFellingLicenceService.Object,
        _notificationsService.Object,
        _externalApplicantSiteOptions.Object);
    }

    [Fact]
    public async Task RetrieveApprovedInErrorAsync_ShouldReturnNone_WhenApplicationNotFound()
    {
        // Arrange
        var sut = CreateSut();
        var applicationId = Guid.NewGuid();
        _flaRepository.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<Forestry.Flo.Services.FellingLicenceApplications.Entities.FellingLicenceApplication>.None);

        // Act
        var result = await sut.RetrieveApprovedInErrorAsync(applicationId, new InternalUser(new ClaimsPrincipal()), CancellationToken.None);

        // Assert
        Assert.True(result.HasNoValue);
        Mock.Get(_logger.Object).Verify(x => x.Log(
        LogLevel.Error,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("Felling licence application not found, application id:")),
        It.IsAny<Exception?>(),
        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task RetrieveApprovedInErrorAsync_ShouldReturnNone_WhenSummaryExtractionFails()
    {
        // Arrange
        var sut = CreateSut();
        var applicationId = Guid.NewGuid();
        var application = new Forestry.Flo.Services.FellingLicenceApplications.Entities.FellingLicenceApplication
        {
            Id = applicationId,
            ApplicationReference = "ABC/123",
            WoodlandOwnerId = Guid.NewGuid(),
            CreatedById = Guid.NewGuid(),
            CreatedTimestamp = DateTime.UtcNow
        };

        _flaRepository.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe.From(application));

        _woodlandOwnerService
        .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Failure<WoodlandOwnerModel>("Error"));

        // Act
        var result = await sut.RetrieveApprovedInErrorAsync(applicationId, new InternalUser(new ClaimsPrincipal()), CancellationToken.None);

        // Assert
        Assert.True(result.HasNoValue);
        Mock.Get(_logger.Object).Verify(x => x.Log(
        LogLevel.Error,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("Application summary cannot be extracted")),
        It.IsAny<Exception?>(),
        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task RetrieveApprovedInErrorAsync_ShouldMapApprovedInErrorData_WhenPresent()
    {
        // Arrange
        var sut = CreateSut();
        var applicationId = Guid.NewGuid();
        var application = new Forestry.Flo.Services.FellingLicenceApplications.Entities.FellingLicenceApplication
        {
            Id = applicationId,
            ApplicationReference = "ABC/123",
            WoodlandOwnerId = Guid.NewGuid(),
            CreatedById = Guid.NewGuid(),
            CreatedTimestamp = DateTime.UtcNow,
            SubmittedFlaPropertyDetail = new SubmittedFlaPropertyDetail
            {
                SubmittedFlaPropertyCompartments = []
            },
            LinkedPropertyProfile = new LinkedPropertyProfile
            {
                ProposedFellingDetails = []
            }
        };

        _flaRepository.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe.From(application));

        _woodlandOwnerService
        .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Success(new WoodlandOwnerModel { Id = Guid.NewGuid(), ContactName = "Owner" }));

        _agentAuthorityService
        .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<AgencyModel>.None);

        _approvedInErrorService
        .Setup(x => x.GetApprovedInErrorAsync(applicationId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe.From(new ApprovedInErrorModel
        {
            ApplicationId = applicationId,
            PreviousReference = "ABC/120",
            ReasonExpiryDate = true,
            ReasonSupplementaryPoints = false,
            ReasonOther = false,
            ReasonExpiryDateText = "note"
        }));

        _externalUserAccountService
        .Setup(x => x.RetrieveUserAccountEntityByIdAsync(application.CreatedById, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Success(new Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount
        {
            Email = "someone@example.com",
            FirstName = "A",
            LastName = "B"
        }));

        // Act
        var result = await sut.RetrieveApprovedInErrorAsync(applicationId, new InternalUser(new ClaimsPrincipal()), CancellationToken.None);

        // Assert
        Assert.True(result.HasValue);
        Assert.Equal("ABC/123", result.Value.PreviousReference);
        Assert.True(result.Value.ReasonExpiryDate);
        Assert.False(result.Value.ReasonSupplementaryPoints);
        Assert.False(result.Value.ReasonOther);
        Assert.Equal("note", result.Value.ReasonExpiryDateText);
    }

    [Fact]
    public async Task ConfirmApprovedInErrorAsync_ShouldFail_WhenApplicationNotFound()
    {
        // Arrange
        var sut = CreateSut();
        var model = new ApprovedInErrorModel { ApplicationId = Guid.NewGuid(), PreviousReference = "ABC/1" };
        var user = CreateInternalUser();

        _approvedInErrorService
        .Setup(x => x.SetToApprovedInErrorAsync(model.ApplicationId, model, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Failure("Felling licence application not found, application id: " + model.ApplicationId));

        // Act
        var result = await sut.ConfirmApprovedInErrorAsync(model, user, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Felling licence application not found", result.Error);
    }

    [Fact]
    public async Task ConfirmApprovedInErrorAsync_ShouldFail_WhenHideMostRecentApplicationDocumentFails()
    {
        // Arrange
        var sut = CreateSut();
        var appId = Guid.NewGuid();
        var user = CreateInternalUser();
        var model = new ApprovedInErrorModel { ApplicationId = appId, PreviousReference = "ABC/1" };

        _approvedInErrorService
        .Setup(x => x.SetToApprovedInErrorAsync(appId, model, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Failure("Failed to hide application document: hide-error"));

        // Act
        var result = await sut.ConfirmApprovedInErrorAsync(model, user, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Failed to hide application document", result.Error);
        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.ConfirmApprovedInErrorFailure), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmApprovedInErrorAsync_ShouldSucceed_AndPublishAuditEvent_WhenSaved()
    {
        // Arrange
        var sut = CreateSut();
        var appId = Guid.NewGuid();
        var user = CreateInternalUser();
        var model = new ApprovedInErrorModel { ApplicationId = appId, PreviousReference = "ABC/1" };

        _approvedInErrorService
        .Setup(x => x.SetToApprovedInErrorAsync(appId, model, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Success());

        // Act
        var result = await sut.ConfirmApprovedInErrorAsync(model, user, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.ConfirmApprovedInError), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmApprovedInErrorAsync_ShouldPublishFailureAudit_WhenSaveFails()
    {
        // Arrange
        var sut = CreateSut();
        var appId = Guid.NewGuid();
        var user = CreateInternalUser();
        var model = new ApprovedInErrorModel { ApplicationId = appId, PreviousReference = "ABC/1" };
        var docNew = new Document { CreatedTimestamp = DateTime.UtcNow, Purpose = DocumentPurpose.ApplicationDocument, FileName = "new.pdf", FileSize = 1, FileType = "pdf", MimeType = "application/pdf", FellingLicenceApplicationId = appId, FellingLicenceApplication = new Forestry.Flo.Services.FellingLicenceApplications.Entities.FellingLicenceApplication() };
        var application = new FellingLicenceApplication
        {
            Id = appId,
            ApplicationReference = "ABC/123",
            WoodlandOwnerId = Guid.NewGuid(),
            CreatedById = Guid.NewGuid(),
            CreatedTimestamp = DateTime.UtcNow,
            Documents = new List<Document> { docNew },
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
            }
        };

        _flaRepository.Setup(x => x.GetAsync(appId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe.From(application));

        _approvedInErrorService
        .Setup(x => x.SetToApprovedInErrorAsync(appId, model, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Failure("save-error"));

        // Act
        var result = await sut.ConfirmApprovedInErrorAsync(model, user, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.ConfirmApprovedInErrorFailure), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmApprovedInErrorAsync_ShouldFail_WhenApplicationStatusIsNotApproved()
    {
        // Arrange
        var sut = CreateSut();
        var appId = Guid.NewGuid();
        var user = CreateInternalUser();
        var model = new ApprovedInErrorModel { ApplicationId = appId, PreviousReference = "ABC/1" };

        _approvedInErrorService
        .Setup(x => x.SetToApprovedInErrorAsync(appId, model, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Failure("Application cannot be marked as approved in error as it is not in the approved state."));

        // Act
        var result = await sut.ConfirmApprovedInErrorAsync(model, user, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Application cannot be marked as approved in error as it is not in the approved state.", result.Error);
    }

    private static InternalUser CreateInternalUser(AccountTypeInternal? accountType = AccountTypeInternal.AccountAdministrator)
    {
        var claims = new List<Claim>
        {
            new(FloClaimTypes.LocalAccountId, Guid.NewGuid().ToString())
        };

        if (accountType.HasValue)
        {
            claims.Add(new Claim(FloClaimTypes.AccountType, accountType.Value.ToString()));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
        return new InternalUser(principal);
    }

    [Fact]
    public async Task ConfirmApprovedInErrorAsync_ShouldFail_WhenUserIsNotAccountAdministrator()
    {
        // Arrange
        var sut = CreateSut();
        var model = new ApprovedInErrorModel { ApplicationId = Guid.NewGuid(), PreviousReference = "ABC/1" };
        var user = CreateInternalUser(AccountTypeInternal.WoodlandOfficer); // Not an admin

        // Act
        var result = await sut.ConfirmApprovedInErrorAsync(model, user, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("You do not have permission to mark an application as approved in error.", result.Error);
    }
}
