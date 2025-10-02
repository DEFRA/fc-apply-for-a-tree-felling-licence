using System.Text.Json;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class RevertApplicationFromWithdrawnUseCaseTests
{
    private readonly Mock<IUpdateFellingLicenceApplication> _updateFellingLicenceService = new();
    private readonly Mock<IAuditService<RevertApplicationFromWithdrawnUseCase>> _auditMock = new();
    private readonly string _requestContextCorrelationId = Guid.NewGuid().ToString();

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private RevertApplicationFromWithdrawnUseCase CreateSut()
    {
        return new RevertApplicationFromWithdrawnUseCase(
            _auditMock.Object,
            new RequestContext(
                _requestContextCorrelationId,
                new RequestUserModel(UserFactory.CreateUnauthenticatedUser())),
            _updateFellingLicenceService.Object,
            new NullLogger<RevertApplicationFromWithdrawnUseCase>());
    }

    [Fact]
    public async Task ShouldRevertApplicationFromWithdrawn_WhenUserIsAdmin()
    {
        // Arrange
        var sut = CreateSut();
        var performingUser =
            new InternalUser(
                UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                    localAccountId: Guid.NewGuid(),
                    accountTypeInternal: AccountTypeInternal.AccountAdministrator));
        var applicationId = Guid.NewGuid();

        _updateFellingLicenceService
            .Setup(x => x.TryRevertApplicationFromWithdrawnAsync(
                performingUser.UserAccountId!.Value,
                applicationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await sut.RevertApplicationFromWithdrawnAsync(performingUser, applicationId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _updateFellingLicenceService.Verify(x => x.TryRevertApplicationFromWithdrawnAsync(
            performingUser.UserAccountId!.Value,
            applicationId,
            It.IsAny<CancellationToken>()), Times.Once);
        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RevertApplicationFromWithdrawnSuccess
                         && e.SourceEntityId == applicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new { }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory, CombinatorialData]
    public async Task ShouldNotRevertApplicationFromWithdrawn_WhenUserIsNotAdmin(AccountTypeInternal role)
    {
        if (role is AccountTypeInternal.AccountAdministrator)
        {
            return;
        }

        // Arrange
        var sut = CreateSut();
        var performingUser =
            new InternalUser(
                UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                    localAccountId: Guid.NewGuid(),
                    accountTypeInternal: role));
        var applicationId = Guid.NewGuid();
        const string error = "You do not have permission to revert applications from withdrawn";

        // Act
        var result = await sut.RevertApplicationFromWithdrawnAsync(performingUser, applicationId, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        _updateFellingLicenceService.Verify(x => x.TryRevertApplicationFromWithdrawnAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RevertApplicationFromWithdrawnFailure
                         && e.SourceEntityId == applicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             Error = error,
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ShouldLogErrorAndAuditFailure_WhenRevertFails()
    {
        // Arrange
        var sut = CreateSut();
        var performingUser =
            new InternalUser(
                UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                    localAccountId: Guid.NewGuid(),
                    accountTypeInternal: AccountTypeInternal.AccountAdministrator));
        var applicationId = Guid.NewGuid();
        const string errorMessage = "Revert failed";

        _updateFellingLicenceService
            .Setup(x => x.TryRevertApplicationFromWithdrawnAsync(
                performingUser.UserAccountId!.Value,
                applicationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        // Act
        var result = await sut.RevertApplicationFromWithdrawnAsync(performingUser, applicationId, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessage, result.Error);
        _updateFellingLicenceService.Verify(x => x.TryRevertApplicationFromWithdrawnAsync(
            performingUser.UserAccountId!.Value,
            applicationId,
            It.IsAny<CancellationToken>()), Times.Once);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RevertApplicationFromWithdrawnFailure
                         && e.SourceEntityId == applicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             Error = errorMessage,
                         }, _options)),
                CancellationToken.None), Times.Once);
    }
}
