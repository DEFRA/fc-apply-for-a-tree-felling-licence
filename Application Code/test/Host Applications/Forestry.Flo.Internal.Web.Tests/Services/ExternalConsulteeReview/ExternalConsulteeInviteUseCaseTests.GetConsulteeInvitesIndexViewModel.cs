using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using FluentEmail.Core;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.ExternalConsulteeReview;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using NodaTime.Testing;
using System.Text.Json;

namespace Forestry.Flo.Internal.Web.Tests.Services.ExternalConsulteeReview;

public partial class ExternalConsulteeInviteUseCaseTests
{
    private const int InviteTokenExpiryDays = 5;
    private readonly Mock<IUserAccountService> _internalUserAccountService;
    private readonly Mock<IRetrieveUserAccountsService> _externalUserAccountService;
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _internalUserContextFlaRepository;
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerService;
    private readonly Mock<ISendNotifications> _emailService;
    private readonly Mock<IAuditService<ExternalConsulteeInviteUseCase>> _auditService;
    private readonly Mock<IAgentAuthorityService> _mockAgentAuthorityService;
    private readonly IClock _fakeClock;
    private readonly Mock<IOptions<UserInviteOptions>> _userInviteOptions;
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas;
    private readonly Mock<IUpdateWoodlandOfficerReviewService> _mockUpdateWoodlandOfficerReviewService;
    private readonly Mock<IExternalConsulteeReviewService> _mockExternalConsulteeReviewService;
    private readonly InternalUser _testUser;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Fixture _fixture;

    public ExternalConsulteeInviteUseCaseTests()
    {
        _fixture = new Fixture();
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: Guid.NewGuid(),
            accountTypeInternal: AccountTypeInternal.AdminOfficer);
        _testUser = new InternalUser(userPrincipal);

         _emailService = new Mock<ISendNotifications>();
         _auditService = new Mock<IAuditService<ExternalConsulteeInviteUseCase>>();
         _fakeClock = new FakeClock(Instant.FromDateTimeUtc(DateTime.UtcNow));
         
         _userInviteOptions = new Mock<IOptions<UserInviteOptions>>();
         _userInviteOptions.Setup(c => c.Value).Returns(new UserInviteOptions { InviteLinkExpiryDays = InviteTokenExpiryDays });

         _getConfiguredFcAreas = new Mock<IGetConfiguredFcAreas>();

        _internalUserAccountService = new Mock<IUserAccountService>();
         _externalUserAccountService = new Mock<IRetrieveUserAccountsService>();
         _woodlandOwnerService = new Mock<IRetrieveWoodlandOwners>();
         _internalUserContextFlaRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
         _mockAgentAuthorityService = new();

        _mockUpdateWoodlandOfficerReviewService = new Mock<IUpdateWoodlandOfficerReviewService>();
        _mockExternalConsulteeReviewService = new();
    }

    private ExternalConsulteeInviteUseCase CreateSut()
    {
         var logger = new NullLogger<ExternalConsulteeInviteUseCase>();
         _externalUserAccountService.Setup(r => r.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(_fixture.Create<UserAccount>()));
        _woodlandOwnerService.Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(_fixture.Create<WoodlandOwnerModel>()));
        _internalUserAccountService.Setup(s => s.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(_fixture.Create<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>()));

        return new ExternalConsulteeInviteUseCase(
            _internalUserAccountService.Object,
            _externalUserAccountService.Object,
            _internalUserContextFlaRepository.Object,
            _woodlandOwnerService.Object,
            _emailService.Object,
            _auditService.Object,
            _mockAgentAuthorityService.Object,
            _getConfiguredFcAreas.Object,
            _mockUpdateWoodlandOfficerReviewService.Object,
            _mockExternalConsulteeReviewService.Object,
            logger,
            _fakeClock,
            _userInviteOptions.Object,
            new RequestContext("test", new RequestUserModel(_testUser.Principal)));
    }

    [Theory, AutoData]
    public async Task WhenApplicationNotFound(Guid applicationId)
    {
        var sut = CreateSut();

        _internalUserContextFlaRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var (isSuccess, error, _) = await sut.GetConsulteeInvitesIndexViewModelAsync(applicationId, CancellationToken.None);

        Assert.False(isSuccess);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _woodlandOwnerService.VerifyNoOtherCalls();
        _mockAgentAuthorityService.VerifyNoOtherCalls();
        _externalUserAccountService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
    }

    // ExtractApplicationSummaryAsync failure scenarios assumed to be tested elsewhere

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasNoExistingLinksOrWoReview(
        Guid applicationId,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner)
    {
        application.ExternalAccessLinks = [];
        application.WoodlandOfficerReview = null;
        application.AssigneeHistories = [];
        application.LinkedPropertyProfile.ProposedFellingDetails = [];

        var sut = CreateSut();

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _woodlandOwnerService
            .Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _mockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        var (isSuccess, error, model) = await sut.GetConsulteeInvitesIndexViewModelAsync(applicationId, CancellationToken.None);

        Assert.True(isSuccess);

        Assert.Equal(applicationId, model.ApplicationId);
        Assert.Empty(model.InviteLinks);
        Assert.Null(model.ApplicationNeedsConsultations);
        Assert.False(model.ConsultationsComplete);
        Assert.Equal(_fakeClock.GetCurrentInstant().ToDateTimeUtc(), model.CurrentDateTimeUtc);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _woodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once());
        _woodlandOwnerService.VerifyNoOtherCalls();

        _mockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgentAuthorityService.VerifyNoOtherCalls();
        
        _externalUserAccountService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasNoExistingLinksAndNoConsultationsNeeded(
        Guid applicationId,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner)
    {
        application.ExternalAccessLinks = [];
        application.WoodlandOfficerReview.ApplicationNeedsConsultations = false;
        application.WoodlandOfficerReview.ConsultationsComplete = false;
        application.AssigneeHistories = [];
        application.LinkedPropertyProfile.ProposedFellingDetails = [];

        var sut = CreateSut();

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _woodlandOwnerService
            .Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _mockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        var (isSuccess, error, model) = await sut.GetConsulteeInvitesIndexViewModelAsync(applicationId, CancellationToken.None);

        Assert.True(isSuccess);

        Assert.Equal(applicationId, model.ApplicationId);
        Assert.Empty(model.InviteLinks);
        Assert.False(model.ApplicationNeedsConsultations);
        Assert.False(model.ConsultationsComplete);
        Assert.Equal(_fakeClock.GetCurrentInstant().ToDateTimeUtc(), model.CurrentDateTimeUtc);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _woodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once());
        _woodlandOwnerService.VerifyNoOtherCalls();

        _mockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgentAuthorityService.VerifyNoOtherCalls();

        _externalUserAccountService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasNoExistingLinksAndConsultationsNeeded(
        Guid applicationId,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner)
    {
        application.ExternalAccessLinks = [];
        application.WoodlandOfficerReview.ApplicationNeedsConsultations = true;
        application.WoodlandOfficerReview.ConsultationsComplete = false;
        application.AssigneeHistories = [];
        application.LinkedPropertyProfile.ProposedFellingDetails = [];

        var sut = CreateSut();

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _woodlandOwnerService
            .Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _mockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        var (isSuccess, error, model) = await sut.GetConsulteeInvitesIndexViewModelAsync(applicationId, CancellationToken.None);

        Assert.True(isSuccess);

        Assert.Equal(applicationId, model.ApplicationId);
        Assert.Empty(model.InviteLinks);
        Assert.True(model.ApplicationNeedsConsultations);
        Assert.False(model.ConsultationsComplete);
        Assert.Equal(_fakeClock.GetCurrentInstant().ToDateTimeUtc(), model.CurrentDateTimeUtc);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _woodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once());
        _woodlandOwnerService.VerifyNoOtherCalls();

        _mockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgentAuthorityService.VerifyNoOtherCalls();

        _externalUserAccountService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasExistingLinksAndNotCompleteYet(
        Guid applicationId,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner)
    {
        application.WoodlandOfficerReview.ApplicationNeedsConsultations = true;
        application.WoodlandOfficerReview.ConsultationsComplete = false;
        application.ConsulteeComments.ForEach(x => x.AccessCode = application.ExternalAccessLinks.First().AccessCode);
        application.AssigneeHistories = [];
        application.LinkedPropertyProfile.ProposedFellingDetails = [];

        var expectedInviteLinks =
            ModelMapping.ToExternalInviteLinkList(application.ExternalAccessLinks, application.ConsulteeComments);

        var sut = CreateSut();

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _woodlandOwnerService
            .Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _mockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        var (isSuccess, error, model) = await sut.GetConsulteeInvitesIndexViewModelAsync(applicationId, CancellationToken.None);

        Assert.True(isSuccess);

        Assert.Equal(applicationId, model.ApplicationId);
        Assert.Equivalent(expectedInviteLinks, model.InviteLinks);
        Assert.True(model.ApplicationNeedsConsultations);
        Assert.False(model.ConsultationsComplete);
        Assert.Equal(_fakeClock.GetCurrentInstant().ToDateTimeUtc(), model.CurrentDateTimeUtc);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _woodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once());
        _woodlandOwnerService.VerifyNoOtherCalls();

        _mockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgentAuthorityService.VerifyNoOtherCalls();

        _externalUserAccountService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasExistingLinksAndConsultationsAreComplete(
        Guid applicationId,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner)
    {
        application.WoodlandOfficerReview.ApplicationNeedsConsultations = true;
        application.WoodlandOfficerReview.ConsultationsComplete = true;
        application.ConsulteeComments.ForEach(x => x.AccessCode = application.ExternalAccessLinks.First().AccessCode);
        application.AssigneeHistories = [];
        application.LinkedPropertyProfile.ProposedFellingDetails = [];

        var expectedInviteLinks =
            ModelMapping.ToExternalInviteLinkList(application.ExternalAccessLinks, application.ConsulteeComments);

        var sut = CreateSut();

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _woodlandOwnerService
            .Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _mockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        var (isSuccess, error, model) = await sut.GetConsulteeInvitesIndexViewModelAsync(applicationId, CancellationToken.None);

        Assert.True(isSuccess);

        Assert.Equal(applicationId, model.ApplicationId);
        Assert.Equivalent(expectedInviteLinks, model.InviteLinks);
        Assert.True(model.ApplicationNeedsConsultations);
        Assert.True(model.ConsultationsComplete);
        Assert.Equal(_fakeClock.GetCurrentInstant().ToDateTimeUtc(), model.CurrentDateTimeUtc);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _woodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once());
        _woodlandOwnerService.VerifyNoOtherCalls();

        _mockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgentAuthorityService.VerifyNoOtherCalls();

        _externalUserAccountService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
    }
}