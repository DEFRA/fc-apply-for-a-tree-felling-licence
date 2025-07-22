using System.Text.Json;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AdminHub;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.AdminHubs.Model;
using Forestry.Flo.Services.AdminHubs.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.ManageAdminHubUseCase;

public class EditAdminHubTests
{
    protected readonly string RequestContextCorrelationId = Guid.NewGuid().ToString();
    protected readonly Guid RequestContextUserId = Guid.NewGuid();
    private readonly Mock<IAuditService<Web.Services.AdminHub.ManageAdminHubUseCase>> MockAuditService = new();
    private readonly Mock<IAdminHubService> MockAdminHubService = new();
    private readonly Mock<IUserAccountService> MockUserAccountService = new();
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task WhenServiceReturnsSuccess(
        Guid adminHubId,
        Guid adminManagerId,
        string name)
    {
        var input = new ViewAdminHubModel
        {
            Id = adminHubId,
            SelectedOfficerId = adminManagerId,
            Name = name
        };

        var principal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: RequestContextUserId,
            accountTypeInternal: AccountTypeInternal.AdminHubManager);
        var user = new InternalUser(principal);

        var sut = CreateSut();

        MockAdminHubService
            .Setup(x => x.UpdateAdminHubDetailsAsync(It.IsAny<UpdateAdminHubDetailsRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<ManageAdminHubOutcome>());

        var result = await sut.EditAdminHub(input, user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        MockAdminHubService
            .Verify(x => x.UpdateAdminHubDetailsAsync(It.Is<UpdateAdminHubDetailsRequestModel>(x =>
                x.AdminHubId == adminHubId
                && x.UserId == adminManagerId
                && x.PerformingUserId == user.UserAccountId.Value
                && x.PerformingUserAccountType == user.AccountType
                && x.NewAdminHubName == name),
                It.IsAny<CancellationToken>()), Times.Once);
        MockAdminHubService.VerifyNoOtherCalls();
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateAdminHubDetails
                && a.ActorType == ActorType.InternalUser
                && a.UserId == RequestContextUserId
                && a.SourceEntityId == adminHubId
                && a.SourceEntityType == SourceEntityType.AdminHub
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    name = name,
                    adminManagerId = adminManagerId,
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
    }


    [Theory, AutoMoqData]
    public async Task WhenServiceReturnsNoChange(
        Guid adminHubId,
        Guid adminManagerId,
        string name)
    {
        var input = new ViewAdminHubModel
        {
            Id = adminHubId,
            SelectedOfficerId = adminManagerId,
            Name = name
        };

        var principal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: RequestContextUserId,
            accountTypeInternal: AccountTypeInternal.AdminHubManager);
        var user = new InternalUser(principal);

        var sut = CreateSut();

        MockAdminHubService
            .Setup(x => x.UpdateAdminHubDetailsAsync(It.IsAny<UpdateAdminHubDetailsRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure<ManageAdminHubOutcome>(ManageAdminHubOutcome.NoChangeSubmitted));

        var result = await sut.EditAdminHub(input, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        MockAdminHubService
            .Verify(x => x.UpdateAdminHubDetailsAsync(It.Is<UpdateAdminHubDetailsRequestModel>(x =>
                x.AdminHubId == adminHubId
                && x.UserId == adminManagerId
                && x.PerformingUserId == user.UserAccountId.Value
                && x.PerformingUserAccountType == user.AccountType
                && x.NewAdminHubName == name),
                It.IsAny<CancellationToken>()), Times.Once);
        MockAdminHubService.VerifyNoOtherCalls();
        MockAuditService.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(ManageAdminHubOutcome.AdminHubNotFound)]
    [InlineData(ManageAdminHubOutcome.UpdateFailure)]
    [InlineData(ManageAdminHubOutcome.InvalidAssignment)]
    [InlineData(ManageAdminHubOutcome.Unauthorized)]
    public async Task WhenServiceReturnsOtherError(ManageAdminHubOutcome outcome)
    {
        Guid adminHubId = Guid.NewGuid();
        Guid adminManagerId = Guid.NewGuid();
        string name = Guid.NewGuid().ToString();

        var input = new ViewAdminHubModel
        {
            Id = adminHubId,
            SelectedOfficerId = adminManagerId,
            Name = name
        };

        var principal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: RequestContextUserId,
            accountTypeInternal: AccountTypeInternal.AdminHubManager);
        var user = new InternalUser(principal);

        var sut = CreateSut();

        MockAdminHubService
            .Setup(x => x.UpdateAdminHubDetailsAsync(It.IsAny<UpdateAdminHubDetailsRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure<ManageAdminHubOutcome>(outcome));

        var result = await sut.EditAdminHub(input, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        MockAdminHubService
            .Verify(x => x.UpdateAdminHubDetailsAsync(It.Is<UpdateAdminHubDetailsRequestModel>(x =>
                    x.AdminHubId == adminHubId
                    && x.UserId == adminManagerId
                    && x.PerformingUserId == user.UserAccountId.Value
                    && x.PerformingUserAccountType == user.AccountType
                    && x.NewAdminHubName == name),
                It.IsAny<CancellationToken>()), Times.Once);
        MockAdminHubService.VerifyNoOtherCalls();
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateAdminHubDetailsFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == RequestContextUserId
                && a.SourceEntityId == adminHubId
                && a.SourceEntityType == SourceEntityType.AdminHub
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    name = name,
                    adminManagerId = adminManagerId,
                    error = outcome
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
    }

    private Web.Services.AdminHub.ManageAdminHubUseCase CreateSut()
    {
        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: RequestContextUserId,
            accountTypeInternal: AccountTypeInternal.AdminHubManager);
        var requestContext = new RequestContext(
            RequestContextCorrelationId,
            new RequestUserModel(user));

        MockAdminHubService.Reset();
        MockAuditService.Reset();
        MockUserAccountService.Reset();

        return new Web.Services.AdminHub.ManageAdminHubUseCase(
            MockUserAccountService.Object,
            MockAdminHubService.Object,
            MockAuditService.Object,
            requestContext,
            new NullLogger<Web.Services.AdminHub.ManageAdminHubUseCase>());
    }
}