using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Castle.Components.DictionaryAdapter;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FileStorage.Model;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Common;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;
using Forestry.Flo.Tests.Common;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateWoodlandOfficerReviewServiceSiteVisitNotesRetrievedTests: UpdateWoodlandOfficerReviewServiceTestsBase
{
    [Theory, AutoData]
    public async Task ReturnsFailureWhenRepositoryThrows(
        Guid applicationId,
        Guid userId,
        DateTime retrievedDateTime,
        List<SiteVisitNotes<Guid>> notes)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("done gone wrong"));

        var result = await sut.SiteVisitNotesRetrievedAsync(applicationId, userId, retrievedDateTime, notes, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
        MockCaseNotesService.VerifyNoOtherCalls();
        MockAddDocumentService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailureWhenNotInCorrectState(
        Guid applicationId,
        Guid userId,
        DateTime retrievedDateTime,
        List<SiteVisitNotes<Guid>> notes)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory>(0));

        var result = await sut.SiteVisitNotesRetrievedAsync(applicationId, userId, retrievedDateTime, notes, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
        MockCaseNotesService.VerifyNoOtherCalls();
        MockAddDocumentService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailureWhenNoAssignedWoForApplication(
        Guid applicationId,
        Guid userId,
        DateTime retrievedDateTime,
        List<SiteVisitNotes<Guid>> notes)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory>{new StatusHistory{Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview}});
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>(0));

        var result = await sut.SiteVisitNotesRetrievedAsync(applicationId, userId, retrievedDateTime, notes, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
        MockCaseNotesService.VerifyNoOtherCalls();
        MockAddDocumentService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailureWhenDifferentAssignedWoForApplication(
        Guid applicationId,
        Guid userId,
        DateTime retrievedDateTime,
        List<SiteVisitNotes<Guid>> notes)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>{new AssigneeHistory{Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = Guid.NewGuid()}});

        var result = await sut.SiteVisitNotesRetrievedAsync(applicationId, userId, retrievedDateTime, notes, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
        MockCaseNotesService.VerifyNoOtherCalls();
        MockAddDocumentService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailureWhenWoodlandOfficerReviewDoesNotExist(
        Guid applicationId,
        Guid userId,
        DateTime retrievedDateTime,
        List<SiteVisitNotes<Guid>> notes)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        var result = await sut.SiteVisitNotesRetrievedAsync(applicationId, userId, retrievedDateTime, notes, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
        MockCaseNotesService.VerifyNoOtherCalls();
        MockAddDocumentService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailureWhenNewNotesWithAttachmentsButCannotLoadApplication(
        Guid applicationId,
        Guid userId,
        DateTime retrievedDateTime,
        WoodlandOfficerReview review,
        List<SiteVisitNotes<Guid>> notes)
    {
        review.SiteVisitNotNeeded = true;
        var allAttachments = notes
            .SelectMany(x => x.AttachmentDetails)
            .Select(x => new FileToStoreModel
            {
                ContentType = x.ContentType,
                FileBytes = x.File,
                FileName = x.Name
            })
            .ToList();

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);
        MockCaseNotesService
            .Setup(x => x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.SiteVisitNotesRetrievedAsync(applicationId, userId, retrievedDateTime, notes, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.VerifyNoOtherCalls();

        foreach (var siteVisitNotes in notes)
        {
            FellingLicenceApplicationRepository.Verify(x => x.AddCaseNoteAsync(It.Is<CaseNote>(c =>
                c.Id == siteVisitNotes.ObjectID
                && c.CreatedTimestamp == retrievedDateTime
                && c.CreatedByUserId == userId
                && c.VisibleToApplicant == true
                && c.VisibleToConsultee == true
                && c.Text == siteVisitNotes.Notes
                && c.FellingLicenceApplicationId == applicationId), It.IsAny<CancellationToken>()), Times.Once);
        }

        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        MockCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new[] { CaseNoteType.SiteVisitComment }, It.IsAny<CancellationToken>()), Times.Once);
        MockCaseNotesService.VerifyNoOtherCalls();
        MockAddDocumentService.VerifyNoOtherCalls();

        Assert.Equal(retrievedDateTime, review.LastUpdatedDate);
        Assert.Equal(retrievedDateTime, review.SiteVisitNotesRetrieved);
        Assert.Equal(userId, review.LastUpdatedById);
        Assert.False(review.SiteVisitNotNeeded);
    }

    [Theory, AutoMoqData]
    public async Task WhenNoNotesToSave(
        Guid applicationId,
        Guid userId,
        DateTime retrievedDateTime,
        WoodlandOfficerReview review)
    {
        review.SiteVisitNotNeeded = true;

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        var result = await sut.SiteVisitNotesRetrievedAsync(applicationId, userId, retrievedDateTime, new List<SiteVisitNotes<Guid>>(0), CancellationToken.None);

        Assert.True(result.IsSuccess);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.VerifyNoOtherCalls();
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        MockCaseNotesService.VerifyNoOtherCalls();
        MockAddDocumentService.VerifyNoOtherCalls();

        Assert.Equal(retrievedDateTime, review.LastUpdatedDate);
        Assert.Equal(retrievedDateTime, review.SiteVisitNotesRetrieved);
        Assert.Equal(userId, review.LastUpdatedById);
        Assert.False(review.SiteVisitNotNeeded);
    }

    [Theory, AutoMoqData]
    public async Task WhenNotesAlreadyRetrieved(
        Guid applicationId,
        Guid userId,
        DateTime retrievedDateTime,
        WoodlandOfficerReview review,
        List<SiteVisitNotes<Guid>> notes)
    {
        review.SiteVisitNotNeeded = true;
        var existingNotes = notes.Select(x => new CaseNoteModel
        {
            Id = x.ObjectID,
            Type = CaseNoteType.SiteVisitComment,
            Text = x.Notes
        }).ToList();

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));
        MockCaseNotesService
            .Setup(x => x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingNotes);

        var result = await sut.SiteVisitNotesRetrievedAsync(applicationId, userId, retrievedDateTime, notes, CancellationToken.None);

        Assert.True(result.IsSuccess);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.VerifyNoOtherCalls();
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        MockCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new [] { CaseNoteType.SiteVisitComment }, It.IsAny<CancellationToken>()), Times.Once);
        MockCaseNotesService.VerifyNoOtherCalls();
        MockAddDocumentService.VerifyNoOtherCalls();

        Assert.Equal(retrievedDateTime, review.LastUpdatedDate);
        Assert.Equal(retrievedDateTime, review.SiteVisitNotesRetrieved);
        Assert.Equal(userId, review.LastUpdatedById);
        Assert.False(review.SiteVisitNotNeeded);
    }

    [Theory, AutoMoqData]
    public async Task WhenNewNotesWithNoAttachments(
        Guid applicationId,
        Guid userId,
        DateTime retrievedDateTime,
        WoodlandOfficerReview review,
        List<SiteVisitNotes<Guid>> notes)
    {
        review.SiteVisitNotNeeded = true;
        notes.ForEach(x => x.AttachmentDetails = new List<AttachmentDetails<Guid>>(0));

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));
        MockCaseNotesService
            .Setup(x => x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.SiteVisitNotesRetrievedAsync(applicationId, userId, retrievedDateTime, notes, CancellationToken.None);

        Assert.True(result.IsSuccess);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.VerifyNoOtherCalls();

        foreach (var siteVisitNotes in notes)
        {
            FellingLicenceApplicationRepository.Verify(x => x.AddCaseNoteAsync(It.Is<CaseNote>(c =>
                c.Id == siteVisitNotes.ObjectID
                && c.CreatedTimestamp == retrievedDateTime
                && c.CreatedByUserId == userId
                && c.VisibleToApplicant == true
                && c.VisibleToConsultee == true
                && c.Text == siteVisitNotes.Notes
                && c.FellingLicenceApplicationId == applicationId), It.IsAny<CancellationToken>()), Times.Once);
        }

        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        MockCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new[] { CaseNoteType.SiteVisitComment }, It.IsAny<CancellationToken>()), Times.Once);
        MockCaseNotesService.VerifyNoOtherCalls();
        MockAddDocumentService.VerifyNoOtherCalls();

        Assert.Equal(retrievedDateTime, review.LastUpdatedDate);
        Assert.Equal(retrievedDateTime, review.SiteVisitNotesRetrieved);
        Assert.Equal(userId, review.LastUpdatedById);
        Assert.False(review.SiteVisitNotNeeded);
    }

    [Theory, AutoMoqData]
    public async Task WhenNewNotesWithAttachments(
        Guid applicationId,
        Guid userId,
        DateTime retrievedDateTime,
        WoodlandOfficerReview review,
        List<SiteVisitNotes<Guid>> notes,
        FellingLicenceApplication fellingLicenceApplication)
    {
        review.SiteVisitNotNeeded = true;
        var allAttachments = notes
            .SelectMany(x => x.AttachmentDetails)
            .Select(x => new FileToStoreModel
            {
                ContentType = x.ContentType,
                FileBytes = x.File,
                FileName = x.Name
            })
            .ToList();

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fellingLicenceApplication));
        MockCaseNotesService
            .Setup(x => x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));
        MockAddDocumentService.Setup(r => r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsSuccessResult(new List<string>())));

        var result = await sut.SiteVisitNotesRetrievedAsync(applicationId, userId, retrievedDateTime, notes, CancellationToken.None);

        Assert.True(result.IsSuccess);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.VerifyNoOtherCalls();

        foreach (var siteVisitNotes in notes)
        {
            FellingLicenceApplicationRepository.Verify(x => x.AddCaseNoteAsync(It.Is<CaseNote>(c =>
                c.Id == siteVisitNotes.ObjectID
                && c.CreatedTimestamp == retrievedDateTime
                && c.CreatedByUserId == userId
                && c.VisibleToApplicant == true
                && c.VisibleToConsultee == true
                && c.Text == siteVisitNotes.Notes
                && c.FellingLicenceApplicationId == applicationId), It.IsAny<CancellationToken>()), Times.Once);
        }

        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        MockCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new[] { CaseNoteType.SiteVisitComment }, It.IsAny<CancellationToken>()), Times.Once);
        MockCaseNotesService.VerifyNoOtherCalls();
        MockAddDocumentService.Verify(x => x.AddDocumentsAsInternalUserAsync(
            It.Is<AddDocumentsRequest>(r => r.UserAccountId == userId 
                                            && r.ActorType == ActorType.InternalUser
                                            && r.FellingApplicationId == applicationId
                                            && r.DocumentPurpose == DocumentPurpose.SiteVisitAttachment),
            It.IsAny<CancellationToken>()), Times.Once());
        MockAddDocumentService.VerifyNoOtherCalls();

        Assert.Equal(retrievedDateTime, review.LastUpdatedDate);
        Assert.Equal(retrievedDateTime, review.SiteVisitNotesRetrieved);
        Assert.Equal(userId, review.LastUpdatedById);
        Assert.False(review.SiteVisitNotNeeded);
    }
}