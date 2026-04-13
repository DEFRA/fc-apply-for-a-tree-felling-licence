using System.Text.Json;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.PublicRegister;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Microsoft.Extensions.Logging;
using Moq;
using NodaTime;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.Api;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class PublicRegisterCommentsUseCaseTests
{
    private readonly Mock<IGetFellingLicenceApplicationForInternalUsers> _applicationService = new();
    private readonly Mock<IPublicRegister> _publicRegister = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<ILogger<PublicRegisterCommentsUseCase>> _logger = new();
    private readonly Mock<INotificationHistoryService> _notificationHistoryService = new();
    private DateTime _runTime;

    private PublicRegisterCommentsUseCase CreateSut()
    {
        _runTime = DateTime.UtcNow;
        _clock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(_runTime));
        return new PublicRegisterCommentsUseCase(
            _applicationService.Object,
            _publicRegister.Object,
            _clock.Object,
            _logger.Object,
            _notificationHistoryService.Object);
    }

    [Fact]
    public async Task ReturnsZeroCounts_WhenNoApplications()
    {
        _applicationService.Setup(x => x.RetrieveApplicationsOnTheConsultationPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PublicRegisterPeriodEndModel>());

        var sut = CreateSut();
        var result = await sut.GetNewCommentsFromPublicRegisterAsync(CancellationToken.None);

        Assert.Equal("Total comments retrieved: 0, total comments imported: 0", result);

        // Assert no attempt was made to fetch comments when there are no applications
        _publicRegister.Verify(
            x => x.GetCaseCommentsByCaseReferenceAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReturnsZeroCounts_WhenNoCommentsAreRetrieved()
    {
        // Arrange
        var appRef = "APP-789";
        var appId = Guid.NewGuid();
        var app = new PublicRegisterPeriodEndModel { ApplicationId = appId, ApplicationReference = appRef };
        var ct = CancellationToken.None;

        // Use a fixed instant to avoid time-based flakiness (not strictly needed with non-date API)
        var fixedInstant = Instant.FromUtc(2025, 1, 2, 10, 0);

        _applicationService
            .Setup(x => x.RetrieveApplicationsOnTheConsultationPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PublicRegisterPeriodEndModel> { app });

        _publicRegister
            .Setup(x => x.GetCaseCommentsByCaseReferenceAsync(appRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<EsriCaseComments>()));
        
        _notificationHistoryService.Setup(x =>
                x.RetrieveNotificationHistoryAsync(It.IsAny<Guid>(), It.IsAny<NotificationType[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<List<NotificationHistoryModel>>([]));

        var sut = CreateSut();

        // Override the default clock setup from CreateSut
        _clock.Setup(x => x.GetCurrentInstant()).Returns(fixedInstant);

        // Act
        var result = await sut.GetNewCommentsFromPublicRegisterAsync(ct);

        // Assert
        Assert.Equal("Total comments retrieved: 0, total comments imported: 0", result);
        _publicRegister.Verify(x => x.GetCaseCommentsByCaseReferenceAsync(
            appRef,
            It.Is<CancellationToken>(t => t == ct)
        ), Times.Once);
    }

    [Fact]
    public async Task LogsError_WhenCommentsResultIsFailure()
    {
        var appRef = "APP-999";
        var appId = Guid.NewGuid();
        var app = new PublicRegisterPeriodEndModel { ApplicationId = appId, ApplicationReference = appRef };
        _applicationService.Setup(x => x.RetrieveApplicationsOnTheConsultationPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PublicRegisterPeriodEndModel> { app });
        _publicRegister
            .Setup(x => x.GetCaseCommentsByCaseReferenceAsync(appRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<EsriCaseComments>>("Error"));
        _notificationHistoryService.Setup(x =>
                x.RetrieveNotificationHistoryAsync(It.IsAny<Guid>(), It.IsAny<NotificationType[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<List<NotificationHistoryModel>>([]));

        var sut = CreateSut();
        var result = await sut.GetNewCommentsFromPublicRegisterAsync(CancellationToken.None);

        Assert.Equal("Total comments retrieved: 0, total comments imported: 0", result);
        _logger.Verify(l => l.Log(
            It.Is<LogLevel>(lvl => lvl == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to get comments for application")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CallsAddNotificationHistoryListAsync_WithExpectedModels_AndReturnsCounts_OnSuccess()
    {
        // Arrange
        var ct = CancellationToken.None;
        var appRef = "APP-123";
        var appId = Guid.NewGuid();
        var app = new PublicRegisterPeriodEndModel
        {
            ApplicationId = appId,
            ApplicationReference = appRef,
            PublicRegister = new PublicRegister
            {
                FellingLicenceApplicationId = appId
            }
        };

        var comment1 = new EsriCaseComments
        {
            CaseReference = appRef,
            Firstname = "Alice",
            Surname = "Smith",
            Organisation = "Burbles Corp",
            EmailAddress = "asmith@burbles.com",
            CaseNote = "First comment",
            CreatedDate = new DateTime(2025, 1, 10, 9, 30, 0, DateTimeKind.Utc),
            GlobalIDOne = Guid.NewGuid()
        };
        var comment2 = new EsriCaseComments
        {
            CaseReference = appRef,
            Firstname = "Bob",
            Surname = "Jones",
            Organisation = "Mediatek",
            CaseNote = "Second comment",
            CreatedDate = new DateTime(2025, 1, 10, 10, 45, 0, DateTimeKind.Utc),
            GlobalIDOne = Guid.NewGuid()
        };

        _applicationService
            .Setup(x => x.RetrieveApplicationsOnTheConsultationPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PublicRegisterPeriodEndModel> { app });

        _publicRegister
            .Setup(x => x.GetCaseCommentsByCaseReferenceAsync(appRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<EsriCaseComments> { comment1, comment2 }));

        _notificationHistoryService.Setup(x =>
                x.RetrieveNotificationHistoryAsync(It.IsAny<Guid>(), It.IsAny<NotificationType[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<List<NotificationHistoryModel>>([]));

        // Capture the models passed to AddNotificationHistoryListAsync
        IEnumerable<NotificationHistoryModel> capturedModels = null!;
        _notificationHistoryService
            .Setup(x => x.AddNotificationHistoryListAsync(It.IsAny<IEnumerable<NotificationHistoryModel>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<NotificationHistoryModel>, CancellationToken>((models, token) => capturedModels = models.ToList())
            .ReturnsAsync(Result.Success());

        var sut = CreateSut();

        // Act
        var result = await sut.GetNewCommentsFromPublicRegisterAsync(ct);

        // Assert message reflects retrieved (2) and imported (2)
        Assert.Equal("Total comments retrieved: 2, total comments imported: 2", result);

        // Verify PR called once with correct reference and token
        _publicRegister.Verify(x => x.GetCaseCommentsByCaseReferenceAsync(
            appRef, It.Is<CancellationToken>(t => t == ct)), Times.Once);

        _notificationHistoryService.Verify(x => x.RetrieveNotificationHistoryAsync(appId, It.Is<NotificationType[]?>(n => n.Single() == NotificationType.PublicRegisterComment), It.IsAny<CancellationToken>()), Times.Once);
        // Verify AddNotificationHistoryListAsync is called once with expected token and models
        _notificationHistoryService.Verify(x => x.AddNotificationHistoryListAsync(
            It.IsAny<IEnumerable<NotificationHistoryModel>>(),
            It.Is<CancellationToken>(t => t == ct)), Times.Once);

        Assert.NotNull(capturedModels);
        var list = capturedModels.ToList();
        Assert.Equal(2, list.Count);

        // Validate each mapped model
        var m1 = list[0];
        Assert.Equal(NotificationType.PublicRegisterComment, m1.Type);
        Assert.Equal(JsonSerializer.Serialize(comment1), m1.Source);
        Assert.Equal("First comment", m1.Text);
        Assert.Equal(appRef, m1.ApplicationReference);
        Assert.Equal(appId, m1.ApplicationId);
        Assert.Equal(_runTime, m1.CreatedTimestamp);
        Assert.Equal(comment1.GlobalIDOne, m1.ExternalId);

        var m2 = list[1];
        Assert.Equal(NotificationType.PublicRegisterComment, m2.Type);
        Assert.Equal(JsonSerializer.Serialize(comment2), m2.Source);
        Assert.Equal("Second comment", m2.Text);
        Assert.Equal(appRef, m2.ApplicationReference);
        Assert.Equal(appId, m2.ApplicationId);
        Assert.Equal(_runTime, m2.CreatedTimestamp);
        Assert.Equal(comment2.GlobalIDOne, m2.ExternalId);
    }

    [Fact]
    public async Task CallsAddNotificationHistoryListAsync_WithExpectedModels_AndReturnsCounts_WhenCommentAlreadyInNotificationHistory_OnSuccess()
    {
        // Arrange
        var ct = CancellationToken.None;
        var appRef = "APP-123";
        var appId = Guid.NewGuid();
        var app = new PublicRegisterPeriodEndModel
        {
            ApplicationId = appId,
            ApplicationReference = appRef,
            PublicRegister = new PublicRegister
            {
                FellingLicenceApplicationId = appId
            }
        };

        var comment1 = new EsriCaseComments
        {
            CaseReference = appRef,
            Firstname = "Alice",
            Surname = "Smith",
            Organisation = "Burbles Corp",
            EmailAddress = "asmith@burbles.com",
            CaseNote = "First comment",
            CreatedDate = new DateTime(2025, 1, 10, 9, 30, 0, DateTimeKind.Utc),
            GlobalIDOne = Guid.NewGuid()
        };
        var comment2 = new EsriCaseComments
        {
            CaseReference = appRef,
            Firstname = "Bob",
            Surname = "Jones",
            Organisation = "Mediatek",
            CaseNote = "Second comment",
            CreatedDate = new DateTime(2025, 1, 10, 10, 45, 0, DateTimeKind.Utc),
            GlobalIDOne = Guid.NewGuid()
        };

        var comment1NotificationHistory = new NotificationHistoryModel
        {
            ApplicationId = appId,
            ApplicationReference = appRef,
            CreatedTimestamp = DateTime.Today.ToUniversalTime(),
            ExternalId = comment1.GlobalIDOne,
            Id = Guid.NewGuid(),
            Reviewed = false,
            Source = JsonSerializer.Serialize(comment1),
            Status = NotificationStatus.New,
            Text = comment1.CaseNote,
            Type = NotificationType.PublicRegisterComment
        };

        _applicationService
            .Setup(x => x.RetrieveApplicationsOnTheConsultationPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PublicRegisterPeriodEndModel> { app });

        _publicRegister
            .Setup(x => x.GetCaseCommentsByCaseReferenceAsync(appRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<EsriCaseComments> { comment1, comment2 }));

        _notificationHistoryService.Setup(x =>
                x.RetrieveNotificationHistoryAsync(It.IsAny<Guid>(), It.IsAny<NotificationType[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<List<NotificationHistoryModel>>([comment1NotificationHistory]));

        // Capture the models passed to AddNotificationHistoryListAsync
        IEnumerable<NotificationHistoryModel> capturedModels = null!;
        _notificationHistoryService
            .Setup(x => x.AddNotificationHistoryListAsync(It.IsAny<IEnumerable<NotificationHistoryModel>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<NotificationHistoryModel>, CancellationToken>((models, token) => capturedModels = models.ToList())
            .ReturnsAsync(Result.Success());

        var sut = CreateSut();

        // Act
        var result = await sut.GetNewCommentsFromPublicRegisterAsync(ct);

        // Assert message reflects retrieved (2) and imported (1)
        Assert.Equal("Total comments retrieved: 2, total comments imported: 1", result);

        // Verify PR called once with correct reference and token
        _publicRegister.Verify(x => x.GetCaseCommentsByCaseReferenceAsync(
            appRef, It.Is<CancellationToken>(t => t == ct)), Times.Once);

        _notificationHistoryService.Verify(x => x.RetrieveNotificationHistoryAsync(appId, It.Is<NotificationType[]?>(n => n.Single() == NotificationType.PublicRegisterComment), It.IsAny<CancellationToken>()), Times.Once);
        // Verify AddNotificationHistoryListAsync is called once with expected token and models
        _notificationHistoryService.Verify(x => x.AddNotificationHistoryListAsync(
            It.IsAny<IEnumerable<NotificationHistoryModel>>(),
            It.Is<CancellationToken>(t => t == ct)), Times.Once);

        Assert.NotNull(capturedModels);
        var list = capturedModels.ToList();
        Assert.Single(list);

        // Validate each mapped model
        var m2 = list[0];
        Assert.Equal(NotificationType.PublicRegisterComment, m2.Type);
        Assert.Equal(JsonSerializer.Serialize(comment2), m2.Source);
        Assert.Equal("Second comment", m2.Text);
        Assert.Equal(appRef, m2.ApplicationReference);
        Assert.Equal(appId, m2.ApplicationId);
        Assert.Equal(_runTime, m2.CreatedTimestamp);
        Assert.Equal(comment2.GlobalIDOne, m2.ExternalId);
    }

    [Fact]
    public async Task ReturnsCounts_WhenAddNotificationHistoryListAsyncReturnsDifferentImportCount()
    {
        // Arrange
        var ct = CancellationToken.None;
        var appRef = "APP-456";
        var appId = Guid.NewGuid();
        var app = new PublicRegisterPeriodEndModel
        {
            ApplicationId = appId,
            ApplicationReference = appRef,
            PublicRegister = new PublicRegister
            {
                FellingLicenceApplicationId = appId
            }
        };

        var comment = new EsriCaseComments
        {
            CaseReference = appRef,
            Firstname = "Charlie",
            Surname = "Brown",
            CaseNote = "Hello",
            CreatedDate = new DateTime(2025, 1, 11, 8, 0, 0, DateTimeKind.Utc),
            GlobalID = Guid.NewGuid()
        };

        _applicationService
            .Setup(x => x.RetrieveApplicationsOnTheConsultationPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PublicRegisterPeriodEndModel> { app });

        _publicRegister
            .Setup(x => x.GetCaseCommentsByCaseReferenceAsync(appRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<EsriCaseComments> { comment }));

        _notificationHistoryService.Setup(x =>
                x.RetrieveNotificationHistoryAsync(It.IsAny<Guid>(), It.IsAny<NotificationType[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<List<NotificationHistoryModel>>([]));

        // Simulate failure so the overall use case returns a failure Result<string>
        _notificationHistoryService
            .Setup(x => x.AddNotificationHistoryListAsync(It.IsAny<IEnumerable<NotificationHistoryModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetNewCommentsFromPublicRegisterAsync(ct);

        // Assert: expect failure result when persisting notifications fails
        Assert.True(result.IsFailure);
        Assert.Contains("fail", result.Error);
    }

    [Fact]
    public async Task ReturnsError_WhenAddNotificationHistoryListAsyncThrows()
    {
        // Arrange
        var ct = CancellationToken.None;
        var appRef = "APP-ERR";
        var appId = Guid.NewGuid();
        var app = new PublicRegisterPeriodEndModel
        {
            ApplicationId = appId,
            ApplicationReference = appRef,
            PublicRegister = new PublicRegister
            {
                FellingLicenceApplicationId = appId
            }
        };

        _applicationService
            .Setup(x => x.RetrieveApplicationsOnTheConsultationPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PublicRegisterPeriodEndModel> { app });

        _publicRegister
            .Setup(x => x.GetCaseCommentsByCaseReferenceAsync(appRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<EsriCaseComments>
            {
                new EsriCaseComments
                {
                    CaseReference = appRef,
                    Firstname = "Dana",
                    Surname = "White",
                    CaseNote = "Boom",
                    CreatedDate = new DateTime(2025, 1, 12, 12, 0, 0, DateTimeKind.Utc),
                    GlobalID = Guid.NewGuid()
                }
            }));

        _notificationHistoryService.Setup(x =>
                x.RetrieveNotificationHistoryAsync(It.IsAny<Guid>(), It.IsAny<NotificationType[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<List<NotificationHistoryModel>>([]));

        _notificationHistoryService
            .Setup(x => x.AddNotificationHistoryListAsync(It.IsAny<IEnumerable<NotificationHistoryModel>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB failure"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetNewCommentsFromPublicRegisterAsync(ct);

        // Assert (service now returns Result<string> failure instead of throwing)
        Assert.True(result.IsFailure);
        Assert.Contains("DB failure", result.Error);

        // Ensure comments retrieval was invoked
        _publicRegister.Verify(x => x.GetCaseCommentsByCaseReferenceAsync(
            appRef, It.Is<CancellationToken>(t => t == ct)), Times.Once);

        // Ensure AddNotificationHistoryListAsync was attempted
        _notificationHistoryService.Verify(x => x.AddNotificationHistoryListAsync(
            It.IsAny<IEnumerable<NotificationHistoryModel>>(),
            It.Is<CancellationToken>(t => t == ct)), Times.Once);
    }
}
