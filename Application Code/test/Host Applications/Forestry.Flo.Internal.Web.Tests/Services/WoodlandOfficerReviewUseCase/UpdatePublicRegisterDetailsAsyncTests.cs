using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class UpdatePublicRegisterDetailsAsyncTests : WoodlandOfficerReviewUseCase.WoodlandOfficerReviewUseCaseTestsBase<PublicRegisterUseCase>
{
    private PublicRegisterUseCase CreateSut(ILogger<PublicRegisterUseCase>? logger = null)
    {
        ResetMocks();
        return new PublicRegisterUseCase(
            InternalUserAccountService.Object,
            ExternalUserAccountRepository.Object,
            FlaRepository.Object,
            WoodlandOwnerService.Object,
            WoodlandOfficerReviewService.Object,
            UpdateWoodlandOfficerReviewService.Object,
            PublicRegisterService.Object,
            NotificationHistoryService.Object,
            AuditingService.Object,
            MockAgentAuthorityService.Object,
            RequestContext,
            Clock.Object,
            NotificationService.Object,
            GetConfiguredFcAreas.Object,
            new OptionsWrapper<WoodlandOfficerReviewOptions>(WoodlandOfficerReviewOptions),
            logger ?? new NullLogger<PublicRegisterUseCase>());
    }

    [Fact]
    public async Task ReturnsFailure_AndLogsError_WhenNotificationUpdateFails()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PublicRegisterUseCase>>();

        var commentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var updatedAt = new DateTime(2025, 9, 1, 12, 30, 0, DateTimeKind.Utc);

        var model = new NotificationHistoryModel
        {
            Status = NotificationStatus.Reviewed,
            Response = "Some response",
            LastUpdatedById = userId,
            LastUpdatedDate = updatedAt
        };

        var sut = CreateSut(loggerMock.Object);

        // Set up AFTER CreateSut because CreateSut resets mocks
        NotificationHistoryService
            .Setup(x => x.UpdateResponseStatusByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<NotificationStatus>(),
                It.IsAny<string?>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("db error"));

        // Act
        var result = await sut.UpdatePublicRegisterDetailsAsync(commentId, model, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);

        NotificationHistoryService.Verify(x => x.UpdateResponseStatusByIdAsync(
            commentId,
            NotificationStatus.Reviewed,
            "Some response",
            userId,
            updatedAt,
            It.IsAny<CancellationToken>()), Times.Once);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to update public register comment")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}