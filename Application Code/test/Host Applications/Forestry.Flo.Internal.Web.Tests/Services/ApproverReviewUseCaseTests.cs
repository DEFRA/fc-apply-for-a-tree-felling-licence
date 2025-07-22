using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using System.Security.Claims;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class ApproverReviewUseCaseTests
{
    private readonly Mock<IAgentAuthorityInternalService> _agentAuthorityInternalService = new();
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationInternalRepository = new();
    private readonly Mock<IPropertyProfileRepository> _propertyProfileRepository = new();
    private readonly Mock<ILogger<FellingLicenceApplicationUseCase>> _logger = new();
    private readonly Mock<Flo.Services.InternalUsers.Repositories.IUserAccountRepository> _userAccountRepository = new();
    private readonly Mock<IActivityFeedItemProvider> _activityFeedItemProvider = new();
    private readonly Mock<IGetWoodlandOfficerReviewService> _getWoodlandOfficerReviewService = new();
    private readonly Mock<IApproverReviewService> _approverReviewService = new();
    private readonly Mock<IAuditService<FellingLicenceApplicationUseCase>> _auditService = new();
    private readonly Mock<IUserAccountService> _internalUserAccountService = new();
    private readonly Mock<IRetrieveUserAccountsService> _externalUserAccountService = new();
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerService = new();
    private readonly Mock<IAgentAuthorityService> _agentAuthorityService = new();
    private readonly RequestContext _requestContext = new("requestId", new RequestUserModel(new ClaimsPrincipal()));

    private readonly Mock<IOptions<FellingLicenceApplicationOptions> > _fellingLicenceApplicationOptions = new();
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas = new();
    private const string AdminHubAddress = "admin hub address";

    private ApproverReviewUseCase CreateSut()
    {
        _getConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);

        return new ApproverReviewUseCase(
            _internalUserAccountService.Object,
            _externalUserAccountService.Object,
            _fellingLicenceApplicationInternalRepository.Object,
            _propertyProfileRepository.Object,
            _woodlandOwnerService.Object,
            _auditService.Object,
            _userAccountRepository.Object,
            _activityFeedItemProvider.Object,
            _agentAuthorityService.Object,
            _agentAuthorityInternalService.Object,
            _getWoodlandOfficerReviewService.Object,
            _approverReviewService.Object,
            _getConfiguredFcAreas.Object,
            _requestContext,
            _fellingLicenceApplicationOptions.Object,
            _logger.Object);
    }

    [Fact]
    public async Task RetrieveApproverReviewAsync_ShouldReturnNone_WhenApplicationNotFound()
    {
        // Arrange
        var sut = CreateSut();
        var applicationId = Guid.NewGuid();
        var viewingUser = new InternalUser(new System.Security.Claims.ClaimsPrincipal());
        var cancellationToken = CancellationToken.None;

        _fellingLicenceApplicationInternalRepository
            .Setup(x => x.GetAsync(applicationId, cancellationToken))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        // Act
        var result = await sut.RetrieveApproverReviewAsync(applicationId, viewingUser, cancellationToken);

        // Assert
        Assert.True(result.HasNoValue);
        Mock.Get(_logger.Object).Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Felling licence application not found, application id:")),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task RetrieveApproverReviewAsync_ShouldReturnNone_WhenWoodlandOwnerNotFound()
    {
        // Arrange
        var sut = CreateSut();
        var applicationId = Guid.NewGuid();
        var viewingUser = new InternalUser(new System.Security.Claims.ClaimsPrincipal());
        var cancellationToken = CancellationToken.None;
        var application = new FellingLicenceApplication { Id = applicationId, WoodlandOwnerId = Guid.NewGuid() };

        _fellingLicenceApplicationInternalRepository
            .Setup(x => x.GetAsync(applicationId, cancellationToken))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _woodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, cancellationToken))
            .ReturnsAsync(Result.Failure<WoodlandOwnerModel>("Error"));

        // Act
        var result = await sut.RetrieveApproverReviewAsync(applicationId, viewingUser, cancellationToken);

        // Assert
        Assert.True(result.HasNoValue);
        Mock.Get(_logger.Object).Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Application woodland owner not found, application id:") &&
                                           v.ToString().Contains(applicationId.ToString()) &&
                                           v.ToString().Contains(application.WoodlandOwnerId.ToString()) &&
                                           v.ToString().Contains("Error")),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task SaveApproverReviewAsync_ShouldReturnSuccess_WhenUpdateIsSuccessful()
    {
        // Arrange
        var sut = CreateSut();
        var model = new ApproverReviewModel { ApplicationId = Guid.NewGuid() };
        var user = new InternalUser(new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(FloClaimTypes.LocalAccountId, Guid.NewGuid().ToString())
        })));
        var cancellationToken = CancellationToken.None;

        _approverReviewService
            .Setup(x => x.SaveApproverReviewAsync(model.ApplicationId, model, user.UserAccountId!.Value, cancellationToken))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await sut.SaveApproverReviewAsync(model, user, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.SaveApproverReview), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task SaveApproverReviewAsync_ShouldReturnFailure_WhenUpdateFails()
    {
        // Arrange
        var sut = CreateSut();
        var model = new ApproverReviewModel { ApplicationId = Guid.NewGuid() };
        var user = new InternalUser(new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(FloClaimTypes.LocalAccountId, Guid.NewGuid().ToString())
        })));
        var cancellationToken = CancellationToken.None;
        var errorMessage = "Error";

        _approverReviewService
            .Setup(x => x.SaveApproverReviewAsync(model.ApplicationId, model, user.UserAccountId!.Value, cancellationToken))
            .ReturnsAsync(Result.Failure(errorMessage));

        // Act
        var result = await sut.SaveApproverReviewAsync(model, user, cancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessage, result.Error);
        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.SaveApproverReviewFailure), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task DeleteApproverReviewAsync_ShouldReturnSuccess_WhenDeleteIsSuccessful()
    {
        // Arrange
        var sut = CreateSut();
        var applicationId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _approverReviewService
            .Setup(x => x.DeleteApproverReviewAsync(applicationId, cancellationToken))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _approverReviewService.Object.DeleteApproverReviewAsync(applicationId, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        _approverReviewService.Verify(x => x.DeleteApproverReviewAsync(applicationId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task DeleteApproverReviewAsync_ShouldReturnFailure_WhenDeleteFails()
    {
        // Arrange
        var sut = CreateSut();
        var applicationId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var errorMessage = "Delete failed";

        _approverReviewService
            .Setup(x => x.DeleteApproverReviewAsync(applicationId, cancellationToken))
            .ReturnsAsync(Result.Failure(errorMessage));

        // Act
        var result = await _approverReviewService.Object.DeleteApproverReviewAsync(applicationId, cancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessage, result.Error);
        _approverReviewService.Verify(x => x.DeleteApproverReviewAsync(applicationId, cancellationToken), Times.Once);
    }
}
