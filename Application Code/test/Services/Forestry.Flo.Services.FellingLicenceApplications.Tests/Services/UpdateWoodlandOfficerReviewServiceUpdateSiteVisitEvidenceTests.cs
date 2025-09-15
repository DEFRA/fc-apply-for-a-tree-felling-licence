using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Tests.Common;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using AssignedUserRole = Forestry.Flo.Services.FellingLicenceApplications.Entities.AssignedUserRole;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateWoodlandOfficerReviewServiceUpdateSiteVisitEvidenceTests : UpdateWoodlandOfficerReviewServiceTestsBase
{
    private readonly Fixture _fixture = new();

    public UpdateWoodlandOfficerReviewServiceUpdateSiteVisitEvidenceTests()
    {
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Theory, AutoData]
    public async Task ReturnsFailureWhenRepositoryThrows(
        Guid applicationId,
        Guid userId,
        SiteVisitEvidenceDocument[] evidence,
        FormLevelCaseNote observations,
        bool isComplete)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("done gone wrong"));

        var result = await sut.UpdateSiteVisitEvidenceAsync(applicationId, userId, evidence, observations, isComplete, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailureWhenNotInCorrectState(
        Guid applicationId,
        Guid userId,
        SiteVisitEvidenceDocument[] evidence,
        FormLevelCaseNote observations,
        bool isComplete)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory>(0));

        var result = await sut.UpdateSiteVisitEvidenceAsync(applicationId, userId, evidence, observations, isComplete, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailureWhenNoAssignedWoForApplication(
        Guid applicationId,
        Guid userId,
        SiteVisitEvidenceDocument[] evidence,
        FormLevelCaseNote observations,
        bool isComplete)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>(0));

        var result = await sut.UpdateSiteVisitEvidenceAsync(applicationId, userId, evidence, observations, isComplete, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailureWhenDifferentAssignedWoForApplication(
        Guid applicationId,
        Guid userId,
        SiteVisitEvidenceDocument[] evidence,
        FormLevelCaseNote observations,
        bool isComplete)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = Guid.NewGuid() } });

        var result = await sut.UpdateSiteVisitEvidenceAsync(applicationId, userId, evidence, observations, isComplete, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData] 
    public async Task WhenNoWoodlandOfficerReviewEntityExists(
        Guid applicationId,
        Guid userId,
        SiteVisitEvidenceDocument[] evidence,
        FormLevelCaseNote observations,
        bool isComplete,
        List<Document> documents)
    {
        var sut = CreateSut();

        documents.ForEach(d =>
        {
            d.FellingLicenceApplicationId = applicationId;
            d.Purpose = DocumentPurpose.Attachment;
            d.DeletionTimestamp = null;
            d.DeletedByUserId = null;

            typeof(Document)
                .GetProperty("Id", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .SetValue(d, Guid.NewGuid());
        });

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetApplicationDocumentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        FellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        var result = await sut.UpdateSiteVisitEvidenceAsync(applicationId, userId, evidence, observations, isComplete, CancellationToken.None);

        Assert.True(result.IsSuccess);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.AddWoodlandOfficerReviewAsync(It.Is<WoodlandOfficerReview>(y => y.FellingLicenceApplicationId == applicationId && y.LastUpdatedById == userId && y.LastUpdatedDate == Now.ToDateTimeUtc()), It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.AddCaseNoteAsync(It.Is<CaseNote>(c => c.CreatedByUserId == userId && c.FellingLicenceApplicationId == applicationId && c.Type == CaseNoteType.SiteVisitComment && c.Text == observations.CaseNote && c.VisibleToApplicant == observations.VisibleToApplicant && c.VisibleToConsultee == observations.VisibleToConsultee && c.CreatedTimestamp == Now.ToDateTimeUtc()), It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetApplicationDocumentsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenWoodlandOfficerReviewEntityExists_NoExistingEvidence(
        Guid applicationId,
        Guid userId,
        SiteVisitEvidenceDocument[] evidence,
        FormLevelCaseNote observations,
        bool isComplete)
    {
        var sut = CreateSut();

        var entity = new WoodlandOfficerReview
        {
            FellingLicenceApplicationId = applicationId,
            LastUpdatedById = Guid.NewGuid(),
            LastUpdatedDate = DateTime.UtcNow.AddDays(-1),
            SiteVisitComplete = false,
            SiteVisitNeeded = true,
            SiteVisitArrangementsMade = true
        };

        List<Document> documents = [];
        
        foreach (var ev in evidence)
        {
            var doc = _fixture.Build<Document>()
                .With(x => x.FellingLicenceApplicationId, applicationId)
                .With(x => x.Purpose, DocumentPurpose.SiteVisitAttachment)
                .With(x => x.FileName, ev.FileName)
                .With(x => x.VisibleToApplicant, !ev.VisibleToApplicant)
                .With(x => x.VisibleToConsultee, !ev.VisibleToConsultee)
                .Without(x => x.DeletionTimestamp)
                .Without(x => x.DeletedByUserId)
                .Create();

            typeof(Document)
                .GetProperty("Id", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .SetValue(doc, ev.DocumentId);

            documents.Add(doc);
        }

        // Add an extra supporting document
        var supportingDoc = _fixture.Build<Document>()
            .With(x => x.FellingLicenceApplicationId, applicationId)
            .With(x => x.Purpose, DocumentPurpose.Attachment)
            .Without(x => x.DeletionTimestamp)
            .Without(x => x.DeletedByUserId)
            .Create();
        documents.Add(supportingDoc);

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetApplicationDocumentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        FellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(entity));

        var result = await sut.UpdateSiteVisitEvidenceAsync(applicationId, userId, evidence, observations, isComplete, CancellationToken.None);

        Assert.True(result.IsSuccess);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.AddCaseNoteAsync(It.Is<CaseNote>(c => c.CreatedByUserId == userId && c.FellingLicenceApplicationId == applicationId && c.Type == CaseNoteType.SiteVisitComment && c.Text == observations.CaseNote && c.VisibleToApplicant == observations.VisibleToApplicant && c.VisibleToConsultee == observations.VisibleToConsultee && c.CreatedTimestamp == Now.ToDateTimeUtc()), It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetApplicationDocumentsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();

        var expectedEvidenceEntities = evidence.Select(x => new SiteVisitEvidence
        {
            Comment = x.Comment,
            DocumentId = x.DocumentId,
            Label = x.Label,
            LastUpdatedById = userId,
            LastUpdatedDate = Now.ToDateTimeUtc()
        });

        Assert.Equal(userId, entity.LastUpdatedById);
        Assert.Equal(Now.ToDateTimeUtc(), entity.LastUpdatedDate);
        Assert.Equal(isComplete, entity.SiteVisitComplete);
        Assert.Equivalent(expectedEvidenceEntities, entity.SiteVisitEvidences);

        foreach (var document in documents)
        {
            if (document.Purpose == DocumentPurpose.SiteVisitAttachment)
            {
                var matchingEvidence = evidence.Single(x => x.DocumentId == document.Id);
                Assert.Equal(matchingEvidence.VisibleToApplicant, document.VisibleToApplicant);
                Assert.Equal(matchingEvidence.VisibleToConsultee, document.VisibleToConsultee);
            }
        }
    }

    [Theory, AutoMoqData]
    public async Task WhenWoodlandOfficerReviewEntityExists_WithExistingEvidence(
        Guid applicationId,
        Guid userId,
        SiteVisitEvidenceDocument[] evidence,
        FormLevelCaseNote observations,
        bool isComplete)
    {
        var sut = CreateSut();

        var existingEvidence = new List<SiteVisitEvidence>
        {
            new SiteVisitEvidence  // this evidence file is still in the new set, will be updated
            {
                Id = Guid.NewGuid(),
                DocumentId = Guid.NewGuid(),
                Label = "Old Evidence to be replaced",
                Comment = "Old Comment 1",
                LastUpdatedById = Guid.NewGuid(),
                LastUpdatedDate = DateTime.UtcNow.AddDays(-10)
            },
            new SiteVisitEvidence  // this evidence file is not in the new set, will be removed
            {
                Id = Guid.NewGuid(),
                DocumentId = Guid.NewGuid(),
                Label = "Old Evidence no longer included",
                Comment = "Old Comment 2",
                LastUpdatedById = Guid.NewGuid(),
                LastUpdatedDate = DateTime.UtcNow.AddDays(-5)
            }
        };

        evidence.First().DocumentId = existingEvidence[0].DocumentId;  // the first new evidence is updating the first existing one

        var entity = new WoodlandOfficerReview
        {
            FellingLicenceApplicationId = applicationId,
            LastUpdatedById = Guid.NewGuid(),
            LastUpdatedDate = DateTime.UtcNow.AddDays(-1),
            SiteVisitComplete = false,
            SiteVisitNeeded = true,
            SiteVisitArrangementsMade = true,
            SiteVisitEvidences = existingEvidence.Select(x => x).ToList()
        };

        List<Document> documents = [];

        var docToRemove = _fixture.Build<Document>()   // supporting doc for the removed evidence, will have deletion values set
            .With(x => x.FellingLicenceApplicationId, applicationId)
            .With(x => x.Purpose, DocumentPurpose.SiteVisitAttachment)
            .Without(x => x.DeletionTimestamp)
            .Without(x => x.DeletedByUserId)
            .Create();

        typeof(Document)
            .GetProperty("Id", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .SetValue(docToRemove, existingEvidence[1].DocumentId);

        documents.Add(docToRemove);

        foreach (var ev in evidence)  // supporting docs for the new and updated evidence
        {
            var doc = _fixture.Build<Document>()
                .With(x => x.FellingLicenceApplicationId, applicationId)
                .With(x => x.Purpose, DocumentPurpose.SiteVisitAttachment)
                .With(x => x.FileName, ev.FileName)
                .With(x => x.VisibleToApplicant, !ev.VisibleToApplicant)
                .With(x => x.VisibleToConsultee, !ev.VisibleToConsultee)
                .Without(x => x.DeletionTimestamp)
                .Without(x => x.DeletedByUserId)
                .Create();

            typeof(Document)
                .GetProperty("Id", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .SetValue(doc, ev.DocumentId);

            documents.Add(doc);
        }

        // Add an extra supporting document, should be untouched
        var supportingDoc = _fixture.Build<Document>()
            .With(x => x.FellingLicenceApplicationId, applicationId)
            .With(x => x.Purpose, DocumentPurpose.Attachment)
            .Without(x => x.DeletionTimestamp)
            .Without(x => x.DeletedByUserId)
            .Create();
        documents.Add(supportingDoc);

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetApplicationDocumentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        FellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(entity));

        var result = await sut.UpdateSiteVisitEvidenceAsync(applicationId, userId, evidence, observations, isComplete, CancellationToken.None);

        Assert.True(result.IsSuccess);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.AddCaseNoteAsync(It.Is<CaseNote>(c => c.CreatedByUserId == userId && c.FellingLicenceApplicationId == applicationId && c.Type == CaseNoteType.SiteVisitComment && c.Text == observations.CaseNote && c.VisibleToApplicant == observations.VisibleToApplicant && c.VisibleToConsultee == observations.VisibleToConsultee && c.CreatedTimestamp == Now.ToDateTimeUtc()), It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetApplicationDocumentsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.RemoveSiteVisitEvidenceAsync(existingEvidence[1]), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();

        var expectedEvidenceEntities = evidence.Select(x => new SiteVisitEvidence
        {
            Id = existingEvidence.FirstOrDefault(ev => ev.DocumentId == x.DocumentId)?.Id ?? Guid.Empty,
            Comment = x.Comment,
            DocumentId = x.DocumentId,
            Label = x.Label,
            LastUpdatedById = userId,
            LastUpdatedDate = Now.ToDateTimeUtc()
        });

        Assert.Equal(userId, entity.LastUpdatedById);
        Assert.Equal(Now.ToDateTimeUtc(), entity.LastUpdatedDate);
        Assert.Equal(isComplete, entity.SiteVisitComplete);
        Assert.Equivalent(expectedEvidenceEntities, entity.SiteVisitEvidences);

        foreach (var document in documents)
        {
            if (document.Purpose == DocumentPurpose.SiteVisitAttachment)
            {
                if (document.Id == docToRemove.Id)  // supporting doc for the removed evidence should be set as deleted
                {
                    Assert.NotNull(document.DeletionTimestamp);
                    Assert.Equal(userId, document.DeletedByUserId);
                    continue;
                }

                var matchingEvidence = evidence.Single(x => x.DocumentId == document.Id);  // evidence supporting docs should have visibility flags updated
                Assert.Equal(matchingEvidence.VisibleToApplicant, document.VisibleToApplicant);
                Assert.Equal(matchingEvidence.VisibleToConsultee, document.VisibleToConsultee);
            }
        }
    }
}