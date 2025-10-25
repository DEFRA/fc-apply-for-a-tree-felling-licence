using System;
using System.Text;
using System.Text.Json;
using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class AddSiteVisitEvidenceAsyncTests : WoodlandOfficerReviewUseCaseTestsBase<SiteVisitUseCase>
{
    private FormFileCollection _formFileCollection = [];
    private static readonly Fixture Fixture = new Fixture();
    private InternalUser _testUser;

    [Theory, AutoMoqData]
    public async Task WhenStoringNewFileContentFails(
        AddSiteVisitEvidenceModel model,
        AddDocumentsFailureResult failure)
    {
        var sut = CreateSut();

        model.SiteVisitEvidenceMetadata.First().FileName = _formFileCollection.First().FileName;
        model.SiteVisitEvidenceMetadata.First().SupportingDocumentId = null;
        model.SiteVisitEvidenceMetadata.First().MarkedForDeletion = false;

        MockAddDocumentService
            .Setup(m => m.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AddDocumentsSuccessResult, AddDocumentsFailureResult>(failure));

        var result = await sut.AddSiteVisitEvidenceAsync(model, _formFileCollection, _testUser, CancellationToken.None);

        Assert.True(result.IsFailure);

        MockAddDocumentService.Verify(x => x.AddDocumentsAsInternalUserAsync(
            It.Is<AddDocumentsRequest>(a =>
                a.ActorType == ActorType.InternalUser
                && a.DocumentPurpose == DocumentPurpose.SiteVisitAttachment
                && a.FellingApplicationId == model.ApplicationId
                && a.FileToStoreModels.Count == 1
                && a.FileToStoreModels.Single().FileName == _formFileCollection.Single().FileName
                && a.FileToStoreModels.Single().ContentType == _formFileCollection.Single().ContentType),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAddDocumentService.VerifyNoOtherCalls();

        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateSiteVisitFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == _testUser.UserAccountId!.Value
                && a.SourceEntityId == model.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                    JsonSerializer.Serialize(new
                    {
                        Error = "Failed to update site visit with evidence: " + string.Join(", ", failure.UserFacingFailureMessages),
                    }, SerializerOptions)
                ),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();

        MockRemoveDocumentService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenStoringSecondNewFileContentFails(
        AddSiteVisitEvidenceModel model,
        AddDocumentsSuccessResult success,
        AddDocumentsFailureResult failure,
        string failingFileName)
    {
        var sut = CreateSut();
        AddFileToFormCollection(fileName: failingFileName);

        model.SiteVisitEvidenceMetadata.First().FileName = _formFileCollection.First().FileName;
        model.SiteVisitEvidenceMetadata.First().SupportingDocumentId = null;
        model.SiteVisitEvidenceMetadata.First().MarkedForDeletion = false;

        model.SiteVisitEvidenceMetadata.Last().FileName = _formFileCollection.Last().FileName;
        model.SiteVisitEvidenceMetadata.Last().SupportingDocumentId = null;
        model.SiteVisitEvidenceMetadata.Last().MarkedForDeletion = false;

        MockAddDocumentService
            .Setup(m => m.AddDocumentsAsInternalUserAsync(It.Is<AddDocumentsRequest>(a => a.FileToStoreModels.Single().FileName == _formFileCollection.First().FileName), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(success));
        MockAddDocumentService
            .Setup(m => m.AddDocumentsAsInternalUserAsync(It.Is<AddDocumentsRequest>(a => a.FileToStoreModels.Single().FileName == failingFileName), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AddDocumentsSuccessResult, AddDocumentsFailureResult>(failure));

        var result = await sut.AddSiteVisitEvidenceAsync(model, _formFileCollection, _testUser, CancellationToken.None);

        Assert.True(result.IsFailure);

        MockAddDocumentService.Verify(x => x.AddDocumentsAsInternalUserAsync(
            It.Is<AddDocumentsRequest>(a =>
                a.ActorType == ActorType.InternalUser
                && a.DocumentPurpose == DocumentPurpose.SiteVisitAttachment
                && a.FellingApplicationId == model.ApplicationId
                && a.FileToStoreModels.Count == 1
                && a.FileToStoreModels.Single().FileName == _formFileCollection.First().FileName
                && a.FileToStoreModels.Single().ContentType == _formFileCollection.First().ContentType),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAddDocumentService.Verify(x => x.AddDocumentsAsInternalUserAsync(
            It.Is<AddDocumentsRequest>(a =>
                a.ActorType == ActorType.InternalUser
                && a.DocumentPurpose == DocumentPurpose.SiteVisitAttachment
                && a.FellingApplicationId == model.ApplicationId
                && a.FileToStoreModels.Count == 1
                && a.FileToStoreModels.Single().FileName == failingFileName
                && a.FileToStoreModels.Single().ContentType == _formFileCollection.Last().ContentType),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAddDocumentService.VerifyNoOtherCalls();

        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateSiteVisitFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == _testUser.UserAccountId!.Value
                && a.SourceEntityId == model.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                    JsonSerializer.Serialize(new
                    {
                        Error = "Failed to update site visit with evidence: " + string.Join(", ", failure.UserFacingFailureMessages),
                    }, SerializerOptions)
                ),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();

        foreach (var successDocumentId in success.DocumentIds)
        {
            MockRemoveDocumentService.Verify(x => x.PermanentlyRemoveDocumentAsync(
                model.ApplicationId, successDocumentId, It.IsAny<CancellationToken>()), Times.Once);
        }
        MockRemoveDocumentService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUpdatingSiteVisitFails(
        AddSiteVisitEvidenceModel model,
        AddDocumentsSuccessResult success,
        string error)
    {
        var sut = CreateSut();

        model.SiteVisitEvidenceMetadata.First().FileName = _formFileCollection.First().FileName;
        model.SiteVisitEvidenceMetadata.First().SupportingDocumentId = null;
        model.SiteVisitEvidenceMetadata.First().MarkedForDeletion = false;

        var expectedSiteVisitMetadata = model.SiteVisitEvidenceMetadata.Skip(1)
            .Where(x => !x.MarkedForDeletion)
            .Select(x => new SiteVisitEvidenceDocument
            {
                FileName = x.FileName,
                DocumentId = x.SupportingDocumentId!.Value,
                VisibleToApplicant = x.VisibleToApplicants,
                VisibleToConsultee = x.VisibleToConsultees,
                Comment = x.Comment,
                Label = x.Label
            }).ToList();
        expectedSiteVisitMetadata.Add(new SiteVisitEvidenceDocument
        {
            FileName = model.SiteVisitEvidenceMetadata.First().FileName,
            DocumentId = success.DocumentIds.FirstOrDefault(),
            VisibleToApplicant = model.SiteVisitEvidenceMetadata.First().VisibleToApplicants,
            VisibleToConsultee = model.SiteVisitEvidenceMetadata.First().VisibleToConsultees,
            Comment = model.SiteVisitEvidenceMetadata.First().Comment,
            Label = model.SiteVisitEvidenceMetadata.First().Label
        });

        MockAddDocumentService
            .Setup(m => m.AddDocumentsAsInternalUserAsync(It.Is<AddDocumentsRequest>(a => a.FileToStoreModels.Single().FileName == _formFileCollection.First().FileName), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(success));

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdateSiteVisitEvidenceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<SiteVisitEvidenceDocument[]>(), It.IsAny<FormLevelCaseNote>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.AddSiteVisitEvidenceAsync(model, _formFileCollection, _testUser, CancellationToken.None);

        Assert.True(result.IsFailure);

        MockAddDocumentService.Verify(x => x.AddDocumentsAsInternalUserAsync(
            It.Is<AddDocumentsRequest>(a =>
                a.ActorType == ActorType.InternalUser
                && a.DocumentPurpose == DocumentPurpose.SiteVisitAttachment
                && a.FellingApplicationId == model.ApplicationId
                && a.FileToStoreModels.Count == 1
                && a.FileToStoreModels.Single().FileName == _formFileCollection.First().FileName
                && a.FileToStoreModels.Single().ContentType == _formFileCollection.First().ContentType),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAddDocumentService.VerifyNoOtherCalls();

        UpdateWoodlandOfficerReviewService.Verify(x => x.UpdateSiteVisitEvidenceAsync(
            model.ApplicationId,
            _testUser.UserAccountId!.Value,
            It.Is<SiteVisitEvidenceDocument[]>(docs => docs.Count() == expectedSiteVisitMetadata.Count()
                && docs.All(d => expectedSiteVisitMetadata.Any(e =>
                    e.FileName == d.FileName
                    && e.DocumentId == d.DocumentId
                    && e.VisibleToApplicant == d.VisibleToApplicant
                    && e.VisibleToConsultee == d.VisibleToConsultee
                    && e.Comment == d.Comment
                    && e.Label == d.Label))),
            model.SiteVisitComment,
            model.SiteVisitComplete,
            It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateSiteVisitFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == _testUser.UserAccountId!.Value
                && a.SourceEntityId == model.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                    JsonSerializer.Serialize(new
                    {
                        Error = "Failed to update site visit with evidence: " + error,
                    }, SerializerOptions)
                ),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();

        foreach (var successDocumentId in success.DocumentIds)
        {
            MockRemoveDocumentService.Verify(x => x.PermanentlyRemoveDocumentAsync(
                model.ApplicationId, successDocumentId, It.IsAny<CancellationToken>()), Times.Once);
        }
        MockRemoveDocumentService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUpdatingSiteVisitSucceeds(
        AddSiteVisitEvidenceModel model,
        AddDocumentsSuccessResult success)
    {
        var sut = CreateSut();

        model.SiteVisitEvidenceMetadata.First().FileName = _formFileCollection.First().FileName;
        model.SiteVisitEvidenceMetadata.First().SupportingDocumentId = null;
        model.SiteVisitEvidenceMetadata.First().MarkedForDeletion = false;

        var expectedSiteVisitMetadata = model.SiteVisitEvidenceMetadata.Skip(1)
            .Where(x => !x.MarkedForDeletion)
            .Select(x => new SiteVisitEvidenceDocument
            {
                FileName = x.FileName,
                DocumentId = x.SupportingDocumentId!.Value,
                VisibleToApplicant = x.VisibleToApplicants,
                VisibleToConsultee = x.VisibleToConsultees,
                Comment = x.Comment,
                Label = x.Label
            }).ToList();
        expectedSiteVisitMetadata.Add(new SiteVisitEvidenceDocument
        {
            FileName = model.SiteVisitEvidenceMetadata.First().FileName,
            DocumentId = success.DocumentIds.FirstOrDefault(),
            VisibleToApplicant = model.SiteVisitEvidenceMetadata.First().VisibleToApplicants,
            VisibleToConsultee = model.SiteVisitEvidenceMetadata.First().VisibleToConsultees,
            Comment = model.SiteVisitEvidenceMetadata.First().Comment,
            Label = model.SiteVisitEvidenceMetadata.First().Label
        });

        MockAddDocumentService
            .Setup(m => m.AddDocumentsAsInternalUserAsync(It.Is<AddDocumentsRequest>(a => a.FileToStoreModels.Single().FileName == _formFileCollection.First().FileName), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(success));

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdateSiteVisitEvidenceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<SiteVisitEvidenceDocument[]>(), It.IsAny<FormLevelCaseNote>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.AddSiteVisitEvidenceAsync(model, _formFileCollection, _testUser, CancellationToken.None);

        Assert.True(result.IsSuccess);

        MockAddDocumentService.Verify(x => x.AddDocumentsAsInternalUserAsync(
            It.Is<AddDocumentsRequest>(a =>
                a.ActorType == ActorType.InternalUser
                && a.DocumentPurpose == DocumentPurpose.SiteVisitAttachment
                && a.FellingApplicationId == model.ApplicationId
                && a.FileToStoreModels.Count == 1
                && a.FileToStoreModels.Single().FileName == _formFileCollection.First().FileName
                && a.FileToStoreModels.Single().ContentType == _formFileCollection.First().ContentType),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAddDocumentService.VerifyNoOtherCalls();

        UpdateWoodlandOfficerReviewService.Verify(x => x.UpdateSiteVisitEvidenceAsync(
            model.ApplicationId,
            _testUser.UserAccountId!.Value,
            It.Is<SiteVisitEvidenceDocument[]>(docs => docs.Count() == expectedSiteVisitMetadata.Count()
                && docs.All(d => expectedSiteVisitMetadata.Any(e =>
                    e.FileName == d.FileName
                    && e.DocumentId == d.DocumentId
                    && e.VisibleToApplicant == d.VisibleToApplicant
                    && e.VisibleToConsultee == d.VisibleToConsultee
                    && e.Comment == d.Comment
                    && e.Label == d.Label))),
            model.SiteVisitComment,
            model.SiteVisitComplete,
            It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == _testUser.UserAccountId!.Value
                && a.SourceEntityId == model.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                    JsonSerializer.Serialize(new
                    {
                        Section = "Site Visit"
                    }, SerializerOptions)
                ),
            It.IsAny<CancellationToken>()), Times.Once);

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateSiteVisit
                && a.ActorType == ActorType.InternalUser
                && a.UserId == _testUser.UserAccountId!.Value
                && a.SourceEntityId == model.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    SiteVisitEvidenceCount = model.SiteVisitEvidenceMetadata.Count(c => !c.MarkedForDeletion),
                    SiteVisitComment = model.SiteVisitComment.CaseNote,
                    SiteVisitComplete = model.SiteVisitComplete
                }, SerializerOptions)
            ),
            It.IsAny<CancellationToken>()), Times.Once);

        AuditingService.VerifyNoOtherCalls();

        MockRemoveDocumentService.VerifyNoOtherCalls();
    }


    [Theory, AutoMoqData]
    public async Task WhenUpdatingSiteVisitSucceedsWithMultipleFiles(
        AddSiteVisitEvidenceModel model,
        AddDocumentsSuccessResult[] successes)
    {
        var sut = CreateSut();
        AddFileToFormCollection("file2.csv");

        model.SiteVisitEvidenceMetadata[0].FileName = _formFileCollection[0].FileName;
        model.SiteVisitEvidenceMetadata[0].SupportingDocumentId = null;
        model.SiteVisitEvidenceMetadata[0].MarkedForDeletion = false;

        model.SiteVisitEvidenceMetadata[1].FileName = _formFileCollection[1].FileName;
        model.SiteVisitEvidenceMetadata[1].SupportingDocumentId = null;
        model.SiteVisitEvidenceMetadata[1].MarkedForDeletion = false;

        var expectedSiteVisitMetadata = model.SiteVisitEvidenceMetadata.Skip(2)
            .Where(x => !x.MarkedForDeletion)
            .Select(x => new SiteVisitEvidenceDocument
            {
                FileName = x.FileName,
                DocumentId = x.SupportingDocumentId!.Value,
                VisibleToApplicant = x.VisibleToApplicants,
                VisibleToConsultee = x.VisibleToConsultees,
                Comment = x.Comment,
                Label = x.Label
            }).ToList();
        expectedSiteVisitMetadata.Add(new SiteVisitEvidenceDocument
        {
            FileName = model.SiteVisitEvidenceMetadata[0].FileName,
            DocumentId = successes[0].DocumentIds.FirstOrDefault(),
            VisibleToApplicant = model.SiteVisitEvidenceMetadata[0].VisibleToApplicants,
            VisibleToConsultee = model.SiteVisitEvidenceMetadata[0].VisibleToConsultees,
            Comment = model.SiteVisitEvidenceMetadata[0].Comment,
            Label = model.SiteVisitEvidenceMetadata[0].Label
        });
        expectedSiteVisitMetadata.Add(new SiteVisitEvidenceDocument
        {
            FileName = model.SiteVisitEvidenceMetadata[1].FileName,
            DocumentId = successes[1].DocumentIds.FirstOrDefault(),
            VisibleToApplicant = model.SiteVisitEvidenceMetadata[1].VisibleToApplicants,
            VisibleToConsultee = model.SiteVisitEvidenceMetadata[1].VisibleToConsultees,
            Comment = model.SiteVisitEvidenceMetadata[1].Comment,
            Label = model.SiteVisitEvidenceMetadata[1].Label
        });

        MockAddDocumentService
            .Setup(m => m.AddDocumentsAsInternalUserAsync(It.Is<AddDocumentsRequest>(a => a.FileToStoreModels.Single().FileName == _formFileCollection[0].FileName), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(successes[0]));

        MockAddDocumentService
            .Setup(m => m.AddDocumentsAsInternalUserAsync(It.Is<AddDocumentsRequest>(a => a.FileToStoreModels.Single().FileName == _formFileCollection[1].FileName), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(successes[1]));

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdateSiteVisitEvidenceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<SiteVisitEvidenceDocument[]>(), It.IsAny<FormLevelCaseNote>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.AddSiteVisitEvidenceAsync(model, _formFileCollection, _testUser, CancellationToken.None);

        Assert.True(result.IsSuccess);

        MockAddDocumentService.Verify(x => x.AddDocumentsAsInternalUserAsync(
            It.Is<AddDocumentsRequest>(a =>
                a.ActorType == ActorType.InternalUser
                && a.DocumentPurpose == DocumentPurpose.SiteVisitAttachment
                && a.FellingApplicationId == model.ApplicationId
                && a.FileToStoreModels.Count == 1
                && a.FileToStoreModels.Single().FileName == _formFileCollection[0].FileName
                && a.FileToStoreModels.Single().ContentType == _formFileCollection[0].ContentType),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAddDocumentService.Verify(x => x.AddDocumentsAsInternalUserAsync(
            It.Is<AddDocumentsRequest>(a =>
                a.ActorType == ActorType.InternalUser
                && a.DocumentPurpose == DocumentPurpose.SiteVisitAttachment
                && a.FellingApplicationId == model.ApplicationId
                && a.FileToStoreModels.Count == 1
                && a.FileToStoreModels.Single().FileName == _formFileCollection[1].FileName
                && a.FileToStoreModels.Single().ContentType == _formFileCollection[1].ContentType),
            It.IsAny<CancellationToken>()), Times.Once); 
        MockAddDocumentService.VerifyNoOtherCalls();

        UpdateWoodlandOfficerReviewService.Verify(x => x.UpdateSiteVisitEvidenceAsync(
            model.ApplicationId,
            _testUser.UserAccountId!.Value,
            It.Is<SiteVisitEvidenceDocument[]>(docs => docs.Count() == expectedSiteVisitMetadata.Count()
                && docs.All(d => expectedSiteVisitMetadata.Any(e =>
                    e.FileName == d.FileName
                    && e.DocumentId == d.DocumentId
                    && e.VisibleToApplicant == d.VisibleToApplicant
                    && e.VisibleToConsultee == d.VisibleToConsultee
                    && e.Comment == d.Comment
                    && e.Label == d.Label))),
            model.SiteVisitComment,
            model.SiteVisitComplete,
            It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == _testUser.UserAccountId!.Value
                && a.SourceEntityId == model.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                    JsonSerializer.Serialize(new
                    {
                        Section = "Site Visit"
                    }, SerializerOptions)
                ),
            It.IsAny<CancellationToken>()), Times.Once);

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateSiteVisit
                && a.ActorType == ActorType.InternalUser
                && a.UserId == _testUser.UserAccountId!.Value
                && a.SourceEntityId == model.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    SiteVisitEvidenceCount = model.SiteVisitEvidenceMetadata.Count(c => !c.MarkedForDeletion),
                    SiteVisitComment = model.SiteVisitComment.CaseNote,
                    SiteVisitComplete = model.SiteVisitComplete
                }, SerializerOptions)
            ),
            It.IsAny<CancellationToken>()), Times.Once);

        AuditingService.VerifyNoOtherCalls();

        MockRemoveDocumentService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUpdatingSiteVisitSucceedsWithMultipleFilesWithSameFilename(
        AddSiteVisitEvidenceModel model,
        AddDocumentsSuccessResult[] successes)
    {
        var sut = CreateSut();
        AddFileToFormCollection();

        model.SiteVisitEvidenceMetadata[0].FileName = _formFileCollection[0].FileName;
        model.SiteVisitEvidenceMetadata[0].SupportingDocumentId = null;
        model.SiteVisitEvidenceMetadata[0].MarkedForDeletion = false;

        model.SiteVisitEvidenceMetadata[1].FileName = _formFileCollection[1].FileName;  // which is the same name
        model.SiteVisitEvidenceMetadata[1].SupportingDocumentId = null;
        model.SiteVisitEvidenceMetadata[1].MarkedForDeletion = false;

        var expectedSiteVisitMetadata = model.SiteVisitEvidenceMetadata.Skip(2)
            .Where(x => !x.MarkedForDeletion)
            .Select(x => new SiteVisitEvidenceDocument
            {
                FileName = x.FileName,
                DocumentId = x.SupportingDocumentId!.Value,
                VisibleToApplicant = x.VisibleToApplicants,
                VisibleToConsultee = x.VisibleToConsultees,
                Comment = x.Comment,
                Label = x.Label
            }).ToList();
        expectedSiteVisitMetadata.Add(new SiteVisitEvidenceDocument
        {
            FileName = model.SiteVisitEvidenceMetadata[0].FileName,
            DocumentId = successes[0].DocumentIds.FirstOrDefault(),
            VisibleToApplicant = model.SiteVisitEvidenceMetadata[0].VisibleToApplicants,
            VisibleToConsultee = model.SiteVisitEvidenceMetadata[0].VisibleToConsultees,
            Comment = model.SiteVisitEvidenceMetadata[0].Comment,
            Label = model.SiteVisitEvidenceMetadata[0].Label
        });
        expectedSiteVisitMetadata.Add(new SiteVisitEvidenceDocument
        {
            FileName = model.SiteVisitEvidenceMetadata[1].FileName,
            DocumentId = successes[1].DocumentIds.FirstOrDefault(),
            VisibleToApplicant = model.SiteVisitEvidenceMetadata[1].VisibleToApplicants,
            VisibleToConsultee = model.SiteVisitEvidenceMetadata[1].VisibleToConsultees,
            Comment = model.SiteVisitEvidenceMetadata[1].Comment,
            Label = model.SiteVisitEvidenceMetadata[1].Label
        });

        MockAddDocumentService
            .SetupSequence(m => m.AddDocumentsAsInternalUserAsync(It.Is<AddDocumentsRequest>(a => a.FileToStoreModels.Single().FileName == _formFileCollection.First().FileName), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(successes[0]))
            .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(successes[1]));

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdateSiteVisitEvidenceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<SiteVisitEvidenceDocument[]>(), It.IsAny<FormLevelCaseNote>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.AddSiteVisitEvidenceAsync(model, _formFileCollection, _testUser, CancellationToken.None);

        Assert.True(result.IsSuccess);

        MockAddDocumentService.Verify(x => x.AddDocumentsAsInternalUserAsync(
            It.Is<AddDocumentsRequest>(a =>
                a.ActorType == ActorType.InternalUser
                && a.DocumentPurpose == DocumentPurpose.SiteVisitAttachment
                && a.FellingApplicationId == model.ApplicationId
                && a.FileToStoreModels.Count == 1
                && a.FileToStoreModels.Single().FileName == _formFileCollection.First().FileName
                && a.FileToStoreModels.Single().ContentType == _formFileCollection.First().ContentType),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        MockAddDocumentService.VerifyNoOtherCalls();

        UpdateWoodlandOfficerReviewService.Verify(x => x.UpdateSiteVisitEvidenceAsync(
            model.ApplicationId,
            _testUser.UserAccountId!.Value,
            It.Is<SiteVisitEvidenceDocument[]>(docs => docs.Count() == expectedSiteVisitMetadata.Count()
                && docs.All(d => expectedSiteVisitMetadata.Any(e =>
                    e.FileName == d.FileName
                    && e.DocumentId == d.DocumentId
                    && e.VisibleToApplicant == d.VisibleToApplicant
                    && e.VisibleToConsultee == d.VisibleToConsultee
                    && e.Comment == d.Comment
                    && e.Label == d.Label))),
            model.SiteVisitComment,
            model.SiteVisitComplete,
            It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == _testUser.UserAccountId!.Value
                && a.SourceEntityId == model.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                    JsonSerializer.Serialize(new
                    {
                        Section = "Site Visit"
                    }, SerializerOptions)
                ),
            It.IsAny<CancellationToken>()), Times.Once);

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateSiteVisit
                && a.ActorType == ActorType.InternalUser
                && a.UserId == _testUser.UserAccountId!.Value
                && a.SourceEntityId == model.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    SiteVisitEvidenceCount = model.SiteVisitEvidenceMetadata.Count(c => !c.MarkedForDeletion),
                    SiteVisitComment = model.SiteVisitComment.CaseNote,
                    SiteVisitComplete = model.SiteVisitComplete
                }, SerializerOptions)
            ),
            It.IsAny<CancellationToken>()), Times.Once);

        AuditingService.VerifyNoOtherCalls();

        MockRemoveDocumentService.VerifyNoOtherCalls();
    }

    private SiteVisitUseCase CreateSut()
    {
        ResetMocks();
        
        _formFileCollection = [];
        AddFileToFormCollection();

        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: Guid.NewGuid(),
            accountTypeInternal: AccountTypeInternal.WoodlandOfficer);
        _testUser = new InternalUser(userPrincipal);

        return new SiteVisitUseCase(
            InternalUserAccountService.Object,
            ExternalUserAccountRepository.Object,
            FlaRepository.Object,
            WoodlandOwnerService.Object,
            WoodlandOfficerReviewService.Object,
            UpdateWoodlandOfficerReviewService.Object,
            ActivityFeedItemProvider.Object,
            AuditingService.Object,
            MockAgentAuthorityService.Object,
            GetConfiguredFcAreas.Object,
            MockAddDocumentService.Object,
            MockRemoveDocumentService.Object,
            _foresterServices.Object,
            RequestContext,
            WoodlandOfficerReviewSubStatusService.Object,
            new NullLogger<SiteVisitUseCase>());
    }

    private void AddFileToFormCollection(string fileName = "test.csv", string expectedFileContents = "test", string contentType = "text/csv")
    {
        var fileBytes = Encoding.Default.GetBytes(expectedFileContents);
        var formFileMock = new Mock<IFormFile>();

        formFileMock.Setup(ff => ff.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, CancellationToken>((s, _) =>
            {
                var buffer = fileBytes;
                s.Write(buffer, 0, buffer.Length);
                return Task.CompletedTask;
            });

        formFileMock.Setup(ff => ff.FileName).Returns(fileName);
        formFileMock.Setup(ff => ff.Length).Returns(fileBytes.Length);
        formFileMock.Setup(ff => ff.ContentType).Returns(contentType);

        _formFileCollection.Add(formFileMock.Object);
    }
}