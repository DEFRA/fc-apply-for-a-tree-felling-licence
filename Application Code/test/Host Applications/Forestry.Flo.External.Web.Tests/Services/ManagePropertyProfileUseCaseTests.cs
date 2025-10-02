using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.PropertyProfile;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.External.Web.Tests.Services;

public class ManagePropertyProfileUseCaseTests
{
    private ManagePropertyProfileUseCase _sut = null!;
    private readonly Mock<IPropertyProfileRepository> _propertyProfileRepository;
    private readonly Mock<IGetPropertyProfiles> _getPropertyProfilesService;
    private readonly Mock<IRetrieveUserAccountsService> _retrieveUserAccountsService;
    private readonly Mock<IAuditService<ManagePropertyProfileUseCase>> _mockAuditService;
    private readonly ExternalApplicant _externalApplicant;
    private readonly Mock<IUnitOfWork> _unitOfWOrkMock;

    public ManagePropertyProfileUseCaseTests()
    {
        var fixture = new Fixture();
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            fixture.Create<string>(),
            fixture.Create<string>(),
            fixture.Create<Guid>(),
            fixture.Create<Guid>());
        _externalApplicant = new ExternalApplicant(user);
        _mockAuditService = new Mock<IAuditService<ManagePropertyProfileUseCase>>();
        _propertyProfileRepository = new Mock<IPropertyProfileRepository>();
        _getPropertyProfilesService = new();
        _retrieveUserAccountsService = new();
        _unitOfWOrkMock = new Mock<IUnitOfWork>();
        _propertyProfileRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWOrkMock.Object);
    }

    private ManagePropertyProfileUseCase CreateSut()
    {
        return new ManagePropertyProfileUseCase(
            _propertyProfileRepository.Object, 
            _getPropertyProfilesService.Object,
            _mockAuditService.Object,
            _retrieveUserAccountsService.Object,
            new RequestContext("test", new RequestUserModel(_externalApplicant.Principal)),
            new NullLogger<ManagePropertyProfileUseCase>());
    }

    [Theory, AutoData]
    public async Task ShouldCreatePropertyProfile_GivenValidPropertyProfileModel(
        PropertyProfileModel propertyProfileModel)
    {
        //Arrange
        propertyProfileModel.WoodlandOwnerId = Guid.Parse(_externalApplicant.WoodlandOwnerId!);
        _unitOfWOrkMock.Setup(c => c.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _propertyProfileRepository.Setup(r =>
                r.Add(It.Is<PropertyProfile>(p =>
                    p.Name == propertyProfileModel.Name)))
            .Returns(ModelMapping.ToPropertyProfile(propertyProfileModel));
       
        _sut = CreateSut();
        
        //Act
        var result = await _sut.CreatePropertyProfile(propertyProfileModel, _externalApplicant);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(propertyProfileModel.Id, result.Value.Id);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.CreatePropertyProfileEvent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldReturnFailedResult_WhenCreatePropertyProfileFailed_GivenNotUniqueProperty(
        PropertyProfileModel propertyProfileModel)
    {
        //Arrange
        propertyProfileModel.WoodlandOwnerId = Guid.Parse(_externalApplicant.WoodlandOwnerId!);
        _unitOfWOrkMock.Setup(c => c.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.NotUnique));
        _propertyProfileRepository.Setup(r =>
                r.Add(It.Is<PropertyProfile>(p =>
                    p.Name == propertyProfileModel.Name)))
            .Returns(ModelMapping.ToPropertyProfile(propertyProfileModel));
        _sut = CreateSut();
        
        //Act
        var result = await _sut.CreatePropertyProfile(propertyProfileModel, _externalApplicant);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equivalent(ErrorTypes.Conflict, result.Error.ErrorType);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.CreatePropertyProfileFailureEvent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldReturnFailedResult_WhenCreatePropertyProfileFailed(
        PropertyProfileModel propertyProfileModel)
    {
        //Arrange
        propertyProfileModel.WoodlandOwnerId = Guid.Parse(_externalApplicant.WoodlandOwnerId!);
        _unitOfWOrkMock.Setup(c => c.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));
        _propertyProfileRepository.Setup(r =>
                r.Add(It.Is<PropertyProfile>(p =>
                    p.Name == propertyProfileModel.Name)))
            .Returns(ModelMapping.ToPropertyProfile(propertyProfileModel));
        _sut = CreateSut();
        
        //Act
        var result = await _sut.CreatePropertyProfile(propertyProfileModel, _externalApplicant);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.InternalError, result.Error.ErrorType);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.CreatePropertyProfileFailureEvent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    
    [Fact]
    public async Task ShouldThrowException_WhenCreatePropertyProfile_GivenPropertyProfileIsNull()
    {
        //arrange
        _sut = CreateSut();
        
        //act
        var act = async () => await _sut.CreatePropertyProfile(null!, _externalApplicant);

        //assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }
    
    [Theory, AutoData]
    public async Task ShouldUpdatePropertyProfile_GivenValidPropertyProfileModel(
        PropertyProfileModel propertyProfileModel)
    {
        //Arrange
        propertyProfileModel.WoodlandOwnerId = Guid.Parse(_externalApplicant.WoodlandOwnerId!);
        _propertyProfileRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(p => p == propertyProfileModel.Id)
                , It.Is<Guid>(w => w == propertyProfileModel.WoodlandOwnerId)
                , It.IsAny<CancellationToken>()))
            .ReturnsAsync(ModelMapping.ToPropertyProfile(propertyProfileModel));


        _getPropertyProfilesService.Setup(x =>
                x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ModelMapping.ToPropertyProfile(propertyProfileModel));

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = _externalApplicant.UserAccountId!.Value,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { propertyProfileModel.WoodlandOwnerId }
        };

        _retrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccessModel);

        _unitOfWOrkMock.Setup(c => c.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _propertyProfileRepository.Setup(r =>
                r.UpdateAsync(It.Is<PropertyProfile>(p =>
                    p.Name == propertyProfileModel.Name)))
            .ReturnsAsync(Result.Success<PropertyProfile, UserDbErrorReason>(ModelMapping.ToPropertyProfile(propertyProfileModel)));
        _sut = CreateSut();
        
        //Act
        var result = await _sut.EditPropertyProfile(propertyProfileModel, _externalApplicant);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(propertyProfileModel.Name, result.Value.Name);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.UpdatePropertyProfileEvent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldReturnFailedResult_WhenUpdatePropertyProfileFailed_GivenNotExistingProfile(
        PropertyProfileModel propertyProfileModel)
    {
        //Arrange
        propertyProfileModel.WoodlandOwnerId = Guid.Parse(_externalApplicant.WoodlandOwnerId!);
        
        _propertyProfileRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(p => p == propertyProfileModel.Id)
                , It.Is<Guid>(w => w == propertyProfileModel.WoodlandOwnerId)
                , It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PropertyProfile, UserDbErrorReason>(UserDbErrorReason.NotFound));

        _getPropertyProfilesService.Setup(x =>
                x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PropertyProfile>("not found"));

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = _externalApplicant.UserAccountId!.Value,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { propertyProfileModel.WoodlandOwnerId }
        };

        _retrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccessModel);

        _sut = CreateSut();
        
        //Act
        var result = await _sut.EditPropertyProfile(propertyProfileModel, _externalApplicant);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.NotFound, result.Error.ErrorType);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdatePropertyProfileFailureEvent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldReturnFailedResult_WhenUpdatePropertyProfileFailed_GivenWrongWoodlandOwner(
        PropertyProfileModel propertyProfileModel, Guid wrongWoodlandOwnerId)
    {
        //Arrange
        propertyProfileModel.WoodlandOwnerId = wrongWoodlandOwnerId;

        var badUserAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = _externalApplicant.UserAccountId!.Value,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { Guid.NewGuid() }
        };

        _retrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(badUserAccessModel);

        _sut = CreateSut();
        
        //Act
        var result = await _sut.EditPropertyProfile(propertyProfileModel, _externalApplicant);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.NotAuthorised, result.Error.ErrorType);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(
                It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdatePropertyProfileFailureEvent),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    [Fact]
    public async Task ShouldThrowException_WhenUpdatePropertyProfile_GivenPropertyProfileIsNull()
    {
        //arrange
        _sut = CreateSut();
        
        //act
       var act = async () => await _sut.EditPropertyProfile(null!, _externalApplicant);

        //assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }
    
    [Theory, AutoData]
    public async Task ShouldRetrievePropertyProfile_GivenValidPropertyProfileId(
        PropertyProfileModel propertyProfileModel)
    {
        //Arrange
        propertyProfileModel.WoodlandOwnerId = Guid.Parse(_externalApplicant.WoodlandOwnerId!);
      
        _getPropertyProfilesService.Setup(x => x.GetPropertyByIdAsync(It.Is<Guid>(g => g == propertyProfileModel.Id),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ModelMapping.ToPropertyProfile(propertyProfileModel)));

        _sut = CreateSut();

        //Act
        var result = await _sut.RetrievePropertyProfileAsync(propertyProfileModel.Id, _externalApplicant);

        //Assert
        Assert.True(result.HasValue);
    }
    
    [Theory, AutoData]
    public async Task ShouldNotRetrievePropertyProfile_GivenValidPropertyProfileId_AndNotValidWoodlandOwner(
        PropertyProfileModel propertyProfileModel)
    {
        //Arrange
        _sut = CreateSut();
        _propertyProfileRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(p => p == propertyProfileModel.Id)
                , It.Is<Guid>(w => w == propertyProfileModel.WoodlandOwnerId)
                , It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PropertyProfile, UserDbErrorReason>(UserDbErrorReason.NotFound));
        
        //Act
        var result = await _sut.RetrievePropertyProfileAsync(propertyProfileModel.Id, _externalApplicant);

        //Assert
        Assert.False(result.HasValue);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldRetrievePropertyCompartments_GivenPropertyProfileId(
        PropertyProfile propertyProfile)
    {
        //arrange
        propertyProfile.WoodlandOwnerId = Guid.Parse(_externalApplicant.WoodlandOwnerId!);

        _getPropertyProfilesService.Setup(x => x.GetPropertyByIdAsync(It.Is<Guid>(g => g==propertyProfile.Id),
                It.IsAny<UserAccessModel>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _sut = CreateSut();

        //act
        var result = await _sut.RetrievePropertyProfileCompartments(propertyProfile.Id, _externalApplicant);

        //assert
        Assert.True(result.HasValue);
        Assert.Equal(propertyProfile.Id, result.Value.Id);
        Assert.Equal(propertyProfile.Name, result.Value.Name);
        Assert.Equal(propertyProfile.WoodlandManagementPlanReference, result.Value.WoodlandManagementPlanReference);
        Assert.Equal(propertyProfile.WoodlandCertificationSchemeReference, result.Value.WoodlandCertificationSchemeReference);
        Assert.Equivalent(ModelMapping.ToCompartmentModelList(propertyProfile.Compartments), result.Value.Compartments);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnEmptyResult_WhenRetrievePropertyCompartments_GivenProfileDoesNotBelongToWoodlandOwner(
        PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfileRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(p => p == propertyProfile.Id)
                , It.Is<Guid>(w => w == Guid.Parse(_externalApplicant.WoodlandOwnerId!))
                , It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PropertyProfile, UserDbErrorReason>(UserDbErrorReason.NotFound));

        _getPropertyProfilesService.Setup(x => x.GetPropertyByIdAsync(It.Is<Guid>(g => g == propertyProfile.Id),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PropertyProfile>("failure"));

        _sut = CreateSut();

        //act
        var result = await _sut.RetrievePropertyProfileCompartments(propertyProfile.Id, _externalApplicant);

        //assert
        Assert.False(result.HasValue);
    }
}