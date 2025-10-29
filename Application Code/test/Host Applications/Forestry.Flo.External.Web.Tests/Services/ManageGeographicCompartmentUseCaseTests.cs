using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.Compartment;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.Gis.Models.Internal.Request;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using static MassTransit.ValidationResultExtensions;
using Result = CSharpFunctionalExtensions.Result;

namespace Forestry.Flo.External.Web.Tests.Services;

public class ManageGeographicCompartmentUseCaseTests
{
    private ManageGeographicCompartmentUseCase _sut = null!;
    private readonly Mock<IPropertyProfileRepository> _propertyProfileRepository;
    private readonly Mock<IRetrieveUserAccountsService> _retrieveUserAccountsService;
    private readonly Mock<IGetPropertyProfiles> _getPropertyProfilesService;
    private readonly Mock<IGetCompartments> _getCompartmentsService;

    private readonly Mock<ICompartmentRepository> _compartmentRepository;
    private readonly Mock<IAuditService<ManageGeographicCompartmentUseCase>> _mockAuditService;
    private readonly ExternalApplicant _externalApplicant;
    private readonly Mock<IUnitOfWork> _unitOfWOrkMock;

    public ManageGeographicCompartmentUseCaseTests()
    {
        var fixture = new Fixture();
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            fixture.Create<string>(),
            fixture.Create<string>(),
            fixture.Create<Guid>(),
            fixture.Create<Guid>());
        _externalApplicant = new ExternalApplicant(user);
        _mockAuditService = new Mock<IAuditService<ManageGeographicCompartmentUseCase>>();
        _propertyProfileRepository = new Mock<IPropertyProfileRepository>();
        _compartmentRepository = new Mock<ICompartmentRepository>();
        _retrieveUserAccountsService = new Mock<IRetrieveUserAccountsService>();
        _getPropertyProfilesService = new Mock<IGetPropertyProfiles>();
        _getCompartmentsService = new();

        _unitOfWOrkMock = new Mock<IUnitOfWork>();
        _propertyProfileRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWOrkMock.Object);

        _compartmentRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWOrkMock.Object);
    }

    private ManageGeographicCompartmentUseCase CreateSut()
    {
        return new ManageGeographicCompartmentUseCase(
            new GetPropertyProfilesService(_propertyProfileRepository.Object),
            _getCompartmentsService.Object,
            _retrieveUserAccountsService.Object, 
            _compartmentRepository.Object,
            _mockAuditService.Object,
            new RequestContext("test", new RequestUserModel(_externalApplicant.Principal)),
            new NullLogger<ManageGeographicCompartmentUseCase>());
    }

    [Theory, AutoData]
    public async Task ShouldCreateCompartment_GivenValidCompartmentModel(CompartmentModel compartmentModel)
    {
        //arrange
        _unitOfWOrkMock.Setup(c => c.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _compartmentRepository.Setup(r =>
            r.Add(It.Is<Compartment>(p =>
                p.CompartmentNumber == compartmentModel.CompartmentNumber)))
            .Returns(ModelMapping.ToCompartment(compartmentModel));
         _sut = CreateSut();

        //act
        var result = await _sut.CreateCompartmentAsync(compartmentModel, _externalApplicant);

        //assert
        Assert.True(result.IsSuccess);
        Assert.Equal(compartmentModel.Id, result.Value);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.CreateCompartmentEvent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessResult_WhenVerifyPropertyProfile_GivenProfileFoundAndBelongsToWoodlandOwner(
        PropertyProfile propertyProfile)
    {
        //arrange
        propertyProfile.WoodlandOwnerId = Guid.Parse(_externalApplicant.WoodlandOwnerId!);

        _propertyProfileRepository.Setup(r =>
                r.CheckUserCanAccessPropertyProfileAsync(It.Is<Guid>(id => id == propertyProfile.Id),
                    It.IsAny<UserAccessModel>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _propertyProfileRepository.Setup(r =>
                r.GetByIdAsync(It.Is<Guid>(id => id == propertyProfile.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(propertyProfile);

        _sut = CreateSut();

        //act
        var result = await _sut.VerifyUserPropertyProfileAsync(_externalApplicant, propertyProfile.Id);

        //assert
        Assert.True(result.IsSuccess);
        Assert.Equivalent(ModelMapping.ToPropertyProfileModel(propertyProfile), result.Value);
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailedResult_WhenVerifyPropertyProfile_GivenProfileDoesNotExist(
        Guid propertyProfileId)
    {
        //arrange
        _getPropertyProfilesService.Setup(r =>
                r.GetPropertyByIdAsync(It.Is<Guid>(id => id == propertyProfileId), It.IsAny<UserAccessModel>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PropertyProfile>("error"));
        _sut = CreateSut();

        //act
        var result = await _sut.VerifyUserPropertyProfileAsync(_externalApplicant, propertyProfileId);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.NotFound, result.Error.ErrorType);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailedResult_WhenVerifyPropertyProfile_GivenProfileDoesBelongToWoodlandOwner(
        PropertyProfile propertyProfile)
    {
        //arrange
        propertyProfile.WoodlandOwnerId = Guid.Parse(_externalApplicant.WoodlandOwnerId!);
        _propertyProfileRepository.Setup(r =>
                r.CheckUserCanAccessPropertyProfileAsync(It.Is<Guid>(id => id == propertyProfile.Id),
                    It.IsAny<UserAccessModel>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));
        _sut = CreateSut();

        //act
        var result = await _sut.VerifyUserPropertyProfileAsync(_externalApplicant, propertyProfile.Id);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.NotFound, result.Error.ErrorType);
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailedResult_WhenCreateCompartmentFailedDuringSaving(
        CompartmentModel compartmentModel)
    {
        //arrange
        _sut = CreateSut();
        _compartmentRepository.Setup(r => r.Add(It.Is<Compartment>(c => c.Id == compartmentModel.Id)))
            .Returns(ModelMapping.ToCompartment(compartmentModel));
        _unitOfWOrkMock.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        //act
        var result = await _sut.CreateCompartmentAsync(compartmentModel, _externalApplicant);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.InternalError, result.Error.ErrorType);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.CreateCompartmentFailureEvent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ShouldThrowException_WhenCreateCompartment_GivenCompartmentIsNull()
    {
        //arrange
        _sut = CreateSut();

        //act
        var act = async () => await _sut.CreateCompartmentAsync(null!, _externalApplicant);

        //assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailedResult_WhenCreateCompartmentFailedDuringSaving_Bulk(
    ImportCompartmentModel importCompartmentModel, Guid propertyID)
    {
        //arrange
        _sut = CreateSut();
        _unitOfWOrkMock.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        //act
        var result = await _sut.CreateCompartmentAsync(importCompartmentModel,propertyID, _externalApplicant);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equivalent(ErrorTypes.InternalError, result.Error.ErrorType);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.CreateCompartmentFailureEvent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [Theory, AutoData]
    public async Task ShouldThrowException_WhenVerifyUserPropertyProfile_GivenExternalApplicantIsNull(
        Guid propertyProfileId)
    {
        //arrange
        _sut = CreateSut();

        //act
        var act = async () => await _sut.VerifyUserPropertyProfileAsync(null!, propertyProfileId);

        //assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }

    [Theory, AutoMoqData]
    public async Task ShouldRetrievePropertyCompartments_GivenCompartmentId(
        Compartment compartment)
    {
        //arrange
        var woodlandOwnerId = Guid.Parse(_externalApplicant.WoodlandOwnerId!);
        compartment.PropertyProfile.WoodlandOwnerId = woodlandOwnerId;

        _getCompartmentsService.Setup(x => x.GetCompartmentByIdAsync(
                It.Is<Guid>(id => id == compartment.Id),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(compartment);

        _sut = CreateSut();

        //act
        var result = await _sut.RetrieveCompartmentAsync(compartment.Id, _externalApplicant);

        //assert
        Assert.True(result.HasValue);
        Assert.Equal(compartment.Id, result.Value.Id);
        Assert.Equal(compartment.PropertyProfile.Name, result.Value.PropertyProfileName);
    }

    [Theory, AutoMoqData]
    public async Task ShouldRetrieveEmptyResults_WhenRetrievePropertyCompartments_GivenProfileDoesBelongToWoodlandOwner(
        PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfileRepository.Setup(r =>
                r.GetAsync(It.Is<Guid>(id => id == propertyProfile.Id),
                    It.Is<Guid>(w => w == propertyProfile.WoodlandOwnerId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(propertyProfile);
        _sut = CreateSut();

        //act
        var result = await _sut.RetrieveCompartmentAsync(propertyProfile.Compartments.First().Id, _externalApplicant);

        //assert
        Assert.False(result.HasValue);
    }
}