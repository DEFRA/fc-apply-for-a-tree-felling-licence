using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;
using Forestry.Flo.Tests.Common;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.ExternalConsulteeReview;

public partial class ExternalConsulteeInviteUseCaseTests
{
    [Theory, AutoData]
    public async Task WhenApplicationNotFoundToViewReceivedComments(
        Guid applicationId,
        Guid accessCode)
    {
        var sut = CreateSut();

        _internalUserContextFlaRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var (isSuccess, error, _) = await sut.GetReceivedCommentsAsync(applicationId, accessCode, CancellationToken.None);

        Assert.False(isSuccess);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _woodlandOwnerService.VerifyNoOtherCalls();
        _mockAgentAuthorityService.VerifyNoOtherCalls();
        _externalUserAccountService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();

        _mockExternalConsulteeReviewService.VerifyNoOtherCalls();
    }

    // ExtractApplicationSummaryAsync failure scenarios assumed to be tested elsewhere

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasNoExistingComments(
        Guid applicationId,
        Guid accessCode,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner)
    {
        var link = _fixture.Build<ExternalAccessLink>()
            .With(x => x.AccessCode, accessCode)
            .Create();
        application.ExternalAccessLinks = [ link ];
        application.AssigneeHistories = [];
        application.LinkedPropertyProfile.ProposedFellingDetails = [];

        var sut = CreateSut();

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _woodlandOwnerService
            .Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _mockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        _mockExternalConsulteeReviewService.Setup(x => x.RetrieveConsulteeCommentsForAccessCodeAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var (isSuccess, error, model) = await sut.GetReceivedCommentsAsync(applicationId, accessCode, CancellationToken.None);

        Assert.True(isSuccess);

        Assert.Equal(applicationId, model.ApplicationId);
        Assert.Empty(model.ReceivedComments);
        Assert.Equal(link.Name, model.ConsulteeName);
        Assert.Equal(link.ContactEmail, model.Email);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _woodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once());
        _woodlandOwnerService.VerifyNoOtherCalls();

        _mockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgentAuthorityService.VerifyNoOtherCalls();

        _externalUserAccountService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();

        _mockExternalConsulteeReviewService.Verify(x => 
            x.RetrieveConsulteeCommentsForAccessCodeAsync(applicationId, accessCode, It.IsAny<CancellationToken>()), Times.Once);
        _mockExternalConsulteeReviewService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasExistingComments(
        Guid applicationId,
        Guid accessCode,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner,
        Document attachment)
    {
        var link = _fixture.Build<ExternalAccessLink>()
            .With(x => x.AccessCode, accessCode)
            .Create();

        typeof(Document)
            .GetProperty("Id", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .SetValue(attachment, Guid.NewGuid());

        var comments = _fixture.Build<ConsulteeCommentModel>()
            .With(x => x.AccessCode, accessCode)
            .With(x => x.ConsulteeAttachmentIds, [])
            .CreateMany(3)
            .OrderByDescending(x => x.CreatedTimestamp)
            .ToList();
        comments[0].ConsulteeAttachmentIds = [attachment.Id];

        application.ExternalAccessLinks = [link];
        application.AssigneeHistories = [];
        application.LinkedPropertyProfile.ProposedFellingDetails = [];
        application.Documents = [attachment];

        var sut = CreateSut();

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _woodlandOwnerService
            .Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _mockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        _mockExternalConsulteeReviewService.Setup(x => x.RetrieveConsulteeCommentsForAccessCodeAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments);

        var (isSuccess, error, model) = await sut.GetReceivedCommentsAsync(applicationId, accessCode, CancellationToken.None);

        Assert.True(isSuccess);

        var expectedComments = comments
            .Select(x => new ReceivedConsulteeCommentModel
            {
                AuthorName = x.AuthorName,
                Comment = x.Comment,
                CreatedTimestamp = x.CreatedTimestamp,
                Attachments = x.ConsulteeAttachmentIds.Any() ? new Dictionary<Guid, string>{ {attachment.Id, attachment.FileName} } : new Dictionary<Guid, string>()
            }).ToList();

        Assert.Equal(applicationId, model.ApplicationId);
        Assert.Equivalent(expectedComments, model.ReceivedComments);
        Assert.Equal(link.Name, model.ConsulteeName);
        Assert.Equal(link.ContactEmail, model.Email);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _woodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once());
        _woodlandOwnerService.VerifyNoOtherCalls();

        _mockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgentAuthorityService.VerifyNoOtherCalls();

        _externalUserAccountService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();

        _mockExternalConsulteeReviewService.Verify(x =>
            x.RetrieveConsulteeCommentsForAccessCodeAsync(applicationId, accessCode, It.IsAny<CancellationToken>()), Times.Once);
        _mockExternalConsulteeReviewService.VerifyNoOtherCalls();
    }
}