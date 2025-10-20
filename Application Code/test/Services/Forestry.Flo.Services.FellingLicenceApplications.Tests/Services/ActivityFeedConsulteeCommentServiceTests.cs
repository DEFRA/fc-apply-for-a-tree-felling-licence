using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class ActivityFeedConsulteeCommentServiceTests
{
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository = new();

    [Theory, AutoMoqData]
    public async Task ShouldReturnExpectedItemsWithAttachments(
        Guid applicationId,
        List<ConsulteeComment> comments,
        Document document)
    {
        var sut = CreateSut();

        comments.ForEach(x => x.DocumentIds = [document.Id]);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetConsulteeCommentsAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApplicationDocumentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([document]);

        var input = new ActivityFeedItemProviderModel
        {
            FellingLicenceId = applicationId,
            ItemTypes = [ActivityFeedItemType.ConsulteeComment]
        };

        var result = await sut.RetrieveActivityFeedItemsAsync(input, ActorType.InternalUser, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(comments.Count, result.Value.Count);

        foreach (var comment in comments)
        {
            Assert.Contains(result.Value, x =>
                x.ActivityFeedItemType == ActivityFeedItemType.ConsulteeComment
                && x.VisibleToApplicant == true
                && x.VisibleToConsultee == true
                && x.CreatedTimestamp == comment.CreatedTimestamp
                && x.Text == comment.Comment
                && x.Source == $"{comment.AuthorName} ({comment.AuthorContactEmail})"
                && x.Attachments.Count == 1
                && x.Attachments.ContainsKey(document.Id)
                && x.Attachments.ContainsValue(document.FileName));
        }
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnExpectedItemsWithoutAttachments(
        Guid applicationId,
        List<ConsulteeComment> comments)
    {
        var sut = CreateSut();

        comments.ForEach(x => x.DocumentIds = []);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetConsulteeCommentsAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments);

        var input = new ActivityFeedItemProviderModel
        {
            FellingLicenceId = applicationId,
            ItemTypes = [ActivityFeedItemType.ConsulteeComment]
        };

        var result = await sut.RetrieveActivityFeedItemsAsync(input, ActorType.InternalUser, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(comments.Count, result.Value.Count);

        foreach (var comment in comments)
        {
            Assert.Contains(result.Value, x =>
                x.ActivityFeedItemType == ActivityFeedItemType.ConsulteeComment
                && x.VisibleToApplicant == true
                && x.VisibleToConsultee == true
                && x.CreatedTimestamp == comment.CreatedTimestamp
                && x.Text == comment.Comment
                && x.Source == $"{comment.AuthorName} ({comment.AuthorContactEmail})"
                && x.Attachments.Count == 0);
        }

        _fellingLicenceApplicationRepository.Verify(x => x.GetConsulteeCommentsAsync(applicationId, null, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetApplicationDocumentsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
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