using AutoFixture;
using Forestry.Flo.External.Web.Models.FcUser;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.External.Web.Services.FcUser;
using Forestry.Flo.Services.Applicants.Entities;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.External.Web.Tests.Services.FcUser;

public class GetDataForFcUserHomepageUseCaseTests
{
    private readonly Mock<IApplicantRepository> _mockApplicantRepo = new();

    private readonly Fixture _fixture = new();
    private ExternalApplicant? _externalApplicant;

    [Theory, AutoMoqData]
    public async Task ReturnsFailureForNonFcUser(FcUserHomePageSearchAndSortModel searchModel)
    {
        var sut = CreateSut(false);

        var result = await sut.ExecuteAsync(
            _externalApplicant!,
            searchModel,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockApplicantRepo.Verify(r =>
            r.GetApplicantsCountAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);

        _mockApplicantRepo.Verify(x => x.GetApplicants(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailureWhenGetApplicantCountThrows(FcUserHomePageSearchAndSortModel searchModel)
    {
        var sut = CreateSut();

        _mockApplicantRepo.Setup(x => x.GetApplicantsCountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var result = await sut.ExecuteAsync(
            _externalApplicant!,
            searchModel,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockApplicantRepo.Verify(r =>
            r.GetApplicantsCountAsync(
                searchModel.SearchTerm,
                It.IsAny<CancellationToken>()), Times.Once);

        _mockApplicantRepo.Verify(x => x.GetApplicants(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailureWhenGetApplicantsThrows(
        FcUserHomePageSearchAndSortModel searchModel,
        int count)
    {
        var sut = CreateSut();

        _mockApplicantRepo.Setup(x => x.GetApplicantsCountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);

        _mockApplicantRepo.Setup(x => x.GetApplicants(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Throws(new Exception());

        var result = await sut.ExecuteAsync(
            _externalApplicant!,
            searchModel,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockApplicantRepo.Verify(r =>
            r.GetApplicantsCountAsync(
                searchModel.SearchTerm,
                It.IsAny<CancellationToken>()), Times.Once);

        _mockApplicantRepo.Verify(x => x.GetApplicants(
            searchModel.SearchTerm,
            searchModel.SortColumn,
            searchModel.SortAscending,
            searchModel.PageNumber,
            searchModel.PageSize), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsSuccess_WithExpectedModel(
        FcUserHomePageSearchAndSortModel searchModel,
        int count,
        List<Applicant> applicantResults)
    {
        // arrange
        var sut = CreateSut();

        _mockApplicantRepo.Setup(r =>
                r.GetApplicantsCountAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);

        _mockApplicantRepo.Setup(x => x.GetApplicants(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(applicantResults);

        // act
        var result = await sut.ExecuteAsync(
            _externalApplicant!,
            searchModel,
            CancellationToken.None);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal(count, result.Value.TotalApplicants);
        Assert.Equal(searchModel, result.Value.SearchAndSortModel);
        Assert.Equivalent(applicantResults, result.Value.Applicants);

        _mockApplicantRepo.Verify(r =>
            r.GetApplicantsCountAsync(
                searchModel.SearchTerm,
                It.IsAny<CancellationToken>()), Times.Once);

        _mockApplicantRepo.Verify(x => x.GetApplicants(
            searchModel.SearchTerm,
            searchModel.SortColumn,
            searchModel.SortAscending,
            searchModel.PageNumber,
            searchModel.PageSize), Times.Once);
    }

    private GetDataForFcUserHomepageUseCase CreateSut(bool isFcUser = true)
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            agencyId: _fixture.Create<Guid>(),
            woodlandOwnerName: _fixture.Create<string>(),
            isFcUser: isFcUser);

        _externalApplicant = new ExternalApplicant(user);

        _mockApplicantRepo.Reset();

        return new GetDataForFcUserHomepageUseCase(
            _mockApplicantRepo.Object,
            new NullLogger<GetDataForFcUserHomepageUseCase>()
        );
    }
}
