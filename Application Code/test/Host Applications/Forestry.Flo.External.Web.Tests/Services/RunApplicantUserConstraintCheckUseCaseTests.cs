using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.External.Web.Tests.Services;

public class RunApplicantUserConstraintCheckUseCaseTests
{
    private static readonly Fixture FixtureInstance = new();
    private readonly Mock<ILandInformationSearch> _landInformationSearchService;
    private readonly Mock<IAuditService<ConstraintCheckerService>> _auditMock;
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository;
    private readonly Mock<IPropertyProfileRepository> _propertyProfileRepository;
    private readonly ExternalApplicant _user;
    private readonly LandInformationSearchOptions _settings = new() { DeepLinkUrlAndPath = "http://www.tempuri.org/abc", LisConfig = "someValue"};

    public RunApplicantUserConstraintCheckUseCaseTests()
    {
        FixtureInstance.Customizations.Add(new CompartmentEntitySpecimenBuilder());
        FixtureInstance.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => FixtureInstance.Behaviors.Remove(b));
        FixtureInstance.Behaviors.Add(new OmitOnRecursionBehavior());
        _auditMock = new Mock<IAuditService<ConstraintCheckerService>>();
        _landInformationSearchService = new Mock<ILandInformationSearch>();
        _fellingLicenceApplicationRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
        _propertyProfileRepository = new Mock<IPropertyProfileRepository>();
        var userPrincipal = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(localAccountId:Guid.NewGuid());
        _user = new ExternalApplicant(userPrincipal);
        _fellingLicenceApplicationRepository.Reset();
    }

    [Theory, AutoMoqData]
    public async Task WhenCompletesRequest(FellingLicenceApplication application)
    {
        //Arrange
        EnsurePreSubmittedCompartmentsInApplication(application);
        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync( Maybe.From(application));

        foreach (var submittedFlaPropertyCompartment in application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!)
        {
            submittedFlaPropertyCompartment.GISData = 
                "{\"spatialReference\":{\"latestWkid\":27700,\"wkid\":27700},\"rings\":[[[1,1], [1,2], [2,2],[2,1], [1,1]]]}";
        }

        var expectedQueryParams = QueryHelpers.ParseQuery($"isFlo=true&config={_settings.LisConfig}&caseId={application.Id}");
        var sut = CreateSut();

        //Act
        var result = await sut.ExecuteConstraintsCheckAsync(_user, application.Id, new CancellationToken());
        
        //Assert
        Assert.True(result.IsSuccess);
        Assert.IsType<RedirectResult>(result.Value);

        var actualQueryParams = QueryHelpers.ParseQuery(new Uri(result.Value.Url).Query);

        Assert.True(actualQueryParams.All(e => expectedQueryParams.Contains(e)));
        Assert.Equal(_settings.DeepLinkUrlAndPath, result.Value.Url.Split('?')[0]);
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotCompleteRequest(FellingLicenceApplication application)
    {
        //Arrange
        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync( Maybe.None);

        var sut = CreateSut();

        //Act
        var result = await sut.ExecuteConstraintsCheckAsync(_user, application.Id, new CancellationToken());

        //Assert
        Assert.True(result.IsFailure);
    }

    private RunApplicantConstraintCheckUseCase CreateSut(
        bool landInformationSearchIsSuccess = true)
    {
        if (landInformationSearchIsSuccess)
        {
            _landInformationSearchService.Setup(x => x.AddFellingLicenceGeometriesAsync(It.IsAny<Guid>(),
                It.IsAny<List<InternalCompartmentDetails<Polygon>>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(new Result<CreateUpdateDeleteResponse<int>>());
        }

        var constraintCheckerService = new ConstraintCheckerService(
            _landInformationSearchService.Object,
            Options.Create(_settings),
            _fellingLicenceApplicationRepository.Object,
            _propertyProfileRepository.Object,
            _auditMock.Object,
            new RequestContext("test", new RequestUserModel(_user.Principal)),
            new NullLogger<ConstraintCheckerService>());

        return new RunApplicantConstraintCheckUseCase(constraintCheckerService,
            new NullLogger<RunApplicantConstraintCheckUseCase>());
    }

    private void EnsurePreSubmittedCompartmentsInApplication(FellingLicenceApplication application)
    {
        var propertyProfile = FixtureInstance.Create<PropertyProfile>();
        propertyProfile.Compartments.Clear();

        //ensure enough property compartments exist to match the count of ProposedFellingDetails by AutoFixture FLA 
        var propertyComps = FixtureInstance.CreateMany<Compartment>(application.LinkedPropertyProfile.ProposedFellingDetails.Count).ToList();
        propertyProfile.Compartments.AddRange(propertyComps);

        //force the pfd compartment Ids to match the actual compartmentIds
        const int i = 0;
        foreach (var pfd in application.LinkedPropertyProfile.ProposedFellingDetails)
        {
            pfd.PropertyProfileCompartmentId = propertyComps.ElementAt(i).Id;
        }

        _propertyProfileRepository.Setup(x => x.GetByIdAsync(application.LinkedPropertyProfile.PropertyProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PropertyProfile, UserDbErrorReason>(propertyProfile));
    }

}