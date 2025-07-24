using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using System.Text.Json;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class ConditionsUseCaseSaveConditionsTests
{
    private readonly Mock<IGetWoodlandOfficerReviewService> _getWoodlandOfficerReviewService = new();
    private readonly Mock<ICalculateConditions> _conditionsService = new();
    private readonly Mock<IUpdateWoodlandOfficerReviewService> _updateWoodlandOfficerReviewService = new();
    private readonly Mock<IAuditService<ConditionsUseCase>> _auditService = new();
    private readonly Mock<IUpdateConfirmedFellingAndRestockingDetailsService> _fellingAndRestockingService = new();
    private readonly Mock<IAgentAuthorityService> _agentAuthorityService = new();
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas = new();
    private const string AdminHubAddress = "admin hub address";

    private readonly string RequestContextCorrelationId = Guid.NewGuid().ToString();
    private readonly Guid RequestContextUserId = Guid.NewGuid();

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task WhenStoreConditionsFails(
        Guid applicationId,
        Guid userId,
        List<CalculatedCondition> conditions,
        string error)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        _conditionsService
            .Setup(x => x.StoreConditionsAsync(It.IsAny<StoreConditionsRequest>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.SaveConditionsAsync(applicationId, user, conditions, CancellationToken.None);

        Assert.True(result.IsFailure);

        _conditionsService.Verify(x => x.StoreConditionsAsync(
            It.Is<StoreConditionsRequest>(r => r.FellingLicenceApplicationId == applicationId && r.Conditions == conditions),
            userId, It.IsAny<CancellationToken>()), Times.Once);
        _conditionsService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Conditions",
                    error = error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenStoreConditionsSucceeds(
        Guid applicationId,
        Guid userId,
        List<CalculatedCondition> conditions)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        _conditionsService
            .Setup(x => x.StoreConditionsAsync(It.IsAny<StoreConditionsRequest>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.SaveConditionsAsync(applicationId, user, conditions, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _conditionsService.Verify(x => x.StoreConditionsAsync(
            It.Is<StoreConditionsRequest>(r => r.FellingLicenceApplicationId == applicationId && r.Conditions == conditions),
            userId, It.IsAny<CancellationToken>()), Times.Once);
        _conditionsService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Conditions"
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
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
        _conditionsService.Reset();
        _updateWoodlandOfficerReviewService.Reset();
        _auditService.Reset();
        _fellingAndRestockingService.Reset();
        _agentAuthorityService.Reset();
        _getConfiguredFcAreas.Reset();

        _getConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);

        return new ConditionsUseCase(
            new Mock<IUserAccountService>().Object,
            new Mock<IRetrieveUserAccountsService>().Object,
            new Mock<IFellingLicenceApplicationInternalRepository>().Object,
            new Mock<IRetrieveWoodlandOwners>().Object,
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