using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.ApproverReview;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class ApproverReviewUseCaseTests
{
    private readonly Mock<IAgentAuthorityInternalService> _agentAuthorityInternalService = new();
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationInternalRepository = new();
    private readonly Mock<ILogger<FellingLicenceApplicationUseCase>> _logger = new();
    private readonly Mock<IActivityFeedItemProvider> _activityFeedItemProvider = new();
    private readonly Mock<IGetWoodlandOfficerReviewService> _getWoodlandOfficerReviewService = new();
    private readonly Mock<IApproverReviewService> _approverReviewService = new();
    private readonly Mock<IAuditService<FellingLicenceApplicationUseCase>> _auditService = new();
    private readonly Mock<IUserAccountService> _internalUserAccountService = new();
    private readonly Mock<IRetrieveUserAccountsService> _externalUserAccountService = new();
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerService = new();
    private readonly Mock<IAgentAuthorityService> _agentAuthorityService = new();
    private readonly Mock<IWoodlandOfficerReviewSubStatusService> _woodlandOfficerReviewSubStatusService = new();
    private readonly RequestContext _requestContext = new("requestId", new RequestUserModel(new ClaimsPrincipal()));

    private readonly Mock<IOptions<FellingLicenceApplicationOptions>> _fellingLicenceApplicationOptions = new();
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas = new();
    private const string AdminHubAddress = "admin hub address";

    private ApproverReviewUseCase CreateSut()
    {
        _getConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);

        // Setup default FellingLicenceApplicationOptions to prevent NullReferenceException
        _fellingLicenceApplicationOptions
            .Setup(x => x.Value)
            .Returns(new FellingLicenceApplicationOptions
            {
                DefaultLicenseDuration = 5
            });

        return new ApproverReviewUseCase(
            _internalUserAccountService.Object,
            _externalUserAccountService.Object,
            _fellingLicenceApplicationInternalRepository.Object,
            _woodlandOwnerService.Object,
            _auditService.Object,
            _activityFeedItemProvider.Object,
            _agentAuthorityService.Object,
            _agentAuthorityInternalService.Object,
            _getWoodlandOfficerReviewService.Object,
            _approverReviewService.Object,
            _getConfiguredFcAreas.Object,
            _requestContext,
            _fellingLicenceApplicationOptions.Object,
            _woodlandOfficerReviewSubStatusService.Object,
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
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), cancellationToken))
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

    [Theory]
    [InlineData(true, 5)]
    [InlineData(false, 5)]
    [InlineData(null, 5)]
    public async Task RetrieveApproverReviewAsync_ShouldSetDefaultRecommendedLicenceDuration_BasedOnIsForTenYearLicence(
        bool? isForTenYearLicence, 
        int defaultLicenseDuration)
    {
        // Arrange
        var sut = CreateSut();
        var applicationId = Guid.NewGuid();
        var woodlandOwnerId = Guid.NewGuid();
        var createdById = Guid.NewGuid();
        var viewingUserId = Guid.NewGuid();
        var viewingUser = new InternalUser(new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(FloClaimTypes.LocalAccountId, viewingUserId.ToString())
        })));
        var cancellationToken = CancellationToken.None;
        
        var application = new FellingLicenceApplication 
        { 
            Id = applicationId, 
            WoodlandOwnerId = woodlandOwnerId,
            CreatedById = createdById,
            IsForTenYearLicence = isForTenYearLicence,
            ApplicationReference = "TEST-001",
            AdministrativeRegion = "Test Region",
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.SentForApproval, Created = DateTime.UtcNow }
            },
            AssigneeHistories = new List<AssigneeHistory>
            {
                new AssigneeHistory 
                { 
                    Role = AssignedUserRole.FieldManager, 
                    AssignedUserId = viewingUserId,
                    TimestampUnassigned = null
                }
            },
            SubmittedFlaPropertyDetail = new SubmittedFlaPropertyDetail
            {
                SubmittedFlaPropertyCompartments = []
            },
            LinkedPropertyProfile = new LinkedPropertyProfile
            {
                ProposedFellingDetails = []
            }
        };

        var woodlandOwner = new WoodlandOwnerModel
        {
            Id = woodlandOwnerId,
            ContactName = "Test Owner",
            ContactEmail = "test@example.com"
        };

        var woodlandOfficerReview = new WoodlandOfficerReviewStatusModel
        {
            // No recommended licence duration set
            RecommendedLicenceDuration = null,
            WoodlandOfficerReviewTaskListStates = new WoodlandOfficerReviewTaskListStates(
                PublicRegisterStepStatus: InternalReviewStepStatus.Completed,
                SiteVisitStepStatus: InternalReviewStepStatus.Completed,
                Pw14ChecksStepStatus: InternalReviewStepStatus.Completed,
                FellingAndRestockingStepStatus: InternalReviewStepStatus.Completed,
                ConditionsStepStatus: InternalReviewStepStatus.Completed,
                ConsultationStepStatus: InternalReviewStepStatus.Completed,
                CompartmentDesignationsStepStatus: InternalReviewStepStatus.Completed,
                LarchApplicationStatus: InternalReviewStepStatus.Completed,
                LarchFlyoverStatus: InternalReviewStepStatus.Completed,
                EiaScreeningStatus: InternalReviewStepStatus.Completed,
                TreeHealthStatus: InternalReviewStepStatus.Completed,
                PriorityOpenHabitatStepStatus: InternalReviewStepStatus.Completed,
                WoodlandOfficerReviewComplete: true)
        };

        var externalUser = new Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount
        {
            Email = "external@example.com"
        };

        var fellingLicenceApplicationOptions = new FellingLicenceApplicationOptions
        {
            DefaultLicenseDuration = defaultLicenseDuration
        };

        _fellingLicenceApplicationOptions
            .Setup(x => x.Value)
            .Returns(fellingLicenceApplicationOptions);

        _fellingLicenceApplicationInternalRepository
            .Setup(x => x.GetAsync(applicationId, cancellationToken))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _woodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(woodlandOwnerId, It.IsAny<UserAccessModel>(), cancellationToken))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _agentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(woodlandOwnerId, cancellationToken))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        _activityFeedItemProvider
            .Setup(x => x.RetrieveAllRelevantActivityFeedItemsAsync(It.IsAny<ActivityFeedItemProviderModel>(), ActorType.InternalUser, cancellationToken))
            .ReturnsAsync(Result.Success<IList<ActivityFeedItemModel>>(new List<ActivityFeedItemModel>()));

        _approverReviewService
            .Setup(x => x.GetApproverReviewAsync(applicationId, cancellationToken))
            .ReturnsAsync(Maybe<ApproverReviewModel>.None);

        _getWoodlandOfficerReviewService
            .Setup(x => x.GetWoodlandOfficerReviewStatusAsync(applicationId, cancellationToken))
            .ReturnsAsync(Result.Success(woodlandOfficerReview));

        _externalUserAccountService
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(createdById, cancellationToken))
            .ReturnsAsync(Result.Success(externalUser));

        _internalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), cancellationToken))
            .ReturnsAsync(Maybe.From(new Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test.internal@example.com"
            }));

        _woodlandOfficerReviewSubStatusService
            .Setup(x => x.GetCurrentSubStatuses(It.IsAny<FellingLicenceApplication>()))
            .Returns(new HashSet<WoodlandOfficerReviewSubStatus>());

        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), cancellationToken))
            .ReturnsAsync("Admin Hub Address");

        // Act
        var result = await sut.RetrieveApproverReviewAsync(applicationId, viewingUser, cancellationToken);

        // Assert
        Assert.True(result.HasValue);
        
        // Calculate expected duration based on isForTenYearLicence
        var expectedDuration = isForTenYearLicence == true
            ? RecommendedLicenceDuration.TenYear
            : (RecommendedLicenceDuration)defaultLicenseDuration;
            
        Assert.Equal(expectedDuration, result.Value.RecommendedLicenceDuration);
        Assert.Equal(expectedDuration, result.Value.ApproverReview.ApprovedLicenceDuration);
        
        // Verify the RecommendedLicenceDurations list contains the correct default marker
        var defaultItem = result.Value.RecommendedLicenceDurations
            .FirstOrDefault(x => x.Value == ((int)expectedDuration).ToString());
        Assert.NotNull(defaultItem);
        Assert.Contains("(default)", defaultItem.Text);
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
