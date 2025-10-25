using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.External.Web.Tests.Services;

public partial class TenYearLicenceUseCaseTests
{
    [Theory, AutoMoqData]
    public async Task IsForTenYearLicenceApplicationWhenCannotRetrieveUserAccess(
        Guid applicationId,
        Guid userId,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccessModel>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.IsTenYearLicenceApplicationAsync(
            user,
            applicationId,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task IsForTenYearLicenceApplicationWhenCannotRetrieveApplication(
        Guid applicationId,
        Guid userId,
        UserAccessModel userAccess,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccess));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.IsTenYearLicenceApplicationAsync(
            user,
            applicationId,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccess, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, CombinatorialData]
    public async Task IsForTenYearLicenceApplicationWhenSuccess(
        [CombinatorialValues(null, true, false)] bool? isTenYearLicence)
    {
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var application = new FellingLicenceApplication
        {
            IsForTenYearLicence = isTenYearLicence
        };
        var userAccess = _fixture.Create<UserAccessModel>();

        application.IsForTenYearLicence = isTenYearLicence;

        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccess));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        var user = GetExternalApplicant(userId);

        var result = await sut.IsTenYearLicenceApplicationAsync(
            user,
            applicationId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(isTenYearLicence == true, result.Value);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccess, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }
}