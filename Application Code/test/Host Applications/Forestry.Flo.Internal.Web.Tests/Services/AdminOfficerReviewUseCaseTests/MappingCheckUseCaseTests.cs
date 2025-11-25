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
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using System.Text.Json;
using Forestry.Flo.Services.Common.Models;
using WoodlandOwnerModel = Forestry.Flo.Services.Applicants.Models.WoodlandOwnerModel;

namespace Forestry.Flo.Internal.Web.Tests.Services.AdminOfficerReviewUseCaseTests;

public class MappingCheckUseCaseTests
{
    private readonly MappingCheckUseCase _sut;

    private readonly IFixture _fixture;
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly Mock<IUserAccountService> _internalUserAccountService;
    private readonly Mock<IClock> _clock;
    private readonly Mock<IAgentAuthorityService> _agentAuthorityServiceMock = new();
    private readonly Mock<IRetrieveUserAccountsService> _retrieveUserAccountsServiceMock = new();
    private readonly Mock<IRetrieveWoodlandOwners> _retrieveWoodlandOwnersMock = new();
    private readonly Mock<IAuditService<AdminOfficerReviewUseCaseBase>> _auditServiceMock = new();
    private readonly Mock<IAgentAuthorityService> _agentAuthorityService = new();
    private readonly Mock<IWoodlandOfficerReviewSubStatusService> _woodlandOfficerReviewSubStatusService = new();

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

    public MappingCheckUseCaseTests()
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
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _retrieveUserAccountsServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_applicantUser);

        _sut = CreateSut(Guid.NewGuid());
    }

    private MappingCheckUseCase CreateSut(Guid userId)
    {
        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: userId,
            accountTypeInternal: AccountTypeInternal.WoodlandOfficer);

        return new MappingCheckUseCase(
            _internalUserAccountService.Object,
            _retrieveUserAccountsServiceMock.Object,
            new NullLogger<MappingCheckUseCase>(),
            _internalRepo,
            _retrieveWoodlandOwnersMock.Object,
            new UpdateAdminOfficerReviewService(_internalRepo, new NullLogger<UpdateAdminOfficerReviewService>(), _clock.Object),
            new GetFellingLicenceApplicationForInternalUsersService(_internalRepo, _clock.Object),
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

        var result = await _sut.GetMappingCheckModelAsync(_fellingLicenceApplication.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.NotNull(result.Value);
        Assert.Equal(_fellingLicenceApplication.Id, result.Value.ApplicationId);

        Assert.NotNull(result.Value.FellingAndRestockingDetails);
        Assert.Equal(_fellingLicenceApplication.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.Count, result.Value.FellingAndRestockingDetails.Count);

        // verify mapped correctly

        var returnedModel = result.Value.FellingAndRestockingDetails.First();
        var expectedValues = _fellingLicenceApplication.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.First();

        Assert.NotNull(returnedModel);
        Assert.Equal(_fellingLicenceApplication.Id, returnedModel.ApplicationId);
        Assert.Equal(expectedValues.CompartmentId, returnedModel.CompartmentId);
        Assert.Equal(expectedValues.GISData, returnedModel.GISData);
    }

    [Fact]
    public async Task ShouldReturnFailure_WhenFellingLicenceNotRetrieved()
    {
        _fellingLicenceApplication = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview);

        _fellingLicenceApplicationsContext.Remove(_fellingLicenceApplication);

        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.GetMappingCheckModelAsync(_fellingLicenceApplication.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        
        _retrieveUserAccountsServiceMock.VerifyNoOtherCalls();
        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        _agentAuthorityServiceMock.VerifyNoOtherCalls();
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

        var result = await _sut.CompleteMappingCheckAsync(
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
            Assert.Null(aoReviewEntity!.MappingCheckFailureReason);
        }
        else
        {
            Assert.Equal(failureReason, aoReviewEntity!.MappingCheckFailureReason);
        }
        Assert.Equal(checkPassed, aoReviewEntity!.MappingCheckPassed);
            
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

        var result = await _sut.CompleteMappingCheckAsync(
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

        var result = await _sut.CompleteMappingCheckAsync(
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

        var propertyProfileId = Guid.NewGuid();
        var compartmentId = Guid.NewGuid();

        application.SubmittedFlaPropertyDetail = new SubmittedFlaPropertyDetail
        {
            Name = _fixture.Create<string>(),
            NameOfWood = _fixture.Create<string>(),
            NearestTown = _fixture.Create<string>(),
            HasWoodlandManagementPlan = false,
            WoodlandManagementPlanReference = _fixture.Create<string>(),
            IsWoodlandCertificationScheme = false,
            WoodlandCertificationSchemeReference = _fixture.Create<string>(),
            WoodlandOwnerId = Guid.NewGuid(),
            FellingLicenceApplication = _fellingLicenceApplication, 
            PropertyProfileId = propertyProfileId
        };

        application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments =
            new List<SubmittedFlaPropertyCompartment>
            {
                new()
                {
                    CompartmentId = compartmentId,
                    CompartmentNumber = _fixture.Create<string>(),
                    SubCompartmentName = _fixture.Create<string>(),
                    TotalHectares = _fixture.Create<double>(),
                    WoodlandName = _fixture.Create<string>(),
                    GISData = _fixture.Create<string>(),
                    PropertyProfileId = propertyProfileId,
                    SubmittedFlaPropertyDetail = application.SubmittedFlaPropertyDetail,
                    ConfirmedFellingDetails = _fixture.Create<List<ConfirmedFellingDetail>>(),
                }
            };

        application.LinkedPropertyProfile = new LinkedPropertyProfile
        {
            PropertyProfileId = propertyProfileId,
        };

        application.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>
        {
            new()
            {
                LinkedPropertyProfile = application.LinkedPropertyProfile,
                PropertyProfileCompartmentId = compartmentId,
                OperationType = FellingOperationType.ClearFelling,
                AreaToBeFelled = _fixture.Create<int>(),
                NumberOfTrees = _fixture.Create<int>(),
                TreeMarking = _fixture.Create<string>(),
                IsPartOfTreePreservationOrder = false,
                TreePreservationOrderReference = null,
                IsWithinConservationArea = false,
                ConservationAreaReference = null,
                FellingSpecies = new List<FellingSpecies>(),
                FellingOutcomes = new List<FellingOutcome>(),
                ProposedRestockingDetails = new List<ProposedRestockingDetail>()
            }
        };

        application.LinkedPropertyProfile.ProposedFellingDetails[0].ProposedRestockingDetails = new List<ProposedRestockingDetail>
        {
            new()
            {
                ProposedFellingDetail = application.LinkedPropertyProfile.ProposedFellingDetails[0],
                PropertyProfileCompartmentId = compartmentId,
                RestockingProposal = TypeOfProposal.PlantAnAlternativeArea,
                Area = _fixture.Create<double>(),
                PercentageOfRestockArea = _fixture.Create<double>(),
                RestockingDensity = _fixture.Create<double>(),
                RestockingSpecies = new List<RestockingSpecies>(),
                RestockingOutcomes = new List<RestockingOutcome>()
            }
        };

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync().ConfigureAwait(false);

        return application;
    }
}