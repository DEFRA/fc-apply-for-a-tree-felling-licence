using AutoFixture;
using AutoFixture.Xunit2;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.DataImport.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.DataImport.Models;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.External.Web.Tests.Services;

public class DataImportUseCaseTests
{
    private static readonly Fixture FixtureInstance = new();

    private readonly Mock<IImportData> _mockImportData = new();
    private readonly Mock<IRetrieveUserAccountsService> _mockRetrieveUserAccountsService = new();
    private FormFileCollection _formFileCollection = new();
    private ExternalApplicant _user;

    [Theory, AutoData]
    public async Task ParseWhenUnableToGetUserAccess(Guid woodlandOwnerId)
    {
        var sut = CreateSut();
        
        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccessModel>("error"));

        var result = await sut.ParseImportDataFilesAsync(_user, woodlandOwnerId, _formFileCollection, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Failed to retrieve user access", result.Error.Single());

        _mockRetrieveUserAccountsService.Verify(s => s.RetrieveUserAccessAsync(_user.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.Once);
        _mockRetrieveUserAccountsService.VerifyNoOtherCalls();

        _mockImportData.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ParseWhenUnableToParseFiles(
        Guid woodlandOwnerId,
        UserAccessModel uam,
        List<string> errors)
    {
        var sut = CreateSut();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uam));

        _mockImportData.Setup(x => x.ParseDataImportRequestAsync(It.IsAny<DataImportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ImportFileSetContents, List<string>>(errors));

        var result = await sut.ParseImportDataFilesAsync(_user, woodlandOwnerId, _formFileCollection, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(errors, result.Error);

        _mockRetrieveUserAccountsService.Verify(s => s.RetrieveUserAccessAsync(_user.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.Once);
        _mockRetrieveUserAccountsService.VerifyNoOtherCalls();

        _mockImportData.Verify(s => s.ParseDataImportRequestAsync(
            It.Is<DataImportRequest>(r => r.UserAccessModel == uam && r.WoodlandOwnerId == woodlandOwnerId && r.ImportFileSet == _formFileCollection),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockImportData.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ParseWhenSuccessful(
        Guid woodlandOwnerId,
        UserAccessModel uam,
        ImportFileSetContents importFiles)
    {
        var sut = CreateSut();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uam));

        _mockImportData.Setup(x => x.ParseDataImportRequestAsync(It.IsAny<DataImportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ImportFileSetContents, List<string>>(importFiles));

        var result = await sut.ParseImportDataFilesAsync(_user, woodlandOwnerId, _formFileCollection, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(importFiles, result.Value);

        _mockRetrieveUserAccountsService.Verify(s => s.RetrieveUserAccessAsync(_user.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.Once);
        _mockRetrieveUserAccountsService.VerifyNoOtherCalls();

        _mockImportData.Verify(s => s.ParseDataImportRequestAsync(
            It.Is<DataImportRequest>(r => r.UserAccessModel == uam && r.WoodlandOwnerId == woodlandOwnerId && r.ImportFileSet == _formFileCollection),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockImportData.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ImportDataWhenUnableToGetUserAccess(
        Guid woodlandOwnerId,
        ImportFileSetContents importFiles)
    {
        var sut = CreateSut();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccessModel>("error"));

        var result = await sut.ImportDataAsync(_user, woodlandOwnerId, importFiles, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Failed to retrieve user access", result.Error.Single());

        _mockRetrieveUserAccountsService.Verify(s => s.RetrieveUserAccessAsync(_user.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.Once);
        _mockRetrieveUserAccountsService.VerifyNoOtherCalls();

        _mockImportData.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ImportDataWhenUnableToImportData(
        Guid woodlandOwnerId,
        UserAccessModel uam,
        ImportFileSetContents importFiles,
        List<string> errors)
    {
        var sut = CreateSut();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uam));

        _mockImportData.Setup(x => x.ImportDataAsync(It.IsAny<UserAccessModel>(), It.IsAny<Guid>(), It.IsAny<ImportFileSetContents>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Dictionary<Guid, string>, List<string>>(errors));

        var result = await sut.ImportDataAsync(_user, woodlandOwnerId, importFiles, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(errors, result.Error);

        _mockRetrieveUserAccountsService.Verify(s => s.RetrieveUserAccessAsync(_user.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.Once);
        _mockRetrieveUserAccountsService.VerifyNoOtherCalls();

        _mockImportData.Verify(s => s.ImportDataAsync(
            uam, woodlandOwnerId, importFiles, It.IsAny<CancellationToken>()), Times.Once);
        _mockImportData.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ImportDataWhenSuccessful(
        Guid woodlandOwnerId,
        UserAccessModel uam,
        ImportFileSetContents importFiles,
        Dictionary<Guid, string> results)
    {
        var sut = CreateSut();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uam));

        _mockImportData.Setup(x => x.ImportDataAsync(It.IsAny<UserAccessModel>(), It.IsAny<Guid>(), It.IsAny<ImportFileSetContents>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Dictionary<Guid, string>, List<string>>(results));

        var result = await sut.ImportDataAsync(_user, woodlandOwnerId, importFiles, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(results, result.Value);

        _mockRetrieveUserAccountsService.Verify(s => s.RetrieveUserAccessAsync(_user.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.Once);
        _mockRetrieveUserAccountsService.VerifyNoOtherCalls();

        _mockImportData.Verify(s => s.ImportDataAsync(
            uam, woodlandOwnerId, importFiles, It.IsAny<CancellationToken>()), Times.Once);
        _mockImportData.VerifyNoOtherCalls();
    }

    private DataImportUseCase CreateSut()
    {
        _user = new ExternalApplicant(UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(localAccountId: Guid.NewGuid()));

        _mockImportData.Reset();
        _mockRetrieveUserAccountsService.Reset();
        
        _formFileCollection = new FormFileCollection();
        AddFileToFormCollection();

        return new DataImportUseCase(
            _mockImportData.Object,
            _mockRetrieveUserAccountsService.Object,
            new NullLogger<DataImportUseCase>());
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