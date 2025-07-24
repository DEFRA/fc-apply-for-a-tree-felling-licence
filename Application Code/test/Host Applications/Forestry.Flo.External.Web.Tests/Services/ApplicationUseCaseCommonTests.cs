using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.External.Web.Tests.Services;

public class ApplicationUseCaseCommonTests
{
    private readonly ApplicationUseCaseCommon _sut;
    private readonly Mock<IRetrieveUserAccountsService> _retrieveUserAccountsService;
    private readonly Mock<IGetFellingLicenceApplicationForExternalUsers> _getFellingLicenceApplicationForExternalUsersService;
    private readonly Mock<IRetrieveWoodlandOwners> _retrieveWoodlandOwnersService;
    private readonly Mock<IGetPropertyProfiles> _getPropertyProfilesService;
    private readonly Mock<IGetCompartments> _getCompartmentsService;
    private readonly Mock<IAgentAuthorityService> _agentAuthorityService;

    public ApplicationUseCaseCommonTests()
    {
        _retrieveUserAccountsService = new Mock<IRetrieveUserAccountsService>();
        _retrieveWoodlandOwnersService = new Mock<IRetrieveWoodlandOwners>();
        _getFellingLicenceApplicationForExternalUsersService = new Mock<IGetFellingLicenceApplicationForExternalUsers>();
        _getPropertyProfilesService = new Mock<IGetPropertyProfiles>();
        _getCompartmentsService = new Mock<IGetCompartments>();
        _agentAuthorityService = new();

        _sut = new ApplicationUseCaseCommon(
            _retrieveUserAccountsService.Object, 
            _retrieveWoodlandOwnersService.Object,
            _getFellingLicenceApplicationForExternalUsersService.Object, 
            _getPropertyProfilesService.Object, 
            _getCompartmentsService.Object,
            _agentAuthorityService.Object,
            new NullLogger<ApplicationUseCaseCommon>());
    }

    [Theory, AutoMoqData]
    public async Task UseCaseCommon_EnsureApplicationNotSubmitted_ShouldFail_GivenApplicationInUneditableState(UserAccessModel userAccessModel)
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(localAccountId: Guid.NewGuid());
        var externalApplicant = new ExternalApplicant(user);

        var testFellingLicenceApplicationId = Guid.NewGuid();

        _retrieveUserAccountsService
            .Setup(s => s.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _getFellingLicenceApplicationForExternalUsersService.Setup(x => x.GetIsEditable(
            It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), CancellationToken.None)).ReturnsAsync(false);
        
        var result = await _sut.EnsureApplicationIsEditable(testFellingLicenceApplicationId, externalApplicant, CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsService.Verify(x => x.RetrieveUserAccessAsync(externalApplicant.UserAccountId!.Value, It.IsAny<CancellationToken>()),
            Times.Once);

        _getFellingLicenceApplicationForExternalUsersService.Verify(x => x.GetIsEditable(testFellingLicenceApplicationId, userAccessModel, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task UseCaseCommon_EnsureApplicationNotSubmitted_ShouldFail_GivenCannotRetrieveUserAccess()
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(localAccountId: Guid.NewGuid());
        var externalApplicant = new ExternalApplicant(user);

        var testFellingLicenceApplicationId = Guid.NewGuid();

        _retrieveUserAccountsService
            .Setup(s => s.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccessModel>("error"));

        var result = await _sut.EnsureApplicationIsEditable(testFellingLicenceApplicationId, externalApplicant, CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsService.Verify(x => x.RetrieveUserAccessAsync(externalApplicant.UserAccountId!.Value, It.IsAny<CancellationToken>()),
            Times.Once);

        _getFellingLicenceApplicationForExternalUsersService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task UseCaseCommon_EnsureApplicationNotSubmitted_ShouldBeSuccess_GivenApplicationInEditableState(UserAccessModel userAccessModel)
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(localAccountId: Guid.NewGuid());
        var externalApplicant = new ExternalApplicant(user);

        var testFellingLicenceApplicationId = Guid.NewGuid();

        _retrieveUserAccountsService
            .Setup(s => s.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _getFellingLicenceApplicationForExternalUsersService.Setup(x => x.GetIsEditable(
            It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), CancellationToken.None)).ReturnsAsync(true);

        var result = await _sut.EnsureApplicationIsEditable(testFellingLicenceApplicationId, externalApplicant, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveUserAccountsService.Verify(x => x.RetrieveUserAccessAsync(externalApplicant.UserAccountId!.Value, It.IsAny<CancellationToken>()),
            Times.Once);

        _getFellingLicenceApplicationForExternalUsersService.Verify(x => x.GetIsEditable(testFellingLicenceApplicationId, userAccessModel, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}