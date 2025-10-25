using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public partial class ExternalUserContextFlaRepositoryTests
{
    [Theory, AutoMoqData]
    public async Task CreateAndSaveWhenDatabaseSuccess(
        FellingLicenceApplication entity,
        string postFix,
        int offset,
        long expectedReferenceCounter,
        string expectedReference)
    {
        _mockReferenceRepository
            .Setup(x => x.GetNextApplicationReferenceIdValueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReferenceCounter);

        _referenceGenerator.Setup(x => x.GenerateReferenceNumber(It.IsAny<FellingLicenceApplication>(),
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(expectedReference);

        var result = await _sut.CreateAndSaveAsync(entity, postFix, offset, CancellationToken.None);
        
        Assert.Equal(Result.Success<FellingLicenceApplication, UserDbErrorReason>(entity), result);

        _mockReferenceRepository
            .Verify(x => x.GetNextApplicationReferenceIdValueAsync(entity.CreatedTimestamp.Year, It.IsAny<CancellationToken>()), Times.Once);
        _mockReferenceRepository.VerifyNoOtherCalls();

        _referenceGenerator
            .Verify(x => x.GenerateReferenceNumber(entity, expectedReferenceCounter, postFix, offset), Times.Once);
        _referenceGenerator.VerifyNoOtherCalls();
    }
}