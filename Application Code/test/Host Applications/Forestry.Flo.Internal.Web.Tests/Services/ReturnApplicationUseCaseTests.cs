using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;
using System.Security.Claims;
using AutoFixture.Xunit2;
using Forestry.Flo.Services.FellingLicenceApplications;

namespace Forestry.Flo.Internal.Web.Tests.Services;
public class ReturnApplicationUseCaseTests
{
    private readonly Mock<ILogger<ApproveRefuseOrReferApplicationUseCase>> _logger = new();
    private readonly Mock<IGetFellingLicenceApplicationForInternalUsers> _getFellingLicenceService = new();
    private readonly Mock<IUpdateFellingLicenceApplication> _updateFellingLicenceService = new();
    private readonly Mock<IAuditService<ApproveRefuseOrReferApplicationUseCase>> _auditService = new();
    private readonly Mock<IApproverReviewService> _approverReviewService = new();
    private readonly RequestContext _requestContext = new("requestId", new RequestUserModel(new ClaimsPrincipal()));
    private readonly InternalUser _user = new(new System.Security.Claims.ClaimsPrincipal());
    private readonly Mock<IAmendCaseNotes> _amendCaseNotes = new();
    private readonly Guid _applicationId = Guid.NewGuid();

    private ReturnApplicationUseCase CreateSut()
    {
        _logger.Reset();
        _getFellingLicenceService.Reset();
        _updateFellingLicenceService.Reset();
        _auditService.Reset();
        _approverReviewService.Reset();
        _amendCaseNotes.Reset();

        return new(
            _logger.Object,
            _getFellingLicenceService.Object,
            _updateFellingLicenceService.Object,
            _auditService.Object,
            _approverReviewService.Object,
            _amendCaseNotes.Object,
            _requestContext);
    }

    [Theory, AutoData]
    public async Task ReturnApplication_ShouldReturnFailure_WhenApplicationNotFound(FormLevelCaseNote caseNote)
    {
        var sut = CreateSut();

        _getFellingLicenceService.Setup(x => x.GetApplicationByIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>("Not found"));

        var result = await sut.ReturnApplication(_user, _applicationId, caseNote, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotRetrieveApplication);
    }

    [Theory, AutoData]
    public async Task ReturnApplication_ShouldReturnFailure_WhenNoPreviousStatus(FormLevelCaseNote caseNote)
    {
        var sut = CreateSut();

        var application = new FellingLicenceApplication
        {
            StatusHistories = new List<StatusHistory>() // Simulate no previous status
        };
        _getFellingLicenceService.Setup(x => x.GetApplicationByIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        var result = await sut.ReturnApplication(_user, _applicationId, caseNote, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.IncorrectFellingApplicationState);
    }

    [Theory, AutoData]
    public async Task ReturnApplication_ShouldReturnFailure_WhenPreviousStatusIsNotWOorAO(FormLevelCaseNote caseNote)
    {
        var sut = CreateSut();

        var application = new FellingLicenceApplication
        {
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Submitted }
            }
        };
        _getFellingLicenceService.Setup(x => x.GetApplicationByIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        var result = await sut.ReturnApplication(_user, _applicationId, caseNote, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.IncorrectFellingApplicationState);
    }

    [Theory, AutoData]
    public async Task ReturnApplication_ShouldReturnFailure_WhenNotSentForApproval(FormLevelCaseNote caseNote)
    {
        var sut = CreateSut();

        var application = new FellingLicenceApplication
        {
            StatusHistories = new List<StatusHistory>
            {
                // Simulate previous status is WO review, but not sent for approval
                new StatusHistory { Status = FellingLicenceStatus.WoodlandOfficerReview, Created = DateTime.UtcNow },
                new StatusHistory { Status = FellingLicenceStatus.Submitted, Created = DateTime.UtcNow.AddMinutes(-1) }
            }
        };
        _getFellingLicenceService.Setup(x => x.GetApplicationByIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        var result = await sut.ReturnApplication(_user, _applicationId, caseNote, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.IncorrectFellingApplicationState);
    }

    [Theory, AutoData]
    public async Task ReturnApplication_ShouldReturnFailure_WhenUserNotFieldManager(FormLevelCaseNote caseNote)
    {
        var sut = CreateSut();

        var application = new FellingLicenceApplication
        {
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.SentForApproval, Created = DateTime.UtcNow },
                new StatusHistory { Status = FellingLicenceStatus.WoodlandOfficerReview, Created = DateTime.UtcNow.AddMinutes(-1) }
            },
            AssigneeHistories = new List<AssigneeHistory>() // No FieldManager assigned
        };
        _getFellingLicenceService.Setup(x => x.GetApplicationByIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        var result = await sut.ReturnApplication(_user, _applicationId, caseNote, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.UserRoleNotAuthorised);
    }

    [Theory, AutoData]
    public async Task ReturnApplication_ShouldSucceed_AndAudit_WhenWOReview(FormLevelCaseNote caseNote)
    {
        var sut = CreateSut();

        var woodlandOfficerId = Guid.NewGuid();
        var user = new InternalUser(new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(FloClaimTypes.LocalAccountId, Guid.NewGuid().ToString())
        })));
        var application = new FellingLicenceApplication
        {
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.WoodlandOfficerReview, Created = DateTime.UtcNow.AddMinutes(-1) },
                new StatusHistory { Status = FellingLicenceStatus.SentForApproval, Created = DateTime.UtcNow }
            },
            AssigneeHistories = new List<AssigneeHistory>
            {
                new AssigneeHistory { AssignedUserId = woodlandOfficerId, Role = AssignedUserRole.WoodlandOfficer },
                new AssigneeHistory { AssignedUserId = user.UserAccountId.Value, Role = AssignedUserRole.FieldManager }
            },
            WoodlandOwnerId = Guid.NewGuid()
        };

        _getFellingLicenceService.Setup(x => x.GetApplicationByIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));
        _updateFellingLicenceService.Setup(x => x.AddStatusHistoryAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), FellingLicenceStatus.WoodlandOfficerReview, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _approverReviewService.Setup(x => x.DeleteApproverReviewAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _auditService.Setup(x => x.PublishAuditEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _amendCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        var result = await sut.ReturnApplication(user, _applicationId, caseNote, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _auditService.Verify(x => x.PublishAuditEventAsync(
            It.Is<AuditEvent>(e => e.EventName == AuditEvents.RevertApproveToWoodlandOfficerReview),
            It.IsAny<CancellationToken>()), Times.Once);

        _amendCaseNotes.Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(c => 
            c.Type == CaseNoteType.ApproverReviewComment
            && c.Text == caseNote.CaseNote
            && c.VisibleToApplicant == caseNote.VisibleToApplicant
            && c.VisibleToConsultee == caseNote.VisibleToConsultee), user.UserAccountId.Value, It.IsAny<CancellationToken>()), Times.Once);
        _amendCaseNotes.VerifyNoOtherCalls();
    }



    [Theory, AutoData]
    public async Task ReturnApplication_ShouldSucceed_AndAudit_WhenAOReview(FormLevelCaseNote caseNote)
    {
        var sut = CreateSut();

        var fieldManagerId = Guid.NewGuid();
        var user = new InternalUser(new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(FloClaimTypes.LocalAccountId, Guid.NewGuid().ToString())
        })));
        var application = new FellingLicenceApplication
        {
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.SentForApproval, Created = DateTime.UtcNow },
                new StatusHistory { Status = FellingLicenceStatus.AdminOfficerReview, Created = DateTime.UtcNow.AddMinutes(-1) }
            },
            AssigneeHistories = new List<AssigneeHistory>
            {
                new AssigneeHistory { AssignedUserId = user.UserAccountId.Value, Role = AssignedUserRole.FieldManager }
            },
            WoodlandOwnerId = Guid.NewGuid()
        };

        _getFellingLicenceService.Setup(x => x.GetApplicationByIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));
        _updateFellingLicenceService.Setup(x => x.AddStatusHistoryAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), FellingLicenceStatus.AdminOfficerReview, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _approverReviewService.Setup(x => x.DeleteApproverReviewAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _auditService.Setup(x => x.PublishAuditEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _amendCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        var result = await sut.ReturnApplication(user, _applicationId, caseNote, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _auditService.Verify(x => x.PublishAuditEventAsync(
            It.Is<AuditEvent>(e => e.EventName == AuditEvents.RevertApproveToAdminOfficerReview),
            It.IsAny<CancellationToken>()), Times.Once);

        _amendCaseNotes.Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(c =>
            c.Type == CaseNoteType.ApproverReviewComment
            && c.Text == caseNote.CaseNote
            && c.VisibleToApplicant == caseNote.VisibleToApplicant
            && c.VisibleToConsultee == caseNote.VisibleToConsultee), user.UserAccountId.Value, It.IsAny<CancellationToken>()), Times.Once);
        _amendCaseNotes.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnApplication_ShouldSucceed_WithError_WhenCaseNoteFails(FormLevelCaseNote caseNote)
    {
        var sut = CreateSut();

        var fieldManagerId = Guid.NewGuid();
        var user = new InternalUser(new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(FloClaimTypes.LocalAccountId, Guid.NewGuid().ToString())
        })));
        var application = new FellingLicenceApplication
        {
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.SentForApproval, Created = DateTime.UtcNow },
                new StatusHistory { Status = FellingLicenceStatus.AdminOfficerReview, Created = DateTime.UtcNow.AddMinutes(-1) }
            },
            AssigneeHistories = new List<AssigneeHistory>
            {
                new AssigneeHistory { AssignedUserId = user.UserAccountId.Value, Role = AssignedUserRole.FieldManager }
            },
            WoodlandOwnerId = Guid.NewGuid()
        };

        _getFellingLicenceService.Setup(x => x.GetApplicationByIdAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));
        _updateFellingLicenceService.Setup(x => x.AddStatusHistoryAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), FellingLicenceStatus.AdminOfficerReview, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _approverReviewService.Setup(x => x.DeleteApproverReviewAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _auditService.Setup(x => x.PublishAuditEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _amendCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("error"));

        var result = await sut.ReturnApplication(user, _applicationId, caseNote, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotStoreCaseNote);

        _auditService.Verify(x => x.PublishAuditEventAsync(
            It.Is<AuditEvent>(e => e.EventName == AuditEvents.RevertApproveToAdminOfficerReview),
            It.IsAny<CancellationToken>()), Times.Once);

        _amendCaseNotes.Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(c =>
            c.Type == CaseNoteType.ApproverReviewComment
            && c.Text == caseNote.CaseNote
            && c.VisibleToApplicant == caseNote.VisibleToApplicant
            && c.VisibleToConsultee == caseNote.VisibleToConsultee), user.UserAccountId.Value, It.IsAny<CancellationToken>()), Times.Once);
        _amendCaseNotes.VerifyNoOtherCalls();
    }

}
