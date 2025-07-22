using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.Model;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using static System.Net.Mime.MediaTypeNames;

namespace Forestry.Flo.External.Web.Tests.Services;

public class GetSupportingDocumentUseCaseTests
{
    private readonly Mock<IGetDocumentServiceExternal> _mockDocumentService = new();
    private readonly Mock<IRetrieveUserAccountsService> _mockRetrieveUserAccountsService = new();

    [Theory, AutoMoqData]
    public async Task WhenDocumentServiceReturnsFailure(
        Guid applicationId,
        Guid userId,
        Guid woodlandOwnerId,
        Guid documentId,
        string error)
    {
        var user = new ExternalApplicant(UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            localAccountId: userId,
            woodlandOwnerId: woodlandOwnerId));
        
        //arrange
        var sut = CreateSut();

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = user.UserAccountId!.Value,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { woodlandOwnerId }
        };

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccessModel);

        _mockDocumentService
            .Setup(x => x.GetDocumentAsync(It.IsAny<GetDocumentExternalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FileToStoreModel>(error));

        //act
        var result = await sut.GetSupportingDocumentAsync(
            user,
            applicationId,
            documentId,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockDocumentService
            .Verify(x => x.GetDocumentAsync(It.Is<GetDocumentExternalRequest>(
                r => r.ApplicationId == applicationId && 
                     r.DocumentId == documentId && 
                     r.UserAccessModel.IsFcUser == user.IsFcUser &&
                     r.UserAccessModel.AgencyId == userAccessModel.AgencyId &&
                     r.UserAccessModel.UserAccountId == user.UserAccountId &&
                     r.UserAccessModel.WoodlandOwnerIds!.Contains(Guid.Parse(user.WoodlandOwnerId!)))
                , It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task WhenDocumentIsFound(
        Guid applicationId,
        Guid userId,
        Guid woodlandOwnerId,
        Guid documentId,
        FileToStoreModel returnedFile)
    {
        var user = new ExternalApplicant(UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            localAccountId: userId,
            woodlandOwnerId: woodlandOwnerId));

        //arrange
        var sut = CreateSut();

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = user.UserAccountId!.Value,
            AgencyId = null,
            WoodlandOwnerIds = new List<Guid> { woodlandOwnerId }
        };

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccessModel);

        _mockDocumentService
            .Setup(x => x.GetDocumentAsync(It.IsAny<GetDocumentExternalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(returnedFile));

        returnedFile.ContentType = "image/jpg";

        //act
        var result = await sut.GetSupportingDocumentAsync(
            user,
            applicationId,
            documentId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(returnedFile.FileBytes, result.Value.FileContents);
        Assert.Equal(returnedFile.ContentType, result.Value.ContentType);
        Assert.Equal(returnedFile.FileName, result.Value.FileDownloadName);

        _mockDocumentService
            .Verify(x => x.GetDocumentAsync(It.Is<GetDocumentExternalRequest>(
                    r => r.ApplicationId == applicationId &&
                         r.DocumentId == documentId &&
                         r.UserAccessModel.IsFcUser == user.IsFcUser &&
                         r.UserAccessModel.AgencyId == userAccessModel.AgencyId &&
                         r.UserAccessModel.UserAccountId == user.UserAccountId &&
                         r.UserAccessModel.WoodlandOwnerIds!.Contains(Guid.Parse(user.WoodlandOwnerId!)))
                , It.IsAny<CancellationToken>()), Times.Once);
    }
    
    private GetSupportingDocumentUseCase CreateSut()
    {
        _mockDocumentService.Reset();

        return new GetSupportingDocumentUseCase(
            _mockDocumentService.Object,
            _mockRetrieveUserAccountsService.Object,
            new NullLogger<GetSupportingDocumentUseCase>());
    }
}

