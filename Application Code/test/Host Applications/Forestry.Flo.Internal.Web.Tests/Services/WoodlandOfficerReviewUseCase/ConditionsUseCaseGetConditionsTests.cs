using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using WoodlandOwnerModel = Forestry.Flo.Services.Applicants.Models.WoodlandOwnerModel;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class ConditionsUseCaseGetConditionsTests
{
    private readonly Mock<IGetWoodlandOfficerReviewService> _getWoodlandOfficerReviewService = new();
    private readonly Mock<ICalculateConditions> _conditionsService = new();
    private readonly Mock<IUpdateWoodlandOfficerReviewService> _updateWoodlandOfficerReviewService = new();
    private readonly Mock<IAuditService<ConditionsUseCase>> _auditService = new();
    private readonly Mock<IUpdateConfirmedFellingAndRestockingDetailsService> _fellingAndRestockingService = new();

    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationInternalRepository = new();
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerService = new();
    private readonly Mock<IUserAccountService> _internalUserAccountService = new();
    private readonly Mock<IRetrieveUserAccountsService> _externalApplicantRepository = new();
    private readonly Mock<IAgentAuthorityService> _agentAuthorityService = new();

    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas = new();
    private const string AdminHubAddress = "admin hub address";

    private readonly string RequestContextCorrelationId = Guid.NewGuid().ToString();
    private readonly Guid RequestContextUserId = Guid.NewGuid();

    [Theory, AutoData]
    public async Task WhenCannotRetrieveConditionsStatus(
        Guid applicationId,
        Guid userId,
        string error)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        _getWoodlandOfficerReviewService
            .Setup(x => x.GetConditionsStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ConditionsStatusModel>(error));

        var result = await sut.GetConditionsAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getWoodlandOfficerReviewService.Verify(x => x.GetConditionsStatusAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _conditionsService.VerifyNoOtherCalls();
        
        _internalUserAccountService.VerifyNoOtherCalls();
        _externalApplicantRepository.VerifyNoOtherCalls();
        _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();
        _woodlandOwnerService.VerifyNoOtherCalls();

        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenCannotRetrieveWoodlandOfficerReviewStatus(
        Guid applicationId,
        Guid userId,
        ConditionsStatusModel conditionsStatus,
        string error)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        _getWoodlandOfficerReviewService
            .Setup(x => x.GetConditionsStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(conditionsStatus));
        _getWoodlandOfficerReviewService
            .Setup(x => x.GetWoodlandOfficerReviewStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WoodlandOfficerReviewStatusModel>(error));
        _conditionsService
            .Setup(x => x.RetrieveExistingConditionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConditionsResponse());

        var result = await sut.GetConditionsAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getWoodlandOfficerReviewService.Verify(x => x.GetConditionsStatusAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getWoodlandOfficerReviewService.Verify(x => x.GetWoodlandOfficerReviewStatusAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _conditionsService.Verify(x => x.RetrieveExistingConditionsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _conditionsService.VerifyNoOtherCalls();

        _internalUserAccountService.VerifyNoOtherCalls();
        _externalApplicantRepository.VerifyNoOtherCalls();
        _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();
        _woodlandOwnerService.VerifyNoOtherCalls();

        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenCannotRetrieveApplicationSummary(
        Guid applicationId,
        Guid userId,
        ConditionsStatusModel conditionsStatus,
        WoodlandOfficerReviewStatusModel woodlandOfficerReviewStatus)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        _getWoodlandOfficerReviewService
            .Setup(x => x.GetConditionsStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(conditionsStatus));
        _getWoodlandOfficerReviewService
            .Setup(x => x.GetWoodlandOfficerReviewStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOfficerReviewStatus));
        _conditionsService
            .Setup(x => x.RetrieveExistingConditionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConditionsResponse());
        _fellingLicenceApplicationInternalRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.GetConditionsAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getWoodlandOfficerReviewService.Verify(x => x.GetConditionsStatusAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getWoodlandOfficerReviewService.Verify(x => x.GetWoodlandOfficerReviewStatusAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _conditionsService.Verify(x => x.RetrieveExistingConditionsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _conditionsService.VerifyNoOtherCalls();

        _internalUserAccountService.VerifyNoOtherCalls();
        _externalApplicantRepository.VerifyNoOtherCalls();
        _fellingLicenceApplicationInternalRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();
        _woodlandOwnerService.VerifyNoOtherCalls();

        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenFellingAndRestockingIsComplete(
        Guid applicationId,
        Guid userId,
        ConditionsStatusModel conditionsStatus,
        WoodlandOfficerReviewStatusModel woodlandOfficerReviewStatus,
        List<CalculatedCondition> conditions,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicant,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        woodlandOfficerReviewStatus.WoodlandOfficerReviewTaskListStates = new WoodlandOfficerReviewTaskListStates(
            InternalReviewStepStatus.Completed,
            InternalReviewStepStatus.Completed,
            InternalReviewStepStatus.Completed,
            InternalReviewStepStatus.Completed,
            InternalReviewStepStatus.InProgress,
            InternalReviewStepStatus.NotStarted,
            InternalReviewStepStatus.NotStarted,
            InternalReviewStepStatus.NotStarted,
            InternalReviewStepStatus.NotStarted);

        _getWoodlandOfficerReviewService
            .Setup(x => x.GetConditionsStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(conditionsStatus));
        _getWoodlandOfficerReviewService
            .Setup(x => x.GetWoodlandOfficerReviewStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOfficerReviewStatus));
        _conditionsService
            .Setup(x => x.RetrieveExistingConditionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConditionsResponse { Conditions = conditions });
        _fellingLicenceApplicationInternalRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));
        _woodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));
        _externalApplicantRepository
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(externalApplicant));
        _internalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(internalUser));

        var result = await sut.GetConditionsAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(result.Value.Conditions, conditions);
        Assert.True(result.Value.ConfirmedFellingAndRestockingComplete);
        Assert.Equal(applicationId, result.Value.ApplicationId);
        Assert.Equal(conditionsStatus, result.Value.ConditionsStatus);

        _getWoodlandOfficerReviewService.Verify(x => x.GetConditionsStatusAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getWoodlandOfficerReviewService.Verify(x => x.GetWoodlandOfficerReviewStatusAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _conditionsService.Verify(x => x.RetrieveExistingConditionsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _conditionsService.VerifyNoOtherCalls();

        _fellingLicenceApplicationInternalRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenFellingAndRestockingIsIncomplete(
        Guid applicationId,
        Guid userId,
        ConditionsStatusModel conditionsStatus,
        WoodlandOfficerReviewStatusModel woodlandOfficerReviewStatus,
        List<CalculatedCondition> conditions,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicant,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        woodlandOfficerReviewStatus.WoodlandOfficerReviewTaskListStates = new WoodlandOfficerReviewTaskListStates(
            InternalReviewStepStatus.Completed,
            InternalReviewStepStatus.Completed,
            InternalReviewStepStatus.Completed,
            InternalReviewStepStatus.InProgress,
            InternalReviewStepStatus.InProgress,
            InternalReviewStepStatus.NotStarted,
            InternalReviewStepStatus.NotStarted,
            InternalReviewStepStatus.NotStarted,
            InternalReviewStepStatus.NotStarted);

        _getWoodlandOfficerReviewService
            .Setup(x => x.GetConditionsStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(conditionsStatus));
        _getWoodlandOfficerReviewService
            .Setup(x => x.GetWoodlandOfficerReviewStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOfficerReviewStatus));
        _conditionsService
            .Setup(x => x.RetrieveExistingConditionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConditionsResponse { Conditions = conditions });
        _fellingLicenceApplicationInternalRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));
        _woodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));
        _externalApplicantRepository
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(externalApplicant));
        _internalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(internalUser));

        var result = await sut.GetConditionsAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(result.Value.Conditions, conditions);
        Assert.False(result.Value.ConfirmedFellingAndRestockingComplete);
        Assert.Equal(applicationId, result.Value.ApplicationId);
        Assert.Equal(conditionsStatus, result.Value.ConditionsStatus);

        _getWoodlandOfficerReviewService.Verify(x => x.GetConditionsStatusAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getWoodlandOfficerReviewService.Verify(x => x.GetWoodlandOfficerReviewStatusAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _conditionsService.Verify(x => x.RetrieveExistingConditionsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _conditionsService.VerifyNoOtherCalls();

        _fellingLicenceApplicationInternalRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

        _auditService.VerifyNoOtherCalls();
    }

    private ConditionsUseCase CreateSut()
    {
        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: RequestContextUserId,
            accountTypeInternal: AccountTypeInternal.WoodlandOfficer);
        var requestContext = new RequestContext(
            RequestContextCorrelationId,
            new RequestUserModel(user));

        _getWoodlandOfficerReviewService.Reset();
        _agentAuthorityService.Reset();
        _conditionsService.Reset();
        _updateWoodlandOfficerReviewService.Reset();
        _auditService.Reset();
        _fellingAndRestockingService.Reset();

        _fellingLicenceApplicationInternalRepository.Reset();
        _internalUserAccountService.Reset();
        _externalApplicantRepository.Reset();
        _woodlandOwnerService.Reset();
        _getConfiguredFcAreas.Reset();

        _getConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);

        return new ConditionsUseCase(
            _internalUserAccountService.Object,
            _externalApplicantRepository.Object,
            _fellingLicenceApplicationInternalRepository.Object,
            _woodlandOwnerService.Object,
            _getWoodlandOfficerReviewService.Object,
            _updateWoodlandOfficerReviewService.Object,
            _auditService.Object,
            requestContext,
            _conditionsService.Object,
            _fellingAndRestockingService.Object,
            new Mock<ISendNotifications>().Object,
            _agentAuthorityService.Object,
            _getConfiguredFcAreas.Object,
            new Mock<IClock>().Object,
            new OptionsWrapper<ExternalApplicantSiteOptions>(new ExternalApplicantSiteOptions()),
            new NullLogger<ConditionsUseCase>());
    }
}