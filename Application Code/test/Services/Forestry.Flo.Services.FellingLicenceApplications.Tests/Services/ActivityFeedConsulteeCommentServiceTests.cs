using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class ActivityFeedConsulteeCommentServiceTests
{
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository = new();

    [Theory, AutoMoqData]
    public async Task ShouldReturnExpectedItems(
        Guid applicationId,
        List<ConsulteeComment> comments)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetConsulteeCommentsAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments);

        var input = new ActivityFeedItemProviderModel
        {
            FellingLicenceId = applicationId,
            ItemTypes = new[] { ActivityFeedItemType.ConsulteeComment }
        };

        var result = await sut.RetrieveActivityFeedItemsAsync(input, ActorType.InternalUser, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(comments.Count, result.Value.Count);

        foreach (var comment in comments)
        {
            var expectedText = comment.ApplicableToSection.HasValue
                ? $"Regarding {comment.ApplicableToSection.Value.GetDisplayName()}:\n{comment.Comment}"
                : comment.Comment;

            Assert.Contains(result.Value, x =>
                x.ActivityFeedItemType == ActivityFeedItemType.ConsulteeComment
                && x.VisibleToApplicant == true
                && x.VisibleToConsultee == true
                && x.CreatedTimestamp == comment.CreatedTimestamp
                && x.Text == expectedText
                && x.Source == $"{comment.AuthorName} ({comment.AuthorContactEmail})");
        }
    }

    [Fact]
    public void ShouldSupportCorrectItemTypes()
    {
        var sut = CreateSut();
        var result = sut.SupportedItemTypes();
        Assert.Equal(new[] { ActivityFeedItemType.ConsulteeComment }, result);
    }

    private ActivityFeedConsulteeCommentService CreateSut()
    {
        _fellingLicenceApplicationRepository.Reset();

        return new ActivityFeedConsulteeCommentService(
            _fellingLicenceApplicationRepository.Object,
            new NullLogger<ActivityFeedConsulteeCommentService>());
    }
}