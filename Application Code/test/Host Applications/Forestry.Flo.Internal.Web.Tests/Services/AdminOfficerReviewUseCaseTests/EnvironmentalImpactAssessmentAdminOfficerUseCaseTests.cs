using System.Text.Json;
using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using UserAccount = Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount;
using ExternalUserAccount = Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount;

namespace Forestry.Flo.Internal.Web.Tests.Services.AdminOfficerReviewUseCaseTests;

public class EnvironmentalImpactAssessmentAdminOfficerUseCaseTests
{
    private readonly Mock<IUserAccountService> _internalUserAccountService = new();
    private readonly Mock<IRetrieveUserAccountsService> _externalUserAccountService = new();
    private readonly Mock<ILogger<EnvironmentalImpactAssessmentAdminOfficerUseCase>> _logger = new();
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationInternalRepository = new();
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerService = new();
    private readonly Mock<IUpdateAdminOfficerReviewService> _updateAdminOfficerReviewService = new();
    private readonly Mock<IGetFellingLicenceApplicationForInternalUsers> _getFellingLicenceApplication = new();
    private readonly Mock<IAgentAuthorityService> _agentAuthorityService = new();
    private readonly Mock<IAuditService<AdminOfficerReviewUseCaseBase>> _auditService = new();
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreasService = new();
    private readonly Mock<IUpdateFellingLicenceApplication> _updateFellingLicenceApplication = new();
    private readonly Mock<ISendNotifications> _sendNotifications = new();
    private readonly Mock<IOptions<EiaOptions>> _eiaOptions = new();
    private readonly Mock<IClock> _clock = new();
    private RequestContext _requestContext = null!;
    private readonly IFixture _fixture = new Fixture().CustomiseFixtureForFellingLicenceApplications();
    private readonly string _requestContextCorrelationId = Guid.NewGuid().ToString();

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly EiaOptions _eiaOptionsValue = new EiaOptions
    {
        EiaApplicationExternalUri = "http://test",
        EiaContactEmail = "test@test.com",
        EiaContactPhone = "123456"
    };

    private EnvironmentalImpactAssessmentAdminOfficerUseCase CreateSut()
    {
        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: Guid.NewGuid(),
            accountTypeInternal: AccountTypeInternal.WoodlandOfficer);

        _requestContext = new RequestContext(
            _requestContextCorrelationId,
            new RequestUserModel(user));

        _eiaOptions.Setup(x => x.Value).Returns(_eiaOptionsValue);
        return new EnvironmentalImpactAssessmentAdminOfficerUseCase(
            _internalUserAccountService.Object,
            _externalUserAccountService.Object,
            _logger.Object,
            _fellingLicenceApplicationInternalRepository.Object,
            _woodlandOwnerService.Object,
            _updateAdminOfficerReviewService.Object,
            _getFellingLicenceApplication.Object,
            _agentAuthorityService.Object,
            _auditService.Object,
            _getConfiguredFcAreasService.Object,
            _updateFellingLicenceApplication.Object,
            _sendNotifications.Object,
            _eiaOptions.Object,
            _clock.Object,
            _requestContext
        );
    }

    [Fact]
    public async Task GetEnvironmentalImpactAssessmentAsync_ReturnsModel_WhenSuccessful()
    {
        var sut = CreateSut();
        var applicationId = Guid.NewGuid();
        var eia = _fixture.Build<EnvironmentalImpactAssessment>()
            .With(x => x.Id, Guid.NewGuid())
            .Create();
        var fla = _fixture.Build<FellingLicenceApplication>()
            .With(x => x.ApplicationReference, "APP-REF")
            .With(x => x.Documents, new List<Document>())
            .Create();

        _getFellingLicenceApplication.Setup(x => x.GetApplicationByIdAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(fla));
        _getFellingLicenceApplication.Setup(x => x.GetEnvironmentalImpactAssessmentAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Result<EnvironmentalImpactAssessment>)Result.Success(eia));

        var result = await sut.GetEnvironmentalImpactAssessmentAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("APP-REF", result.Value.ApplicationReference);
        Assert.NotNull(fla.EnvironmentalImpactAssessment);

        Assert.Equal(eia.HasApplicationBeenCompleted, result.Value.HasApplicationBeenCompleted);
        Assert.Equal(eia.HasApplicationBeenSent, result.Value.HasApplicationBeenSent);
        Assert.Equal(eia.AreAttachedFormsCorrect, result.Value.AreAttachedFormsCorrect);
        Assert.Equal(eia.HasTheEiaFormBeenReceived, result.Value.HasTheEiaFormBeenReceived);
        Assert.Equal(eia.EiaTrackerReferenceNumber, result.Value.EiaTrackerReferenceNumber);
    }

    [Fact]
    public async Task GetEnvironmentalImpactAssessmentAsync_ReturnsFailure_WhenFlaNotFound()
    {
        var sut = CreateSut();
        var applicationId = Guid.NewGuid();
        _getFellingLicenceApplication.Setup(x => x.GetApplicationByIdAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>("not found"));

        var result = await sut.GetEnvironmentalImpactAssessmentAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ConfirmAttachedEiaFormsAreCorrectAsync_Fails_WhenFormsCorrectNotSpecified()
    {
        var sut = CreateSut();
        var performingUserId = Guid.NewGuid();
        var viewModel = _fixture.Build<EiaWithFormsPresentViewModel>()
            .With(x => x.AreTheFormsCorrect, (bool?)null)
            .Create();

        var result = await sut.ConfirmAttachedEiaFormsAreCorrectAsync(viewModel, performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Must specify if the EIA forms are correct", result.Error);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateAdminOfficerReviewFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Must specify if the EIA forms are correct",
                    PerformingUserId = performingUserId
                }, _options)),
            CancellationToken.None), Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AdminOfficerCompleteEiaCheckFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Must specify if the EIA forms are correct",
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ConfirmAttachedEiaFormsAreCorrectAsync_Fails_WhenUpdateEnvironmentalImpactAssessmentFails()
    {
        var sut = CreateSut();
        var performingUserId = Guid.NewGuid();
        var transaction = new Mock<IDbContextTransaction>();
        _updateFellingLicenceApplication.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var viewModel = _fixture.Build<EiaWithFormsPresentViewModel>()
            .With(x => x.AreTheFormsCorrect, true)
            .Create();

        _updateFellingLicenceApplication.Setup(x => x.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                viewModel.ApplicationId, It.IsAny<EnvironmentalImpactAssessmentAdminOfficerRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("update failed"));

        var result = await sut.ConfirmAttachedEiaFormsAreCorrectAsync(viewModel, performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("update failed", result.Error);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateAdminOfficerReviewFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "update failed",
                    PerformingUserId = performingUserId
                }, _options)),
            CancellationToken.None), Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AdminOfficerCompleteEiaCheckFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "update failed",
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ConfirmAttachedEiaFormsAreCorrectAsync_Fails_WhenNotificationFails()
    {
        var sut = CreateSut();
        var performingUserId = Guid.NewGuid();
        var transaction = new Mock<IDbContextTransaction>();
        _updateFellingLicenceApplication.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        var fla = _fixture.Build<FellingLicenceApplication>()
            .With(x => x.ApplicationReference, "APP-REF")
            .With(x => x.Documents, new List<Document>())
            .Create();

        fla.AssigneeHistories.Clear();
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.AdminOfficer
        });
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.Author
        });

        var viewModel = _fixture.Build<EiaWithFormsPresentViewModel>()
            .With(x => x.AreTheFormsCorrect, false)
            .With(x => x.ApplicationId, fla.Id)
            .Create();

        _updateFellingLicenceApplication.Setup(x => x.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                viewModel.ApplicationId, It.IsAny<EnvironmentalImpactAssessmentAdminOfficerRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getFellingLicenceApplication.Setup(x => x.GetApplicationByIdAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(fla));

        _sendNotifications.Setup(x => x.SendNotificationAsync(
                It.IsAny<object>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("failure"));

        _updateAdminOfficerReviewService.Setup(x => x.AddEnvironmentalImpactAssessmentRequestHistoryAsync(
                It.IsAny<EnvironmentalImpactAssessmentRequestHistoryRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserDbErrorReason>());

        _internalUserAccountService.Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(_fixture.Create<UserAccount>()));

        _externalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount>());

        var result = await sut.ConfirmAttachedEiaFormsAreCorrectAsync(viewModel, performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to send notification for incomplete EIA forms", result.Error);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateAdminOfficerReviewFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "failure",
                    PerformingUserId = performingUserId
                }, _options)),
            CancellationToken.None), Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AdminOfficerCompleteEiaCheckFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "failure",
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ConfirmAttachedEiaFormsAreCorrectAsync_Fails_WhenApplicationRetrievalFails()
    {
        var sut = CreateSut();
        var performingUserId = Guid.NewGuid();
        var transaction = new Mock<IDbContextTransaction>();
        _updateFellingLicenceApplication.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var viewModel = _fixture.Build<EiaWithFormsPresentViewModel>()
            .With(x => x.AreTheFormsCorrect, false)
            .With(x => x.ApplicationId, Guid.NewGuid())
            .Create();

        _updateFellingLicenceApplication.Setup(x => x.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                viewModel.ApplicationId, It.IsAny<EnvironmentalImpactAssessmentAdminOfficerRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getFellingLicenceApplication.Setup(x => x.GetApplicationByIdAsync(viewModel.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>("fail"));

        var result = await sut.ConfirmAttachedEiaFormsAreCorrectAsync(viewModel, performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to send notification for incomplete EIA forms", result.Error);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateAdminOfficerReviewFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Unable to retrieve felling licence application",
                    PerformingUserId = performingUserId
                }, _options)),
            CancellationToken.None), Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AdminOfficerCompleteEiaCheckFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Unable to retrieve felling licence application",
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ConfirmAttachedEiaFormsAreCorrectAsync_Fails_WhenApplicantRetrievalFails()
    {
        var sut = CreateSut();
        var performingUserId = Guid.NewGuid();
        var transaction = new Mock<IDbContextTransaction>();
        _updateFellingLicenceApplication.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var fla = _fixture.Build<FellingLicenceApplication>()
            .With(x => x.ApplicationReference, "APP-REF")
            .With(x => x.Documents, new List<Document>())
            .Create();

        var applicantId = Guid.NewGuid();
        fla.AssigneeHistories.Clear();
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = applicantId,
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.Author
        });
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.AdminOfficer
        });

        var viewModel = _fixture.Build<EiaWithFormsPresentViewModel>()
            .With(x => x.AreTheFormsCorrect, false)
            .With(x => x.ApplicationId, fla.Id)
            .Create();

        _updateFellingLicenceApplication.Setup(x => x.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                viewModel.ApplicationId, It.IsAny<EnvironmentalImpactAssessmentAdminOfficerRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getFellingLicenceApplication.Setup(x => x.GetApplicationByIdAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(fla));

        _externalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(applicantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ExternalUserAccount>("fail"));

        var result = await sut.ConfirmAttachedEiaFormsAreCorrectAsync(viewModel, performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to send notification for incomplete EIA forms", result.Error);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateAdminOfficerReviewFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Unable to retrieve applicant user account",
                    PerformingUserId = performingUserId
                }, _options)),
            CancellationToken.None), Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AdminOfficerCompleteEiaCheckFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Unable to retrieve applicant user account",
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ConfirmAttachedEiaFormsAreCorrectAsync_Fails_WhenAdminOfficerRetrievalFails()
    {
        var sut = CreateSut();
        var performingUserId = Guid.NewGuid();
        var transaction = new Mock<IDbContextTransaction>();
        _updateFellingLicenceApplication.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var fla = _fixture.Build<FellingLicenceApplication>()
            .With(x => x.ApplicationReference, "APP-REF")
            .With(x => x.Documents, new List<Document>())
            .Create();

        var applicantId = Guid.NewGuid();
        var adminOfficerId = Guid.NewGuid();
        fla.AssigneeHistories.Clear();
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = applicantId,
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.Author
        });
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = adminOfficerId,
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.AdminOfficer
        });

        var viewModel = _fixture.Build<EiaWithFormsPresentViewModel>()
            .With(x => x.AreTheFormsCorrect, false)
            .With(x => x.ApplicationId, fla.Id)
            .Create();

        _updateFellingLicenceApplication.Setup(x => x.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                viewModel.ApplicationId, It.IsAny<EnvironmentalImpactAssessmentAdminOfficerRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getFellingLicenceApplication.Setup(x => x.GetApplicationByIdAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(fla));

        _externalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(applicantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<ExternalUserAccount>());

        _internalUserAccountService.Setup(x => x.GetUserAccountAsync(adminOfficerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.None);

        var result = await sut.ConfirmAttachedEiaFormsAreCorrectAsync(viewModel, performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to send notification for incomplete EIA forms", result.Error);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateAdminOfficerReviewFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Unable to retrieve admin officer user account",
                    PerformingUserId = performingUserId
                }, _options)),
            CancellationToken.None), Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AdminOfficerCompleteEiaCheckFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Unable to retrieve admin officer user account",
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ConfirmAttachedEiaFormsAreCorrectAsync_Fails_WhenStoringEiaRequestHistoryFails()
    {
        var sut = CreateSut();
        var performingUserId = Guid.NewGuid();
        var transaction = new Mock<IDbContextTransaction>();
        _updateFellingLicenceApplication.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var fla = _fixture.Build<FellingLicenceApplication>()
            .With(x => x.ApplicationReference, "APP-REF")
            .With(x => x.Documents, new List<Document>())
            .Create();

        var applicantId = Guid.NewGuid();
        var adminOfficerId = Guid.NewGuid();
        fla.AssigneeHistories.Clear();
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = applicantId,
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.Author
        });
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = adminOfficerId,
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.AdminOfficer
        });

        var viewModel = _fixture.Build<EiaWithFormsPresentViewModel>()
            .With(x => x.AreTheFormsCorrect, false)
            .With(x => x.ApplicationId, fla.Id)
            .Create();

        _updateFellingLicenceApplication.Setup(x => x.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                viewModel.ApplicationId, It.IsAny<EnvironmentalImpactAssessmentAdminOfficerRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getFellingLicenceApplication.Setup(x => x.GetApplicationByIdAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(fla));

        _externalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(applicantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<ExternalUserAccount>());

        _internalUserAccountService.Setup(x => x.GetUserAccountAsync(adminOfficerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(_fixture.Create<UserAccount>()));

        _sendNotifications.Setup(x => x.SendNotificationAsync(
                It.IsAny<object>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateAdminOfficerReviewService.Setup(x => x.AddEnvironmentalImpactAssessmentRequestHistoryAsync(
                It.IsAny<EnvironmentalImpactAssessmentRequestHistoryRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure<UserDbErrorReason>(UserDbErrorReason.General));

        var result = await sut.ConfirmAttachedEiaFormsAreCorrectAsync(viewModel, performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to send notification for incomplete EIA forms", result.Error);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateAdminOfficerReviewFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Unable to store EIA request history",
                    PerformingUserId = performingUserId
                }, _options)),
            CancellationToken.None), Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AdminOfficerCompleteEiaCheckFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Unable to store EIA request history",
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ConfirmAttachedEiaFormsAreCorrectAsync_Succeeds_WithNotification()
    {
        var sut = CreateSut();
        var performingUserId = Guid.NewGuid();
        var transaction = new Mock<IDbContextTransaction>();
        _updateFellingLicenceApplication.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var fla = _fixture.Build<FellingLicenceApplication>()
            .With(x => x.ApplicationReference, "APP-REF")
            .With(x => x.Documents, new List<Document>())
            .Create();

        fla.AssigneeHistories.Clear();
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.AdminOfficer
        });
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.Author
        });

        var viewModel = _fixture.Build<EiaWithFormsPresentViewModel>()
            .With(x => x.AreTheFormsCorrect, false)
            .With(x => x.ApplicationId, fla.Id)
            .Create();

        _updateFellingLicenceApplication.Setup(x => x.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                viewModel.ApplicationId, It.IsAny<EnvironmentalImpactAssessmentAdminOfficerRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getFellingLicenceApplication.Setup(x => x.GetApplicationByIdAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(fla));

        _sendNotifications.Setup(x => x.SendNotificationAsync(
                It.IsAny<object>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateAdminOfficerReviewService.Setup(x => x.AddEnvironmentalImpactAssessmentRequestHistoryAsync(
                It.IsAny<EnvironmentalImpactAssessmentRequestHistoryRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserDbErrorReason>());

        var internalModel = _fixture.Create<UserAccount>();

        _internalUserAccountService.Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(internalModel));

        var externalModel = _fixture.Create<ExternalUserAccount>();

        _externalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalModel);

        var result = await sut.ConfirmAttachedEiaFormsAreCorrectAsync(viewModel, performingUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        _updateFellingLicenceApplication.Verify(x =>
                x.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                    viewModel.ApplicationId,
                    It.IsAny<EnvironmentalImpactAssessmentAdminOfficerRecord>(),
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _sendNotifications.Verify(x =>
                x.SendNotificationAsync(
                    It.Is<EnvironmentalImpactAssessmentReminderDataModel>(y => 
                        y.PropertyName == fla.SubmittedFlaPropertyDetail!.Name &&
                        y.ApplicationReference == fla.ApplicationReference &&
                        y.SenderName == internalModel.FullName(true) && 
                        y.RecipientName == externalModel.FullName(true) &&
                        y.ContactEmail == _eiaOptionsValue.EiaContactEmail &&
                        y.ContactNumber == _eiaOptionsValue.EiaContactPhone &&
                        y.ApplicationFormUri == _eiaOptionsValue.EiaApplicationExternalUri),
                    NotificationType.EiaReminderMissingDocuments,
                    It.IsAny<NotificationRecipient>(),
                    null, null, null,
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _updateAdminOfficerReviewService.Verify(x =>
                x.AddEnvironmentalImpactAssessmentRequestHistoryAsync(
                    It.IsAny<EnvironmentalImpactAssessmentRequestHistoryRecord>(),
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _internalUserAccountService.Verify(x =>
                x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        _externalUserAccountService.Verify(x =>
                x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateAdminOfficerReview
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    ReviewCompleted = false,
                    PerformingUserId = performingUserId
                }, _options)),
            CancellationToken.None), Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AdminOfficerCompleteEiaCheck
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                    { }, _options)),
            CancellationToken.None), Times.Once);
    }


    [Fact]
    public async Task ConfirmEiaFormsHaveBeenReceivedAsync_Fails_WhenFormsReceivedNotSpecified()
    {
        var sut = CreateSut();
        var performingUserId = Guid.NewGuid();
        var viewModel = _fixture.Build<EiaWithFormsAbsentViewModel>()
            .With(x => x.HaveTheFormsBeenReceived, (bool?)null)
            .Create();

        var result = await sut.ConfirmEiaFormsHaveBeenReceivedAsync(viewModel, performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Must specify if the EIA forms are correct", result.Error);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateAdminOfficerReviewFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Must specify if the EIA forms are correct",
                    PerformingUserId = performingUserId
                }, _options)),
            CancellationToken.None), Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AdminOfficerCompleteEiaCheckFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Must specify if the EIA forms are correct",
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ConfirmEiaFormsHaveBeenReceivedAsync_Fails_WhenUpdateEnvironmentalImpactAssessmentFails()
    {
        var sut = CreateSut();
        var performingUserId = Guid.NewGuid();
        var transaction = new Mock<IDbContextTransaction>();
        _updateFellingLicenceApplication.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var viewModel = _fixture.Build<EiaWithFormsAbsentViewModel>()
            .With(x => x.HaveTheFormsBeenReceived, true)
            .Create();

        _updateFellingLicenceApplication.Setup(x => x.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                viewModel.ApplicationId, It.IsAny<EnvironmentalImpactAssessmentAdminOfficerRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("update failed"));

        var result = await sut.ConfirmEiaFormsHaveBeenReceivedAsync(viewModel, performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("update failed", result.Error);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);


        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateAdminOfficerReviewFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "update failed",
                    PerformingUserId = performingUserId
                }, _options)),
            CancellationToken.None), Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AdminOfficerCompleteEiaCheckFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "update failed",
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ConfirmEiaFormsHaveBeenReceivedAsync_Fails_WhenNotificationFails()
    {
        var sut = CreateSut();
        var performingUserId = Guid.NewGuid();
        var transaction = new Mock<IDbContextTransaction>();
        _updateFellingLicenceApplication.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        var fla = _fixture.Build<FellingLicenceApplication>()
            .With(x => x.ApplicationReference, "APP-REF")
            .With(x => x.Documents, new List<Document>())
            .Create();

        fla.AssigneeHistories.Clear();
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.AdminOfficer
        });
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.Author
        });

        var viewModel = _fixture.Build<EiaWithFormsAbsentViewModel>()
            .With(x => x.HaveTheFormsBeenReceived, false)
            .With(x => x.ApplicationId, fla.Id)
            .Create();

        _updateFellingLicenceApplication.Setup(x => x.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                viewModel.ApplicationId, It.IsAny<EnvironmentalImpactAssessmentAdminOfficerRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getFellingLicenceApplication.Setup(x => x.GetApplicationByIdAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(fla));

        _sendNotifications.Setup(x => x.SendNotificationAsync(
                It.IsAny<object>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("failure"));

        _updateAdminOfficerReviewService.Setup(x => x.AddEnvironmentalImpactAssessmentRequestHistoryAsync(
                It.IsAny<EnvironmentalImpactAssessmentRequestHistoryRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserDbErrorReason>());

        _internalUserAccountService.Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(_fixture.Create<UserAccount>()));

        _externalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<ExternalUserAccount>());

        var result = await sut.ConfirmEiaFormsHaveBeenReceivedAsync(viewModel, performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to send EIA reminder notification", result.Error);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateAdminOfficerReviewFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "failure",
                    PerformingUserId = performingUserId
                }, _options)),
            CancellationToken.None), Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AdminOfficerCompleteEiaCheckFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "failure",
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ConfirmEiaFormsHaveBeenReceivedAsync_Succeeds_WithNotification()
    {
        var sut = CreateSut();
        var performingUserId = Guid.NewGuid();
        var transaction = new Mock<IDbContextTransaction>();
        _updateFellingLicenceApplication.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var fla = _fixture.Build<FellingLicenceApplication>()
            .With(x => x.ApplicationReference, "APP-REF")
            .With(x => x.Documents, new List<Document>())
            .Create();

        fla.AssigneeHistories.Clear();
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.AdminOfficer
        });
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.Author
        });

        var viewModel = _fixture.Build<EiaWithFormsAbsentViewModel>()
            .With(x => x.HaveTheFormsBeenReceived, false)
            .With(x => x.ApplicationId, fla.Id)
            .Create();

        _updateFellingLicenceApplication.Setup(x => x.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                viewModel.ApplicationId, It.IsAny<EnvironmentalImpactAssessmentAdminOfficerRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getFellingLicenceApplication.Setup(x => x.GetApplicationByIdAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(fla));

        _sendNotifications.Setup(x => x.SendNotificationAsync(
                It.IsAny<object>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateAdminOfficerReviewService.Setup(x => x.AddEnvironmentalImpactAssessmentRequestHistoryAsync(
                It.IsAny<EnvironmentalImpactAssessmentRequestHistoryRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserDbErrorReason>());

        var internalModel = _fixture.Create<UserAccount>();
        _internalUserAccountService.Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(internalModel));

        var externalModel = _fixture.Create<ExternalUserAccount>();
        _externalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalModel);

        var result = await sut.ConfirmEiaFormsHaveBeenReceivedAsync(viewModel, performingUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        _updateFellingLicenceApplication.Verify(x =>
                x.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                    viewModel.ApplicationId,
                    It.IsAny<EnvironmentalImpactAssessmentAdminOfficerRecord>(),
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _sendNotifications.Verify(x =>
                x.SendNotificationAsync(
                    It.Is<EnvironmentalImpactAssessmentReminderDataModel>(y =>
                        y.PropertyName == fla.SubmittedFlaPropertyDetail!.Name &&
                        y.ApplicationReference == fla.ApplicationReference &&
                        y.SenderName == internalModel.FullName(true) &&
                        y.RecipientName == externalModel.FullName(true) &&
                        y.ContactEmail == _eiaOptionsValue.EiaContactEmail &&
                        y.ContactNumber == _eiaOptionsValue.EiaContactPhone &&
                        y.ApplicationFormUri == _eiaOptionsValue.EiaApplicationExternalUri),
                    NotificationType.EiaReminderToSendDocuments,
                    It.IsAny<NotificationRecipient>(),
                    null, null, null,
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _updateAdminOfficerReviewService.Verify(x =>
                x.AddEnvironmentalImpactAssessmentRequestHistoryAsync(
                    It.IsAny<EnvironmentalImpactAssessmentRequestHistoryRecord>(),
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _internalUserAccountService.Verify(x =>
                x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        _externalUserAccountService.Verify(x =>
                x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateAdminOfficerReview
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    ReviewCompleted = false,
                    PerformingUserId = performingUserId
                }, _options)),
            CancellationToken.None), Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AdminOfficerCompleteEiaCheck
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {}, _options)),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ConfirmEiaFormsHaveBeenReceivedAsync_Succeeds_WithoutNotification()
    {
        var sut = CreateSut();
        var performingUserId = Guid.NewGuid();
        var transaction = new Mock<IDbContextTransaction>();
        _updateFellingLicenceApplication.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var viewModel = _fixture.Build<EiaWithFormsAbsentViewModel>()
            .With(x => x.HaveTheFormsBeenReceived, true)
            .Create();

        _updateFellingLicenceApplication.Setup(x => x.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                viewModel.ApplicationId, It.IsAny<EnvironmentalImpactAssessmentAdminOfficerRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.ConfirmEiaFormsHaveBeenReceivedAsync(viewModel, performingUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        _updateFellingLicenceApplication.Verify(x =>
                x.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                    viewModel.ApplicationId,
                    It.IsAny<EnvironmentalImpactAssessmentAdminOfficerRecord>(),
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _sendNotifications.Verify(x =>
                x.SendNotificationAsync(
                    It.IsAny<object>(),
                    It.IsAny<NotificationType>(),
                    It.IsAny<NotificationRecipient>(),
                    null, null, null,
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateAdminOfficerReview
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    ReviewCompleted = false,
                    PerformingUserId = performingUserId
                }, _options)),
            CancellationToken.None), Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AdminOfficerCompleteEiaCheck
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                    { }, _options)),
            CancellationToken.None), Times.Once);
    }


    [Fact]
    public async Task ConfirmEiaFormsHaveBeenReceivedAsync_Fails_WhenApplicantRetrievalFails()
    {
        var sut = CreateSut();
        var performingUserId = Guid.NewGuid();
        var transaction = new Mock<IDbContextTransaction>();
        _updateFellingLicenceApplication.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var fla = _fixture.Build<FellingLicenceApplication>()
            .With(x => x.ApplicationReference, "APP-REF")
            .With(x => x.Documents, new List<Document>())
            .Create();

        // Add required assignee histories
        var applicantId = Guid.NewGuid();
        var adminOfficerId = Guid.NewGuid();
        fla.AssigneeHistories.Clear();
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = adminOfficerId,
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.AdminOfficer
        });
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = applicantId,
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.Author
        });

        var viewModel = _fixture.Build<EiaWithFormsAbsentViewModel>()
            .With(x => x.HaveTheFormsBeenReceived, false)
            .With(x => x.ApplicationId, fla.Id)
            .Create();

        _updateFellingLicenceApplication.Setup(x => x.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                viewModel.ApplicationId, It.IsAny<EnvironmentalImpactAssessmentAdminOfficerRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getFellingLicenceApplication.Setup(x => x.GetApplicationByIdAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(fla));

        // Simulate applicant retrieval failure
        _externalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(applicantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ExternalUserAccount>("fail"));

        var result = await sut.ConfirmEiaFormsHaveBeenReceivedAsync(viewModel, performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to send EIA reminder notification", result.Error);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateAdminOfficerReviewFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Unable to retrieve applicant user account",
                    PerformingUserId = performingUserId
                }, _options)),
            CancellationToken.None), Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AdminOfficerCompleteEiaCheckFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Unable to retrieve applicant user account",
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ConfirmEiaFormsHaveBeenReceivedAsync_Fails_WhenAdminOfficerRetrievalFails()
    {
        var sut = CreateSut();
        var performingUserId = Guid.NewGuid();
        var transaction = new Mock<IDbContextTransaction>();
        _updateFellingLicenceApplication.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var fla = _fixture.Build<FellingLicenceApplication>()
            .With(x => x.ApplicationReference, "APP-REF")
            .With(x => x.Documents, new List<Document>())
            .Create();

        // Add required assignee histories
        var applicantId = Guid.NewGuid();
        var adminOfficerId = Guid.NewGuid();
        fla.AssigneeHistories.Clear();
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = adminOfficerId,
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.AdminOfficer
        });
        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = applicantId,
            TimestampAssigned = _fixture.Create<DateTime>(),
            Role = AssignedUserRole.Author
        });

        var viewModel = _fixture.Build<EiaWithFormsAbsentViewModel>()
            .With(x => x.HaveTheFormsBeenReceived, false)
            .With(x => x.ApplicationId, fla.Id)
            .Create();

        _updateFellingLicenceApplication.Setup(x => x.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                viewModel.ApplicationId, It.IsAny<EnvironmentalImpactAssessmentAdminOfficerRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getFellingLicenceApplication.Setup(x => x.GetApplicationByIdAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(fla));

        _externalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(applicantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<ExternalUserAccount>());

        // Simulate admin officer retrieval failure
        _internalUserAccountService.Setup(x => x.GetUserAccountAsync(adminOfficerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.None);

        var result = await sut.ConfirmEiaFormsHaveBeenReceivedAsync(viewModel, performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to send EIA reminder notification", result.Error);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.UpdateAdminOfficerReviewFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Unable to retrieve admin officer user account",
                    PerformingUserId = performingUserId
                }, _options)),
            CancellationToken.None), Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AdminOfficerCompleteEiaCheckFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Unable to retrieve admin officer user account",
                }, _options)),
            CancellationToken.None), Times.Once);
    }
}