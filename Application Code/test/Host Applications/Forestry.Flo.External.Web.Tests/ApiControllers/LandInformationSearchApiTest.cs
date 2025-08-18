using System.Net;
using System.Net.Http.Headers;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Forestry.Flo.External.Web.Tests.ApiControllers
{
    public class LandInformationSearchApiTest: IClassFixture<ExternalWebApplicationFactory<Startup>>
    {
        private readonly ExternalWebApplicationFactory<Startup> _factory;

        public LandInformationSearchApiTest(ExternalWebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _factory.FellingLicenceApplicationInternalRepositoryMock.Reset();
            _factory.FellingLicenceApplicationRepositoryMock.Reset();
            _factory.FileStorageServiceMock.Reset();
            _factory.UnitOfWorkMock = new Mock<IUnitOfWork>();
            _factory.RetrieveUserAccountsServiceMock.Reset();
            _factory.FellingLicenceApplicationRepositoryMock.SetupGet(r => r.UnitOfWork).Returns(_factory.UnitOfWorkMock.Object);
            _factory.FileStorageServiceMock.Setup(f => f.StoreFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<FileUploadReason>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<StoreFileSuccessResult, StoreFileFailureResult>(new StoreFileSuccessResult("testLocation",4)));
        }

        [Theory, AutoMoqData]
        public async Task WhenValidRequest_ReturnsExpectedStatusCode(FellingLicenceApplication application)
        {
            // Arrange
            AddRequiredCurrentFlaStatus(application, FellingLicenceStatus.Draft);
            
            _factory.FellingLicenceApplicationRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync( application);
            _factory.FellingLicenceApplicationRepositoryMock.Setup(x =>
                    x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(true));

            _factory.AddDocumentServiceMock.Setup(r =>
                    r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult> (new AddDocumentsSuccessResult([Guid.NewGuid()], new List<string>())));

            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key","iazo54uwhDnydbAqbrHcDvUr4UTf4w5zr1hKwSm4JxJGNvCsS");
            var applicationId = Guid.NewGuid().ToString();
            var content = new ByteArrayContent(new byte[10]);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            // Act
            var response = await client.PutAsync($"/api/lis/{applicationId}", content);
            
            // Assert
            response.EnsureSuccessStatusCode(); 
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            _factory.AddDocumentServiceMock.Verify(x => x.AddDocumentsAsInternalUserAsync(
                It.Is<AddDocumentsRequest>(r =>
                    r.FellingApplicationId == application.Id
                    && r.ActorType == ActorType.System
                    && r.DocumentPurpose == DocumentPurpose.ExternalLisConstraintReport),
                It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Theory, AutoMoqData]
        public async Task WhenMissingPdfContentType_ReturnsBadRequestStatusCode(FellingLicenceApplication application)
        {
            // Arrange
            AddRequiredCurrentFlaStatus(application, FellingLicenceStatus.Draft);
            
            _factory.FellingLicenceApplicationRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync( application);
            _factory.FellingLicenceApplicationRepositoryMock.Setup(x =>
                    x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(true));

            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key","iazo54uwhDnydbAqbrHcDvUr4UTf4w5zr1hKwSm4JxJGNvCsS");
            var applicationId = Guid.NewGuid().ToString();
            var content = new ByteArrayContent(new byte[10]);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            // Act
            var response = await client.PutAsync($"/api/lis/{applicationId}", content);
            
            // Assert
            Assert.False(response.IsSuccessStatusCode); 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory, AutoMoqData]
        public async Task WhenIncorrectApplicationIdFormat_ReturnsBadRequestStatusCode(FellingLicenceApplication application)
        {
            // Arrange
            AddRequiredCurrentFlaStatus(application, FellingLicenceStatus.Draft);
            
            _factory.FellingLicenceApplicationRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync( application);
            _factory.FellingLicenceApplicationRepositoryMock.Setup(x =>
                    x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(true));

            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key","iazo54uwhDnydbAqbrHcDvUr4UTf4w5zr1hKwSm4JxJGNvCsS");
            const int applicationId = 123;
            var content = new ByteArrayContent(new byte[10]);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            // Act
            var response = await client.PutAsync($"/api/lis/{applicationId}", content);
            
            // Assert
            Assert.False(response.IsSuccessStatusCode); 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory, AutoMoqData]
        public async Task WhenNoApiKeyInHeader_ReturnsUnauthorizedRequestStatusCode(FellingLicenceApplication application)
        {
            // Arrange
            AddRequiredCurrentFlaStatus(application, FellingLicenceStatus.Draft);
            
            _factory.FellingLicenceApplicationRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync( application);
            _factory.FellingLicenceApplicationRepositoryMock.Setup(x =>
                    x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(true));

            var client = CreateClient();
            var applicationId = Guid.NewGuid();
            var content = new ByteArrayContent(new byte[10]);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            // Act
            var response = await client.PutAsync($"/api/lis/{applicationId}", content);
            
            // Assert
            Assert.False(response.IsSuccessStatusCode); 
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory, AutoMoqData]
        public async Task WhenIncorrectApiKeyValueInHeader_ReturnsUnauthorizedRequestStatusCode(FellingLicenceApplication application)
        {
            // Arrange
            AddRequiredCurrentFlaStatus(application, FellingLicenceStatus.Draft);
            
            _factory.FellingLicenceApplicationRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync( application);
            _factory.FellingLicenceApplicationRepositoryMock.Setup(x =>
                    x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(true));

            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key","letmein");
            var applicationId = Guid.NewGuid();
            var content = new ByteArrayContent(new byte[10]);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            // Act
            var response = await client.PutAsync($"/api/lis/{applicationId}", content);
            
            // Assert
            Assert.False(response.IsSuccessStatusCode); 
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory, AutoMoqData]
        public async Task WhenEmptyApiKeyValueInHeader_ReturnsUnauthorizedRequestStatusCode(FellingLicenceApplication application)
        {
            // Arrange
            AddRequiredCurrentFlaStatus(application, FellingLicenceStatus.Draft);
            
            _factory.FellingLicenceApplicationRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync( application);
            _factory.FellingLicenceApplicationRepositoryMock.Setup(x =>
                    x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(true));

            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key",string.Empty);
            var applicationId = Guid.NewGuid();
            var content = new ByteArrayContent(new byte[10]);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            // Act
            var response = await client.PutAsync($"/api/lis/{applicationId}", content);
            
            // Assert
            Assert.False(response.IsSuccessStatusCode); 
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory, AutoMoqData]
        public async Task WhenNoBytesInBody_ReturnsBadRequestStatusCode(FellingLicenceApplication application)
        {
            // Arrange
            AddRequiredCurrentFlaStatus(application, FellingLicenceStatus.Draft);
            
            _factory.FellingLicenceApplicationRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync( application);
            _factory.FellingLicenceApplicationRepositoryMock.Setup(x =>
                    x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(true));

            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key","iazo54uwhDnydbAqbrHcDvUr4UTf4w5zr1hKwSm4JxJGNvCsS");
            var applicationId = Guid.NewGuid();
            var content = new ByteArrayContent(Array.Empty<byte>());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            // Act
            var response = await client.PutAsync($"/api/lis/{applicationId}", content);
            
            // Assert
            Assert.False(response.IsSuccessStatusCode); 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory, AutoMoqData]
        public async Task WhenFileStorageDoesNotSucceed_ReturnsInternalServerErrorStatusCode(FellingLicenceApplication application)
        {
            // Arrange
            AddRequiredCurrentFlaStatus(application, FellingLicenceStatus.Draft);
            
            _factory.FellingLicenceApplicationRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync( application);
            _factory.FellingLicenceApplicationRepositoryMock.Setup(x =>
                    x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(true));

            _factory.FileStorageServiceMock.Setup(f => f.StoreFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<FileUploadReason>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<StoreFileSuccessResult, StoreFileFailureResult>(new StoreFileFailureResult(StoreFileFailureResultReason.FailedValidation )));

            _factory.AddDocumentServiceMock.Setup(r =>
                    r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsFailureResult(It.IsAny<List<string>>())));

            //_factory.retr

            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key","iazo54uwhDnydbAqbrHcDvUr4UTf4w5zr1hKwSm4JxJGNvCsS");
            var applicationId = Guid.NewGuid();
            var content = new ByteArrayContent(new byte[10]);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            // Act
            var response = await client.PutAsync($"/api/lis/{applicationId}", content);
            
            // Assert
            Assert.False(response.IsSuccessStatusCode); 
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            _factory.AddDocumentServiceMock.Verify(x => x.AddDocumentsAsInternalUserAsync(
                It.Is<AddDocumentsRequest>(r => 
                                                r.FellingApplicationId == application.Id
                                                && r.ActorType == ActorType.System),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        private static void AddRequiredCurrentFlaStatus(FellingLicenceApplication fla, FellingLicenceStatus statusRequired)
        {
            fla.StatusHistories.Add(
                new StatusHistory
                {
                    Created = DateTime.Now.AddYears(25),//make sure most recent!
                    Status = statusRequired,
                    FellingLicenceApplication = fla,
                    FellingLicenceApplicationId = fla.Id
                }
            );
        }

        private HttpClient CreateClient()
        {
            var client= _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            return client;
        }
    }
}
