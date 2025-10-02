using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateCentrePointServiceTests
{
    private readonly ExternalUserContextFlaRepository _externalUserContextFlaRepository;
    private readonly FellingLicenceApplicationsContext _fellingLicenceApplicationsContext;
    private readonly UpdateCentrePointService _updateCentrePointService;

    public UpdateCentrePointServiceTests()
    {
        _fellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();

        var referenceGenerator = new Mock<IApplicationReferenceHelper>();
        referenceGenerator.Setup(r => r.GenerateReferenceNumber(It.IsAny<FellingLicenceApplication>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns("test");

        var mockReferenceRepository = new Mock<IFellingLicenceApplicationReferenceRepository>();
        mockReferenceRepository
            .Setup(x => x.GetNextApplicationReferenceIdValueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _externalUserContextFlaRepository = new ExternalUserContextFlaRepository(_fellingLicenceApplicationsContext, referenceGenerator.Object, mockReferenceRepository.Object);
        _updateCentrePointService = new UpdateCentrePointService(_externalUserContextFlaRepository, new GetFellingLicenceApplicationForExternalUsersService(_externalUserContextFlaRepository));
    }

    [Theory]
    [AutoMoqData]
    public async Task ShouldUpdateFlaWithCentrePoint(FellingLicenceApplication app, string areaCode, string adminRegion, string centrePt, string osGrid)
    {
        _fellingLicenceApplicationsContext.Add(app);
        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = app.WoodlandOwnerId,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { app.WoodlandOwnerId }
        };
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _updateCentrePointService.UpdateCentrePointAsync(
            app.Id, userAccessModel, areaCode, adminRegion, centrePt, osGrid, CancellationToken.None);

        var updatedFlaResult = await _externalUserContextFlaRepository.GetAsync(app.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.True(updatedFlaResult.HasValue);

        var updatedFla = updatedFlaResult.Value;

        Assert.NotNull(updatedFla);
        Assert.Equal(centrePt, updatedFla!.CentrePoint);
        Assert.Equal(osGrid, updatedFla!.OSGridReference);
        Assert.Equal(areaCode, updatedFla!.AreaCode);
        Assert.Equal(adminRegion, updatedFla!.AdministrativeRegion);
    }

    [Theory]
    [AutoMoqData]
    public async Task ShouldReturnFailure_WhenSubmittedPropertyNotRetrieved(FellingLicenceApplication app, string areaCode, string adminRegion, string centrePt, string osGrid)
    {
        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = app.WoodlandOwnerId,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { app.WoodlandOwnerId }
        };

        var result = await _updateCentrePointService.UpdateCentrePointAsync(app.Id, userAccessModel, areaCode, adminRegion, centrePt, osGrid, CancellationToken.None);

        var updatedFla = await _externalUserContextFlaRepository.GetAsync(app.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.True(updatedFla.HasNoValue);
    }
}