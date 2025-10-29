using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.External.Web.Services.AgentAuthority;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.External.Web.Tests.Services
{
    public class GetAgentAuthorityFormDocumentsUseCaseTests
    {
        private readonly Mock<IAgentAuthorityService> _agentAuthorityServiceMock;
        private readonly Mock<IRetrieveUserAccountsService> _retrieveUserAccountsServiceMock;
        private readonly Mock<IUpdateFellingLicenceApplicationForExternalUsers> _updateFlaServiceMock;
        private readonly Mock<ILogger<GetAgentAuthorityFormDocumentsUseCase>> _loggerMock;
        private readonly GetAgentAuthorityFormDocumentsUseCase _useCase;

        public GetAgentAuthorityFormDocumentsUseCaseTests()
        {
            _agentAuthorityServiceMock = new Mock<IAgentAuthorityService>();
            _retrieveUserAccountsServiceMock = new Mock<IRetrieveUserAccountsService>();
            _updateFlaServiceMock = new Mock<IUpdateFellingLicenceApplicationForExternalUsers>();
            _loggerMock = new Mock<ILogger<GetAgentAuthorityFormDocumentsUseCase>>();

            _useCase = new GetAgentAuthorityFormDocumentsUseCase(
                _agentAuthorityServiceMock.Object,
                _retrieveUserAccountsServiceMock.Object,
                _updateFlaServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GetCurrentAafDocumentAsync_ReturnsNone_WhenAuthorityIdNotFound()
        {
            // Arrange
            var agencyId = Guid.NewGuid();
            var woodlandOwnerId = Guid.NewGuid();
            var user = new ExternalApplicant(new ClaimsPrincipal());
            _agentAuthorityServiceMock.Setup(x => x.FindAgentAuthorityIdAsync(agencyId, woodlandOwnerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<Guid>.None);

            // Act
            var result = await _useCase.GetCurrentAafDocumentAsync(agencyId, woodlandOwnerId, user, CancellationToken.None);

            // Assert
            Assert.True(result.HasNoValue);
        }

        [Fact]
        public async Task GetCurrentAafDocumentAsync_ReturnsNone_WhenDocumentsResultIsFailure()
        {
            // Arrange
            var agencyId = Guid.NewGuid();
            var woodlandOwnerId = Guid.NewGuid();
            var authorityId = Guid.NewGuid();
            var userAccountId = Guid.NewGuid();

            var claims = new[] { new Claim(FloClaimTypes.LocalAccountId, userAccountId.ToString()) };
            var identity = new ClaimsIdentity(claims, FloClaimTypes.ClaimsIdentityAuthenticationType);
            var principal = new ClaimsPrincipal(identity);
            var user = new ExternalApplicant(principal);

            _agentAuthorityServiceMock.Setup(x => x.FindAgentAuthorityIdAsync(agencyId, woodlandOwnerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<Guid>.From(authorityId));
            _agentAuthorityServiceMock.Setup(x => x.GetAgentAuthorityFormDocumentsByAuthorityIdAsync(userAccountId, authorityId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<AgentAuthorityFormsWithWoodlandOwnerResponseModel>("error"));

            // Act
            var result = await _useCase.GetCurrentAafDocumentAsync(agencyId, woodlandOwnerId, user, CancellationToken.None);

            // Assert
            Assert.True(result.HasNoValue);
        }

        [Fact]
        public async Task GetCurrentAafDocumentAsync_ReturnsFileName_WhenCurrentFormHasDocument()
        {
            // Arrange
            var agencyId = Guid.NewGuid();
            var woodlandOwnerId = Guid.NewGuid();
            var authorityId = Guid.NewGuid();
            var userAccountId = Guid.NewGuid();

            var claims = new[] { new Claim(FloClaimTypes.LocalAccountId, userAccountId.ToString()) };
            var identity = new ClaimsIdentity(claims, FloClaimTypes.ClaimsIdentityAuthenticationType);
            var principal = new ClaimsPrincipal(identity);
            var user = new ExternalApplicant(principal);

            var fileName = "test.pdf";
            var responseModel = new AgentAuthorityFormsWithWoodlandOwnerResponseModel
            {
                AgencyId = agencyId,
                WoodlandOwnerModel = new WoodlandOwnerModel(),
                AgentAuthorityFormResponseModels = new System.Collections.Generic.List<AgentAuthorityFormResponseModel>
                {
                    new AgentAuthorityFormResponseModel
                    {
                        ValidToDate = null,
                        ValidFromDate = DateTime.UtcNow,
                        AafDocuments = new System.Collections.Generic.List<AafDocumentResponseModel>
                        {
                            new AafDocumentResponseModel { FileName = fileName }
                        }
                    }
                }
            };
            _agentAuthorityServiceMock.Setup(x => x.FindAgentAuthorityIdAsync(agencyId, woodlandOwnerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<Guid>.From(authorityId));
            _agentAuthorityServiceMock.Setup(x => x.GetAgentAuthorityFormDocumentsByAuthorityIdAsync(userAccountId, authorityId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(responseModel));

            // Act
            var result = await _useCase.GetCurrentAafDocumentAsync(agencyId, woodlandOwnerId, user, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(fileName, result.Value.FileName);
        }

        [Fact]
        public async Task CompleteAafStepAsync_ReturnsSuccess_WhenUserAccessAndUpdateSucceeds()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var userAccountId = Guid.NewGuid();
            var aafStepStatus = true;

            var claims = new[] { new Claim(FloClaimTypes.LocalAccountId, userAccountId.ToString()) };
            var identity = new ClaimsIdentity(claims, FloClaimTypes.ClaimsIdentityAuthenticationType);
            var principal = new ClaimsPrincipal(identity);
            var user = new ExternalApplicant(principal);

            var userAccess = new UserAccessModel { UserAccountId = userAccountId, IsFcUser = false };

            _retrieveUserAccountsServiceMock
                .Setup(s => s.RetrieveUserAccessAsync(userAccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(userAccess));

            _updateFlaServiceMock
                .Setup(s => s.UpdateAafStepAsync(applicationId, userAccess, aafStepStatus, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            // Act
            var result = await _useCase.CompleteAafStepAsync(applicationId, user, aafStepStatus, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            _retrieveUserAccountsServiceMock.Verify(s => s.RetrieveUserAccessAsync(userAccountId, It.IsAny<CancellationToken>()), Times.Once);
            _updateFlaServiceMock.Verify(s => s.UpdateAafStepAsync(applicationId, userAccess, aafStepStatus, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CompleteAafStepAsync_ReturnsFailure_WhenRetrieveUserAccessFails()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var userAccountId = Guid.NewGuid();
            var aafStepStatus = false;
            var error = "Could not retrieve user access";

            var claims = new[] { new Claim(FloClaimTypes.LocalAccountId, userAccountId.ToString()) };
            var identity = new ClaimsIdentity(claims, FloClaimTypes.ClaimsIdentityAuthenticationType);
            var principal = new ClaimsPrincipal(identity);
            var user = new ExternalApplicant(principal);

            _retrieveUserAccountsServiceMock
                .Setup(s => s.RetrieveUserAccessAsync(userAccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<UserAccessModel>(error));

            // Act
            var result = await _useCase.CompleteAafStepAsync(applicationId, user, aafStepStatus, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(error, result.Error);
            _updateFlaServiceMock.Verify(s => s.UpdateAafStepAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompleteAafStepAsync_ReturnsFailure_WhenUpdateAafStepFails()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var userAccountId = Guid.NewGuid();
            var aafStepStatus = true;
            var error = "Update failed";

            var claims = new[] { new Claim(FloClaimTypes.LocalAccountId, userAccountId.ToString()) };
            var identity = new ClaimsIdentity(claims, FloClaimTypes.ClaimsIdentityAuthenticationType);
            var principal = new ClaimsPrincipal(identity);
            var user = new ExternalApplicant(principal);

            var userAccess = new UserAccessModel { UserAccountId = userAccountId, IsFcUser = false };

            _retrieveUserAccountsServiceMock
                .Setup(s => s.RetrieveUserAccessAsync(userAccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(userAccess));

            _updateFlaServiceMock
                .Setup(s => s.UpdateAafStepAsync(applicationId, userAccess, aafStepStatus, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure(error));

            // Act
            var result = await _useCase.CompleteAafStepAsync(applicationId, user, aafStepStatus, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(error, result.Error);
            _retrieveUserAccountsServiceMock.Verify(s => s.RetrieveUserAccessAsync(userAccountId, It.IsAny<CancellationToken>()), Times.Once);
            _updateFlaServiceMock.Verify(s => s.UpdateAafStepAsync(applicationId, userAccess, aafStepStatus, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
