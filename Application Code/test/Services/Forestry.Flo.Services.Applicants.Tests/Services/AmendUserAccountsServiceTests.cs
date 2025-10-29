using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class AmendUserAccountsServiceTests
{
    private readonly Mock<IUserAccountRepository> _mockUserRepository = new();
    private readonly Mock<IClock> _clockMock = new();
    private static readonly Fixture FixtureInstance = new();

    private DateTime _now;

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_WhenUserRetrieved(UserAccount user, UpdateUserAccountModel updateModel)
    {
        var sut = CreateSut();

        updateModel.UserAccountId = user.Id;

        _mockUserRepository.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(s => s.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await sut.UpdateUserAccountDetailsAsync(updateModel, CancellationToken.None);
        Assert.True(result.IsSuccess);

        Assert.Equal(updateModel.FirstName, user.FirstName);
        Assert.Equal(updateModel.LastName, user.LastName);
        Assert.Equal(updateModel.Title, user.Title);
        Assert.Equal(updateModel.PreferredContactMethod, user.PreferredContactMethod);

        Assert.Equal(updateModel.ContactAddress, user.ContactAddress);
        Assert.Equal(updateModel.ContactMobileNumber, user.ContactMobileTelephone);
        Assert.Equal(updateModel.ContactTelephoneNumber, user.ContactTelephone);

        Assert.Equal(_now, user.LastChanged);

        _mockUserRepository.Verify(v => v.GetAsync(user.Id, CancellationToken.None), Times.Once);
        _mockUserRepository.Verify(v => v.UnitOfWork.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailure_WhenUserNotRetrieved(UserAccount user, UpdateUserAccountModel updateModel)
    {
        var sut = CreateSut();

        updateModel.UserAccountId = user.Id;

        _mockUserRepository.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.UpdateUserAccountDetailsAsync(updateModel, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserRepository.Verify(v => v.GetAsync(user.Id, CancellationToken.None), Times.Once);
        _mockUserRepository.Verify(v => v.UnitOfWork.SaveEntitiesAsync(CancellationToken.None), Times.Never);
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailure_WhenChangesNotSaved(UserAccount user, UpdateUserAccountModel updateModel)
    {
        var sut = CreateSut();

        updateModel.UserAccountId = user.Id;

        _mockUserRepository.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(s => s.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Throws(new DbUpdateException("error"));

        var result = await sut.UpdateUserAccountDetailsAsync(updateModel, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserRepository.Verify(v => v.GetAsync(user.Id, CancellationToken.None), Times.Once);
        _mockUserRepository.Verify(v => v.UnitOfWork.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Theory]
    [InlineData(UserAccountStatus.Active)]
    [InlineData(UserAccountStatus.Deactivated)]
    [InlineData(UserAccountStatus.Invited)]
    [InlineData(UserAccountStatus.Migrated)]

    public async Task ReturnsPopulatedModel_WhenAccountRetrievedAndStatusUpdated(UserAccountStatus status)
    {
        var sut = CreateSut();

        var user = FixtureInstance.Create<UserAccount>();

        _mockUserRepository.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(s => s.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.UpdateApplicantAccountStatusAsync(user.Id, status, CancellationToken.None);

        Assert.True(result.IsSuccess);
        
        // assert model mapped correctly

        Assert.Equal(user.AccountType, result.Value.AccountType);
        Assert.Equal(user.FirstName, result.Value.FirstName);
        Assert.Equal(user.LastName, result.Value.LastName);
        Assert.Equal(user.Id, result.Value.UserAccountId);
        Assert.Equal(UserAccountStatus.Active, result.Value.Status);

        _mockUserRepository.Verify(v => v.GetAsync(user.Id, CancellationToken.None), Times.Once);
        _mockUserRepository.Verify(v => v.UnitOfWork.SaveEntitiesAsync(CancellationToken.None), Times.Once);
    }

    [Theory]
    [InlineData(UserAccountStatus.Active)]
    [InlineData(UserAccountStatus.Deactivated)]
    [InlineData(UserAccountStatus.Invited)]
    [InlineData(UserAccountStatus.Migrated)]

    public async Task ReturnsFailure_WhenEntitiesNotSaved(UserAccountStatus status)
    {
        var sut = CreateSut();

        var user = FixtureInstance.Create<UserAccount>();

        _mockUserRepository.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(s => s.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.UpdateApplicantAccountStatusAsync(user.Id, status, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserRepository.Verify(v => v.GetAsync(user.Id, CancellationToken.None), Times.Once);
        _mockUserRepository.Verify(v => v.UnitOfWork.SaveEntitiesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]

    public async Task ReturnsFailure_WhenUserNotFound()
    {
        var sut = CreateSut();

        var user = FixtureInstance.Create<UserAccount>();

        _mockUserRepository.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.General));

        var result = await sut.UpdateApplicantAccountStatusAsync(user.Id, UserAccountStatus.Deactivated, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserRepository.Verify(v => v.GetAsync(user.Id, CancellationToken.None), Times.Once);
        _mockUserRepository.Verify(v => v.UnitOfWork.SaveEntitiesAsync(CancellationToken.None), Times.Never);
    }

    private AmendUserAccountsService CreateSut()
    {
        _mockUserRepository.Reset();
        _clockMock.Reset();

        _now = DateTime.UtcNow;

        _clockMock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(_now));

        return new AmendUserAccountsService(
            _mockUserRepository.Object,
            new NullLogger<AmendUserAccountsService>(),
            _clockMock.Object);
    }
}