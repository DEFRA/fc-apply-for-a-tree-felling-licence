using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class AgentAuthorityServiceAddAgentAuthorityFormTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = new();
    private Mock<IAgencyRepository> _mockRepository = new();
    private Mock<IUserAccountRepository> _mockUserAccountRepository = new();
    private Mock<IFileStorageService> _mockFileStorageService = new();
    private Mock<IClock> _mockClock = new();
    private Instant _now = new Instant();
    private FileTypesProvider _fileTypesProvider = new ();

    [Theory, AutoData]
    public async Task WhenCannotLocateUserAccount(
        AddAgentAuthorityFormRequest request)
    {
        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.AddAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.UploadedByUser, It.IsAny<CancellationToken>()), Times.Once);

        _mockUserAccountRepository.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
        _mockFileStorageService.VerifyNoOtherCalls();
        _mockUnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenCannotLocateAgentAuthority(
        UserAccount user,
        AddAgentAuthorityFormRequest request)
    {
        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AgentAuthority, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.AddAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.UploadedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();
        
        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);
        
        _mockFileStorageService.VerifyNoOtherCalls();
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenAgentAuthorityIsDeactivated(
        UserAccount user,
        AgentAuthority agentAuthority,
        AddAgentAuthorityFormRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Deactivated;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        var result = await sut.AddAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.UploadedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.VerifyNoOtherCalls();
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenAgentAuthorityIsNotLinkedToUsersAgency(
        UserAccount user,
        AgentAuthority agentAuthority,
        AddAgentAuthorityFormRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(0);
        user.Agency.IsFcAgency = false;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        var result = await sut.AddAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.UploadedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.VerifyNoOtherCalls();
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenUserIsNotAnAgent(
        UserAccount user,
        AgentAuthority agentAuthority,
        AddAgentAuthorityFormRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(0);
        user.AgencyId = null;
        user.Agency = null;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        var result = await sut.AddAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.UploadedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.VerifyNoOtherCalls();
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenFileStorageFailsOnFirstFile(
        UserAccount user,
        AgentAuthority agentAuthority,
        AddAgentAuthorityFormRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(0);
        user.AgencyId = agentAuthority.Agency.Id;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        _mockFileStorageService
            .Setup(x => x.StoreFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<FileUploadReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<StoreFileSuccessResult, StoreFileFailureResult>(StoreFileFailureResult.CreateWithInvalidFileReason(FileInvalidReason.FailedVirusScan)));

        var result = await sut.AddAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.UploadedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        var firstFile = request.AafDocuments.First();
        var storedLocationPath = Path.Combine(agentAuthority.Id.ToString(), "AAF_document");
        _mockFileStorageService.Verify(x => x.StoreFileAsync(firstFile.FileName, firstFile.FileBytes, storedLocationPath, false, FileUploadReason.AgentAuthorityForm, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.VerifyNoOtherCalls();
        
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenFileStorageFailsOnSecondFileFile(
        UserAccount user,
        AgentAuthority agentAuthority,
        StoreFileSuccessResult storeFileResult,
        AddAgentAuthorityFormRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(0);
        user.AgencyId = agentAuthority.Agency.Id;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));
        
        var firstFile = request.AafDocuments.First();
        var secondFile = request.AafDocuments.Skip(1).First();
        _mockFileStorageService
            .Setup(x => x.StoreFileAsync(firstFile.FileName, It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<FileUploadReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<StoreFileSuccessResult, StoreFileFailureResult>(storeFileResult));
        _mockFileStorageService
            .Setup(x => x.StoreFileAsync(secondFile.FileName, It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<FileUploadReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<StoreFileSuccessResult, StoreFileFailureResult>(StoreFileFailureResult.CreateWithInvalidFileReason(FileInvalidReason.FailedVirusScan)));

        var result = await sut.AddAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.UploadedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        var storedLocationPath = Path.Combine(agentAuthority.Id.ToString(), "AAF_document");
        _mockFileStorageService.Verify(x => x.StoreFileAsync(firstFile.FileName, firstFile.FileBytes, storedLocationPath, false, FileUploadReason.AgentAuthorityForm, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.Verify(x => x.StoreFileAsync(secondFile.FileName, secondFile.FileBytes, storedLocationPath, false, FileUploadReason.AgentAuthorityForm, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.Verify(x => x.RemoveFileAsync(storeFileResult.Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.VerifyNoOtherCalls();

        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenSaveChangesToDatabaseFails(
        UserAccount user,
        AgentAuthority agentAuthority,
        StoreFileSuccessResult storeFileResult,
        AddAgentAuthorityFormRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(0);
        user.AgencyId = agentAuthority.Agency.Id;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        _mockUnitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        _mockFileStorageService
            .Setup(x => x.StoreFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<FileUploadReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<StoreFileSuccessResult, StoreFileFailureResult>(storeFileResult));

        var result = await sut.AddAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.UploadedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        var storedLocationPath = Path.Combine(agentAuthority.Id.ToString(), "AAF_document");
        foreach (var file in request.AafDocuments)
        {
            _mockFileStorageService.Verify(x => x.StoreFileAsync(file.FileName, file.FileBytes, storedLocationPath, false, FileUploadReason.AgentAuthorityForm, It.IsAny<CancellationToken>()), Times.Once);
        }
        _mockFileStorageService.Verify(x => x.RemoveFileAsync(storeFileResult.Location, It.IsAny<CancellationToken>()), Times.Exactly(request.AafDocuments.Count));

        _mockFileStorageService.VerifyNoOtherCalls();

        _mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenAafAddedSuccessfullyWithNoExistingAafs(
        UserAccount user,
        AgentAuthority agentAuthority,
        StoreFileSuccessResult storeFileResult,
        AddAgentAuthorityFormRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(0);
        user.AgencyId = agentAuthority.Agency.Id;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        _mockUnitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _mockFileStorageService
            .Setup(x => x.StoreFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<FileUploadReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<StoreFileSuccessResult, StoreFileFailureResult>(storeFileResult));

        var result = await sut.AddAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.UploadedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        var storedLocationPath = Path.Combine(agentAuthority.Id.ToString(), "AAF_document");
        foreach (var file in request.AafDocuments)
        {
            _mockFileStorageService.Verify(x => x.StoreFileAsync(file.FileName, file.FileBytes, storedLocationPath, false, FileUploadReason.AgentAuthorityForm, It.IsAny<CancellationToken>()), Times.Once);
        }

        _mockFileStorageService.VerifyNoOtherCalls();

        _mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();

        AssertChangesToEntity(user, agentAuthority, storeFileResult, request);
        Assert.Equal(1, agentAuthority.AgentAuthorityForms.Count);


        //assert response values
        var responseModel = result.Value;
        AssertResponseModel(user, agentAuthority, storeFileResult, request, responseModel);
    }

    [Theory, AutoData]
    public async Task WhenAafAddedSuccessfullyByFcUser(
        UserAccount user,
        AgentAuthority agentAuthority,
        StoreFileSuccessResult storeFileResult,
        AddAgentAuthorityFormRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(0);
        user.Agency.IsFcAgency = true;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        _mockUnitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _mockFileStorageService
            .Setup(x => x.StoreFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<FileUploadReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<StoreFileSuccessResult, StoreFileFailureResult>(storeFileResult));

        var result = await sut.AddAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.UploadedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        var storedLocationPath = Path.Combine(agentAuthority.Id.ToString(), "AAF_document");
        foreach (var file in request.AafDocuments)
        {
            _mockFileStorageService.Verify(x => x.StoreFileAsync(file.FileName, file.FileBytes, storedLocationPath, false, FileUploadReason.AgentAuthorityForm, It.IsAny<CancellationToken>()), Times.Once);
        }

        _mockFileStorageService.VerifyNoOtherCalls();

        _mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();

        AssertChangesToEntity(user, agentAuthority, storeFileResult, request);
        Assert.Equal(1, agentAuthority.AgentAuthorityForms.Count);


        //assert response values
        var responseModel = result.Value;
        AssertResponseModel(user, agentAuthority, storeFileResult, request, responseModel);
    }

    [Theory, AutoData]
    public async Task WhenAafAddedSuccessfullyWithExistingAaf(
        UserAccount user,
        AgentAuthority agentAuthority,
        StoreFileSuccessResult storeFileResult,
        AddAgentAuthorityFormRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.FormUploaded;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(1)
        {
            new AgentAuthorityForm
            {
                ValidFromDate = _now.ToDateTimeUtc().AddDays(-1)
            }
        };
        user.AgencyId = agentAuthority.Agency.Id;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        _mockUnitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _mockFileStorageService
            .Setup(x => x.StoreFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<FileUploadReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<StoreFileSuccessResult, StoreFileFailureResult>(storeFileResult));

        var result = await sut.AddAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.UploadedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        var storedLocationPath = Path.Combine(agentAuthority.Id.ToString(), "AAF_document");
        foreach (var file in request.AafDocuments)
        {
            _mockFileStorageService.Verify(x => x.StoreFileAsync(file.FileName, file.FileBytes, storedLocationPath, false, FileUploadReason.AgentAuthorityForm, It.IsAny<CancellationToken>()), Times.Once);
        }

        _mockFileStorageService.VerifyNoOtherCalls();

        _mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();

        AssertChangesToEntity(user, agentAuthority, storeFileResult, request);

        Assert.Equal(2, agentAuthority.AgentAuthorityForms.Count);
        var existingAuthorityForm = agentAuthority.AgentAuthorityForms.Single(x => x.ValidFromDate == _now.ToDateTimeUtc().AddDays(-1));
        Assert.Equal(_now.ToDateTimeUtc(), existingAuthorityForm.ValidToDate);

        //assert response values
        var responseModel = result.Value;
        AssertResponseModel(user, agentAuthority, storeFileResult, request, responseModel);
    }

    private void AssertChangesToEntity(
        UserAccount user,
        AgentAuthority agentAuthority,
        StoreFileSuccessResult storeFileResult,
        AddAgentAuthorityFormRequest request)
    {
        Assert.Equal(user, agentAuthority.ChangedByUser);
        Assert.Equal(AgentAuthorityStatus.FormUploaded, agentAuthority.Status);
        Assert.Equal(_now.ToDateTimeUtc(), agentAuthority.ChangedTimestamp);

        var aafEntity = agentAuthority.AgentAuthorityForms.SingleOrDefault(x => x.ValidFromDate == _now.ToDateTimeUtc());
        Assert.Equal(_now.ToDateTimeUtc(), aafEntity.ValidFromDate);
        Assert.Equal(agentAuthority.Id, aafEntity.AgentAuthorityId);
        Assert.Equal(request.AafDocuments.Count, aafEntity.AafDocuments.Count);

        foreach (var requestDoc in request.AafDocuments)
        {
            var docEntity = aafEntity.AafDocuments.SingleOrDefault(x => x.FileName == requestDoc.FileName);
            Assert.NotNull(docEntity);
            Assert.Equal(storeFileResult.FileSize, docEntity.FileSize);
            Assert.Equal(storeFileResult.Location, docEntity.Location);
            Assert.Equal(_fileTypesProvider.FindFileTypeByMimeTypeWithFallback(requestDoc.ContentType).Extension, docEntity.FileType);
            Assert.Equal(requestDoc.ContentType, docEntity.MimeType);
        }
    }

    private void AssertResponseModel(
        UserAccount user,
        AgentAuthority agentAuthority,
        StoreFileSuccessResult storeFileResult,
        AddAgentAuthorityFormRequest request,
        AgentAuthorityFormResponseModel responseModel)
    {
        Assert.NotNull(responseModel);

        Assert.Equal(agentAuthority.Id, responseModel.Id);
        Assert.Equal(_now.ToDateTimeUtc(), responseModel.ValidFromDate);
        Assert.False(responseModel.ValidToDate.HasValue);
        Assert.Equal(request.AafDocuments.Count, responseModel.AafDocuments.Count);

        foreach (var requestDoc in request.AafDocuments)
        {
            var docEntity = responseModel.AafDocuments.SingleOrDefault(x => x.FileName == requestDoc.FileName);
            Assert.NotNull(docEntity);
            Assert.Equal(storeFileResult.FileSize, docEntity.FileSize);
            Assert.Equal(storeFileResult.Location, docEntity.Location);
            Assert.Equal(_fileTypesProvider.FindFileTypeByMimeTypeWithFallback(requestDoc.ContentType).Extension, docEntity.FileType);
            Assert.Equal(requestDoc.ContentType, docEntity.MimeType);
        }
    }

    private IAgentAuthorityService CreateSut()
    {
        _mockUnitOfWork.Reset();
        _mockRepository.Reset();
        _mockRepository.SetupGet(x => x.UnitOfWork).Returns(_mockUnitOfWork.Object);

        _mockUserAccountRepository.Reset();
        
        _mockClock.Reset();
        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(_now);

        _mockFileStorageService.Reset();

        return new AgentAuthorityService(
            _mockRepository.Object,
            _mockUserAccountRepository.Object,
            _mockFileStorageService.Object,
            _fileTypesProvider,
            _mockClock.Object,
            new NullLogger<AgentAuthorityService>());
    }
}