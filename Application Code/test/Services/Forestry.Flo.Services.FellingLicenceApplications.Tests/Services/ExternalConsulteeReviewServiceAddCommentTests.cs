using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class ExternalConsulteeReviewServiceAddCommentTests
{
    private readonly Mock<IClock> MockClock = new();
    private readonly Mock<IFellingLicenceApplicationInternalRepository> MockRepository = new();

    [Theory, AutoData]
    public async Task WhenCommentStoredSuccessfully(ConsulteeCommentModel model)
    {
        var sut = CreateSut();

        MockRepository
            .Setup(x => x.AddConsulteeCommentAsync(It.IsAny<ConsulteeComment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.AddCommentAsync(model, CancellationToken.None);

        Assert.True(result.IsSuccess);
        MockRepository.Verify(x => x.AddConsulteeCommentAsync(It.Is<ConsulteeComment>(c => 
            c.CreatedTimestamp == model.CreatedTimestamp
            && c.AuthorContactEmail == model.AuthorContactEmail
            && c.AuthorName == model.AuthorName
            && c.Comment == model.Comment
            && c.FellingLicenceApplicationId == model.FellingLicenceApplicationId), It.IsAny<CancellationToken>()),
            Times.Once);
        MockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenCommentNotStoredSuccessfully(ConsulteeCommentModel model)
    {
        var sut = CreateSut();

        MockRepository
            .Setup(x => x.AddConsulteeCommentAsync(It.IsAny<ConsulteeComment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.NotUnique));

        var result = await sut.AddCommentAsync(model, CancellationToken.None);

        Assert.True(result.IsFailure);
        MockRepository.Verify(x => x.AddConsulteeCommentAsync(It.Is<ConsulteeComment>(c =>
                c.CreatedTimestamp == model.CreatedTimestamp
                && c.AuthorContactEmail == model.AuthorContactEmail
                && c.AuthorName == model.AuthorName
                && c.Comment == model.Comment
                && c.FellingLicenceApplicationId == model.FellingLicenceApplicationId), It.IsAny<CancellationToken>()),
            Times.Once);
        MockRepository.VerifyNoOtherCalls();
    }

    private ExternalConsulteeReviewService CreateSut()
    {
        MockClock.Reset();
        MockRepository.Reset();

        return new ExternalConsulteeReviewService(
            MockRepository.Object,
            MockClock.Object,
            new NullLogger<ExternalConsulteeReviewService>());
    }
}