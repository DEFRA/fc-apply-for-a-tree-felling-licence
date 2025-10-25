using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Tests.Common;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public partial class DeleteFellingLicenceServiceTests
{
    [Theory, AutoMoqData]
    public async Task PermanentlyRemoveDocumentWhenStorageServiceReturnsFailure(
        Guid applicationId,
        Document document)
    {
        var sut = CreateSut();

        _fileStorageService
            .Setup(x => x.RemoveFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure<FileAccessFailureReason>(FileAccessFailureReason.NotFound));

        var result = await sut.PermanentlyRemoveDocumentAsync(applicationId, document, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fileStorageService.Verify(x => x.RemoveFileAsync(document.Location!, It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.RemoveFellingLicenceAttachmentFailureEvent
                && a.ActorType == ActorType.InternalUser
                && a.UserId == null
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContext.RequestId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    document.Purpose,
                    DocumentId = document.Id,
                    FailureReason = FileAccessFailureReason.NotFound.ToString()
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task PermanentlyRemoveDocumentWhenStorageServiceReturnsSuccess(
        Guid applicationId,
        Document document)
    {
        var sut = CreateSut();

        _fileStorageService
            .Setup(x => x.RemoveFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<FileAccessFailureReason>());

        var result = await sut.PermanentlyRemoveDocumentAsync(applicationId, document, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _fileStorageService.Verify(x => x.RemoveFileAsync(document.Location!, It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.RemoveFellingLicenceAttachmentEvent
                && a.ActorType == ActorType.InternalUser
                && a.UserId == null
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContext.RequestId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    DocumentId = document.Id,
                    document.Purpose,
                    document.FileName,
                    document.Location
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }
}