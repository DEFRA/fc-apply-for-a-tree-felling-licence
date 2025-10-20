using CSharpFunctionalExtensions;
using Forestry.Flo.Tests.Common;
using Moq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public partial class GetWoodlandOfficerReviewServiceTests
{
    [Theory, AutoMoqData]
    public async Task GetPublicRegisterShouldReturnFailureWhenRepositoryThrows(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var result = await sut.GetPublicRegisterDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetPublicRegisterShouldReturnMaybeNoneIfNoEntryExists(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        var result = await sut.GetPublicRegisterDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasNoValue);

        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetPublicRegisterShouldReturnExpectedModelIfEntryExists(
        Guid applicationId,
        PublicRegister publicRegister)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(publicRegister));

        var result = await sut.GetPublicRegisterDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasValue);

        var actual = result.Value.Value;
        Assert.Equal(publicRegister.WoodlandOfficerSetAsExemptFromConsultationPublicRegister, actual.WoodlandOfficerSetAsExemptFromConsultationPublicRegister);
        Assert.Equal(publicRegister.WoodlandOfficerConsultationPublicRegisterExemptionReason, actual.WoodlandOfficerConsultationPublicRegisterExemptionReason);
        Assert.Equal(publicRegister.ConsultationPublicRegisterPublicationTimestamp, actual.ConsultationPublicRegisterPublicationTimestamp);
        Assert.Equal(publicRegister.ConsultationPublicRegisterExpiryTimestamp, actual.ConsultationPublicRegisterExpiryTimestamp);
        Assert.Equal(publicRegister.ConsultationPublicRegisterRemovedTimestamp, actual.ConsultationPublicRegisterRemovedTimestamp);

        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }
}