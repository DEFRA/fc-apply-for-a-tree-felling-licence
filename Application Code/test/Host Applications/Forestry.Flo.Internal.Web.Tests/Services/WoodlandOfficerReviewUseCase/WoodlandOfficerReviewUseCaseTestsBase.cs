using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Moq;
using NodaTime;
using System.Text.Json;
using AutoFixture;
using Forestry.Flo.Services.Applicants.Services;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public abstract class WoodlandOfficerReviewUseCaseTestsBase<T>
{
    protected readonly Mock<IGetWoodlandOfficerReviewService> WoodlandOfficerReviewService = new();
    protected readonly Mock<IRetrieveUserAccountsService> ExternalUserAccountRepository = new();
    protected readonly Mock<IUserAccountService> InternalUserAccountService = new();
    protected readonly Mock<IFellingLicenceApplicationInternalRepository> FlaRepository = new();
    protected readonly Mock<IRetrieveWoodlandOwners> WoodlandOwnerService = new();
    protected readonly Mock<INotificationHistoryService> NotificationHistoryService = new();
    protected readonly Mock<IUpdateWoodlandOfficerReviewService> UpdateWoodlandOfficerReviewService = new();
    protected readonly Mock<IAuditService<T>> AuditingService = new();
    protected readonly Mock<IAgentAuthorityService> MockAgentAuthorityService = new();

    protected readonly Mock<IAddDocumentService> MockAddDocumentService = new();
    protected readonly Mock<IRemoveDocumentService> MockRemoveDocumentService = new();

    protected readonly Mock<IPublicRegister> PublicRegisterService = new();
    protected readonly string RequestContextCorrelationId = Guid.NewGuid().ToString();
    protected readonly Guid RequestContextUserId = Guid.NewGuid();
    protected readonly Mock<IClock> Clock = new();
    protected readonly Instant Now = Instant.FromDateTimeUtc(DateTime.UtcNow);
    protected readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    protected readonly Mock<IActivityFeedItemProvider> ActivityFeedItemProvider = new();
    protected readonly Mock<ICreateApplicationSnapshotDocumentService> CreateApplicationDocument = new();
    protected readonly Mock<IForestryServices>  _forestryServices = new();
    protected readonly Mock<IForesterServices> _foresterServices = new();
    protected readonly Mock<ISendNotifications> NotificationService = new();

    protected readonly Mock<IGetConfiguredFcAreas> GetConfiguredFcAreas = new();

    protected readonly WoodlandOfficerReviewOptions WoodlandOfficerReviewOptions = new WoodlandOfficerReviewOptions
    {
        PublicRegisterPeriod = TimeSpan.FromDays(30)
    };

    protected readonly Fixture Fixture = new();

    protected RequestContext RequestContext;

    protected readonly string AdminHubAddress = "admin hub address";

    protected void ResetMocks()
    {
        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: RequestContextUserId,
            accountTypeInternal: AccountTypeInternal.WoodlandOfficer);
        RequestContext = new RequestContext(
            RequestContextCorrelationId,
            new RequestUserModel(user));

        AuditingService.Reset();
        WoodlandOfficerReviewService.Reset();
        ExternalUserAccountRepository.Reset();
        InternalUserAccountService.Reset();
        FlaRepository.Reset();
        WoodlandOwnerService.Reset();
        NotificationHistoryService.Reset();
        UpdateWoodlandOfficerReviewService.Reset();
        PublicRegisterService.Reset();
        _forestryServices.Reset();
        _foresterServices.Reset();
        Clock.Reset();
        Clock.Setup(x => x.GetCurrentInstant()).Returns(Now);
        NotificationService.Reset();
        MockAgentAuthorityService.Reset();
        GetConfiguredFcAreas.Reset();
        MockAddDocumentService.Reset();
        MockRemoveDocumentService.Reset();

        GetConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);
    }
}