using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public partial class ExternalUserContextFlaRepositoryTests
{
    [Theory, AutoMoqData]
    public async Task CanGetApplicationCompartmentDetail(FellingLicenceApplication application)
    {

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var queriedCompartmentId = application.LinkedPropertyProfile.ProposedFellingDetails.First().PropertyProfileCompartmentId;
        var expectedFelling = application.LinkedPropertyProfile.ProposedFellingDetails.First();

        var result = await _sut.GetApplicationCompartmentDetailAsync(application.Id, application.WoodlandOwnerId, queriedCompartmentId, CancellationToken.None);

        Assert.True(result.HasValue);
        Assert.Equal(application.Id, result.Value.ApplicationId);
        Assert.Equal(application.StatusHistories, result.Value.StatusHistories);
        Assert.Equal(application.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(application.LinkedPropertyProfile.PropertyProfileId, result.Value.PropertyProfileId);
        Assert.Equivalent(expectedFelling, result.Value.ProposedFellingDetails.Single());

    }

    [Theory, AutoMoqData]
    public async Task GetApplicationCompartmentDetailShouldReturnNone_WhenNoApplicationExistsForId(Guid applicationId, Guid woodlandOwnerId, Guid compartmentId)
    {
        //arrange
        //act
        var result = await _sut.GetApplicationCompartmentDetailAsync(applicationId, woodlandOwnerId, compartmentId, CancellationToken.None);
        //assert
        Assert.True(result.HasNoValue);
    }
}