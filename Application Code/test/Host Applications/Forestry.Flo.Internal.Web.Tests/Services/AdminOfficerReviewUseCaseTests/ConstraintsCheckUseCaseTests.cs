using System.Text.Json;
using AutoFixture;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using WoodlandOwnerModel = Forestry.Flo.Services.Applicants.Models.WoodlandOwnerModel;

namespace Forestry.Flo.Internal.Web.Tests.Services.AdminOfficerReviewUseCaseTests;

public class ConstraintsCheckUseCaseTests
{
    private readonly ConstraintsCheckUseCase _sut;

    private readonly IFixture _fixture;
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly Mock<IUserAccountService> _internalUserAccountService;
    private readonly Mock<IClock> _clock;
    private readonly Mock<IAgentAuthorityInternalService> _agentAuthorityServiceMock = new();
    private readonly Mock<IRetrieveUserAccountsService> _retrieveUserAccountsServiceMock = new();
    private readonly Mock<IRetrieveWoodlandOwners> _retrieveWoodlandOwnersMock = new();
    private readonly Mock<IAuditService<AdminOfficerReviewUseCaseBase>> _auditServiceMock = new();
    private readonly Mock<IAgentAuthorityService> _agentAuthorityService = new();

    private readonly InternalUserContextFlaRepository _internalRepo;
    private readonly FellingLicenceApplicationsContext _fellingLicenceApplicationsContext;

    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas;
    private const string AdminHubAddress = "admin hub address";

    private FellingLicenceApplication _fellingLicenceApplication;
    private readonly WoodlandOwner _woodlandOwner;
    private readonly InternalUser _internalUser;
    private readonly UserAccount _applicantUser;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ConstraintsCheckUseCaseTests()
    {
        _fellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _internalRepo = new InternalUserContextFlaRepository(_fellingLicenceApplicationsContext);

        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: Guid.NewGuid(),
            accountTypeInternal: Flo.Services.Common.User.AccountTypeInternal.AdminOfficer);
        _internalUser = new InternalUser(userPrincipal);

        _fixture = new Fixture().CustomiseFixtureForFellingLicenceApplications();

        _applicantUser = _fixture.Build<UserAccount>()
            .With(x => x.AccountType, AccountTypeExternal.Agent)
            .Create();
        
        _internalUserAccountService = new Mock<IUserAccountService>();
        _clock = new Mock<IClock>();

        _woodlandOwner = _fixture.Build<WoodlandOwner>()
            .With(wo => wo.IsOrganisation, true)
            .With(wo => wo.Id, Guid.NewGuid)
            .Create();

        _getConfiguredFcAreas = new Mock<IGetConfiguredFcAreas>();
        _getConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);

        var woodlandOwnerModel = new WoodlandOwnerModel
        {
            Id = _woodlandOwner.Id,
            ContactAddress = _woodlandOwner.ContactAddress,
            ContactTelephone = _woodlandOwner.ContactTelephone,
            ContactEmail = _woodlandOwner.ContactEmail,
            ContactName = _woodlandOwner.ContactName,
            IsOrganisation = _woodlandOwner.IsOrganisation,
            OrganisationName = _woodlandOwner.OrganisationName,
            OrganisationAddress = _woodlandOwner.OrganisationAddress
        };

        _retrieveWoodlandOwnersMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _retrieveUserAccountsServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_applicantUser);

        _sut = CreateSut(Guid.NewGuid());
    }

    private ConstraintsCheckUseCase CreateSut(Guid userId)
    {
        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: userId,
            accountTypeInternal: AccountTypeInternal.WoodlandOfficer);

        return new ConstraintsCheckUseCase(
            _internalUserAccountService.Object,
            _retrieveUserAccountsServiceMock.Object,
            new NullLogger<ConstraintsCheckUseCase>(),
            _internalRepo,
            _retrieveWoodlandOwnersMock.Object,
            new UpdateAdminOfficerReviewService(_internalRepo, new NullLogger<UpdateAdminOfficerReviewService>(), _clock.Object),
            new GetFellingLicenceApplicationForInternalUsersService(_internalRepo, _clock.Object),
            _auditServiceMock.Object,
            _agentAuthorityService.Object,
            _getConfiguredFcAreas.Object,
            new RequestContext("test", new RequestUserModel(_internalUser.Principal)));
    }

    [Fact]
    public async Task ShouldReturnCorrectModel_WhenSuccessful()
    {
        _fellingLicenceApplication = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview);

        _fellingLicenceApplication.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync();

        var result = await _sut.GetConstraintsCheckModel(_fellingLicenceApplication.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.NotNull(result.Value);
        Assert.Equal(_fellingLicenceApplication.Id, result.Value.ApplicationId);
    }

    [Fact]
    public async Task ShouldReturnFailure_WhenFellingLicenceNotRetrieved()
    {
        _fellingLicenceApplication = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview);

        _fellingLicenceApplicationsContext.Remove(_fellingLicenceApplication);

        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.GetConstraintsCheckModel(_fellingLicenceApplication.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        
        _retrieveUserAccountsServiceMock.VerifyNoOtherCalls();
        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        _agentAuthorityServiceMock.VerifyNoOtherCalls();
    }

    [Theory, CombinatorialData]
    public async Task ShouldUpdateAoReviewEntityAndAudit(
        bool checkPassed)
    {
        _fellingLicenceApplication = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview, _internalUser.UserAccountId, _woodlandOwner.Id, true);

        if (_fellingLicenceApplication.AdminOfficerReview is not null)
        {
            _fellingLicenceApplication.AdminOfficerReview.AdminOfficerReviewComplete = false;

            await _fellingLicenceApplicationsContext.SaveEntitiesAsync();
        }

        var result = await _sut.CompleteConstraintsCheckAsync(
            _fellingLicenceApplication.Id,
            false,
            _internalUser.UserAccountId!.Value, 
            checkPassed,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var aoReviewEntity =
            _fellingLicenceApplicationsContext.AdminOfficerReviews.FirstOrDefault(x =>
                x.FellingLicenceApplicationId == _fellingLicenceApplication.Id);

        Assert.NotNull(aoReviewEntity);
        Assert.Equal(checkPassed, aoReviewEntity!.ConstraintsChecked);
            
        _auditServiceMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.UpdateAdminOfficerReview
                         && JsonSerializer.Serialize(e.AuditData, _serializerOptions) ==
                         JsonSerializer.Serialize(new
                         {
                             ReviewCompleted = true,
                             PerformingUserId = _internalUser.UserAccountId,
                         }, _serializerOptions)),
                CancellationToken.None
            ));
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureAndAudit_WhenApplicationNotFound(
        bool checkPassed)
    {
        _fellingLicenceApplication = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview, _internalUser.UserAccountId, _woodlandOwner.Id);

        var invalidId = Guid.NewGuid();

        var result = await _sut.CompleteConstraintsCheckAsync(
            invalidId,
            false,
            _internalUser.UserAccountId!.Value,
            checkPassed,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        Assert.Null(_fellingLicenceApplicationsContext.AdminOfficerReviews.FirstOrDefault(x => x.FellingLicenceApplicationId == invalidId));

        _auditServiceMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.UpdateAdminOfficerReviewFailure
                         && JsonSerializer.Serialize(e.AuditData, _serializerOptions) ==
                         JsonSerializer.Serialize(new
                         {
                             Error = "Unable to find an application with supplied id",
                             PerformingUserId = _internalUser.UserAccountId,
                         }, _serializerOptions)),
                CancellationToken.None
            ));
    }

    [Theory, CombinatorialData]
    public async Task ShouldReturnFailureAndAudit_WhenApplicationNotInRightState(
        bool correctStatus,
        bool reviewInProgress,
        bool userAssigned)
    {
        if (correctStatus && reviewInProgress && userAssigned)
        {
            return;
        }

        _fellingLicenceApplication = await CreateAndSaveAdminOfficerReviewApplicationAsync(
            correctStatus 
                ? FellingLicenceStatus.AdminOfficerReview 
                : FellingLicenceStatus.Draft,
            userAssigned 
                ? _internalUser.UserAccountId 
                : Guid.NewGuid(), 
            _woodlandOwner.Id);

        if (reviewInProgress is false)
        {
            var review = _fellingLicenceApplication.AdminOfficerReview ?? new AdminOfficerReview();
            review.AdminOfficerReviewComplete = true;
        }
        else
        {
            _fellingLicenceApplication.AdminOfficerReview = new AdminOfficerReview();
        }

        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.CompleteConstraintsCheckAsync(
            _fellingLicenceApplication.Id,
            false,
            _internalUser.UserAccountId!.Value,
            false,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        
        var error = correctStatus is false || reviewInProgress is false 
            ? "Cannot update admin officer review for application not in submitted state" 
            : "User is not the assigned admin officer";


        var deleteMe = _auditServiceMock.Invocations.First().Arguments[0] as AuditEvent;

        _auditServiceMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.UpdateAdminOfficerReviewFailure
                         && JsonSerializer.Serialize(e.AuditData, _serializerOptions) ==
                         JsonSerializer.Serialize(new
                         {
                             Error = error,
                             PerformingUserId = _internalUser.UserAccountId,
                         }, _serializerOptions)),
                CancellationToken.None
            ));
    }

    private async Task<FellingLicenceApplication> CreateAndSaveAdminOfficerReviewApplicationAsync(
        FellingLicenceStatus currentStatus, 
        Guid? performingUserId = null, 
        Guid? woodlandOfficerId = null, 
        bool preparedForConstraintsCheck = false)
    {
        var application = _fixture.Create<FellingLicenceApplication>();
        
        application.StatusHistories = new List<StatusHistory>
        {
            new()
            {
                Created = DateTime.UtcNow,
                CreatedById = Guid.NewGuid(),
                Status = currentStatus
            }
        };

        application.AssigneeHistories = new List<AssigneeHistory>();

        if (performingUserId is not null)
        {
            application.AssigneeHistories.Add(new AssigneeHistory
            {
                AssignedUserId = performingUserId.Value,
                TimestampAssigned = DateTime.UtcNow,
                TimestampUnassigned = null,
                Role = AssignedUserRole.AdminOfficer
            });
        }

        if (woodlandOfficerId is not null)
        {
            application.AssigneeHistories.Add(new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = woodlandOfficerId.Value,
                Role = AssignedUserRole.WoodlandOfficer
            });
        }

        if (preparedForConstraintsCheck)
        {
            application.AdminOfficerReview!.MappingCheckPassed = true;
            application.AdminOfficerReview.MappingChecked = true;
            application.AdminOfficerReview.AgentAuthorityCheckPassed = true;
            application.AdminOfficerReview.AgentAuthorityFormChecked = true;
        }
        
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync().ConfigureAwait(false);

        return application;
    }
}