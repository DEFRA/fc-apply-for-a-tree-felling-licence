using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class AgentAuthorityServiceRemoveAgentAuthorityFormTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = new();
    private Mock<IAgencyRepository> _mockRepository = new();
    private Mock<IUserAccountRepository> _mockUserAccountRepository = new();
    private Mock<IClock> _mockClock = new();
    private Instant _now = new Instant();

    [Theory, AutoData]
    public async Task WhenCannotLocateUserAccount(
        RemoveAgentAuthorityFormRequest request)
    {
        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.RemoveAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.RemovedByUser, It.IsAny<CancellationToken>()), Times.Once);

        _mockUserAccountRepository.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
        _mockUnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenCannotLocateAgentAuthority(
        UserAccount user,
        RemoveAgentAuthorityFormRequest request)
    {
        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AgentAuthority, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.RemoveAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.RemovedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenAgentAuthorityIsDeactivated(
        UserAccount user,
        AgentAuthority agentAuthority,
        RemoveAgentAuthorityFormRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Deactivated;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        var result = await sut.RemoveAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.RemovedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenAgentAuthorityIsNotLinkedToUsersAgency(
        UserAccount user,
        AgentAuthority agentAuthority,
        RemoveAgentAuthorityFormRequest request)
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

        var result = await sut.RemoveAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.RemovedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenUserIsNotAnAgent(
        UserAccount user,
        AgentAuthority agentAuthority,
        RemoveAgentAuthorityFormRequest request)
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

        var result = await sut.RemoveAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.RemovedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenAgentAuthorityDoesNotHaveAnAafWithGivenId(
        UserAccount user,
        AgentAuthority agentAuthority,
        RemoveAgentAuthorityFormRequest request)
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

        var result = await sut.RemoveAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.RemovedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenSaveChangesToDatabaseFails(
        UserAccount user,
        AgentAuthority agentAuthority,
        AgentAuthorityForm agentAuthorityForm,
        RemoveAgentAuthorityFormRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(1) { agentAuthorityForm };
        user.AgencyId = agentAuthority.Agency.Id;
        request.AgentAuthorityFormId = agentAuthorityForm.Id;

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

        var result = await sut.RemoveAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.RemovedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenRemoveAafSucceeds(
        UserAccount user,
        AgentAuthority agentAuthority,
        AgentAuthorityForm agentAuthorityForm,
        RemoveAgentAuthorityFormRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(1) { agentAuthorityForm };
        user.AgencyId = agentAuthority.Agency.Id;
        request.AgentAuthorityFormId = agentAuthorityForm.Id;

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

        var result = await sut.RemoveAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.RemovedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();

        Assert.Equal(user, agentAuthority.ChangedByUser);
        Assert.Equal(_now.ToDateTimeUtc(), agentAuthority.ChangedTimestamp);
        Assert.Equal(_now.ToDateTimeUtc(), agentAuthorityForm.ValidToDate);
        Assert.Equal(AgentAuthorityStatus.Created, agentAuthority.Status);
    }

    [Theory, AutoData]
    public async Task WhenRemoveAafByFcUser(
        UserAccount user,
        AgentAuthority agentAuthority,
        AgentAuthorityForm agentAuthorityForm,
        RemoveAgentAuthorityFormRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(1) { agentAuthorityForm };
        user.Agency.IsFcAgency = true;
        request.AgentAuthorityFormId = agentAuthorityForm.Id;

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

        var result = await sut.RemoveAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.RemovedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();

        Assert.Equal(user, agentAuthority.ChangedByUser);
        Assert.Equal(_now.ToDateTimeUtc(), agentAuthority.ChangedTimestamp);
        Assert.Equal(_now.ToDateTimeUtc(), agentAuthorityForm.ValidToDate);
        Assert.Equal(AgentAuthorityStatus.Created, agentAuthority.Status);
    }

    private IAgentAuthorityService CreateSut()
    {
        _mockUnitOfWork.Reset();
        _mockRepository.Reset();
        _mockRepository.SetupGet(x => x.UnitOfWork).Returns(_mockUnitOfWork.Object);

        _mockUserAccountRepository.Reset();

        _mockClock.Reset();
        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(_now);

        return new AgentAuthorityService(
            _mockRepository.Object,
            _mockUserAccountRepository.Object,
            new Mock<IFileStorageService>().Object,
            new FileTypesProvider(),
            _mockClock.Object,
            new NullLogger<AgentAuthorityService>());
    }
}