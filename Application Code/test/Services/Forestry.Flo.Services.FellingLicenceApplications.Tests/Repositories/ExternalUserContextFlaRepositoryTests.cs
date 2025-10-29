using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Tests.Common;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public partial class ExternalUserContextFlaRepositoryTests
{
    private readonly ExternalUserContextFlaRepository _sut;
    private readonly FellingLicenceApplicationsContext _fellingLicenceApplicationsContext;
    private readonly Mock<IApplicationReferenceHelper> _referenceGenerator;
    private readonly Mock<IFellingLicenceApplicationReferenceRepository> _mockReferenceRepository;
    private readonly IFixture _fixture;
    
    public ExternalUserContextFlaRepositoryTests()
    {
        _fellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _referenceGenerator = new Mock<IApplicationReferenceHelper>();
        _referenceGenerator.Setup(r => r.GenerateReferenceNumber(It.IsAny<FellingLicenceApplication>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns("test");
        _mockReferenceRepository = new Mock<IFellingLicenceApplicationReferenceRepository>();
        _mockReferenceRepository
            .Setup(x => x.GetNextApplicationReferenceIdValueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _fixture = new Fixture().Customize(new CompositeCustomization(
            new AutoMoqCustomization(),
            new SupportMutableValueTypesCustomization()));

        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _fixture.Customize<DateOnly>(composer => composer.FromFactory<DateTime>(DateOnly.FromDateTime));

        _sut = new ExternalUserContextFlaRepository(_fellingLicenceApplicationsContext, _referenceGenerator.Object, _mockReferenceRepository.Object);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFellingLicenceApplication_GivenWoodlandOwnerId(List<FellingLicenceApplication> licenceApplications, Guid woodlandOwnerId)
    {
        //arrange
        licenceApplications.ForEach(ap => ap.WoodlandOwnerId = woodlandOwnerId);
        _fellingLicenceApplicationsContext.AddRange(licenceApplications);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        //act
        var result = await _sut.ListAsync(woodlandOwnerId, CancellationToken.None);

        //assert
        Assert.NotEmpty(result);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnEmptyCollection_WhenNoApplicationsFound(Guid woodlandOwnerId)
    {
        //arrange

        //act
        var result = await _sut.ListAsync(woodlandOwnerId, CancellationToken.None);

        //assert
        Assert.Empty(result);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldIncludeDependentObjectsIn_Results_GivenWoodlandOwnerId(List<FellingLicenceApplication> licenceApplications, Guid woodlandOwnerId)
    {
        //arrange
        licenceApplications.ForEach(ap => ap.WoodlandOwnerId = woodlandOwnerId);
        _fellingLicenceApplicationsContext.AddRange(licenceApplications);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        //act
        var result = (await _sut.ListAsync(woodlandOwnerId, CancellationToken.None)).ToList();

        //assert
        Assert.NotEmpty(result);
        Assert.NotEmpty(result.First().StatusHistories);
        Assert.NotNull(result.First().LinkedPropertyProfile);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldUpdateApplication_GivenExistingApplication(FellingLicenceApplication fellingLicenceApplication)
    {
        //arrange
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(fellingLicenceApplication);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();
        var updatedReference = "111";
        fellingLicenceApplication.ApplicationReference = updatedReference;
        
        //act
         _sut.Update(fellingLicenceApplication);
        var saveResult = await _sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
         
        //assert
        Assert.True(saveResult.IsSuccess);
        var result = await _fellingLicenceApplicationsContext.FellingLicenceApplications.FindAsync(fellingLicenceApplication.Id);
        Assert.NotNull(result);
        Assert.Equal(updatedReference, result!.ApplicationReference);
    }

    [Theory, AutoMoqData]
    public async Task CanDeleteSubmittedFlaPropertyProfileBeforeResubmitting(
        FellingLicenceApplication application)
    {
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        await _sut.DeleteSubmittedFlaPropertyDetailAsync(application.SubmittedFlaPropertyDetail, CancellationToken.None);

        Assert.Equal(1, _fellingLicenceApplicationsContext.FellingLicenceApplications.Count());
        Assert.Empty(_fellingLicenceApplicationsContext.SubmittedFlaPropertyDetails);
        Assert.Empty(_fellingLicenceApplicationsContext.SubmittedFlaPropertyCompartments);

    }
}