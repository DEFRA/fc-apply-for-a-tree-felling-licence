using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class RetrieveLegacyDocumentsServiceRetrieveForUserTests
{
    private Mock<ILegacyDocumentsRepository> _mockLegacyDocumentsRepo = new();
    private Mock<IWoodlandOwnerRepository> _mockWoodlandOwnerRepo = new();
    private Mock<IUserAccountRepository> _mockUserAccountRepo = new();
    private Mock<IAgencyRepository> _mockAgencyRepo = new();

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenGetUserAccountFails(Guid userId)
    {
        var sut = CreateSut();
        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.RetrieveLegacyDocumentsForUserAsync(userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockWoodlandOwnerRepo.VerifyNoOtherCalls();
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();
        _mockAgencyRepo.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenUserAccountHasNoWoodlandOwnerIdOrAgencyId(Guid userId)
    {
        var sut = CreateSut();

        var account = new UserAccount();

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(account));

        var result = await sut.RetrieveLegacyDocumentsForUserAsync(userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockWoodlandOwnerRepo.VerifyNoOtherCalls();
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();
        _mockAgencyRepo.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenLegacyDocumentsRepoFailsForWoodlandOwnerUser(Guid userId, Guid woodlandOwnerId)
    {
        var sut = CreateSut();

        var account = new UserAccount
        {
            WoodlandOwnerId = woodlandOwnerId
        };

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(account));

        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAllForWoodlandOwnerIdsAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var result = await sut.RetrieveLegacyDocumentsForUserAsync(userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockLegacyDocumentsRepo.Verify(x => x.GetAllForWoodlandOwnerIdsAsync(It.Is<IList<Guid>>(l => l.Single() == woodlandOwnerId), It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockWoodlandOwnerRepo.VerifyNoOtherCalls();
        _mockAgencyRepo.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenWoodlandOwnerRepoFailsForWoodlandOwnerUser(
        Guid userId,
        Guid woodlandOwnerId,
        List<TestApplicantsDatabaseFactory.TestLegacyDocument> legacyDocuments)
    {
        var sut = CreateSut();

        var account = new UserAccount
        {
            WoodlandOwnerId = woodlandOwnerId
        };

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(account));

        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAllForWoodlandOwnerIdsAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(legacyDocuments);

        _mockWoodlandOwnerRepo
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IEnumerable<WoodlandOwner>, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.RetrieveLegacyDocumentsForUserAsync(userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockLegacyDocumentsRepo.Verify(x => x.GetAllForWoodlandOwnerIdsAsync(It.Is<IList<Guid>>(l => l.Single() == woodlandOwnerId), It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockWoodlandOwnerRepo.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockWoodlandOwnerRepo.VerifyNoOtherCalls();

        _mockAgencyRepo.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenLegacyDocumentsReturnedForWoodlandOwnerUser(
        Guid userId, 
        List<TestApplicantsDatabaseFactory.TestLegacyDocument> legacyDocuments,
        List<WoodlandOwner> woodlandOwners)
    {
        var sut = CreateSut();

        var woodlandOwnerId = woodlandOwners.First().Id;

        var account = new UserAccount
        {
            WoodlandOwnerId = woodlandOwnerId
        };

        foreach (var testLegacyDocument in legacyDocuments)
        {
            testLegacyDocument.WoodlandOwnerId = woodlandOwnerId;
        }

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(account));

        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAllForWoodlandOwnerIdsAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(legacyDocuments);

        _mockWoodlandOwnerRepo
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<WoodlandOwner>, UserDbErrorReason>(woodlandOwners));

        var result = await sut.RetrieveLegacyDocumentsForUserAsync(userId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        AssertResults(legacyDocuments, result.Value, woodlandOwners);

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockLegacyDocumentsRepo.Verify(x => x.GetAllForWoodlandOwnerIdsAsync(It.Is<IList<Guid>>(l => l.Single() == woodlandOwnerId), It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockWoodlandOwnerRepo.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockWoodlandOwnerRepo.VerifyNoOtherCalls();
        
        _mockAgencyRepo.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenWoodlandOwnerRepoFailsForFcAgentUser(
        Guid userId,
        Agency fcAgency,
        List<TestApplicantsDatabaseFactory.TestLegacyDocument> legacyDocuments)
    {
        var sut = CreateSut();

        var account = new UserAccount
        {
            AgencyId = fcAgency.Id
        };

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(account));

        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAllLegacyDocumentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(legacyDocuments);

        _mockWoodlandOwnerRepo
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IEnumerable<WoodlandOwner>, UserDbErrorReason>(UserDbErrorReason.NotFound));

        _mockAgencyRepo
            .Setup(x => x.FindFcAgency(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Agency>.From(fcAgency));

        var result = await sut.RetrieveLegacyDocumentsForUserAsync(userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockLegacyDocumentsRepo.Verify(x => x.GetAllLegacyDocumentsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockWoodlandOwnerRepo.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockWoodlandOwnerRepo.VerifyNoOtherCalls();

        _mockAgencyRepo.Verify(x => x.FindFcAgency(It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepo.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenLegacyDocumentsReturnedForFcAgentUser(
        Guid userId,
        Agency fcAgency,
        List<TestApplicantsDatabaseFactory.TestLegacyDocument> legacyDocuments,
        List<WoodlandOwner> woodlandOwners)
    {
        var sut = CreateSut();

        var account = new UserAccount
        {
            AgencyId = fcAgency.Id
        };

        foreach (var testLegacyDocument in legacyDocuments)
        {
            testLegacyDocument.WoodlandOwnerId = woodlandOwners.First().Id;
        }
        legacyDocuments.Last().WoodlandOwnerId = woodlandOwners.Last().Id;

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(account));

        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAllLegacyDocumentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(legacyDocuments);

        _mockWoodlandOwnerRepo
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<WoodlandOwner>, UserDbErrorReason>(woodlandOwners));

        _mockAgencyRepo
            .Setup(x => x.FindFcAgency(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Agency>.From(fcAgency));

        var result = await sut.RetrieveLegacyDocumentsForUserAsync(userId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        AssertResults(legacyDocuments, result.Value, woodlandOwners);

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockLegacyDocumentsRepo.Verify(x => x.GetAllLegacyDocumentsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockWoodlandOwnerRepo.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockWoodlandOwnerRepo.VerifyNoOtherCalls();

        _mockAgencyRepo.Verify(x => x.FindFcAgency(It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepo.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenAgentAuthorityRepoFailsForNonFcAgentUser(
        Guid userId,
        Guid agencyId,
        Agency fcAgency)
    {
        var sut = CreateSut();

        var account = new UserAccount
        {
            AgencyId = agencyId
        };

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(account));

        _mockAgencyRepo
            .Setup(x => x.FindFcAgency(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Agency>.From(fcAgency));
        _mockAgencyRepo
            .Setup(x => x.ListAuthoritiesByAgencyAsync(It.IsAny<Guid>(), It.IsAny<AgentAuthorityStatus[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IEnumerable<AgentAuthority>, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.RetrieveLegacyDocumentsForUserAsync(userId, CancellationToken.None);

        Assert.True(result.IsFailure);
        
        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockWoodlandOwnerRepo.VerifyNoOtherCalls();

        _mockAgencyRepo.Verify(x => x.FindFcAgency(It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepo.Verify(x => x.ListAuthoritiesByAgencyAsync(agencyId, null, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepo.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenLegacyDocumentsReturnedForNonFcAgentUser(
        Guid userId,
        Guid agencyId,
        Agency fcAgency,
        List<TestApplicantsDatabaseFactory.TestLegacyDocument> legacyDocuments,
        List<WoodlandOwner> woodlandOwners)
    {
        var sut = CreateSut();

        var account = new UserAccount
        {
            AgencyId = agencyId
        };

        foreach (var testLegacyDocument in legacyDocuments)
        {
            testLegacyDocument.WoodlandOwnerId = woodlandOwners.First().Id;
        }
        legacyDocuments.Last().WoodlandOwnerId = woodlandOwners.Last().Id;

        var agentAuthorities = new List<AgentAuthority>
        {
            new AgentAuthority
            {
                WoodlandOwner = woodlandOwners.First()
            },
            new AgentAuthority
            {
                WoodlandOwner = woodlandOwners.Last()
            }
        };

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(account));

        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAllForWoodlandOwnerIdsAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(legacyDocuments);

        _mockAgencyRepo
            .Setup(x => x.FindFcAgency(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Agency>.From(fcAgency));
        _mockAgencyRepo
            .Setup(x => x.ListAuthoritiesByAgencyAsync(It.IsAny<Guid>(), It.IsAny<AgentAuthorityStatus[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<AgentAuthority>, UserDbErrorReason>(agentAuthorities));

        var result = await sut.RetrieveLegacyDocumentsForUserAsync(userId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        AssertResults(legacyDocuments, result.Value, woodlandOwners);

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockLegacyDocumentsRepo.Verify(x => x.GetAllForWoodlandOwnerIdsAsync(It.Is<IList<Guid>>(l => l.Contains(woodlandOwners.First().Id) && l.Contains(woodlandOwners.Last().Id)), It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockWoodlandOwnerRepo.VerifyNoOtherCalls();

        _mockAgencyRepo.Verify(x => x.FindFcAgency(It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepo.Verify(x => x.ListAuthoritiesByAgencyAsync(agencyId, null, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepo.VerifyNoOtherCalls();
    }

    private RetrieveLegacyDocumentsService CreateSut()
    {
        _mockLegacyDocumentsRepo.Reset();
        _mockWoodlandOwnerRepo.Reset();
        _mockUserAccountRepo.Reset();
        _mockAgencyRepo.Reset();

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            localAccountId: Guid.NewGuid());
        var requestContext = new RequestContext(
            Guid.NewGuid().ToString(),
            new RequestUserModel(user));

        return new RetrieveLegacyDocumentsService(
            _mockLegacyDocumentsRepo.Object,
            _mockWoodlandOwnerRepo.Object,
            _mockUserAccountRepo.Object,
            _mockAgencyRepo.Object,
            new Mock<IFileStorageService>().Object,
            new Mock<IAuditService<RetrieveLegacyDocumentsService>>().Object,
            requestContext,
            new NullLogger<RetrieveLegacyDocumentsService>());
    }

    private void AssertResults(
        List<TestApplicantsDatabaseFactory.TestLegacyDocument> expected,
        IList<LegacyDocumentModel> actual,
        List<WoodlandOwner> woodlandOwners)
    {
        Assert.Equal(expected.Count, actual.Count);
        foreach (var legacyDocumentModel in actual)
        {
            var woodlandOwner = woodlandOwners.Single(x => x.Id == legacyDocumentModel.WoodlandOwnerId);
            var woodlandOwnerName = woodlandOwner.OrganisationName ?? woodlandOwner.ContactName;
            Assert.Equal(woodlandOwnerName, legacyDocumentModel.WoodlandOwnerName);

            Assert.Contains(expected, l => l.Id == legacyDocumentModel.Id
                                                  && l.WoodlandOwnerId == legacyDocumentModel.WoodlandOwnerId
                                                  && l.DocumentType == legacyDocumentModel.DocumentType
                                                  && l.FileName == legacyDocumentModel.FileName
                                                  && l.FileSize == legacyDocumentModel.FileSize
                                                  && l.FileType == legacyDocumentModel.FileType
                                                  && l.Location == legacyDocumentModel.Location
                                                  && l.MimeType == legacyDocumentModel.MimeType);
        }
    }
}