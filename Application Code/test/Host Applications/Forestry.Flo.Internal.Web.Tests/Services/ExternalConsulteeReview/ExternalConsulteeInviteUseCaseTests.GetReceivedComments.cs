using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Tests.Common;
using Moq;
using System.Reflection;

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
        _notificationHistoryService.VerifyNoOtherCalls();
    }

    // ExtractApplicationSummaryAsync failure scenarios assumed to be tested elsewhere

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasNoExistingComments(
        Guid applicationId,
        Guid accessCode,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner,
        NotificationHistoryModel notificationHistoryModel)
    {
        var link = _fixture.Build<ExternalAccessLink>()
            .With(x => x.AccessCode, accessCode)
            .Create();
        application.ExternalAccessLinks = [ link ];
        application.AssigneeHistories = [];
        application.LinkedPropertyProfile.ProposedFellingDetails = [];
        
        var sharedDocument = application.Documents[0];
        link.SharedSupportingDocuments = [sharedDocument.Id];
        sharedDocument.DeletionTimestamp = null;
        sharedDocument.VisibleToConsultee = true;


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
        
        _notificationHistoryService.Setup(x =>
                x.GetNotificationHistoryByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(notificationHistoryModel));

        var (isSuccess, error, model) = await sut.GetReceivedCommentsAsync(applicationId, accessCode, CancellationToken.None);

        Assert.True(isSuccess);

        Assert.Equal(applicationId, model.ApplicationId);
        Assert.Empty(model.ReceivedComments);
        Assert.Equal(link.Name, model.ConsulteeName);
        Assert.Equal(link.ContactEmail, model.Email);
        Assert.Equal(application.PublicRegister?.WoodlandOfficerSetAsExemptFromConsultationPublicRegister is true, model.PublicRegisterExempt);
        Assert.Equal(application.PublicRegister?.WoodlandOfficerConsultationPublicRegisterExemptionReason, model.PublicRegisterExemptionReason);
        Assert.Equal(notificationHistoryModel.Text, model.InviteContent);

        Assert.Contains(model.SharedDocuments, d => d.FileName == sharedDocument.FileName
                                                    && d.DocumentPurpose == sharedDocument.Purpose
                                                    && d.Id == sharedDocument.Id);

        Assert.Equal(link.CreatedTimeStamp, model.InvitationDate);
        Assert.Equal(link.Purpose, model.InvitationPurpose);

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

        _notificationHistoryService.Verify(x => x.GetNotificationHistoryByIdAsync(link.NotificationHistoryId.Value, It.IsAny<CancellationToken>()), Times.Once);
        _notificationHistoryService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasExistingComments(
        Guid applicationId,
        Guid accessCode,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner,
        Document attachment,
        NotificationHistoryModel notificationHistoryModel)
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

        _notificationHistoryService.Setup(x =>
                x.GetNotificationHistoryByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(notificationHistoryModel));

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
        Assert.Equal(application.PublicRegister?.WoodlandOfficerSetAsExemptFromConsultationPublicRegister is true, model.PublicRegisterExempt);
        Assert.Equal(application.PublicRegister?.WoodlandOfficerConsultationPublicRegisterExemptionReason, model.PublicRegisterExemptionReason);
        Assert.Equal(notificationHistoryModel.Text, model.InviteContent);
        Assert.Empty(model.SharedDocuments);
        Assert.Equal(link.CreatedTimeStamp, model.InvitationDate);
        Assert.Equal(link.Purpose, model.InvitationPurpose);

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

        _notificationHistoryService.Verify(x => x.GetNotificationHistoryByIdAsync(link.NotificationHistoryId.Value, It.IsAny<CancellationToken>()), Times.Once);
        _notificationHistoryService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUnableToLoadNotificationHistoryItem(
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

        _notificationHistoryService.Setup(x =>
                x.GetNotificationHistoryByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<NotificationHistoryModel>("error"));

        var (isSuccess, error, model) = await sut.GetReceivedCommentsAsync(applicationId, accessCode, CancellationToken.None);

        Assert.True(isSuccess);

        var expectedComments = comments
            .Select(x => new ReceivedConsulteeCommentModel
            {
                AuthorName = x.AuthorName,
                Comment = x.Comment,
                CreatedTimestamp = x.CreatedTimestamp,
                Attachments = x.ConsulteeAttachmentIds.Any() ? new Dictionary<Guid, string> { { attachment.Id, attachment.FileName } } : new Dictionary<Guid, string>()
            }).ToList();

        Assert.Equal(applicationId, model.ApplicationId);
        Assert.Equivalent(expectedComments, model.ReceivedComments);
        Assert.Equal(link.Name, model.ConsulteeName);
        Assert.Equal(link.ContactEmail, model.Email);
        Assert.Equal(application.PublicRegister?.WoodlandOfficerSetAsExemptFromConsultationPublicRegister is true, model.PublicRegisterExempt);
        Assert.Equal(application.PublicRegister?.WoodlandOfficerConsultationPublicRegisterExemptionReason, model.PublicRegisterExemptionReason);
        Assert.Equal("Unable to load invite notification content", model.InviteContent);

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

        _notificationHistoryService.Verify(x => x.GetNotificationHistoryByIdAsync(link.NotificationHistoryId.Value, It.IsAny<CancellationToken>()), Times.Once);
        _notificationHistoryService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenAccessLinkHasNoNotificationHistoryId(
        Guid applicationId,
        Guid accessCode,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner,
        Document attachment)
    {
        var link = _fixture.Build<ExternalAccessLink>()
            .With(x => x.AccessCode, accessCode)
            .Without(x => x.NotificationHistoryId)
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
                Attachments = x.ConsulteeAttachmentIds.Any() ? new Dictionary<Guid, string> { { attachment.Id, attachment.FileName } } : new Dictionary<Guid, string>()
            }).ToList();

        Assert.Equal(applicationId, model.ApplicationId);
        Assert.Equivalent(expectedComments, model.ReceivedComments);
        Assert.Equal(link.Name, model.ConsulteeName);
        Assert.Equal(link.ContactEmail, model.Email);
        Assert.Equal(application.PublicRegister?.WoodlandOfficerSetAsExemptFromConsultationPublicRegister is true, model.PublicRegisterExempt);
        Assert.Equal(application.PublicRegister?.WoodlandOfficerConsultationPublicRegisterExemptionReason, model.PublicRegisterExemptionReason);
        Assert.Equal("Unable to load invite notification content", model.InviteContent);

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

        _notificationHistoryService.VerifyNoOtherCalls();
    }
}