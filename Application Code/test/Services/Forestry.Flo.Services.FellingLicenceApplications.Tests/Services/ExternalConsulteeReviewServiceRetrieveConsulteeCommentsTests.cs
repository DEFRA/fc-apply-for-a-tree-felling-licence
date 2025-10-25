using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class ExternalConsulteeReviewServiceRetrieveConsulteeCommentsTests
{
    private readonly Mock<IClock> MockClock = new();
    private readonly Mock<IFellingLicenceApplicationInternalRepository> MockRepository = new();

    [Theory, AutoData]
    public async Task WhenNoCommentsFound(Guid applicationId, Guid accessCode)
    {
        var sut = CreateSut();

        MockRepository
            .Setup(x => x.GetConsulteeCommentsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await sut.RetrieveConsulteeCommentsForAccessCodeAsync(applicationId, accessCode, CancellationToken.None);

        Assert.Empty(result);

        MockRepository.Verify(x => x.GetConsulteeCommentsAsync(applicationId, accessCode, It.IsAny<CancellationToken>()), Times.Once);
        MockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenCommentsFound(Guid applicationId, Guid accessCode, List<ConsulteeComment> comments)
    {
        var sut = CreateSut();

        var expectedComments = comments.Select(x => new ConsulteeCommentModel
        {
            AuthorContactEmail = x.AuthorContactEmail,
            AuthorName = x.AuthorName,
            Comment = x.Comment,
            CreatedTimestamp = x.CreatedTimestamp,
            FellingLicenceApplicationId = x.FellingLicenceApplicationId,
            ConsulteeAttachmentIds = x.DocumentIds
        }).ToList();

        MockRepository
            .Setup(x => x.GetConsulteeCommentsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments);

        var result = await sut.RetrieveConsulteeCommentsForAccessCodeAsync(applicationId, accessCode, CancellationToken.None);

        Assert.Equal(comments.Count, result.Count);
        Assert.Equivalent(expectedComments, result);

        MockRepository.Verify(x => x.GetConsulteeCommentsAsync(applicationId, accessCode, It.IsAny<CancellationToken>()), Times.Once);
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