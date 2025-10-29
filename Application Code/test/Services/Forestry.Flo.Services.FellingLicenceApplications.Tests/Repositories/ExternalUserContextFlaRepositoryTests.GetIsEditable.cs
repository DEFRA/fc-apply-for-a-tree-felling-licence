using System.Threading;
using AutoFixture;
using System.Threading.Tasks;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public partial class ExternalUserContextFlaRepositoryTests
{
    [Theory]
    [InlineData(FellingLicenceStatus.Received, false)]
    [InlineData(FellingLicenceStatus.Draft, true)]
    [InlineData(FellingLicenceStatus.Submitted, false)]
    [InlineData(FellingLicenceStatus.WithApplicant, true)]
    [InlineData(FellingLicenceStatus.WoodlandOfficerReview, false)]
    [InlineData(FellingLicenceStatus.SentForApproval, false)]
    [InlineData(FellingLicenceStatus.Approved, false)]
    [InlineData(FellingLicenceStatus.Refused, false)]
    [InlineData(FellingLicenceStatus.Withdrawn, false)]
    [InlineData(FellingLicenceStatus.ReturnedToApplicant, true)]
    [InlineData(FellingLicenceStatus.AdminOfficerReview, false)]
    [InlineData(FellingLicenceStatus.ReferredToLocalAuthority, false)]
    public async Task GetIsEditableReturnsExpectedResult(FellingLicenceStatus currentStatus, bool expectedResult)
    {
        var status = _fixture
            .Build<StatusHistory>()
            .With(x => x.Status, currentStatus)
            .Without(x => x.FellingLicenceApplication)
            .Create();
        var application = _fixture
            .Build<FellingLicenceApplication>()
            .Without(x => x.StatusHistories)
            .Create();

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        status.FellingLicenceApplicationId = application.Id;
        application.StatusHistories.Add(status);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.GetIsEditable(application.Id, CancellationToken.None);

        Assert.Equal(expectedResult, result);
    }

}