using AutoFixture;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services.AgentAuthority;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text;

namespace Forestry.Flo.External.Web.Tests.Services;

public class AddAgentAuthorityFormDocumentUseCaseTests
{
    private readonly Mock<IAgentAuthorityService> _mockAgentAuthorityService = new();
    private readonly Mock<IAuditService<AddAgentAuthorityFormDocumentFilesUseCase>> _mockAudit = new();
    private readonly FileTypesProvider _fileTypesProvider = new();
    private readonly Fixture _fixture = new();
    private FormFileCollection _formFileCollection = new();
    private ExternalApplicant? _externalApplicant;

    [Theory, AutoMoqData]
    public async Task ReturnsSuccess_WhenAgentAuthorityFormDocumentAddFilesProcessedCorrectly(
        Guid agentAuthorityId,
        AgentAuthorityFormResponseModel response)
    {
        // arrange
        var sut = CreateSut();

        AddFileToFormCollection();
        
        _mockAgentAuthorityService.Setup(r =>
                r.AddAgentAuthorityFormAsync(
                    It.IsAny<AddAgentAuthorityFormRequest>()
                    , It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        // act
        var result = await sut.AddAgentAuthorityFormDocumentFilesAsync(
            _externalApplicant!,
            agentAuthorityId,
            _formFileCollection,
            CancellationToken.None);

        // assert
        Assert.True(result.IsSuccess);

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId.ToString() == _externalApplicant.AgencyId
                && a.EventName == AuditEvents.AddAgentAuthorityFormFiles),
            CancellationToken.None), Times.Once);

        _mockAgentAuthorityService.Verify(
            x => x.AddAgentAuthorityFormAsync(It.Is<AddAgentAuthorityFormRequest>(
                v => v.AgentAuthorityId == agentAuthorityId &&
                     v.UploadedByUser == _externalApplicant!.UserAccountId &&
                     v.AafDocuments.Count == _formFileCollection.Count), 
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenAgentAuthorityFormDocumentAddFilesNotProcessedDueToServiceFailure(
        Guid agentAuthorityId)
    {
        // arrange
        var sut = CreateSut();

        AddFileToFormCollection();

        _mockAgentAuthorityService.Setup(r =>
                r.AddAgentAuthorityFormAsync(
                    It.IsAny<AddAgentAuthorityFormRequest>()
                    , It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AgentAuthorityFormResponseModel>("error"));

        // act
        var result = await sut.AddAgentAuthorityFormDocumentFilesAsync(
            _externalApplicant!,
            agentAuthorityId,
            _formFileCollection,
            CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId.ToString() == _externalApplicant.AgencyId
                && a.EventName == AuditEvents.AddAgentAuthorityFormFilesFailure),
            CancellationToken.None), Times.Once);

        _mockAgentAuthorityService.Verify(
            x => x.AddAgentAuthorityFormAsync(It.Is<AddAgentAuthorityFormRequest>(
                    v => v.AgentAuthorityId == agentAuthorityId &&
                         v.UploadedByUser == _externalApplicant!.UserAccountId &&
                         v.AafDocuments.Count == _formFileCollection.Count),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenCannotAddFilesDueToValidationFailure_EmptyFormFileCollection(
        Guid agentAuthorityId)
    {
        // arrange
        var sut = CreateSut();

        _mockAgentAuthorityService.Setup(r =>
                r.AddAgentAuthorityFormAsync(
                    It.IsAny<AddAgentAuthorityFormRequest>()
                    , It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AgentAuthorityFormResponseModel>("error"));

        // act
        var result = await sut.AddAgentAuthorityFormDocumentFilesAsync(
            _externalApplicant!,
            agentAuthorityId,
            _formFileCollection,
            CancellationToken.None);


        // assert
        Assert.True(result.IsFailure);

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId.ToString() == _externalApplicant.AgencyId
                && a.EventName == AuditEvents.AddAgentAuthorityFormFilesValidationFailure),
            CancellationToken.None), Times.Once);

        AssertDoesNotCallAgentAuthorityServiceToAddFiles();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenCannotAddFilesDueToValidationFailure_FormFileCollectionTooLarge(
        Guid agentAuthorityId)
    {
        // arrange
        var sut = CreateSut();

        AddFileToFormCollection();
        AddFileToFormCollection();
        AddFileToFormCollection();

        _mockAgentAuthorityService.Setup(r =>
                r.AddAgentAuthorityFormAsync(
                    It.IsAny<AddAgentAuthorityFormRequest>()
                    , It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AgentAuthorityFormResponseModel>("error"));

        // act
        var result = await sut.AddAgentAuthorityFormDocumentFilesAsync(
            _externalApplicant!,
            agentAuthorityId,
            _formFileCollection,
            CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId.ToString() == _externalApplicant.AgencyId
                && a.EventName == AuditEvents.AddAgentAuthorityFormFilesValidationFailure),
            CancellationToken.None), Times.Once);

        AssertDoesNotCallAgentAuthorityServiceToAddFiles();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenCannotAddFilesDueToValidationFailure_FileTooLarge(
        Guid agentAuthorityId)
    {
        // arrange
        var sut = CreateSut();

        AddFileToFormCollection(isTooLarge:false);

        _mockAgentAuthorityService.Setup(r =>
                r.AddAgentAuthorityFormAsync(
                    It.IsAny<AddAgentAuthorityFormRequest>()
                    , It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AgentAuthorityFormResponseModel>("error"));

        // act
        var result = await sut.AddAgentAuthorityFormDocumentFilesAsync(
            _externalApplicant!,
            agentAuthorityId,
            _formFileCollection,
            CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId.ToString() == _externalApplicant.AgencyId
                && a.EventName == AuditEvents.AddAgentAuthorityFormFilesValidationFailure),
            CancellationToken.None), Times.Once);

        AssertDoesNotCallAgentAuthorityServiceToAddFiles();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenCannotAddFilesDueToValidationFailure_WhenFileTypeNotPermitted(
        Guid agentAuthorityId)
    {
        // arrange
        var sut = CreateSut();

        AddFileToFormCollection(fileName:"test.pdf",contentType:"application/pdf");

        _mockAgentAuthorityService.Setup(r =>
                r.AddAgentAuthorityFormAsync(
                    It.IsAny<AddAgentAuthorityFormRequest>()
                    , It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AgentAuthorityFormResponseModel>("error"));

        // act
        var result = await sut.AddAgentAuthorityFormDocumentFilesAsync(
            _externalApplicant!,
            agentAuthorityId,
            _formFileCollection,
            CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId.ToString() == _externalApplicant.AgencyId
                && a.EventName == AuditEvents.AddAgentAuthorityFormFilesValidationFailure),
            CancellationToken.None), Times.Once);

        AssertDoesNotCallAgentAuthorityServiceToAddFiles();
    }


    private AddAgentAuthorityFormDocumentFilesUseCase CreateSut(int maxNumberOfDocuments = 2, int maxFileSizeBytes = 1024)
    {
        _formFileCollection = new FormFileCollection();

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            agencyId: _fixture.Create<Guid>(),
            woodlandOwnerName: _fixture.Create<string>());

        _externalApplicant = new ExternalApplicant(user);

        _mockAgentAuthorityService.Reset();
        _mockAudit.Reset();

        var options = Options.Create(
            new UserFileUploadOptions
            {
                MaxNumberDocuments = maxNumberOfDocuments,
                MaxFileSizeBytes = maxFileSizeBytes,
                AllowedFileTypes = new[]
                {
                    new AllowedFileType
                    {
                        FileUploadReasons = [FileUploadReason.AgentAuthorityForm],
                        Description = "test",
                        Extensions = new[] { "CSV", "TXT" }
                    }
                }
            });


        return new AddAgentAuthorityFormDocumentFilesUseCase(
            _mockAgentAuthorityService.Object,
            options,
            _fileTypesProvider,
            _mockAudit.Object,
            new RequestContext("test", new RequestUserModel(_externalApplicant.Principal)),
            new NullLogger<AddAgentAuthorityFormDocumentFilesUseCase>()
        );
    }

    private void AddFileToFormCollection(string fileName = "test.csv", string expectedFileContents = "test", string contentType = "text/csv", bool isTooLarge = true)
    {
        var fileBytes = !isTooLarge ? _fixture.CreateMany<byte>(100000).ToArray() : Encoding.Default.GetBytes(expectedFileContents);
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

    private void AssertDoesNotCallAgentAuthorityServiceToAddFiles()
    {
        _mockAgentAuthorityService.Verify(
            x => x.AddAgentAuthorityFormAsync(
                It.IsAny<AddAgentAuthorityFormRequest>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }
}