using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using NodaTime.Testing;
using System.Text.Json;
using WoodlandOwnerModel = Forestry.Flo.Services.Applicants.Models.WoodlandOwnerModel;

namespace Forestry.Flo.Internal.Web.Tests.Services.AdminOfficerReviewUseCaseTests;

public class AgentAuthorityFormCheckUseCaseTests
{
    private readonly AgentAuthorityFormCheckUseCase _sut;

    private readonly IFixture _fixture;
    private readonly Mock<IUserAccountRepository> _userAccountRepository;
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserAccountService> _internalUserAccountService;
    private readonly Mock<IClock> _clock;
    private readonly Mock<IAgentAuthorityInternalService> _agentAuthorityInternalServiceMock = new();
    private readonly Mock<IRetrieveUserAccountsService> _retrieveUserAccountsServiceMock = new();
    private readonly Mock<IRetrieveWoodlandOwners> _retrieveWoodlandOwnersMock = new();
    private readonly Mock<IAgentAuthorityService> _agentAuthorityServiceMock = new();
    private readonly Mock<IAuditService<AdminOfficerReviewUseCaseBase>> _auditServiceMock = new();
    private readonly Mock<IWoodlandOfficerReviewSubStatusService> _woodlandOfficerReviewSubStatusService = new();

    private readonly InternalUserContextFlaRepository _internalRepo;
    private readonly FellingLicenceApplicationsContext _fellingLicenceApplicationsContext;

    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas;
    private const string AdminHubAddress = "admin hub address";

    private FellingLicenceApplication _fellingLicenceApplication;
    private readonly GetAgentAuthorityFormResponse _agentAuthorityFormResponse;
    private readonly WoodlandOwner _woodlandOwner;
    private readonly InternalUser _internalUser;
    private readonly UserAccount _applicantUser;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AgentAuthorityFormCheckUseCaseTests()
    {
        _agentAuthorityServiceMock.Reset();
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

        var currentForm = new AgentAuthorityFormDetailsModel(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), null);

        _agentAuthorityFormResponse = _fixture.Build<GetAgentAuthorityFormResponse>()
            .With(x => x.AgentAuthorityId, Guid.NewGuid)
            .With(x => x.CurrentAgentAuthorityForm, currentForm)
            .With(x => x.SpecificTimestampAgentAuthorityForm, currentForm)
            .Create();

        var agencyModel = ModelMapping.ToAgencyModel(_applicantUser.Agency);
        _agentAuthorityServiceMock
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agencyModel);

        new FakeClock(Instant.FromDateTimeUtc(UtcNow));
        _userAccountRepository = new Mock<IUserAccountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _internalUserAccountService = new Mock<IUserAccountService>();
        _clock = new Mock<IClock>();

        _getConfiguredFcAreas = new Mock<IGetConfiguredFcAreas>();
        _getConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);

        _woodlandOwner = _fixture.Build<WoodlandOwner>()
            .With(wo => wo.IsOrganisation, true)
            .With(wo => wo.Id, Guid.NewGuid)
            .Create();

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
        _agentAuthorityInternalServiceMock.Setup(s =>
                s.GetAgentAuthorityFormAsync(It.IsAny<GetAgentAuthorityFormRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_agentAuthorityFormResponse);
        _retrieveUserAccountsServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_applicantUser);

        _userAccountRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWorkMock.Object);

        _sut = CreateSut(Guid.NewGuid());
    }

    private AgentAuthorityFormCheckUseCase CreateSut(Guid userId)
    {


        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: userId,
            accountTypeInternal: AccountTypeInternal.WoodlandOfficer);

        return new AgentAuthorityFormCheckUseCase(
            _internalUserAccountService.Object,
            _retrieveUserAccountsServiceMock.Object,
            new NullLogger<AgentAuthorityFormCheckUseCase>(),
            _internalRepo,
            _retrieveWoodlandOwnersMock.Object,
            new UpdateAdminOfficerReviewService(_internalRepo, new NullLogger<UpdateAdminOfficerReviewService>(), _clock.Object),
            new GetFellingLicenceApplicationForInternalUsersService(_internalRepo, _clock.Object),
            _agentAuthorityInternalServiceMock.Object,
            _agentAuthorityServiceMock.Object,
            _auditServiceMock.Object,
            _getConfiguredFcAreas.Object,
            _woodlandOfficerReviewSubStatusService.Object,
            new RequestContext("test", new RequestUserModel(_internalUser.Principal)));
    }

    [Fact]
    public async Task ShouldReturnCorrectModel_WhenSuccessful()
    {
        _fellingLicenceApplication = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview);

        _fellingLicenceApplication.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync();

        var result = await _sut.GetAgentAuthorityFormCheckModelAsync(_fellingLicenceApplication.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.NotNull(result.Value);
        Assert.Equal(_fellingLicenceApplication.Id, result.Value.ApplicationId);
        Assert.Equivalent(new AgencyModel
        {
            Address = _applicantUser.Agency.Address,
            ContactEmail = _applicantUser.Agency.ContactEmail,
            ContactName = _applicantUser.Agency.ContactName,
            OrganisationName = _applicantUser.Agency.OrganisationName,
            AgencyId = _applicantUser.Agency.Id,
            IsFcAgency = _applicantUser.Agency.IsFcAgency
        }, result.Value.ApplicationOwner.Agency);

        Assert.Equivalent(new Models.UserAccount.WoodlandOwnerModel
        {
            ContactAddress = ModelMapping.ToAddressModel(_woodlandOwner.ContactAddress),
            ContactEmail = _woodlandOwner.ContactEmail,
            ContactName = _woodlandOwner.ContactName,
            ContactTelephone = _woodlandOwner.ContactTelephone,
            IsOrganisation = _woodlandOwner.IsOrganisation,
            OrganisationName = _woodlandOwner.OrganisationName,
            OrganisationAddress = ModelMapping.ToAddressModel(_woodlandOwner.OrganisationAddress)
        }, result.Value.ApplicationOwner.WoodlandOwner);


        Assert.True(result.Value.ApplicationOwner.AgentAuthorityForm.CouldRetrieveAgentAuthorityFormDetails);
        Assert.Equal(_agentAuthorityFormResponse.AgentAuthorityId, result.Value.ApplicationOwner.AgentAuthorityForm.AgentAuthorityId);
        
        Assert.Equivalent(new AgentAuthorityFormDetailsModel(
            _agentAuthorityFormResponse.CurrentAgentAuthorityForm.Value.Id,
            _agentAuthorityFormResponse.CurrentAgentAuthorityForm.Value.ValidFromDate,
            _agentAuthorityFormResponse.CurrentAgentAuthorityForm.Value.ValidToDate), 
            result.Value.ApplicationOwner.AgentAuthorityForm.CurrentAgentAuthorityForm.Value);
        Assert.Equivalent(new AgentAuthorityFormDetailsModel(
            _agentAuthorityFormResponse.SpecificTimestampAgentAuthorityForm.Value.Id,
            _agentAuthorityFormResponse.SpecificTimestampAgentAuthorityForm.Value.ValidFromDate,
            _agentAuthorityFormResponse.SpecificTimestampAgentAuthorityForm.Value.ValidToDate),
            result.Value.ApplicationOwner.AgentAuthorityForm.SpecificTimestampAgentAuthorityForm.Value);
        Assert.False(result.Value.ApplicationOwner.AgentAuthorityForm.AgentAuthorityFormManagementUrl.HasValue);

        _agentAuthorityServiceMock.Verify(x => x.GetAgencyForWoodlandOwnerAsync(_fellingLicenceApplication.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _retrieveWoodlandOwnersMock.Verify(
            v => v.RetrieveWoodlandOwnerByIdAsync(_fellingLicenceApplication.WoodlandOwnerId, CancellationToken.None),
            Times.Exactly(2));
        _agentAuthorityInternalServiceMock.Verify(v => v.GetAgentAuthorityFormAsync(
                It.Is<GetAgentAuthorityFormRequest>(x => 
                    x.WoodlandOwnerId == _fellingLicenceApplication.WoodlandOwnerId && 
                    x.AgencyId == _applicantUser.Agency.Id), 
                CancellationToken.None), 
            Times.Once);
    }

    [Fact]
    public async Task ShouldReturnFailure_WhenFellingLicenceNotRetrieved()
    {
        _fellingLicenceApplication = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview);

        _fellingLicenceApplicationsContext.Remove(_fellingLicenceApplication);

        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.GetAgentAuthorityFormCheckModelAsync(_fellingLicenceApplication.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        
        _retrieveUserAccountsServiceMock.VerifyNoOtherCalls();
        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        _agentAuthorityInternalServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReturnFailure_WhenWoodlandOwnerNotRetrieved()
    {
        _fellingLicenceApplication = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview);

        _retrieveWoodlandOwnersMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WoodlandOwnerModel>("error"));

        var result = await _sut.GetAgentAuthorityFormCheckModelAsync(_fellingLicenceApplication.Id, CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveWoodlandOwnersMock.Verify(
            v => v.RetrieveWoodlandOwnerByIdAsync(_fellingLicenceApplication.WoodlandOwnerId, CancellationToken.None),
            Times.Once);
        _agentAuthorityInternalServiceMock.VerifyNoOtherCalls();
    }


    [Fact]
    public async Task ShouldReturnFailure_WhenAgentAuthorityFormNotRetrieved()
    {
        _fellingLicenceApplication = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview);

        _agentAuthorityInternalServiceMock.Setup(s =>
                s.GetAgentAuthorityFormAsync(It.IsAny<GetAgentAuthorityFormRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<GetAgentAuthorityFormResponse>("error"));

        var result = await _sut.GetAgentAuthorityFormCheckModelAsync(_fellingLicenceApplication.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        
        _retrieveWoodlandOwnersMock.Verify(
            v => v.RetrieveWoodlandOwnerByIdAsync(_fellingLicenceApplication.WoodlandOwnerId, CancellationToken.None),
            Times.Once);
        _agentAuthorityInternalServiceMock.Verify(v => v.GetAgentAuthorityFormAsync(
                It.Is<GetAgentAuthorityFormRequest>(x => 
                    x.WoodlandOwnerId == _fellingLicenceApplication.WoodlandOwnerId && 
                    x.AgencyId == _applicantUser.Agency.Id), 
                CancellationToken.None), 
            Times.Once);
    }

    [Theory, CombinatorialData]
    public async Task ShouldUpdateAoReviewEntityAndAudit(
        bool checkPassed,
        [CombinatorialValues("populated", "", null)] string? failureReason)
    {
        _fellingLicenceApplication = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview, _internalUser.UserAccountId, _woodlandOwner.Id);

        if (_fellingLicenceApplication.AdminOfficerReview is not null)
        {
            _fellingLicenceApplication.AdminOfficerReview.AdminOfficerReviewComplete = false;

            await _fellingLicenceApplicationsContext.SaveEntitiesAsync();
        }

        var result = await _sut.CompleteAgentAuthorityCheckAsync(
            _fellingLicenceApplication.Id,
            _internalUser.UserAccountId!.Value, 
            checkPassed,
            failureReason,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var aoReviewEntity =
            _fellingLicenceApplicationsContext.AdminOfficerReviews.FirstOrDefault(x =>
                x.FellingLicenceApplicationId == _fellingLicenceApplication.Id);

        Assert.NotNull(aoReviewEntity);
        if (checkPassed)
        {
            Assert.Null(aoReviewEntity!.AgentAuthorityCheckFailureReason);
        }
        else
        {
            Assert.Equal(failureReason, aoReviewEntity!.AgentAuthorityCheckFailureReason);
        }
        Assert.Equal(checkPassed, aoReviewEntity!.AgentAuthorityCheckPassed);
            
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
        bool checkPassed,
        string? failureReason)
    {
        _fellingLicenceApplication = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview, _internalUser.UserAccountId, _woodlandOwner.Id);

        var invalidId = Guid.NewGuid();

        var result = await _sut.CompleteAgentAuthorityCheckAsync(
            invalidId,
            _internalUser.UserAccountId!.Value,
            checkPassed,
            failureReason,
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

        var result = await _sut.CompleteAgentAuthorityCheckAsync(
            _fellingLicenceApplication.Id,
            _internalUser.UserAccountId!.Value,
            false,
            "reason",
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

    private async Task<FellingLicenceApplication> CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus currentStatus,  Guid? performingUserId = null, Guid? woodlandOfficerId = null)
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

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync().ConfigureAwait(false);

        return application;
    }
}