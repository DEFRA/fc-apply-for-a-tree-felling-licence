using AutoFixture;
using AutoFixture.AutoMoq;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Controllers;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Services.PropertyProfiles;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Forestry.Flo.Tests.Common;
using MassTransit;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Testing;
using IUserAccountRepository = Forestry.Flo.Services.InternalUsers.Repositories.IUserAccountRepository;
using UserAccount = Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount;

namespace Forestry.Flo.External.Web.Tests.Services;

public class CreateFellingLicenceApplicationUseCaseTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly CreateFellingLicenceApplicationUseCase _sut;
    private readonly Mock<IFellingLicenceApplicationExternalRepository> _fellingLicenceApplicationRepository;
    private readonly Mock<IGetPropertyProfiles> _getPropertyProfilesService;
    private readonly Mock<IGetCompartments> _getCompartmentsService;
    private readonly Mock<IOptions<EiaOptions>> _mockEiaOptions = new();

    private readonly Mock<IRetrieveUserAccountsService> _mockRetrieveUserAccountsService;
    private readonly Mock<IRetrieveWoodlandOwners> _mockRetreiveWoodlandOwnersService;

    private readonly Mock<IPropertyProfileRepository> _propertyProfileRepository;
    private readonly Mock<ICompartmentRepository> _compartmentRepository;
    private readonly Mock<IUserAccountRepository> _internalUserAccountRepository;

    private readonly Mock<ISendNotifications> _sendNotifications;
    private readonly IClock _fixedTimeClock = new FakeClock(Instant.FromDateTimeUtc(UtcNow));
    private readonly Mock<IAuditService<CreateFellingLicenceApplicationUseCase>> _auditService;
    private readonly Mock<IActivityFeedItemProvider> _activityFeedService;
    private readonly Mock<IWithdrawFellingLicenceService> _withdrawFellingLicenceService;
    private readonly Mock<IDeleteFellingLicenceService> _deleteFellingLicenceService;
    private readonly ExternalApplicant _externalApplicant;
    private readonly WoodlandOwner _woodlandOwner;
    private readonly Fixture _fixture;
    private readonly Mock<IUnitOfWork> _unitOfWOrkMock;
    private readonly Mock<IOptions<FellingLicenceApplicationOptions>> _fellingLicenceApplicationOptionsMock;
    private readonly Mock<IWoodlandOwnerRepository> _woodlandOwnerRepository;
    private readonly Mock<IUpdateFellingLicenceApplicationForExternalUsers> _updateFellingLicenceService;
    private readonly Mock<IBusControl> _mockBus;
    private readonly Mock<IAgentAuthorityService> _agentAuthorityService;
    private readonly Mock<IForesterServices> _foresterServices;
    private readonly Mock<IApplicationReferenceHelper> _applicationReferenceHelper;
    private readonly Mock<IPublicRegister> _publicRegisterService;
    private readonly Mock<IDbContextTransaction> _transactionMock;

    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas;
    private const string AdminHubAddress = "admin hub address";

    public CreateFellingLicenceApplicationUseCaseTests()
    {
        _fixture = new Fixture();

        _fixture.Customize(new CompositeCustomization(
            new AutoMoqCustomization(),
            new SupportMutableValueTypesCustomization()));

        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _auditService = new Mock<IAuditService<CreateFellingLicenceApplicationUseCase>>();
        _fellingLicenceApplicationRepository = new Mock<IFellingLicenceApplicationExternalRepository>();

        _getPropertyProfilesService =
            new Mock<IGetPropertyProfiles>();

        _getCompartmentsService = new Mock<IGetCompartments>();

        _propertyProfileRepository = new Mock<IPropertyProfileRepository>();
        _compartmentRepository = new Mock<ICompartmentRepository>();
        _internalUserAccountRepository = new Mock<IUserAccountRepository>();
        _sendNotifications = new Mock<ISendNotifications>();
        _unitOfWOrkMock = new Mock<IUnitOfWork>();
        _fellingLicenceApplicationOptionsMock = new Mock<IOptions<FellingLicenceApplicationOptions>>();
        _activityFeedService = new Mock<IActivityFeedItemProvider>();
        _withdrawFellingLicenceService = new Mock<IWithdrawFellingLicenceService>();
        _deleteFellingLicenceService = new Mock<IDeleteFellingLicenceService>();
        _woodlandOwnerRepository = new Mock<IWoodlandOwnerRepository>();
        _updateFellingLicenceService = new Mock<IUpdateFellingLicenceApplicationForExternalUsers>();
        _mockRetrieveUserAccountsService = new();
        _mockRetreiveWoodlandOwnersService = new();
        _agentAuthorityService = new();
        _foresterServices = new Mock<IForesterServices>();
        _applicationReferenceHelper = new Mock<IApplicationReferenceHelper>();
        _getConfiguredFcAreas = new Mock<IGetConfiguredFcAreas>();
        _getConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);
        _publicRegisterService = new Mock<IPublicRegister>();

        _mockBus = new Mock<IBusControl>();

        _woodlandOwner = _fixture.Build<WoodlandOwner>()
            .With(wo => wo.IsOrganisation, true)
            .Create();

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _woodlandOwner.Id,
            AccountTypeExternal.WoodlandOwnerAdministrator);
        _externalApplicant = new ExternalApplicant(user);

        _fellingLicenceApplicationRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWOrkMock.Object);

        _fellingLicenceApplicationOptionsMock.Setup(x => x.Value).Returns(new FellingLicenceApplicationOptions
            { FinalActionDateDaysFromSubmission = 90 });

        _fellingLicenceApplicationRepository.Setup(r =>
                r.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _transactionMock = new Mock<IDbContextTransaction>();

        _fellingLicenceApplicationRepository.Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);
        _transactionMock.Setup(r => r.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _transactionMock.Setup(r => r.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _fellingLicenceApplicationRepository.Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _mockEiaOptions.Setup(x => x.Value).Returns(new EiaOptions
        {
            EiaApplicationExternalUri = "testURI"
        });

        _sut = new CreateFellingLicenceApplicationUseCase(
            _mockRetrieveUserAccountsService.Object,
            _fellingLicenceApplicationRepository.Object,
            new GetFellingLicenceApplicationForExternalUsersService(_fellingLicenceApplicationRepository.Object),
            _getCompartmentsService.Object,
            _compartmentRepository.Object,
            _internalUserAccountRepository.Object,
            _auditService.Object,
            new NullLogger<CreateFellingLicenceApplicationUseCase>(),
            _sendNotifications.Object,
            _fixedTimeClock,
            new RequestContext("test", new RequestUserModel(_externalApplicant.Principal)),
            _activityFeedService.Object,
            _fellingLicenceApplicationOptionsMock.Object,
            _withdrawFellingLicenceService.Object,
            _deleteFellingLicenceService.Object,
            _woodlandOwnerRepository.Object,
            _mockRetreiveWoodlandOwnersService.Object,
            _getPropertyProfilesService.Object,
            _updateFellingLicenceService.Object,
            _agentAuthorityService.Object,
            _mockBus.Object,
            _foresterServices.Object,
            _applicationReferenceHelper.Object,
            _publicRegisterService.Object,
            _mockEiaOptions.Object,
            _getConfiguredFcAreas.Object);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnApplicationList_GivenExternalUser(
        List<FellingLicenceApplication> licenceApplications,
        TestPropertyProfile[] propertyProfiles,
        Flo.Services.Applicants.Models.WoodlandOwnerModel woodlandOwnerModel)
    {
        //arrange
        var i = 0;
        licenceApplications.ForEach(a =>
        {
            var profile = propertyProfiles[i++];
            profile.SetId(Guid.NewGuid());
            a.LinkedPropertyProfile!.PropertyProfileId = profile.Id;
            a.WoodlandOwnerId = _woodlandOwner.Id;
        });
        _fellingLicenceApplicationRepository
            .Setup(r => r.ListAsync(It.Is<Guid>(v => v == _woodlandOwner.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(licenceApplications);

        _mockRetreiveWoodlandOwnersService.Setup(x =>
                x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = licenceApplications.First().CreatedById,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { _woodlandOwner.Id }
        };

        _getPropertyProfilesService.Setup(x => x.ListAsync(
                It.Is<ListPropertyProfilesQuery>(q =>
                    q.WoodlandOwnerId == _woodlandOwner.Id && q.Ids.Count == propertyProfiles.Length),
                userAccessModel,
                It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(propertyProfiles);

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccessModel);

        //act
        var result =
            await _sut.GetWoodlandOwnerApplicationsAsync(_woodlandOwner.Id, _externalApplicant, CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(licenceApplications.Count);
        result.Value.First().Status.Should()
            .Be(licenceApplications.First().StatusHistories.OrderByDescending(s => s.Created)
                .First().Status);
        result.Value.First().PropertyName.Should()
            .Be(propertyProfiles[0].Name);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnPropertyProfiles_GivenUserWithWoodlandOwnerId(List<PropertyProfile> propertyProfiles)
    {
        //arrange
        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { _woodlandOwner.Id }
        };

        _getPropertyProfilesService.Setup(x => x.ListAsync(
            It.Is<ListPropertyProfilesQuery>(q => q.WoodlandOwnerId == _woodlandOwner.Id),
            userAccessModel,
            It.IsAny<CancellationToken>())
        ).ReturnsAsync(propertyProfiles);

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccessModel);

        //act
        var result =
            await _sut.RetrievePropertyProfilesForWoodlandOwnerAsync(
                _woodlandOwner.Id,
                _externalApplicant,
                CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(ModelMapping.ToPropertyProfileDetailsModelList(propertyProfiles));
    }

    [Theory, AutoMoqData]
    public async Task ShouldCreateApplication_GivenPropertyProfileId(
        Guid propertyProfileId,
        FellingLicenceApplication application)
    {
        //arrange
        application.LinkedPropertyProfile!.PropertyProfileId = propertyProfileId;

        _fellingLicenceApplicationRepository.Setup(r =>
                r.CreateAndSaveAsync(It.IsAny<FellingLicenceApplication>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<FellingLicenceApplication, UserDbErrorReason>(application));

        //act
        var result =
            await _sut.CreateFellingLicenceApplication(
                _externalApplicant,
                propertyProfileId,
                _woodlandOwner.Id,
                null,
                CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        _fellingLicenceApplicationRepository.Verify(r => r.CreateAndSaveAsync(It.Is<FellingLicenceApplication>(app =>
            app.CreatedTimestamp == UtcNow
            && app.StatusHistories.Any()
            && app.StatusHistories.First().Status ==
            Flo.Services.FellingLicenceApplications.Entities.FellingLicenceStatus.Draft
            && app.AssigneeHistories.Any()
            && app.AssigneeHistories.First().AssignedUserId == _externalApplicant.UserAccountId
            && app.AssigneeHistories.First().Role == AssignedUserRole.Author
            && app.CreatedById == _externalApplicant.UserAccountId.GetValueOrDefault()
            && app.LinkedPropertyProfile != null
            && app.LinkedPropertyProfile.PropertyProfileId == propertyProfileId
            && app.WoodlandOwnerId == _woodlandOwner.Id
        ), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.CreateFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldSelectApplicationCompartments_GivenSelectedCompartmentIdsList(
        FellingLicenceApplication application,
        List<Guid> compartmentIds)
    {
        //arrange

        _fellingLicenceApplicationRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(a => a == application.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        // Simulate application in editable state

        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        //act
        var result = await _sut.SelectApplicationCompartmentsAsync(_externalApplicant, application.Id, compartmentIds,
            false,
            CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(application.Id);
        _fellingLicenceApplicationRepository.Verify(r => r.Update(It.Is<FellingLicenceApplication>(app =>
            app.LinkedPropertyProfile!.ProposedFellingDetails!.All(d =>
                compartmentIds.Any(i => i == d.PropertyProfileCompartmentId))
        )), Times.Once);
        _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldUpdateApplicationPropertyProfile_AndClearSelectedCompartments(
        FellingLicenceApplication application,
        SelectWoodlandModel selectWoodlandModel)
    {
        //arrange
        selectWoodlandModel.ApplicationId = application.Id;
        _fellingLicenceApplicationRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(a => a == application.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        // Simulate application in editable state

        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        //act
        var result =
            await _sut.UpdateWoodland(_externalApplicant, selectWoodlandModel, CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(application.Id);

        _fellingLicenceApplicationRepository.Verify(r => r.Update(It.Is<FellingLicenceApplication>(app =>
            !app.LinkedPropertyProfile!.ProposedFellingDetails!.Any()
            && !app.LinkedPropertyProfile!.ProposedFellingDetails!.SelectMany(p => p.ProposedRestockingDetails!).Any()
        )), Times.Once);
        _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnApplication_GivenApplicationId(
        FellingLicenceApplication application,
        IList<ActivityFeedItemModel> applicationActivityFeed,
        PropertyProfile propertyProfile,
        Flo.Services.Applicants.Models.WoodlandOwnerModel woodlandOwnerModel)
    {
        //arrange
        var woodlandOwnerId = application.WoodlandOwnerId;
        application.LinkedPropertyProfile!.PropertyProfileId = propertyProfile.Id;

        for (var i = 0; i < application.LinkedPropertyProfile!.ProposedFellingDetails!.Count; i++)
        {
            application.LinkedPropertyProfile!.ProposedFellingDetails[i].FellingSpecies = new List<FellingSpecies>();

            foreach (var proposedRestockingDetail in application.LinkedPropertyProfile!.ProposedFellingDetails[i]
                         .ProposedRestockingDetails)
            {
                proposedRestockingDetail.PropertyProfileCompartmentId = application.LinkedPropertyProfile!
                    .ProposedFellingDetails[i].PropertyProfileCompartmentId;
                proposedRestockingDetail.RestockingSpecies = new List<RestockingSpecies>();
            }
        }

        _compartmentRepository
            .Setup(r => r.ListAsync(propertyProfile.Id, woodlandOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application.LinkedPropertyProfile!.ProposedFellingDetails!.Select(d =>
                new ModelMappingTests.TestCompartment(d.PropertyProfileCompartmentId)).ToList());

        _mockRetreiveWoodlandOwnersService.Setup(x =>
                x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _fellingLicenceApplicationRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(a => a == application.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _activityFeedService
            .Setup(x => x.RetrieveAllRelevantActivityFeedItemsAsync(
                It.IsAny<ActivityFeedItemProviderModel>(),
                It.IsAny<ActorType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicationActivityFeed));

        _getPropertyProfilesService.Setup(r => r.GetPropertyByIdAsync(
                propertyProfile.Id,
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        //act
        var result =
            await _sut.RetrieveFellingLicenceApplication(_externalApplicant, application.Id, CancellationToken.None);

        //assert
        result.HasValue.Should().BeTrue();
        result.Value.ApplicationId.Should().Be(application.Id);
        result.Value.ApplicationSummary.Should().NotBeNull();
        result.Value.ApplicationSummary.Id.Should().Be(application.Id);
        result.Value.ApplicationSummary.ApplicationReference.Should().Be(application.ApplicationReference);
        result.Value.ApplicationSummary.PropertyProfileId.Should()
            .Be(application.LinkedPropertyProfile.PropertyProfileId);
        result.Value.SelectedCompartments.SelectedCompartmentIds.Should()
            .BeEquivalentTo(
                application.LinkedPropertyProfile!.ProposedFellingDetails!
                    .Select(p => p.PropertyProfileCompartmentId));
        result.Value.FellingAndRestockingDetails.DetailsList.Select(d => d.CompartmentId).Should()
            .BeEquivalentTo(
                application.LinkedPropertyProfile!.ProposedFellingDetails!
                    .Select(p => p.PropertyProfileCompartmentId));
    }

    [Theory, AutoMoqData]
    public async Task ShouldSetApplicationOperationDetails_GivenOperationDetailsModel(
        FellingLicenceApplication application,
        OperationDetailsModel operationDetailsModel)
    {
        //arrange
        operationDetailsModel.ApplicationId = application.Id;
        operationDetailsModel.ProposedFellingStart = new DatePart(DateTime.Now, "felling-start");
        operationDetailsModel.ProposedFellingEnd = new DatePart(DateTime.Now, "felling-end");
        operationDetailsModel.DateReceived = new DatePart(DateTime.Now, "date-received");
        operationDetailsModel.DisplayDateReceived = false;

        application.DateReceived = null;

        _fellingLicenceApplicationRepository.Setup(
            r => r.GetAsync(It.Is<Guid>(a => a == application.Id),
                It.IsAny<CancellationToken>())).ReturnsAsync(application);

        // Simulate application in editable state

        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        //act
        var result =
            await _sut.SetApplicationOperationsAsync(_externalApplicant, operationDetailsModel, CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(application.Id);

        _fellingLicenceApplicationRepository.Verify(r => r.Update(It.Is<FellingLicenceApplication>(app =>
            app.ProposedTiming == operationDetailsModel.ProposedTiming
            && app.Measures == operationDetailsModel.Measures
            && app.ProposedFellingStart == operationDetailsModel.ProposedFellingStart!.CalculateDate().ToUniversalTime()
            && app.ProposedFellingEnd == operationDetailsModel.ProposedFellingEnd!.CalculateDate().ToUniversalTime()
            && app.DateReceived == null
        )), Times.Once);
        _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldSetReceivedDate_GivenOperationDetailsModel_GivenFcUser(
        FellingLicenceApplication application,
        OperationDetailsModel operationDetailsModel)
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _woodlandOwner.Id,
            AccountTypeExternal.FcUser);
        var fcUser = new ExternalApplicant(user);

        application.DateReceived = null;

        //arrange
        operationDetailsModel.ApplicationId = application.Id;
        operationDetailsModel.ProposedFellingStart = new DatePart(DateTime.Now, "felling-start");
        operationDetailsModel.ProposedFellingEnd = new DatePart(DateTime.Now, "felling-end");
        operationDetailsModel.DateReceived = new DatePart(DateTime.Now, "date-received");
        operationDetailsModel.DisplayDateReceived = true;

        _fellingLicenceApplicationRepository.Setup(
            r => r.GetAsync(It.Is<Guid>(a => a == application.Id),
                It.IsAny<CancellationToken>())).ReturnsAsync(application);

        // Simulate application in editable state

        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        //act
        var result =
            await _sut.SetApplicationOperationsAsync(fcUser, operationDetailsModel, CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(application.Id);

        _fellingLicenceApplicationRepository.Verify(r => r.Update(It.Is<FellingLicenceApplication>(app =>
            app.ProposedTiming == operationDetailsModel.ProposedTiming
            && app.Measures == operationDetailsModel.Measures
            && app.ProposedFellingStart == operationDetailsModel.ProposedFellingStart!.CalculateDate().ToUniversalTime()
            && app.ProposedFellingEnd == operationDetailsModel.ProposedFellingEnd!.CalculateDate().ToUniversalTime()
            && app.DateReceived == operationDetailsModel.DateReceived.CalculateDate().ToUniversalTime()
        )), Times.Once);
        _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldUpdateApplicationFellingDetails_GivenProposedFellingDetailsModel(
        FellingLicenceApplication application,
        ProposedFellingDetailModel proposedFellingDetail)
    {
        //arrange
        proposedFellingDetail.Id = application.LinkedPropertyProfile!.ProposedFellingDetails!.First().Id;
        proposedFellingDetail.ApplicationId = application.Id;

        Guid compartmentId = Guid.NewGuid();

        application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses =
            new List<CompartmentFellingRestockingStatus>()
            {
                new() { CompartmentId = compartmentId }
            };

        proposedFellingDetail.FellingCompartmentId = compartmentId;

        _fellingLicenceApplicationRepository.Setup(
            r => r.GetAsync(It.Is<Guid>(a => a == application.Id),
                It.IsAny<CancellationToken>())).ReturnsAsync(application);

        // Simulate application in editable state

        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        //act
        var result =
            await _sut.UpdateApplicationFellingDetailsAsync(_externalApplicant, proposedFellingDetail,
                CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(application.Id);

        _fellingLicenceApplicationRepository.Verify(r => r.Update(It.Is<FellingLicenceApplication>(app =>
            app.Id == application.Id
            && app.LinkedPropertyProfile!.ProposedFellingDetails!.First().Id == proposedFellingDetail.Id
            && app.LinkedPropertyProfile!.ProposedFellingDetails!.First().OperationType ==
            proposedFellingDetail.OperationType
        )), Times.Once);

        _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldUpdateApplicationRestockingDetails_GivenProposedRestockingDetailsModel(
        FellingLicenceApplication application,
        ProposedRestockingDetailModel proposedRestockingDetail)
    {
        //arrange
        var firstFellingId = application.LinkedPropertyProfile!.ProposedFellingDetails!.First().Id;
        proposedRestockingDetail.ApplicationId = application.Id;
        proposedRestockingDetail.ProposedFellingDetailsId = firstFellingId;
        proposedRestockingDetail.Id =
            application.LinkedPropertyProfile!.ProposedFellingDetails!.First().ProposedRestockingDetails!.First().Id;
        proposedRestockingDetail.StepComplete = true;

        application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses =
            new List<CompartmentFellingRestockingStatus>()
            {
                new CompartmentFellingRestockingStatus
                {
                    CompartmentId = proposedRestockingDetail.FellingCompartmentId,
                    FellingStatuses = new List<FellingStatus>()
                    {
                        new FellingStatus()
                        {
                            Id = firstFellingId,
                            Status = true,
                            RestockingCompartmentStatuses = new List<RestockingCompartmentStatus>()
                            {
                                new RestockingCompartmentStatus()
                                {
                                    Status = true,
                                    CompartmentId = proposedRestockingDetail.RestockingCompartmentId,
                                    RestockingStatuses = new List<RestockingStatus>()
                                    {
                                        new RestockingStatus()
                                        {
                                            Id = proposedRestockingDetail.Id,
                                            Status = false
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

        _fellingLicenceApplicationRepository.Setup(
            r => r.GetAsync(It.Is<Guid>(a => a == application.Id),
                It.IsAny<CancellationToken>())).ReturnsAsync(application);

        // Simulate application in editable state

        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        //act
        var result =
            await _sut.UpdateApplicationRestockingDetailsAsync(_externalApplicant, proposedRestockingDetail,
                CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(application.Id);

        _fellingLicenceApplicationRepository.Verify(r => r.Update(It.Is<FellingLicenceApplication>(app =>
            app.Id == application.Id
            && app.LinkedPropertyProfile!.ProposedFellingDetails!.First().ProposedRestockingDetails!.First().Id ==
            proposedRestockingDetail.Id
            && app.LinkedPropertyProfile!.ProposedFellingDetails!.First().ProposedRestockingDetails!.First()
                .RestockingProposal == proposedRestockingDetail.RestockingProposal
            && app.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses.First().FellingStatuses
                .First().RestockingCompartmentStatuses.First().RestockingStatuses.First().Status == true
        )), Times.Once);

        _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
    }

    [Theory, CombinatorialData]
    public async Task ShouldSetApplicationStepStatusCorrectly_GivenProposedFellingDetailsModel(
        FellingOperationType operationType,
        bool? fellingStatus)
    {
        Guid compartmentId = Guid.NewGuid();

        //arrange
        var application = _fixture.Create<FellingLicenceApplication>();
        var proposedFellingDetail = _fixture.Build<ProposedFellingDetailModel>()
            .With(x => x.OperationType, operationType)
            .With(x => x.ReturnToPlayback, fellingStatus)
            .With(x => x.ApplicationId, application.Id)
            .With(x => x.FellingCompartmentId, compartmentId)
            .Create();

        proposedFellingDetail.Id = application.LinkedPropertyProfile!.ProposedFellingDetails!.First().Id;


        application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses =
            new List<CompartmentFellingRestockingStatus>()
            {
                new CompartmentFellingRestockingStatus
                {
                    CompartmentId = compartmentId,
                    FellingStatuses = new List<FellingStatus>()
                    {
                        new FellingStatus()
                        {
                            Id = proposedFellingDetail.Id
                        }
                    }
                }
            };

        _fellingLicenceApplicationRepository.Setup(
            r => r.GetAsync(It.Is<Guid>(a => a == application.Id),
                It.IsAny<CancellationToken>())).ReturnsAsync(application);

        // Simulate application in editable state

        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        //act
        var result =
            await _sut.UpdateApplicationFellingDetailsAsync(_externalApplicant, proposedFellingDetail,
                CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(application.Id);

        var expectedFellingStatus = fellingStatus.HasValue && fellingStatus.Value;

        if (operationType == FellingOperationType.None || operationType == FellingOperationType.Thinning)
        {
            expectedFellingStatus = true;
        }

        _fellingLicenceApplicationRepository.Verify(r => r.Update(It.Is<FellingLicenceApplication>(app =>
            app.Id == application.Id
            && app.LinkedPropertyProfile!.ProposedFellingDetails!.First().Id == proposedFellingDetail.Id
            && app.LinkedPropertyProfile!.ProposedFellingDetails!.First().OperationType ==
            proposedFellingDetail.OperationType
            && app.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses.First().FellingStatuses
                .First().Status == expectedFellingStatus
        )), Times.Once);

        _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldCallServiceToSubmitApplication_OnFlaSubmission(
        Guid applicationId,
        SubmitFellingLicenceApplicationResponse submitResponse,
        UserAccessModel userAccessModel)
    {
        // Arrange

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            AccountTypeExternal.WoodlandOwnerAdministrator);
        var externalApplicant = new ExternalApplicant(user);

        TestUtils.SetProtectedProperty(submitResponse, nameof(submitResponse.PreviousStatus),
            Flo.Services.FellingLicenceApplications.Entities.FellingLicenceStatus.Draft);

        // Create dummy dependencies

        var propertyProfile = new PropertyProfile(
            "Test",
            "Test",
            "Test",
            "Test",
            false,
            "Test",
            false,
            "Test",
            Guid.NewGuid(),
            new List<Compartment>());

        var linkedPropertyProfile = new LinkedPropertyProfile();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _getPropertyProfilesService
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(propertyProfile);

        _fellingLicenceApplicationRepository
            .Setup(r => r.GetLinkedPropertyProfileAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(linkedPropertyProfile);

        _woodlandOwnerRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_woodlandOwner);

        _updateFellingLicenceService.Setup(r => r.SubmitFellingLicenceApplicationAsync(
            It.IsAny<Guid>(),
            It.IsAny<UserAccessModel>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(submitResponse));

        _updateFellingLicenceService
            .Setup(x => x.ConvertProposedFellingAndRestockingToConfirmedAsync(
                It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        await _sut.SubmitFellingLicenceApplicationAsync(applicationId, externalApplicant, "link",
            CancellationToken.None);

        // Assert

        _updateFellingLicenceService.Verify(x => x
                .SubmitFellingLicenceApplicationAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()),
            Times.Once);

        _updateFellingLicenceService.Verify(x => x.ConvertProposedFellingAndRestockingToConfirmedAsync(
            applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldFail_IfUnableToConvertProposedFAndR(
        Guid applicationId,
        SubmitFellingLicenceApplicationResponse submitResponse,
        UserAccessModel userAccessModel)
    {
        // Arrange

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            AccountTypeExternal.WoodlandOwnerAdministrator);
        var externalApplicant = new ExternalApplicant(user);

        TestUtils.SetProtectedProperty(submitResponse, nameof(submitResponse.PreviousStatus),
            Flo.Services.FellingLicenceApplications.Entities.FellingLicenceStatus.Draft);

        // Create dummy dependencies

        var propertyProfile = new PropertyProfile(
            "Test",
            "Test",
            "Test",
            "Test",
            false,
            "Test",
            false,
            "Test",
            Guid.NewGuid(),
            new List<Compartment>());

        var linkedPropertyProfile = new LinkedPropertyProfile();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _getPropertyProfilesService
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(propertyProfile);

        _fellingLicenceApplicationRepository
            .Setup(r => r.GetLinkedPropertyProfileAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(linkedPropertyProfile);

        _woodlandOwnerRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_woodlandOwner);

        _updateFellingLicenceService.Setup(r => r.SubmitFellingLicenceApplicationAsync(
            It.IsAny<Guid>(),
            It.IsAny<UserAccessModel>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(submitResponse));

        _updateFellingLicenceService
            .Setup(x => x.ConvertProposedFellingAndRestockingToConfirmedAsync(
                It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("error"));

        var result = await _sut.SubmitFellingLicenceApplicationAsync(applicationId, externalApplicant, "link",
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);

        _auditService.Verify(s =>
            s.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplicationFailure),
                It.IsAny<CancellationToken>()));

        _updateFellingLicenceService.Verify(x => x
                .SubmitFellingLicenceApplicationAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()),
            Times.Once);

        _updateFellingLicenceService.Verify(x => x.ConvertProposedFellingAndRestockingToConfirmedAsync(
            applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldAddSubmittedFlaPropertyDetailAsync_OnFlaSubmission(
        FellingLicenceApplication application,
        UserAccessModel userAccessModel,
        SubmitFellingLicenceApplicationResponse submitResponse)
    {
        // Arrange

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            AccountTypeExternal.WoodlandOwnerAdministrator);
        var externalApplicant = new ExternalApplicant(user);

        application.AreaCode = "022";
        application.ApplicationReference = "---/1/2023";

        TestUtils.SetProtectedProperty(submitResponse, nameof(submitResponse.PreviousStatus),
            Flo.Services.FellingLicenceApplications.Entities.FellingLicenceStatus.Draft);

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _updateFellingLicenceService.Setup(x => x
                .SubmitFellingLicenceApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(submitResponse));

        _updateFellingLicenceService
            .Setup(x => x.ConvertProposedFellingAndRestockingToConfirmedAsync(
                It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        // Create dummy dependencies

        var propertyProfile = new PropertyProfile(
            "Test_Name",
            "Test_osGridReference",
            "Test_NearestTown",
            "Test_NameOfWood",
            true,
            "Test_WoodlandManagementPlanReference",
            false,
            "Test_WoodlandCertificationSchemeReference",
            Guid.NewGuid(),
            new List<Compartment>());

        var cpt1 = _fixture.Build<Compartment>()
            .With(x => x.PropertyProfile, propertyProfile)
            .With(x => x.PropertyProfileId, propertyProfile.Id)
            .With(x => x.GISData, JsonConvert.SerializeObject(new Polygon()))
            .Create();
        var cpt2 = _fixture.Build<Compartment>()
            .With(x => x.PropertyProfile, propertyProfile)
            .With(x => x.PropertyProfileId, propertyProfile.Id)
            .With(x => x.GISData, JsonConvert.SerializeObject(new Polygon()))
            .Create();
        propertyProfile.Compartments.Add(cpt1);
        propertyProfile.Compartments.Add(cpt2);

        SubmittedFlaPropertyDetail? capturedDetail = null;

        var felling = _fixture.Build<ProposedFellingDetail>()
            .Without(x => x.LinkedPropertyProfile)
            .Without(x => x.LinkedPropertyProfile)
            .With(x => x.PropertyProfileCompartmentId, cpt1.Id)
            .Create();

        var restocking = _fixture.Build<ProposedRestockingDetail>()
            .With(x => x.PropertyProfileCompartmentId, cpt2.Id)
            .With(x => x.ProposedFellingDetail, felling)
            .With(x => x.ProposedFellingDetailsId, felling.Id)
            .Create();

        felling.ProposedRestockingDetails = new List<ProposedRestockingDetail> { restocking };

        var linkedPropertyProfile = new LinkedPropertyProfile
        {
            FellingLicenceApplication = application,
            FellingLicenceApplicationId = application.Id,
            PropertyProfileId = propertyProfile.Id,
            ProposedFellingDetails = new List<ProposedFellingDetail> { felling }
        };

        application.LinkedPropertyProfile = linkedPropertyProfile;

        _getPropertyProfilesService
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(propertyProfile);

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application.AsMaybe);
        _fellingLicenceApplicationRepository
            .Setup(r => r.GetLinkedPropertyProfileAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(linkedPropertyProfile);

        _fellingLicenceApplicationRepository.Setup(r => r.AddSubmittedFlaPropertyDetailAsync(
                It.IsAny<SubmittedFlaPropertyDetail>(),
                It.IsAny<CancellationToken>()))
            .Verifiable();

        // Capture the submitted compartment
        _updateFellingLicenceService.Setup(r => r.AddSubmittedFellingLicenceApplicationPropertyDetailAsync(
                It.IsAny<SubmittedFlaPropertyDetail>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success)
            .Callback<SubmittedFlaPropertyDetail, CancellationToken>((
                detail,
                token) => capturedDetail = detail);

        _woodlandOwnerRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_woodlandOwner);

        _foresterServices.Setup(x =>
                x.GetPhytophthoraRamorumRiskZonesAsync(It.IsAny<BaseShape>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<PhytophthoraRamorumRiskZone>(0)));

        _foresterServices.Setup(x =>
                x.GetWoodlandOfficerAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new WoodlandOfficer
            {
                Code = "010", OfficerName = ""
            }));
        // Act

        var result =
            await _sut.SubmitFellingLicenceApplicationAsync(application.Id, externalApplicant, "link",
                CancellationToken.None);

        // Assert

        result.IsSuccess.Should().BeTrue();

        capturedDetail.Should().NotBeNull();
        capturedDetail!.Name.Should().Be(propertyProfile.Name);
        capturedDetail.NearestTown.Should().Be(propertyProfile.NearestTown);
        capturedDetail.NameOfWood.Should().Be(propertyProfile.NameOfWood);
        capturedDetail.HasWoodlandManagementPlan.Should().Be(propertyProfile.HasWoodlandManagementPlan);
        capturedDetail.WoodlandManagementPlanReference.Should().Be(propertyProfile.WoodlandManagementPlanReference);
        capturedDetail.IsWoodlandCertificationScheme.Should().Be(propertyProfile.IsWoodlandCertificationScheme);
        capturedDetail.WoodlandCertificationSchemeReference.Should()
            .Be(propertyProfile.WoodlandCertificationSchemeReference);
        capturedDetail.WoodlandOwnerId.Should().Be(propertyProfile.WoodlandOwnerId);
        capturedDetail.SubmittedFlaPropertyCompartments!.Count().Should().Be(propertyProfile.Compartments.Count());

        _updateFellingLicenceService.Verify(r => r.AddSubmittedFellingLicenceApplicationPropertyDetailAsync(
            capturedDetail, It.IsAny<CancellationToken>()), Times.Once);

        _updateFellingLicenceService.Verify(x => x.ConvertProposedFellingAndRestockingToConfirmedAsync(
            application.Id, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldCreateStatusHistoryWithdrawn_OnFlaWithdraw(
        FellingLicenceApplication application,
        OperationDetailsModel operationDetailsModel,
        PropertyProfile property)
    {
        // Arrange

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            AccountTypeExternal.WoodlandOwnerAdministrator);
        var externalApplicant = new ExternalApplicant(user);
        application.PublicRegister = null;

        // Create Setup
        _woodlandOwnerRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_woodlandOwner);

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(Maybe.From(application));

        _internalUserAccountRepository
            .Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), CancellationToken.None))
            .ReturnsAsync(new List<UserAccount>());

        _withdrawFellingLicenceService.Setup(r =>
                r.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>
            {
                new Guid("d7494cb4-d3ba-4e52-a524-338d8724f1b4")
            });

        _withdrawFellingLicenceService.Setup(c =>
                c.RemoveAssignedWoodlandOfficerAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), CancellationToken.None))
            .ReturnsAsync(Result.Success());

        _getPropertyProfilesService.Setup(x =>
                x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(property));

        // Act
        var finalResult =
            await _sut.WithdrawFellingLicenceApplicationAsync(application.Id, externalApplicant, "link",
                CancellationToken.None);

        // Assert
        finalResult.IsSuccess.Should().BeTrue();

        _transactionMock.Verify(i => i.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _withdrawFellingLicenceService.Verify(
            r => r.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), CancellationToken.None),
            Times.Once);
        _withdrawFellingLicenceService.Verify(
            r => r.RemoveAssignedWoodlandOfficerAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), CancellationToken.None),
            Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.WithdrawFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.FellingLicenceApplicationWithdrawComplete),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldFailWithdrawnApplicationWhenNotFoundApplication_OnFlaWithdraw(
        FellingLicenceApplication application,
        OperationDetailsModel operationDetailsModel,
        PropertyProfile property)
    {
        // Arrange

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            AccountTypeExternal.WoodlandOwnerAdministrator);
        var externalApplicant = new ExternalApplicant(user);
        application.PublicRegister = null;

        // Setup
        _woodlandOwnerRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_woodlandOwner);

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(Maybe.None);

        _internalUserAccountRepository
            .Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), CancellationToken.None))
            .ReturnsAsync(new List<UserAccount>());

        _withdrawFellingLicenceService.Setup(r =>
                r.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<Guid>>($"Failed to get {nameof(FellingLicenceApplication)}"));

        _getPropertyProfilesService.Setup(x =>
                x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(property));

        // Act
        var finalResult =
            await _sut.WithdrawFellingLicenceApplicationAsync(Guid.Empty, externalApplicant, "link",
                CancellationToken.None);

        // Assert
        finalResult.IsFailure.Should().BeTrue();
        finalResult.Error.Should().Be($"Could not withdraw the {nameof(FellingLicenceApplication)}");

        _withdrawFellingLicenceService.Verify(
            r => r.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), CancellationToken.None),
            Times.Once);
        _internalUserAccountRepository.Verify(
            r => r.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), CancellationToken.None), Times.Never);
        _withdrawFellingLicenceService.Verify(
            r => r.RemoveAssignedWoodlandOfficerAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), CancellationToken.None),
            Times.Never);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.WithdrawFellingLicenceApplicationFailure),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldFailCreateStatusHistoryWithdrawn_OnFlaWithdraw(
        FellingLicenceApplication application,
        OperationDetailsModel operationDetailsModel)
    {
        // Arrange

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            AccountTypeExternal.WoodlandOwnerAdministrator);
        var externalApplicant = new ExternalApplicant(user);
        application.PublicRegister = null;

        // Setup
        _woodlandOwnerRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_woodlandOwner);

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(Maybe.From(application));

        _internalUserAccountRepository
            .Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), CancellationToken.None))
            .ReturnsAsync(new List<UserAccount>());

        _withdrawFellingLicenceService.Setup(r => r.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<Guid>>($"Could not withdraw the {nameof(FellingLicenceApplication)}"));
    }

    // Act
    [Theory, AutoMoqData]
    public async Task ShouldRemoveApplicationFromPublicRegister_WhenApplicationShouldBeRemoved(
        FellingLicenceApplication application,
        PropertyProfile property)
    {
        // Arrange

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            AccountTypeExternal.WoodlandOwnerAdministrator);
        var externalApplicant = new ExternalApplicant(user);
        var publicRegister = _fixture
            .Build<PublicRegister>()
            .Without(x => x.ConsultationPublicRegisterRemovedTimestamp)
            .Create();
        application.PublicRegister = publicRegister;

        // Create Setup
        _woodlandOwnerRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_woodlandOwner);

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(Maybe.From(application));

        _internalUserAccountRepository
            .Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), CancellationToken.None))
            .ReturnsAsync(new List<UserAccount>());

        _withdrawFellingLicenceService.Setup(r =>
                r.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>
            {
                new Guid("d7494cb4-d3ba-4e52-a524-338d8724f1b4")
            });

        _withdrawFellingLicenceService.Setup(c =>
                c.RemoveAssignedWoodlandOfficerAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), CancellationToken.None))
            .ReturnsAsync(Result.Success());

        _getPropertyProfilesService.Setup(x =>
                x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(property));

        _publicRegisterService
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _withdrawFellingLicenceService
            .Setup(x => x.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<Guid>>(new List<Guid>()));

        _withdrawFellingLicenceService.Setup(x =>
                x.UpdatePublicRegisterEntityToRemovedAsync(application.Id,
                    externalApplicant.UserAccountId,
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        // Act
        var result =
            await _sut.WithdrawFellingLicenceApplicationAsync(application.Id, externalApplicant, "link",
                CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _publicRegisterService.Verify(x => x.RemoveCaseFromConsultationRegisterAsync(
            publicRegister.EsriId!.Value,
            application.ApplicationReference,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldRollbackTransaction_WhenPublicRegisterRemovalFails(
        FellingLicenceApplication application,
        PropertyProfile property)
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            AccountTypeExternal.WoodlandOwnerAdministrator);
        var externalApplicant = new ExternalApplicant(user);
        var publicRegister = _fixture
            .Build<PublicRegister>()
            .Without(x => x.ConsultationPublicRegisterRemovedTimestamp)
            .Create();
        application.PublicRegister = publicRegister;

        // Create Setup
        _woodlandOwnerRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_woodlandOwner);

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(Maybe.From(application));

        _internalUserAccountRepository
            .Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), CancellationToken.None))
            .ReturnsAsync(new List<UserAccount>());

        _withdrawFellingLicenceService.Setup(r =>
                r.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>
            {
                new Guid("d7494cb4-d3ba-4e52-a524-338d8724f1b4")
            });

        _withdrawFellingLicenceService.Setup(c =>
                c.RemoveAssignedWoodlandOfficerAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), CancellationToken.None))
            .ReturnsAsync(Result.Success());

        _getPropertyProfilesService.Setup(x =>
                x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(property));

        _publicRegisterService
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("error"));

        _withdrawFellingLicenceService
            .Setup(x => x.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<Guid>>(new List<Guid>()));

        // Arrange
        application.PublicRegister = publicRegister;
        application.LinkedPropertyProfile = new LinkedPropertyProfile
        {
            PropertyProfileId = Guid.NewGuid()
        };

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));

        _publicRegisterService
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Failed to remove from public register"));

        _withdrawFellingLicenceService
            .Setup(x => x.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<Guid>>(new List<Guid>()));

        // Act
        var result =
            await _sut.WithdrawFellingLicenceApplicationAsync(application.Id, externalApplicant, "link",
                CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Could not remove the FellingLicenceApplication from the public register");
        _transactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publicRegisterService.Verify(x => x.RemoveCaseFromConsultationRegisterAsync(
            publicRegister.EsriId!.Value,
            application.ApplicationReference,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _withdrawFellingLicenceService.Verify(
            r => r.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), CancellationToken.None),
            Times.Once);
        _withdrawFellingLicenceService.Verify(
            r => r.RemoveAssignedWoodlandOfficerAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), CancellationToken.None),
            Times.Never);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.WithdrawFellingLicenceApplicationFailure),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldRollbackTransaction_WhenUnableToUpdatePublicRegisterEntity(
        FellingLicenceApplication application,
        PropertyProfile property)
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            AccountTypeExternal.WoodlandOwnerAdministrator);
        var externalApplicant = new ExternalApplicant(user);
        var publicRegister = _fixture
            .Build<PublicRegister>()
            .Without(x => x.ConsultationPublicRegisterRemovedTimestamp)
            .Create();
        application.PublicRegister = publicRegister;

        // Create Setup
        _woodlandOwnerRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_woodlandOwner);

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(Maybe.From(application));

        _internalUserAccountRepository
            .Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), CancellationToken.None))
            .ReturnsAsync(new List<UserAccount>());

        _withdrawFellingLicenceService.Setup(r =>
                r.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>
            {
                new Guid("d7494cb4-d3ba-4e52-a524-338d8724f1b4")
            });

        _withdrawFellingLicenceService.Setup(c =>
                c.RemoveAssignedWoodlandOfficerAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), CancellationToken.None))
            .ReturnsAsync(Result.Success());

        _getPropertyProfilesService.Setup(x =>
                x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(property));

        _publicRegisterService
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("error"));

        _withdrawFellingLicenceService
            .Setup(x => x.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<Guid>>(new List<Guid>()));

        _withdrawFellingLicenceService.Setup(x =>
                x.UpdatePublicRegisterEntityToRemovedAsync(application.Id,
                    externalApplicant.UserAccountId,
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("error"));

        // Arrange
        application.PublicRegister = publicRegister;
        application.LinkedPropertyProfile = new LinkedPropertyProfile
        {
            PropertyProfileId = Guid.NewGuid()
        };

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));

        _publicRegisterService
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _withdrawFellingLicenceService
            .Setup(x => x.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<Guid>>(new List<Guid>()));

        // Act
        var result =
            await _sut.WithdrawFellingLicenceApplicationAsync(application.Id, externalApplicant, "link",
                CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Could not update the FellingLicenceApplication public register data");
        _transactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publicRegisterService.Verify(x => x.RemoveCaseFromConsultationRegisterAsync(
            publicRegister.EsriId!.Value,
            application.ApplicationReference,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _withdrawFellingLicenceService.Verify(
            r => r.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), CancellationToken.None),
            Times.Once);
        _withdrawFellingLicenceService.Verify(
            r => r.RemoveAssignedWoodlandOfficerAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), CancellationToken.None),
            Times.Never);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.WithdrawFellingLicenceApplicationFailure),
                It.IsAny<CancellationToken>()));
        _withdrawFellingLicenceService.Verify(r => r.UpdatePublicRegisterEntityToRemovedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldDeleteApplication_OnFlaDelete(FellingLicenceApplication application)
    {
        // Arrange

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            AccountTypeExternal.WoodlandOwnerAdministrator);
        var externalApplicant = new ExternalApplicant(user);

        // Create Setup
        _woodlandOwnerRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_woodlandOwner);

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(Maybe.From(application));

        _deleteFellingLicenceService.Setup(r => r.DeleteDraftApplicationAsync(It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var finalResult =
            await _sut.DeleteDraftFellingLicenceApplicationAsync(application.Id, externalApplicant,
                CancellationToken.None);

        // Assert
        finalResult.IsSuccess.Should().BeTrue();
        finalResult.Value.Should().Be(application.WoodlandOwnerId);

        _deleteFellingLicenceService.Verify(
            r => r.DeleteDraftApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), CancellationToken.None),
            Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.DeleteDraftFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldFailDeleteApplicationWhenNotFoundApplication_OnFlaDelete(
        FellingLicenceApplication application,
        OperationDetailsModel operationDetailsModel)
    {
        // Arrange

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            AccountTypeExternal.WoodlandOwnerAdministrator);
        var externalApplicant = new ExternalApplicant(user);

        // Create Setup
        _woodlandOwnerRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_woodlandOwner);

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(Maybe.None);

        _deleteFellingLicenceService.Setup(r => r.DeleteDraftApplicationAsync(It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure($"Failed to get {nameof(FellingLicenceApplication)}"));

        // Act
        var finalResult =
            await _sut.DeleteDraftFellingLicenceApplicationAsync(application.Id, externalApplicant,
                CancellationToken.None);

        // Assert
        finalResult.IsFailure.Should().BeTrue();
        finalResult.Error.Should().Be($"Failed to get {nameof(FellingLicenceApplication)}");

        _deleteFellingLicenceService.Verify(
            r => r.DeleteDraftApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), CancellationToken.None),
            Times.Never);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.DeleteDraftFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.DeleteDraftFellingLicenceApplicationFailure),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldFailDeleteApplication_OnFlaDelete(
        FellingLicenceApplication application,
        OperationDetailsModel operationDetailsModel)
    {
        // Arrange

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            AccountTypeExternal.WoodlandOwnerAdministrator);
        var externalApplicant = new ExternalApplicant(user);

        // Setup
        _woodlandOwnerRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_woodlandOwner);

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(Maybe.From(application));

        _deleteFellingLicenceService.Setup(r => r.DeleteDraftApplicationAsync(It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<Guid>>($"Could not Delete the {nameof(FellingLicenceApplication)}"));

        // Act
        var finalResult =
            await _sut.DeleteDraftFellingLicenceApplicationAsync(Guid.Empty, externalApplicant, CancellationToken.None);

        // Assert
        finalResult.IsFailure.Should().BeTrue();
        finalResult.Error.Should().Be($"Could not Delete the {nameof(FellingLicenceApplication)}");

        _deleteFellingLicenceService.Verify(
            r => r.DeleteDraftApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), CancellationToken.None),
            Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.DeleteDraftFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.DeleteDraftFellingLicenceApplicationFailure),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldGetSelectFellingOperationTypesViewModel(
        FellingLicenceApplication application,
        Flo.Services.Applicants.Models.WoodlandOwnerModel woodlandOwnerModel,
        FellingLicenceApplication licenceApplication,
        TestPropertyProfile propertyProfile)
    {
        // arrange
        foreach (var felling in application.LinkedPropertyProfile!.ProposedFellingDetails!)
        {
            felling.FellingSpecies = new List<FellingSpecies>();

            felling.ProposedRestockingDetails = new List<ProposedRestockingDetail>();
        }

        _fellingLicenceApplicationRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(a => a == application.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _compartmentRepository
            .Setup(r => r.ListAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application.LinkedPropertyProfile!.ProposedFellingDetails!.Select(d =>
                new ModelMappingTests.TestCompartment(d.PropertyProfileCompartmentId)).ToList());

        propertyProfile.SetId(Guid.NewGuid());
        licenceApplication.LinkedPropertyProfile!.PropertyProfileId = propertyProfile.Id;
        licenceApplication.WoodlandOwnerId = _woodlandOwner.Id;

        _fellingLicenceApplicationRepository
            .Setup(r => r.ListAsync(It.Is<Guid>(v => v == _woodlandOwner.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FellingLicenceApplication>() { licenceApplication });

        _mockRetreiveWoodlandOwnersService.Setup(x =>
                x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = licenceApplication.CreatedById,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { _woodlandOwner.Id }
        };

        _getPropertyProfilesService.Setup(x => x.ListAsync(
                It.Is<ListPropertyProfilesQuery>(q => q.WoodlandOwnerId == _woodlandOwner.Id),
                userAccessModel,
                It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new List<TestPropertyProfile>() { propertyProfile });

        _getPropertyProfilesService
            .Setup(r => r.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(propertyProfile);

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccessModel);

        // act
        var result = await _sut.GetSelectFellingOperationTypesViewModel(application.Id,
            application.LinkedPropertyProfile.ProposedFellingDetails[0].PropertyProfileCompartmentId,
            _externalApplicant, CancellationToken.None);

        // assert
        result.HasValue.Should().BeTrue();
        result.Value.ApplicationId.Should().Be(application.Id);
        result.Value.FellingCompartmentId.Should()
            .Be(application.LinkedPropertyProfile.ProposedFellingDetails[0].PropertyProfileCompartmentId);
        result.Value.GIS!.ToUpper().Should().Contain(application.LinkedPropertyProfile.ProposedFellingDetails[0]
            .PropertyProfileCompartmentId.ToString().ToUpper());
        Assert.True(result.Value.Compartments.Single(c =>
                c.Id == application.LinkedPropertyProfile.ProposedFellingDetails[0].PropertyProfileCompartmentId) is not
            null);
        Assert.True(result.Value.Compartments.Single(c =>
                c.Id == application.LinkedPropertyProfile.ProposedFellingDetails[1].PropertyProfileCompartmentId) is not
            null);
        Assert.True(result.Value.Compartments.Single(c =>
                c.Id == application.LinkedPropertyProfile.ProposedFellingDetails[2].PropertyProfileCompartmentId) is not
            null);
    }

    [Theory, AutoMoqData]
    public async Task ShouldCreateEmptyProposedFellingDetails_GivenOperationTypes(
        SelectFellingOperationTypesViewModel selectFellingOperationTypesViewModel,
        FellingLicenceApplication application)
    {
        // arrange
        application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses =
            new List<CompartmentFellingRestockingStatus>()
            {
                new CompartmentFellingRestockingStatus()
                {
                    CompartmentId = application.LinkedPropertyProfile!.ProposedFellingDetails![0]
                        .PropertyProfileCompartmentId,
                    FellingStatuses = new List<FellingStatus>()
                    {
                        new FellingStatus()
                        {
                            Id = application.LinkedPropertyProfile.ProposedFellingDetails[0].Id
                        }
                    }
                }
            };

        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _fellingLicenceApplicationRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(a => a == application.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        selectFellingOperationTypesViewModel.ApplicationId = application.Id;
        selectFellingOperationTypesViewModel.FellingCompartmentId = application.LinkedPropertyProfile
            .ProposedFellingDetails[0].PropertyProfileCompartmentId;
        selectFellingOperationTypesViewModel.OperationTypes = new List<FellingOperationType>()
            { FellingOperationType.Thinning, FellingOperationType.FellingOfCoppice };

        application.LinkedPropertyProfile.ProposedFellingDetails[0].OperationType = FellingOperationType.None;

        // act
        var result = await _sut.CreateEmptyProposedFellingDetails(_externalApplicant,
            selectFellingOperationTypesViewModel, CancellationToken.None);

        // assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(application.Id);

        _fellingLicenceApplicationRepository.Verify(r => r.Update(It.Is<FellingLicenceApplication>(app =>
            app.Id == application.Id
            && app.LinkedPropertyProfile!.ProposedFellingDetails![
                app.LinkedPropertyProfile!.ProposedFellingDetails.Count - 2].OperationType ==
            FellingOperationType.Thinning
            && app.LinkedPropertyProfile!.ProposedFellingDetails![
                app.LinkedPropertyProfile!.ProposedFellingDetails.Count - 1].OperationType ==
            FellingOperationType.FellingOfCoppice
            && app.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses[0].FellingStatuses.Count ==
            0
            && app.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses[0].Status == true
        )), Times.Once);

        _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldCreateMissingFellingStatuses(FellingLicenceApplication application)
    {
        // arrange
        application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses =
            new List<CompartmentFellingRestockingStatus>()
            {
                new CompartmentFellingRestockingStatus()
                {
                    CompartmentId = application.LinkedPropertyProfile!.ProposedFellingDetails![0]
                        .PropertyProfileCompartmentId,
                    FellingStatuses = new List<FellingStatus>()
                }
            };

        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _fellingLicenceApplicationRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(a => a == application.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        application.LinkedPropertyProfile.ProposedFellingDetails.First().OperationType = FellingOperationType.None;

        // act
        var result = await _sut.CreateMissingFellingStatuses(_externalApplicant, application.Id,
            application.LinkedPropertyProfile.ProposedFellingDetails[0].PropertyProfileCompartmentId,
            CancellationToken.None);

        // assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(application.Id);

        _fellingLicenceApplicationRepository.Verify(r => r.Update(It.Is<FellingLicenceApplication>(app =>
            app.Id == application.Id
            && app.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses[0].FellingStatuses.Count ==
            1
            && app.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses[0].FellingStatuses[0].Id ==
            app.LinkedPropertyProfile!.ProposedFellingDetails![0].Id
        )), Times.Once);

        _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task UpdateApplicationFellingDetailsWithRestockDecision_GivenRestockDecision(
        FellingLicenceApplication application)
    {
        // arrange
        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fellingLicenceApplicationRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(a => a == application.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        var model = new DecisionToRestockViewModel
        {
            ApplicationId = application.Id,
            ProposedFellingDetailsId = application.LinkedPropertyProfile!.ProposedFellingDetails![0].Id,
            IsRestockSelected = false,
            Reason = "A reason"
        };

        // act
        var result =
            await _sut.UpdateApplicationFellingDetailsWithRestockDecisionAsync(_externalApplicant, model,
                CancellationToken.None);

        // assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(application.Id);

        _fellingLicenceApplicationRepository.Verify(r => r.Update(It.Is<FellingLicenceApplication>(app =>
            app.Id == application.Id
            && app.LinkedPropertyProfile!.ProposedFellingDetails![0].IsRestocking == false
            && app.LinkedPropertyProfile!.ProposedFellingDetails![0].NoRestockingReason == "A reason"
        )), Times.Once);

        _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task UpdateApplicationRestockingCompartments_GivenRestockingCompartmentsForAFelling(
        FellingLicenceApplication application)
    {
        // arrange
        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fellingLicenceApplicationRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(a => a == application.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        var selectedCompartmentIds = new List<Guid>()
        {
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses =
            new List<CompartmentFellingRestockingStatus>()
            {
                new CompartmentFellingRestockingStatus()
                {
                    Status = true,
                    CompartmentId = application.LinkedPropertyProfile!.ProposedFellingDetails![0]
                        .PropertyProfileCompartmentId,
                    FellingStatuses = new List<FellingStatus>()
                    {
                        new FellingStatus()
                        {
                            Id = application.LinkedPropertyProfile!.ProposedFellingDetails![0].Id,
                            RestockingCompartmentStatuses = new List<RestockingCompartmentStatus>()
                            {
                                new RestockingCompartmentStatus()
                                {
                                    CompartmentId =
                                        application.LinkedPropertyProfile!.ProposedFellingDetails![0]
                                            .ProposedRestockingDetails![0].PropertyProfileCompartmentId
                                }
                            }
                        }
                    }
                }
            };

        // act
        var result = await _sut.UpdateRestockingCompartmentsForFellingAsync(_externalApplicant, application.Id,
            application.LinkedPropertyProfile!.ProposedFellingDetails![0].Id, selectedCompartmentIds,
            application.LinkedPropertyProfile!.ProposedFellingDetails![0].PropertyProfileCompartmentId,
            CancellationToken.None);

        // assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(application.Id);

        _fellingLicenceApplicationRepository.Verify(r => r.Update(It.Is<FellingLicenceApplication>(app =>
            app.Id == application.Id
            && app.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses[0].FellingStatuses[0]
                .RestockingCompartmentStatuses.Count == 0
            && app.LinkedPropertyProfile!.ProposedFellingDetails![0].ProposedRestockingDetails!.Count == 2
            && app.LinkedPropertyProfile!.ProposedFellingDetails![0].ProposedRestockingDetails[0]
                .PropertyProfileCompartmentId == selectedCompartmentIds[0]
            && app.LinkedPropertyProfile!.ProposedFellingDetails![0].ProposedRestockingDetails[1]
                .PropertyProfileCompartmentId == selectedCompartmentIds[1]
        )), Times.Once);

        _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldCreateMissingRestockingStatuses(FellingLicenceApplication application)
    {
        // arrange
        application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses =
            new List<CompartmentFellingRestockingStatus>()
            {
                new CompartmentFellingRestockingStatus()
                {
                    CompartmentId = application.LinkedPropertyProfile!.ProposedFellingDetails![0]
                        .PropertyProfileCompartmentId,
                    FellingStatuses = new List<FellingStatus>()
                    {
                        new FellingStatus()
                        {
                            Id = application.LinkedPropertyProfile!.ProposedFellingDetails![0].Id,
                            RestockingCompartmentStatuses = new List<RestockingCompartmentStatus>()
                        }
                    }
                }
            };

        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _fellingLicenceApplicationRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(a => a == application.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        // act
        var result = await _sut.CreateMissingRestockingStatuses(_externalApplicant, application.Id,
            application.LinkedPropertyProfile.ProposedFellingDetails[0].PropertyProfileCompartmentId,
            application.LinkedPropertyProfile.ProposedFellingDetails[0].Id, CancellationToken.None);

        // assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(application.Id);

        _fellingLicenceApplicationRepository.Verify(r => r.Update(It.Is<FellingLicenceApplication>(app =>
            app.Id == application.Id
            && app.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses[0].FellingStatuses[0]
                .RestockingCompartmentStatuses.Count == 3
            && app.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses[0].FellingStatuses[0]
                .RestockingCompartmentStatuses[0].CompartmentId ==
            app.LinkedPropertyProfile!.ProposedFellingDetails![0].ProposedRestockingDetails![0]
                .PropertyProfileCompartmentId
            && app.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses[0].FellingStatuses[0]
                .RestockingCompartmentStatuses[1].CompartmentId ==
            app.LinkedPropertyProfile!.ProposedFellingDetails![0].ProposedRestockingDetails![1]
                .PropertyProfileCompartmentId
            && app.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses[0].FellingStatuses[0]
                .RestockingCompartmentStatuses[2].CompartmentId ==
            app.LinkedPropertyProfile!.ProposedFellingDetails![0].ProposedRestockingDetails![2]
                .PropertyProfileCompartmentId
        )), Times.Once);

        _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldGetSelectRestockingOptionsViewModel(
        FellingLicenceApplication application,
        Flo.Services.Applicants.Models.WoodlandOwnerModel woodlandOwnerModel,
        FellingLicenceApplication licenceApplication,
        TestPropertyProfile propertyProfile)
    {
        application.LinkedPropertyProfile!.ProposedFellingDetails![0].OperationType =
            FellingOperationType.FellingIndividualTrees;

        // arrange
        foreach (var felling in application.LinkedPropertyProfile!.ProposedFellingDetails!)
        {
            felling.FellingSpecies = new List<FellingSpecies>();

            foreach (var restocking in felling.ProposedRestockingDetails!)
            {
                restocking.PropertyProfileCompartmentId = felling.PropertyProfileCompartmentId;
                restocking.RestockingSpecies = new List<RestockingSpecies>();
            }
        }

        _fellingLicenceApplicationRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(a => a == application.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _compartmentRepository
            .Setup(r => r.ListAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application.LinkedPropertyProfile!.ProposedFellingDetails!.Select(d =>
                new ModelMappingTests.TestCompartment(d.PropertyProfileCompartmentId)).ToList());

        propertyProfile.SetId(Guid.NewGuid());
        licenceApplication.LinkedPropertyProfile!.PropertyProfileId = propertyProfile.Id;
        licenceApplication.WoodlandOwnerId = _woodlandOwner.Id;

        _fellingLicenceApplicationRepository
            .Setup(r => r.ListAsync(It.Is<Guid>(v => v == _woodlandOwner.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FellingLicenceApplication>() { licenceApplication });

        _mockRetreiveWoodlandOwnersService.Setup(x =>
                x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = licenceApplication.CreatedById,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { _woodlandOwner.Id }
        };

        _getPropertyProfilesService.Setup(x => x.ListAsync(
                It.Is<ListPropertyProfilesQuery>(q => q.WoodlandOwnerId == _woodlandOwner.Id),
                userAccessModel,
                It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new List<TestPropertyProfile>() { propertyProfile });

        _getPropertyProfilesService
            .Setup(r => r.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(propertyProfile);

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccessModel);

        // act
        var result = await _sut.GetSelectRestockingOptionsViewModel(application.Id,
            application.LinkedPropertyProfile!.ProposedFellingDetails![0].PropertyProfileCompartmentId,
            application.LinkedPropertyProfile!.ProposedFellingDetails![0].ProposedRestockingDetails![0]
                .PropertyProfileCompartmentId,
            application.LinkedPropertyProfile!.ProposedFellingDetails![0].Id,
            true,
            _externalApplicant, CancellationToken.None);

        // assert
        result.HasValue.Should().BeTrue();
        result.Value.ApplicationId.Should().Be(application.Id);
        result.Value.FellingCompartmentId.Should()
            .Be(application.LinkedPropertyProfile.ProposedFellingDetails[0].PropertyProfileCompartmentId);
        result.Value.RestockingCompartmentId.Should()
            .Be(application.LinkedPropertyProfile.ProposedFellingDetails[0].ProposedRestockingDetails![0]
                .PropertyProfileCompartmentId);
        result.Value.ProposedFellingDetailsId.Should()
            .Be(application.LinkedPropertyProfile.ProposedFellingDetails[0].Id);
        result.Value.FellingOperationType.Should().Be(FellingOperationType.FellingIndividualTrees);
        result.Value.RestockAlternativeArea.Should().Be(true);
        result.Value.IsCoppiceRegrowthAllowed.Should().Be(true);
        result.Value.IsCreateOpenSpaceAllowed.Should().Be(true);
        result.Value.IsIndividualTreesAllowed.Should().Be(true);
        result.Value.IsNaturalRegenerationAllowed.Should().Be(true);
        result.Value.IsReplantFelledAreaAllowed.Should().Be(false);
        result.Value.IsIndividualTreesInAlternativeAreaAllowed.Should().Be(true);
        result.Value.IsPlantingInAlternativeAreaAllowed.Should().Be(false);
        result.Value.IsNaturalColonisationAllowed.Should().Be(false);
        result.Value.GIS!.ToUpper().Should()
            .Contain(application.LinkedPropertyProfile.ProposedFellingDetails[0].ProposedRestockingDetails![0]
                .PropertyProfileCompartmentId.ToString().ToUpper());
    }

    [Theory, AutoMoqData]
    public async Task ShouldCreateEmptyProposedRestockingDetails_GivenRestockingOptions(
        SelectRestockingOptionsViewModel selectRestockingOptionsViewModel,
        FellingLicenceApplication application)
    {
        // arrange
        application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses =
            new List<CompartmentFellingRestockingStatus>()
            {
                new CompartmentFellingRestockingStatus()
                {
                    CompartmentId = application.LinkedPropertyProfile!.ProposedFellingDetails![0]
                        .PropertyProfileCompartmentId,
                    FellingStatuses = new List<FellingStatus>()
                    {
                        new FellingStatus()
                        {
                            Id = application.LinkedPropertyProfile.ProposedFellingDetails[0].Id,
                            RestockingCompartmentStatuses = new List<RestockingCompartmentStatus>()
                            {
                                new RestockingCompartmentStatus
                                {
                                    CompartmentId =
                                        application.LinkedPropertyProfile!.ProposedFellingDetails![0]
                                            .ProposedRestockingDetails![0].PropertyProfileCompartmentId,
                                    RestockingStatuses = new List<RestockingStatus>()
                                    {
                                        new RestockingStatus()
                                        {
                                            Id = application.LinkedPropertyProfile!.ProposedFellingDetails![0]
                                                .ProposedRestockingDetails![0].Id
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

        application.LinkedPropertyProfile!.ProposedFellingDetails![0].ProposedRestockingDetails![0].RestockingProposal =
            TypeOfProposal.None;

        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _fellingLicenceApplicationRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(a => a == application.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        selectRestockingOptionsViewModel.ApplicationId = application.Id;
        selectRestockingOptionsViewModel.FellingCompartmentId = application.LinkedPropertyProfile
            .ProposedFellingDetails[0].PropertyProfileCompartmentId;
        selectRestockingOptionsViewModel.ProposedFellingDetailsId =
            application.LinkedPropertyProfile.ProposedFellingDetails[0].Id;
        selectRestockingOptionsViewModel.RestockingCompartmentId =
            application.LinkedPropertyProfile.ProposedFellingDetails[0].ProposedRestockingDetails![0]
                .PropertyProfileCompartmentId;
        selectRestockingOptionsViewModel.RestockingOptions = new List<TypeOfProposal>()
        {
            TypeOfProposal.RestockWithCoppiceRegrowth,
            TypeOfProposal.NaturalColonisation
        };

        // act
        var result = await _sut.CreateEmptyProposedRestockingDetails(_externalApplicant,
            selectRestockingOptionsViewModel, CancellationToken.None);

        // assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(application.Id);

        _fellingLicenceApplicationRepository.Verify(r => r.Update(It.Is<FellingLicenceApplication>(app =>
            app.Id == application.Id
            && app.LinkedPropertyProfile!.ProposedFellingDetails![0].ProposedRestockingDetails![
                    app.LinkedPropertyProfile!.ProposedFellingDetails![0].ProposedRestockingDetails!.Count - 2]
                .RestockingProposal == TypeOfProposal.RestockWithCoppiceRegrowth
            && app.LinkedPropertyProfile!.ProposedFellingDetails![0].ProposedRestockingDetails![
                    app.LinkedPropertyProfile!.ProposedFellingDetails![0].ProposedRestockingDetails!.Count - 1]
                .RestockingProposal == TypeOfProposal.NaturalColonisation
            && app.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses[0].FellingStatuses[0]
                .RestockingCompartmentStatuses[0].RestockingStatuses.Count == 0
            && app.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses[0].FellingStatuses[0]
                .RestockingCompartmentStatuses[0].Status == true
        )), Times.Once);

        _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldGetRestockingDetailViewModel(
        FellingLicenceApplicationModel fellingLicenceApplicationModel,
        Compartment compartment)
    {
        // arrange
        _getCompartmentsService.Setup(s =>
                s.GetCompartmentByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(compartment);

        // act
        var result = await _sut.GetRestockingDetailViewModel(_externalApplicant,
            fellingLicenceApplicationModel.ApplicationId,
            fellingLicenceApplicationModel.FellingAndRestockingDetails.DetailsList[0].FellingDetails[0]
                .ProposedRestockingDetails[0].Id,
            fellingLicenceApplicationModel,
            CancellationToken.None);

        // assert
        result.HasValue.Should().BeTrue();
        result.Value.ApplicationId.Should().Be(fellingLicenceApplicationModel.ApplicationId);
        result.Value.CompartmentName.Should().Be($"{compartment.CompartmentNumber}");
        result.Value.CompartmentTotalHectares.Should().Be(compartment.TotalHectares);
        result.Value.OperationType.Should().Be(fellingLicenceApplicationModel.FellingAndRestockingDetails.DetailsList[0]
            .FellingDetails[0].OperationType);
    }

    [Theory, AutoMoqData]
    public async Task ShouldGetFellingAndRestockingDetailsPlaybackViewModel(FellingLicenceApplication application)
    {
        // arrange
        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fellingLicenceApplicationRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(a => a == application.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        var compartments = application.LinkedPropertyProfile!.ProposedFellingDetails!.Select(d =>
            new ModelMappingTests.TestCompartment(d.PropertyProfileCompartmentId)).ToList();

        _compartmentRepository
            .Setup(r => r.ListAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(compartments);

        foreach (var felling in application.LinkedPropertyProfile!.ProposedFellingDetails!)
        {
            foreach (var restocking in felling.ProposedRestockingDetails!)
            {
                restocking.PropertyProfileCompartmentId = felling.PropertyProfileCompartmentId;
            }
        }

        // act
        var result =
            await _sut.GetFellingAndRestockingDetailsPlaybackViewModel(application.Id, _externalApplicant,
                CancellationToken.None);

        // assert
        result.HasValue.Should().BeTrue();
        result.Value.ApplicationId.Should().Be(application.Id);
        result.Value.GIS!.ToUpper().Should()
            .ContainAll(application.LinkedPropertyProfile!.ProposedFellingDetails!.Select(p =>
                p.PropertyProfileCompartmentId.ToString().ToUpper()));
        result.Value.FellingCompartmentsChangeLink.Controller.Should().Be("FellingLicenceApplication");
        result.Value.FellingCompartmentsChangeLink.Action.Should()
            .Be(nameof(FellingLicenceApplicationController.SelectCompartments));
        object values = new
            { applicationId = application.Id, isForRestockingCompartmentSelection = false, returnToPlayback = true };
        result.Value.FellingCompartmentsChangeLink.Values.Should().BeEquivalentTo(values);
        result.Value.FellingCompartmentDetails.Count.Should()
            .Be(application.LinkedPropertyProfile!.ProposedFellingDetails.Count);
        for (int i = 0; i < application.LinkedPropertyProfile!.ProposedFellingDetails.Count; i++)
        {
            var resultCompartment = result.Value.FellingCompartmentDetails.Find(f =>
                f.CompartmentId == application.LinkedPropertyProfile!.ProposedFellingDetails[i]
                    .PropertyProfileCompartmentId);
            resultCompartment.Should().NotBeNull();
            resultCompartment.FellingOperationsChangeLink.Controller.Should().Be("FellingLicenceApplication");
            resultCompartment.FellingOperationsChangeLink.Action.Should()
                .Be(nameof(FellingLicenceApplicationController.SelectFellingOperationTypes));
            var fellingValues = new
            {
                applicationId = application.Id,
                fellingCompartmentId = application.LinkedPropertyProfile!.ProposedFellingDetails[i]
                    .PropertyProfileCompartmentId,
                returnToPlayback = true
            };
            resultCompartment.FellingOperationsChangeLink.Values.Should().BeEquivalentTo(fellingValues);
            resultCompartment.FellingDetails.Count.Should().Be(1);

            var actualFellingDetail = resultCompartment.FellingDetails[0];
            var expectedfellingDetail = application.LinkedPropertyProfile!.ProposedFellingDetails[i];

            actualFellingDetail.FellingDetail.Should().BeEquivalentTo(expectedfellingDetail);
            actualFellingDetail.FellingCompartmentName.Should().Be(resultCompartment.CompartmentName);

            actualFellingDetail.RestockingCompartmentDetails.Count.Should().Be(1);

            actualFellingDetail.RestockingCompartmentDetails[0].CompartmentId.Should()
                .Be(expectedfellingDetail.PropertyProfileCompartmentId);
            actualFellingDetail.RestockingCompartmentDetails[0].RestockingDetails.Count.Should()
                .Be(expectedfellingDetail.ProposedRestockingDetails!.Count);

            for (int j = 0; j < expectedfellingDetail.ProposedRestockingDetails.Count; j++)
            {
                var restocking = actualFellingDetail.RestockingCompartmentDetails[0].RestockingDetails.Find(r =>
                    r.RestockingDetail.Id == expectedfellingDetail.ProposedRestockingDetails[j].Id);

                restocking.Should().NotBeNull();
                restocking.RestockingDetail.Should().BeEquivalentTo(expectedfellingDetail.ProposedRestockingDetails[j]);
            }
        }
    }

    [Theory, AutoMoqData]
    public async Task UpdateSubmittedFlaPropertyCompartmentZonesAsync_ShouldUpdateZonesSuccessfully(
        List<Guid> compartmentIds,
        SubmittedFlaPropertyCompartment submittedCompartment,
        List<PhytophthoraRamorumRiskZone> riskZones)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();

        submittedCompartment.GISData = JsonConvert.SerializeObject(new Polygon());

        _updateFellingLicenceService
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(submittedCompartment));

        _foresterServices
            .Setup(x => x.GetPhytophthoraRamorumRiskZonesAsync(It.IsAny<Polygon>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(riskZones));

        _updateFellingLicenceService
            .Setup(x => x.UpdateSubmittedFlaPropertyCompartmentZonesAsync(
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Result.Success()));

        // Act
        await _sut.UpdateSubmittedFlaPropertyCompartmentZonesAsync(compartmentIds, userId, applicationId,
            CancellationToken.None);

        // Assert
        foreach (var compartmentId in compartmentIds)
        {
            _updateFellingLicenceService.Verify(
                x => x.GetSubmittedFlaPropertyCompartmentByIdAsync(compartmentId, It.IsAny<CancellationToken>()),
                Times.Once);
            _foresterServices.Verify(
                x => x.GetPhytophthoraRamorumRiskZonesAsync(It.IsAny<Polygon>(), It.IsAny<CancellationToken>()),
                Times.Exactly(compartmentIds.Count));
            _updateFellingLicenceService.Verify(x => x.UpdateSubmittedFlaPropertyCompartmentZonesAsync(
                compartmentId,
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Theory, AutoMoqData]
    public async Task RetrieveFellingLicenceApplication_ShouldSetIsCompleteTrue_WhenEiaNotRequired(
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        Flo.Services.Applicants.Models.WoodlandOwnerModel woodlandOwnerModel,
        IList<ActivityFeedItemModel> activityFeedItems)
    {
        // Arrange
        application.LinkedPropertyProfile!.PropertyProfileId = propertyProfile.Id;
        application.WoodlandOwnerId = _woodlandOwner.Id;
        application.Documents = new List<Document>();
        
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory { Status = FellingLicenceStatus.WoodlandOfficerReview, Created = DateTime.UtcNow }
        };

        application.LinkedPropertyProfile!.PropertyProfileId = propertyProfile.Id;

        application.LinkedPropertyProfile.ProposedFellingDetails =
            application.LinkedPropertyProfile.ProposedFellingDetails!.Take(1).ToList();

        application.LinkedPropertyProfile.ProposedFellingDetails.First().ProposedRestockingDetails =  
            application.LinkedPropertyProfile.ProposedFellingDetails.First().ProposedRestockingDetails!.Take(1).ToList();

        application.FellingLicenceApplicationStepStatus = new FellingLicenceApplicationStepStatus
        {
            CompartmentFellingRestockingStatuses = [],
            SelectCompartmentsStatus = true,
            OperationsStatus = true,
            SupportingDocumentationStatus = true,
            TermsAndConditionsStatus = true,
            ConstraintCheckStatus = true,
            EnvironmentalImpactAssessmentStatus = null,
        };

        for (var i = 0; i < application.LinkedPropertyProfile!.ProposedFellingDetails!.Count; i++)
        {
            var felling = application.LinkedPropertyProfile!.ProposedFellingDetails[i];
            felling.FellingSpecies = new List<FellingSpecies>();
            felling.OperationType = FellingOperationType.FellingIndividualTrees;
            felling.IsRestocking = true;
            felling.NoRestockingReason = null;

            var stepStatus = new CompartmentFellingRestockingStatus
            {
                CompartmentId = felling.PropertyProfileCompartmentId,
                Status = true,
                FellingStatuses =
                [
                    new FellingStatus
                    {
                        Id = felling.Id,
                        Status = true,
                        RestockingCompartmentStatuses = [ new RestockingCompartmentStatus
                            {
                                CompartmentId = felling.PropertyProfileCompartmentId,
                                Status = true,
                                RestockingStatuses = []
                            }
                        ]
                    }
                ]
            };

            foreach (var proposedRestockingDetail in application.LinkedPropertyProfile!.ProposedFellingDetails[i]
                         .ProposedRestockingDetails)
            {
                proposedRestockingDetail.PropertyProfileCompartmentId = application.LinkedPropertyProfile!
                    .ProposedFellingDetails[i].PropertyProfileCompartmentId;
                proposedRestockingDetail.RestockingSpecies = new List<RestockingSpecies>();
                proposedRestockingDetail.RestockingProposal = TypeOfProposal.RestockWithIndividualTrees;

                stepStatus.FellingStatuses.First().RestockingCompartmentStatuses.First().RestockingStatuses.Add(new RestockingStatus
                {
                    Id = proposedRestockingDetail.Id,
                    Status = true,
                });
            }
            application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses.Add(stepStatus);
        }

        _fellingLicenceApplicationRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(a => a == application.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _activityFeedService
            .Setup(x => x.RetrieveAllRelevantActivityFeedItemsAsync(
                It.IsAny<ActivityFeedItemProviderModel>(),
                It.IsAny<ActorType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(activityFeedItems));

        _getPropertyProfilesService.Setup(r => r.GetPropertyByIdAsync(
                propertyProfile.Id,
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _mockRetreiveWoodlandOwnersService.Setup(x =>
                x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _compartmentRepository
            .Setup(r => r.ListAsync(propertyProfile.Id, Guid.NewGuid(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application.LinkedPropertyProfile!.ProposedFellingDetails!.Select(d =>
                new ModelMappingTests.TestCompartment(d.PropertyProfileCompartmentId)).ToList());

        // Act
        var result = await _sut.RetrieveFellingLicenceApplication(_externalApplicant, application.Id, CancellationToken.None);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.IsComplete.Should().BeTrue();
    }

    [Theory, AutoMoqData] public async Task RetrieveFellingLicenceApplication_ShouldSetIsCompleteFalse_WhenEiaIsRequired(
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        Flo.Services.Applicants.Models.WoodlandOwnerModel woodlandOwnerModel,
        IList<ActivityFeedItemModel> activityFeedItems)
    {
        // Arrange
        application.LinkedPropertyProfile!.PropertyProfileId = propertyProfile.Id;
        application.WoodlandOwnerId = _woodlandOwner.Id;
        application.Documents = new List<Document>();

        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory { Status = FellingLicenceStatus.WoodlandOfficerReview, Created = DateTime.UtcNow }
        };

        application.LinkedPropertyProfile!.PropertyProfileId = propertyProfile.Id;

        application.LinkedPropertyProfile.ProposedFellingDetails =
            application.LinkedPropertyProfile.ProposedFellingDetails!.Take(1).ToList();

        application.LinkedPropertyProfile.ProposedFellingDetails.First().ProposedRestockingDetails =
            application.LinkedPropertyProfile.ProposedFellingDetails.First().ProposedRestockingDetails!.Take(1).ToList();

        application.FellingLicenceApplicationStepStatus = new FellingLicenceApplicationStepStatus
        {
            CompartmentFellingRestockingStatuses = [],
            SelectCompartmentsStatus = true,
            OperationsStatus = true,
            SupportingDocumentationStatus = true,
            TermsAndConditionsStatus = true,
            ConstraintCheckStatus = true,
            EnvironmentalImpactAssessmentStatus = null,
        };

        for (var i = 0; i < application.LinkedPropertyProfile!.ProposedFellingDetails!.Count; i++)
        {
            var felling = application.LinkedPropertyProfile!.ProposedFellingDetails[i];
            felling.FellingSpecies = new List<FellingSpecies>();
            felling.OperationType = FellingOperationType.FellingIndividualTrees;
            felling.IsRestocking = true;
            felling.NoRestockingReason = null;

            var stepStatus = new CompartmentFellingRestockingStatus
            {
                CompartmentId = felling.PropertyProfileCompartmentId,
                Status = true,
                FellingStatuses =
                [
                    new FellingStatus
                    {
                        Id = felling.Id,
                        Status = true,
                        RestockingCompartmentStatuses = [ new RestockingCompartmentStatus
                            {
                                CompartmentId = felling.PropertyProfileCompartmentId,
                                Status = true,
                                RestockingStatuses = []
                            }
                        ]
                    }
                ]
            };

            foreach (var proposedRestockingDetail in application.LinkedPropertyProfile!.ProposedFellingDetails[i]
                         .ProposedRestockingDetails)
            {
                proposedRestockingDetail.PropertyProfileCompartmentId = application.LinkedPropertyProfile!
                    .ProposedFellingDetails[i].PropertyProfileCompartmentId;
                proposedRestockingDetail.RestockingSpecies = new List<RestockingSpecies>();
                proposedRestockingDetail.RestockingProposal = TypeOfProposal.DoNotIntendToRestock;

                stepStatus.FellingStatuses.First().RestockingCompartmentStatuses.First().RestockingStatuses.Add(new RestockingStatus
                {
                    Id = proposedRestockingDetail.Id,
                    Status = true,
                });
            }
            application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses.Add(stepStatus);
        }

        _fellingLicenceApplicationRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(a => a == application.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _activityFeedService
            .Setup(x => x.RetrieveAllRelevantActivityFeedItemsAsync(
                It.IsAny<ActivityFeedItemProviderModel>(),
                It.IsAny<ActorType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(activityFeedItems));

        _getPropertyProfilesService.Setup(r => r.GetPropertyByIdAsync(
                propertyProfile.Id,
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _mockRetreiveWoodlandOwnersService.Setup(x =>
                x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _compartmentRepository
            .Setup(r => r.ListAsync(propertyProfile.Id, Guid.NewGuid(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application.LinkedPropertyProfile!.ProposedFellingDetails!.Select(d =>
                new ModelMappingTests.TestCompartment(d.PropertyProfileCompartmentId)).ToList());

        // Act
        var result = await _sut.RetrieveFellingLicenceApplication(_externalApplicant, application.Id, CancellationToken.None);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.IsComplete.Should().BeFalse();
    }
}