using CSharpFunctionalExtensions;
using FluentAssertions;
using FluentEmail.Core;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WoodlandOwnerModel = Forestry.Flo.Services.Applicants.Models.WoodlandOwnerModel;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public partial class FellingLicenceApplicationUseCaseTests
{
    private readonly Mock<IUserAccountService> _userAccountService;
    private readonly Mock<IRetrieveUserAccountsService> _externalUserAccountService;
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerService;
    private readonly Mock<IPropertyProfileRepository> _propertyProfileRepository;
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository;
    private readonly Mock<Flo.Services.InternalUsers.Repositories.IUserAccountRepository> _internalUserAccountRepository;
    private readonly Mock<IActivityFeedItemProvider> _activityFeedItemProvider;
    private readonly Mock<IAgentAuthorityService> _agentAuthorityService;
    private readonly Mock<IAgentAuthorityInternalService> _agentAuthorityInternalService;
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas;
    private const string AdminHubAddress = "admin hub address";

    private FellingLicenceApplicationUseCase _sut;
    private readonly InternalUser _internalUser;
    
    public FellingLicenceApplicationUseCaseTests()
    {
        ILogger<FellingLicenceApplicationUseCase> logger = new NullLogger<FellingLicenceApplicationUseCase>();
        _externalUserAccountService = new Mock<IRetrieveUserAccountsService>();
        _userAccountService = new Mock<IUserAccountService>();
        _woodlandOwnerService = new Mock<IRetrieveWoodlandOwners>();
        _propertyProfileRepository = new Mock<IPropertyProfileRepository>();
        var auditService = new Mock<IAuditService<ExtendApplicationsUseCase>>();
        _internalUserAccountRepository = new Mock<Flo.Services.InternalUsers.Repositories.IUserAccountRepository>();
        _fellingLicenceApplicationRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
        _activityFeedItemProvider = new Mock<IActivityFeedItemProvider>();
        _agentAuthorityService = new Mock<IAgentAuthorityService>();
        _agentAuthorityInternalService = new();
        _getConfiguredFcAreas = new Mock<IGetConfiguredFcAreas>();
        _getConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal();
        _internalUser = new InternalUser(user);

        _sut = new FellingLicenceApplicationUseCase(
            _userAccountService.Object,
            _externalUserAccountService.Object,
            _fellingLicenceApplicationRepository.Object,
            _propertyProfileRepository.Object,
            _woodlandOwnerService.Object,
            auditService.Object,
            _internalUserAccountRepository.Object,
            _activityFeedItemProvider.Object,
            _agentAuthorityService.Object,
            _agentAuthorityInternalService.Object, 
            _getConfiguredFcAreas.Object,
            logger);
    }

    private void ArrangeDefaultMocks(FellingLicenceApplication application, WoodlandOwnerModel woodlandOwner, UserAccount? userAccount)
    {
        application.WoodlandOwnerId = woodlandOwner.Id!.Value;
        application.Documents!.First().Purpose = DocumentPurpose.ApplicationDocument;
        SyncApplicationCompartmentData(application);
        _fellingLicenceApplicationRepository.Setup(r => r.GetAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        _woodlandOwnerService.Setup(r => r.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));
        _externalUserAccountService.Setup(r => r.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount>(userAccount));
        _internalUserAccountRepository.Setup(r => r.GetUsersWithIdsInAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>, UserDbErrorReason>(new List<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldRetrieveFellingLicenceApplicationReviewSummary_GivenApplicationId(
    FellingLicenceApplication application,
    WoodlandOwnerModel woodlandOwner,
    UserAccount userAccount,
    Flo.Services.InternalUsers.Entities.UserAccount.UserAccount account)
    {
        // Arrange
        ArrangeDefaultMocks(application, woodlandOwner, userAccount);

        _userAccountService.Setup(r => r.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(account));

        foreach (var document in application.Documents!)
        {
            document.DeletedByUserId = null;
            document.DeletionTimestamp = null;
        }

        //act
        var result =
            await _sut.RetrieveFellingLicenceApplicationReviewSummaryAsync(application.Id, _internalUser, CancellationToken.None);

        //assert
        result.HasValue.Should().BeTrue();
        result.Value.Documents.Should().HaveCount(application.Documents!.Count);
        result.Value.Id.Should().Be(application.Id);
        result.Value.ApplicationDocument.Should().NotBeNull();
        result.Value.ApplicationOwner!.WoodlandOwner!.ContactName.Should().Be(woodlandOwner.ContactName);
        result.Value.OperationDetailsModel.Should().NotBeNull();

        _fellingLicenceApplicationRepository.VerifyAll();
        _woodlandOwnerService.VerifyAll();
    }

    [Theory, AutoMoqData]
    public async Task ShouldCreateApplicationSummary_WhenRetrieveFellingLicenceApplicationReviewSummary_GivenApplicationId(
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner,
        UserAccount userAccount,
        AgencyModel agencyModel,
        GetAgentAuthorityFormResponse aafResponse,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount account)
    {
        // Arrange
        ArrangeDefaultMocks(application, woodlandOwner, userAccount);
        _userAccountService.Setup(r => r.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(account));
        _agentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agencyModel.AsMaybe());
        _agentAuthorityInternalService.Setup(x =>
                x.GetAgentAuthorityFormAsync(It.IsAny<GetAgentAuthorityFormRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(aafResponse));

        //act
        var result =
            await _sut.RetrieveFellingLicenceApplicationReviewSummaryAsync(application.Id, _internalUser, CancellationToken.None);

        //assert
        result.HasValue.Should().BeTrue();
        result.Value.FellingLicenceApplicationSummary.Should().NotBeNull();
        var summary = result.Value.FellingLicenceApplicationSummary;
        summary!.Id.Should().Be(application.Id);
        summary.AgentOrAgencyName.Should().Be(agencyModel.OrganisationName);
        summary.ApplicationReference.Should().Be(application.ApplicationReference);
        summary.PropertyName.Should().Be(application.SubmittedFlaPropertyDetail?.Name);
        summary.WoodlandOwnerId.Should().Be(application.WoodlandOwnerId);
        summary.WoodlandOwnerName.Should().Be(woodlandOwner.IsOrganisation ? woodlandOwner.OrganisationName : woodlandOwner.ContactName);
        summary.NameOfWood.Should().Be(application.SubmittedFlaPropertyDetail!.NameOfWood);
        _fellingLicenceApplicationRepository.VerifyAll();
        _woodlandOwnerService.VerifyAll();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnEmptyResult_WhenRetrieveFellingLicenceApplicationReviewSummary_GivenUserNotFound(
        FellingLicenceApplication application, 
        WoodlandOwnerModel woodlandOwner)
    {
        // Arrange
        ArrangeDefaultMocks(application, woodlandOwner, null);
        _externalUserAccountService.Setup(r => r.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount>(UserDbErrorReason.NotFound.ToString()));

        //act
        var result =
            await _sut.RetrieveFellingLicenceApplicationReviewSummaryAsync(application.Id, _internalUser, CancellationToken.None);

        //assert
        result.HasValue.Should().BeFalse();
    }

    [Theory, AutoMoqData]
    public async Task ShouldSetFellingAndRestockingDetails_WhenRetrieveFellingLicenceApplicationReviewSummary_GivenApplicationId(
        FellingLicenceApplication application, 
        WoodlandOwnerModel woodlandOwner, 
        UserAccount userAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount account)
    {
        //arrange
        ArrangeDefaultMocks(application, woodlandOwner, userAccount);
        _userAccountService.Setup(r => r.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(account));
        // fix random ConfirmedFellingDetailId
        application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments.ForEach(c =>
        {
            c.ConfirmedFellingDetails.ForEach(f =>
            {
                f.ConfirmedRestockingDetails.ForEach(r =>
                {
                    r.ConfirmedFellingDetailId = f.Id;
                });
            });
        });
        //act
        var result =
            await _sut.RetrieveFellingLicenceApplicationReviewSummaryAsync(application.Id, _internalUser, CancellationToken.None);

        //assert
        result.HasValue.Should().BeTrue();
        result.Value.FellingAndRestockingDetail.Should().NotBeNull();
        var lastCompartment = application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.ToList().Last();
        var detailsList = result.Value.FellingAndRestockingDetail!.DetailsList;
        detailsList.Should()
            .HaveCount(application.LinkedPropertyProfile.ProposedFellingDetails.Count);
        detailsList.SelectMany(x => x.RestockingDetail).Should()
            .HaveCount(application.LinkedPropertyProfile.ProposedFellingDetails
                .SelectMany(x => x.ProposedRestockingDetails).Count());
        detailsList.Should().NotContainNulls(d => d.FellingDetail);
        detailsList.Should().NotContainNulls(d => d.RestockingDetail);
        detailsList.Last().CompartmentId.Should()
            .Be(lastCompartment.CompartmentId);
        detailsList.Last().GISData.Should()
            .Be(lastCompartment.GISData);
        detailsList.Last().CompartmentName.Should()
            .Be(lastCompartment.DisplayName);
        detailsList.Last().FellingDetail.NumberOfTrees.Should()
            .Be(application.LinkedPropertyProfile!.ProposedFellingDetails!.First(d =>
                d.PropertyProfileCompartmentId == lastCompartment.CompartmentId).NumberOfTrees);
        _fellingLicenceApplicationRepository.VerifyAll();
        _woodlandOwnerService.VerifyAll();
    }

    
    [Theory, AutoMoqData]
    public async Task ShouldReturnEmptyResult_WhenWoodlandOwnerNotFound(
        FellingLicenceApplication application, WoodlandOwnerModel woodlandOwner)
    {
        //arrange
        application.WoodlandOwnerId = woodlandOwner.Id!.Value;
        SyncApplicationCompartmentData(application);
        _fellingLicenceApplicationRepository.Setup(r => r.GetAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync( Maybe<FellingLicenceApplication>.From(application));
        _woodlandOwnerService.Setup(r => r.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync( Result.Failure<WoodlandOwnerModel>(UserDbErrorReason.General.ToString()));

        //act
        var result =
            await _sut.RetrieveFellingLicenceApplicationReviewSummaryAsync(application.Id, _internalUser, CancellationToken.None);

        //assert
        result.HasValue.Should().BeFalse();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnEmptyResult_WhenUserAccountNotFound(
        FellingLicenceApplication application, WoodlandOwnerModel woodlandOwner)
    {
        //arrange
        application.WoodlandOwnerId = woodlandOwner.Id!.Value;
        SyncApplicationCompartmentData(application);
        _fellingLicenceApplicationRepository.Setup(r => r.GetAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync( Maybe<FellingLicenceApplication>.From(application));
        _woodlandOwnerService.Setup(r => r.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync( Result.Success<WoodlandOwnerModel>(woodlandOwner));
        _userAccountService.Setup(r => r.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.None);
        _internalUserAccountRepository.Setup(r => r.GetUsersWithIdsInAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>, UserDbErrorReason>(new List<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>()));

        _externalUserAccountService.Setup(r => r.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount>(UserDbErrorReason.NotFound.ToString()));

        //act
        var result =
            await _sut.RetrieveFellingLicenceApplicationReviewSummaryAsync(application.Id, _internalUser, CancellationToken.None);

        //assert
        result.HasValue.Should().BeFalse();
    }


    [Theory, AutoMoqData]
    public async Task ShouldReturnEmptyResult_WhenApplicationNotFound(
        FellingLicenceApplication application)
    {
        //arrange
        _fellingLicenceApplicationRepository.Setup(r => r.GetAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync( Maybe<FellingLicenceApplication>.None);

        //act
        var result =
            await _sut.RetrieveFellingLicenceApplicationReviewSummaryAsync(application.Id, _internalUser, CancellationToken.None);

        //assert
        result.HasValue.Should().BeFalse();
    }

    private static void SyncApplicationCompartmentData(FellingLicenceApplication application)
    {
        var i = 0;
        application.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments.ForEach(
            c =>
            {
                application.LinkedPropertyProfile!.ProposedFellingDetails![i].PropertyProfileCompartmentId =
                    c.CompartmentId;
                var fs = 0;
                application.LinkedPropertyProfile!.ProposedFellingDetails![i].FellingSpecies.ForEach(s =>
                    s.Species = TreeSpeciesFactory.SpeciesDictionary.Values.ToArray()[fs++].Code);
                application.LinkedPropertyProfile!.ProposedFellingDetails![i].ProposedRestockingDetails.ForEach(restock =>
                {
                    restock.PropertyProfileCompartmentId = c.CompartmentId;

                    var rs = 0;
                    restock.RestockingSpecies.ForEach(s =>
                    s.Species = TreeSpeciesFactory.SpeciesDictionary.Values.ToArray()[rs++].Code);
                });
                i++;
            });
        foreach (var (fellingDetail, restockingDetail) in from fellingDetail in application.LinkedPropertyProfile!.ProposedFellingDetails!
                                                          from restockingDetail in fellingDetail.ProposedRestockingDetails
                                                          select (fellingDetail, restockingDetail))
        {
            restockingDetail.ProposedFellingDetail = fellingDetail;
        }
    }
}