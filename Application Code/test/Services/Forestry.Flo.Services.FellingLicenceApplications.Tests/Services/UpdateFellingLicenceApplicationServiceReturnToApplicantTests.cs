using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateFellingLicenceApplicationServiceReturnToApplicantTests
{
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _internalFlaRepository = new();
    private readonly Mock<IAmendCaseNotes> _mockCaseNotes = new();
    private readonly Mock<IGetConfiguredFcAreas> _mockGetConfiguredFcAreas = new();
    private readonly Mock<IClock> _mockClock = new();

    private static readonly Fixture FixtureInstance = new();

    [Theory, AutoMoqData]
    public async Task WhenCannotCheckApplicantAccess(ReturnToApplicantRequest request)
    {
        var sut = CreateSut();

        _internalFlaRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("error"));

        var result = await sut.ReturnToApplicantAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _internalFlaRepository
            .Verify(x => x.CheckUserCanAccessApplicationAsync(
                request.ApplicationId, request.ApplicantToReturnTo, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.VerifyNoOtherCalls();

        _mockCaseNotes.VerifyNoOtherCalls();
        _mockClock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicantDoesNotHaveAccess(ReturnToApplicantRequest request)
    {
        var sut = CreateSut();

        _internalFlaRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));

        var result = await sut.ReturnToApplicantAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _internalFlaRepository
            .Verify(x => x.CheckUserCanAccessApplicationAsync(
                request.ApplicationId, request.ApplicantToReturnTo, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.VerifyNoOtherCalls();

        _mockCaseNotes.VerifyNoOtherCalls();
        _mockClock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(FellingLicenceStatus.Draft)]
    [InlineData(FellingLicenceStatus.ReturnedToApplicant)]
    [InlineData(FellingLicenceStatus.WithApplicant)]
    [InlineData(FellingLicenceStatus.Withdrawn)]
    public async Task WhenCurrentApplicationStatusIsInvalidToReturnToApplicant(FellingLicenceStatus currentStatus)
    {
        var request = FixtureInstance.Create<ReturnToApplicantRequest>();

        var sut = CreateSut();

        var statuses = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.Today, Status = currentStatus, FellingLicenceApplicationId = request.ApplicationId
            }
        };

        _internalFlaRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _internalFlaRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statuses);

        var result = await sut.ReturnToApplicantAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal($"Application is currently in state {currentStatus} and so cannot be returned to applicant", result.Error);

        _internalFlaRepository
            .Verify(x => x.CheckUserCanAccessApplicationAsync(
                request.ApplicationId, request.ApplicantToReturnTo, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository
            .Verify(x => x.GetStatusHistoryForApplicationAsync(
                request.ApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.VerifyNoOtherCalls();

        _mockCaseNotes.VerifyNoOtherCalls();
        _mockClock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenAddingCaseNoteFails(ReturnToApplicantRequest request, string error)
    {
        var sut = CreateSut();

        var statuses = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.Today, Status = FellingLicenceStatus.Submitted, FellingLicenceApplicationId = request.ApplicationId
            }
        };

        _internalFlaRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _internalFlaRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statuses);
        _internalFlaRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>(0));
        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.ReturnToApplicantAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);

        _internalFlaRepository.Verify(x => x.CheckUserCanAccessApplicationAsync(
            request.ApplicationId, request.ApplicantToReturnTo, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(
            request.ApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.AddStatusHistory(
            request.PerformingUserId, request.ApplicationId, FellingLicenceStatus.ReturnedToApplicant, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(request.ApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.VerifyNoOtherCalls();

        _mockCaseNotes
            .Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(r => r.FellingLicenceApplicationId == request.ApplicationId
                && r.Type == CaseNoteType.ReturnToApplicantComment
                && r.Text == request.CaseNoteContent
                && r.VisibleToApplicant == true
                && r.VisibleToConsultee == false), request.PerformingUserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseNotes.VerifyNoOtherCalls();
        _mockClock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUpdatingApplicationStepsStatusFails(ReturnToApplicantRequest request)
    {
        var sut = CreateSut();

        var statuses = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.Today, Status = FellingLicenceStatus.Submitted, FellingLicenceApplicationId = request.ApplicationId
            }
        };

        _internalFlaRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _internalFlaRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statuses);
        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _internalFlaRepository
            .Setup(x => x.UpdateApplicationStepStatusAsync(It.IsAny<Guid>(), It.IsAny<ApplicationStepStatusRecord>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));
        _internalFlaRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>(0));

        var result = await sut.ReturnToApplicantAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Could not update application sections requiring attention", result.Error);

        _internalFlaRepository.Verify(x => x.CheckUserCanAccessApplicationAsync(
            request.ApplicationId, request.ApplicantToReturnTo, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(
            request.ApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.AddStatusHistory(
            request.PerformingUserId, request.ApplicationId, FellingLicenceStatus.ReturnedToApplicant, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.UpdateApplicationStepStatusAsync(
            request.ApplicationId, request.SectionsRequiringAttention, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(request.ApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.VerifyNoOtherCalls();

        _mockCaseNotes
            .Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(r => r.FellingLicenceApplicationId == request.ApplicationId
                                                                          && r.Type == CaseNoteType.ReturnToApplicantComment
                                                                          && r.Text == request.CaseNoteContent
                                                                          && r.VisibleToApplicant == true
                                                                          && r.VisibleToConsultee == false), request.PerformingUserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseNotes.VerifyNoOtherCalls();
        _mockClock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(FellingLicenceStatus.Submitted, FellingLicenceStatus.ReturnedToApplicant)]
    [InlineData(FellingLicenceStatus.AdminOfficerReview, FellingLicenceStatus.ReturnedToApplicant)]
    [InlineData(FellingLicenceStatus.WoodlandOfficerReview, FellingLicenceStatus.WithApplicant)]
    [InlineData(FellingLicenceStatus.SentForApproval, FellingLicenceStatus.WithApplicant)]
    public async Task AssigningToApplicantSetsCorrectNewStatus(FellingLicenceStatus currentStatus, FellingLicenceStatus expectedNewStatus)
    {
        var sut = CreateSut();

        var request = FixtureInstance.Build<ReturnToApplicantRequest>().With(x => x.PerformingUserIsAccountAdmin, true).Create();

        var statuses = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.Today, Status = currentStatus, FellingLicenceApplicationId = request.ApplicationId
            }
        };

        var now = Instant.FromDateTimeUtc(DateTime.UtcNow);

        _internalFlaRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _internalFlaRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statuses);
        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _internalFlaRepository
            .Setup(x => x.UpdateApplicationStepStatusAsync(It.IsAny<Guid>(), It.IsAny<ApplicationStepStatusRecord>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _internalFlaRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>(0));
        _mockClock
            .Setup(x => x.GetCurrentInstant())
            .Returns(now);

        var result = await sut.ReturnToApplicantAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _internalFlaRepository.Verify(x => x.CheckUserCanAccessApplicationAsync(
            request.ApplicationId, request.ApplicantToReturnTo, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(
            request.ApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.AddStatusHistory(
            request.PerformingUserId, request.ApplicationId, expectedNewStatus, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.UpdateApplicationStepStatusAsync(
            request.ApplicationId, request.SectionsRequiringAttention, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.AssignFellingLicenceApplicationToStaffMemberAsync(
            request.ApplicationId, request.ApplicantToReturnTo.UserAccountId, AssignedUserRole.Applicant, now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(request.ApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        if (expectedNewStatus == FellingLicenceStatus.ReturnedToApplicant)
        {
            _internalFlaRepository.Verify(x => x.RemoveAssignedRolesFromApplicationAsync(
                request.ApplicationId, new[] { AssignedUserRole.AdminOfficer, AssignedUserRole.WoodlandOfficer }, now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        }
        _internalFlaRepository.VerifyNoOtherCalls();

        _mockCaseNotes
            .Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(r => r.FellingLicenceApplicationId == request.ApplicationId
                                                                          && r.Text == request.CaseNoteContent
                                                                          && r.VisibleToApplicant == true
                                                                          && r.VisibleToConsultee == false), request.PerformingUserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseNotes.VerifyNoOtherCalls();

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(FellingLicenceStatus.Submitted, CaseNoteType.ReturnToApplicantComment)]
    [InlineData(FellingLicenceStatus.AdminOfficerReview, CaseNoteType.AdminOfficerReviewComment)]
    [InlineData(FellingLicenceStatus.WoodlandOfficerReview, CaseNoteType.WoodlandOfficerReviewComment)]
    [InlineData(FellingLicenceStatus.SentForApproval, CaseNoteType.ReturnToApplicantComment)]
    public async Task AssigningToApplicantCreatesCaseNoteOfCorrectType(FellingLicenceStatus currentStatus, CaseNoteType expectedType)
    {
        var sut = CreateSut();

        var request = FixtureInstance.Build<ReturnToApplicantRequest>().With(x => x.PerformingUserIsAccountAdmin, true).Create();

        var statuses = new List<StatusHistory>
        {
            new()
            {
                Created = DateTime.Today, 
                Status = currentStatus, 
                FellingLicenceApplicationId = request.ApplicationId
            }
        };

        var now = Instant.FromDateTimeUtc(DateTime.UtcNow);

        _internalFlaRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _internalFlaRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statuses);
        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _internalFlaRepository
            .Setup(x => x.UpdateApplicationStepStatusAsync(It.IsAny<Guid>(), It.IsAny<ApplicationStepStatusRecord>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _internalFlaRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>(0));
        _mockClock
            .Setup(x => x.GetCurrentInstant())
            .Returns(now);

        var result = await sut.ReturnToApplicantAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockCaseNotes
            .Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(r =>
                        r.FellingLicenceApplicationId == request.ApplicationId &&
                        r.Text == request.CaseNoteContent &&
                        r.Type == expectedType &&
                        r.VisibleToApplicant == true &&
                        r.VisibleToConsultee == false),
                    request.PerformingUserId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        _mockCaseNotes.VerifyNoOtherCalls();

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RequestWithNoCaseNoteContentDoesNotCreateCaseNote()
    {
        var sut = CreateSut();

        var request = new ReturnToApplicantRequest(
            Guid.NewGuid(), 
            FixtureInstance.Create<UserAccessModel>(),
            Guid.NewGuid(), 
            true,
            FixtureInstance.Create<ApplicationStepStatusRecord>(), 
            null);

        var statuses = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.Today, Status = FellingLicenceStatus.Submitted, FellingLicenceApplicationId = request.ApplicationId
            }
        };

        var now = Instant.FromDateTimeUtc(DateTime.UtcNow);

        _internalFlaRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _internalFlaRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statuses);
        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _internalFlaRepository
            .Setup(x => x.UpdateApplicationStepStatusAsync(It.IsAny<Guid>(), It.IsAny<ApplicationStepStatusRecord>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _internalFlaRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>(0));
        _mockClock
            .Setup(x => x.GetCurrentInstant())
            .Returns(now);

        var result = await sut.ReturnToApplicantAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _internalFlaRepository.Verify(x => x.CheckUserCanAccessApplicationAsync(
            request.ApplicationId, request.ApplicantToReturnTo, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(
            request.ApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.AddStatusHistory(
            request.PerformingUserId, request.ApplicationId, FellingLicenceStatus.ReturnedToApplicant, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.UpdateApplicationStepStatusAsync(
            request.ApplicationId, request.SectionsRequiringAttention, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.AssignFellingLicenceApplicationToStaffMemberAsync(
            request.ApplicationId, request.ApplicantToReturnTo.UserAccountId, AssignedUserRole.Applicant, now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(request.ApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.RemoveAssignedRolesFromApplicationAsync(
            request.ApplicationId, new[] { AssignedUserRole.AdminOfficer, AssignedUserRole.WoodlandOfficer }, now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.VerifyNoOtherCalls();

        _mockCaseNotes.VerifyNoOtherCalls();

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(FellingLicenceStatus.Submitted, null, false, true)]
    [InlineData(FellingLicenceStatus.Submitted, null, true, true)]
    [InlineData(FellingLicenceStatus.Submitted, AssignedUserRole.AdminOfficer, false, true)]
    [InlineData(FellingLicenceStatus.Submitted, AssignedUserRole.WoodlandOfficer, false, true)]
    [InlineData(FellingLicenceStatus.Submitted, AssignedUserRole.FieldManager, false, true)]
    [InlineData(FellingLicenceStatus.AdminOfficerReview, null, false, false)]
    [InlineData(FellingLicenceStatus.AdminOfficerReview, null, true, true)]
    [InlineData(FellingLicenceStatus.AdminOfficerReview, AssignedUserRole.AdminOfficer, false, true)]
    [InlineData(FellingLicenceStatus.AdminOfficerReview, AssignedUserRole.WoodlandOfficer, false, false)]
    [InlineData(FellingLicenceStatus.AdminOfficerReview, AssignedUserRole.FieldManager, false, false)]
    [InlineData(FellingLicenceStatus.WoodlandOfficerReview, null, false, false)]
    [InlineData(FellingLicenceStatus.WoodlandOfficerReview, null, true, true)]
    [InlineData(FellingLicenceStatus.WoodlandOfficerReview, AssignedUserRole.AdminOfficer, false, true)]
    [InlineData(FellingLicenceStatus.WoodlandOfficerReview, AssignedUserRole.WoodlandOfficer, false, true)]
    [InlineData(FellingLicenceStatus.WoodlandOfficerReview, AssignedUserRole.FieldManager, false, false)]
    [InlineData(FellingLicenceStatus.SentForApproval, null, false, false)]
    [InlineData(FellingLicenceStatus.SentForApproval, null, true, true)]
    [InlineData(FellingLicenceStatus.SentForApproval, AssignedUserRole.AdminOfficer, false, true)]
    [InlineData(FellingLicenceStatus.SentForApproval, AssignedUserRole.WoodlandOfficer, false, true)]
    [InlineData(FellingLicenceStatus.SentForApproval, AssignedUserRole.FieldManager, false, true)]
    public async Task VerifiesApplicationStatusAndUserAssignedRoleAllowsReturnToApplicant(
        FellingLicenceStatus currentStatus,
        AssignedUserRole? currentUserRole, 
        bool isAccountAdmin,
        bool expectedResult)
    {
        var sut = CreateSut();

        var request = new ReturnToApplicantRequest(
            Guid.NewGuid(),
            FixtureInstance.Create<UserAccessModel>(),
            Guid.NewGuid(),
            isAccountAdmin,
            FixtureInstance.Create<ApplicationStepStatusRecord>(),
            null);

        var statuses = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.Today, Status = currentStatus, FellingLicenceApplicationId = request.ApplicationId
            }
        };

        var assignedUser = new AssigneeHistory
        {
            Role = currentUserRole.HasValue ? currentUserRole.Value : AssignedUserRole.AdminOfficer,
            TimestampAssigned = DateTime.Today,
            TimestampUnassigned = null,
            AssignedUserId = currentUserRole.HasValue ? request.PerformingUserId : Guid.NewGuid()
        };

        var now = Instant.FromDateTimeUtc(DateTime.UtcNow);

        _internalFlaRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _internalFlaRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statuses);
        _internalFlaRepository
            .Setup(x => x.UpdateApplicationStepStatusAsync(It.IsAny<Guid>(), It.IsAny<ApplicationStepStatusRecord>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _internalFlaRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { assignedUser });
        _mockClock
            .Setup(x => x.GetCurrentInstant())
            .Returns(now);

        var result = await sut.ReturnToApplicantAsync(request, CancellationToken.None);

        Assert.Equal(expectedResult, result.IsSuccess);

        _internalFlaRepository.Verify(x => x.CheckUserCanAccessApplicationAsync(
            request.ApplicationId, request.ApplicantToReturnTo, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(
            request.ApplicationId, It.IsAny<CancellationToken>()), Times.Once);

        if (expectedResult)
        {
            var expectedState =
                currentStatus is FellingLicenceStatus.Submitted or FellingLicenceStatus.AdminOfficerReview
                    ? FellingLicenceStatus.ReturnedToApplicant
                    : FellingLicenceStatus.WithApplicant;

            _internalFlaRepository.Verify(x => x.AddStatusHistory(
                request.PerformingUserId, request.ApplicationId, expectedState, It.IsAny<CancellationToken>()), Times.Once);
            _internalFlaRepository.Verify(x => x.UpdateApplicationStepStatusAsync(
                request.ApplicationId, request.SectionsRequiringAttention, It.IsAny<CancellationToken>()), Times.Once);
            _internalFlaRepository.Verify(x => x.AssignFellingLicenceApplicationToStaffMemberAsync(
                request.ApplicationId, request.ApplicantToReturnTo.UserAccountId, AssignedUserRole.Applicant, now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
            if (currentStatus is FellingLicenceStatus.Submitted or FellingLicenceStatus.AdminOfficerReview)
            {
                _internalFlaRepository.Verify(x => x.RemoveAssignedRolesFromApplicationAsync(
                    request.ApplicationId, new[] { AssignedUserRole.AdminOfficer, AssignedUserRole.WoodlandOfficer }, now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
            }
            _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        }
        _internalFlaRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(request.ApplicationId, It.IsAny<CancellationToken>()), Times.Once);


        _internalFlaRepository.VerifyNoOtherCalls();

        _mockCaseNotes.VerifyNoOtherCalls();

        _mockClock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ReturningApplicationWithAssignedUsersReturnsTheStaffMemberIdsToNotify(List<AssigneeHistory> assigneeHistories)
    {
        foreach (var assigneeHistory in assigneeHistories)
        {
            TestUtils.SetProtectedProperty(assigneeHistory, nameof(assigneeHistory.Id), Guid.NewGuid());
            if (assigneeHistory.Role is AssignedUserRole.Applicant or AssignedUserRole.Author)
            {
                assigneeHistory.Role = AssignedUserRole.AdminOfficer;
            }

            assigneeHistory.TimestampUnassigned = null;
        }

        var sut = CreateSut();

        var request = new ReturnToApplicantRequest(
            Guid.NewGuid(),
            FixtureInstance.Create<UserAccessModel>(),
            Guid.NewGuid(),
            true,
            FixtureInstance.Create<ApplicationStepStatusRecord>(),
            null);

        var statuses = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.Today, Status = FellingLicenceStatus.Submitted, FellingLicenceApplicationId = request.ApplicationId
            }
        };

        var now = Instant.FromDateTimeUtc(DateTime.UtcNow);

        _internalFlaRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _internalFlaRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statuses);
        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _internalFlaRepository
            .Setup(x => x.UpdateApplicationStepStatusAsync(It.IsAny<Guid>(), It.IsAny<ApplicationStepStatusRecord>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _internalFlaRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assigneeHistories);
        _mockClock
            .Setup(x => x.GetCurrentInstant())
            .Returns(now);

        var result = await sut.ReturnToApplicantAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(assigneeHistories.Select(x => x.AssignedUserId).ToList(), result.Value);

        _internalFlaRepository.Verify(x => x.CheckUserCanAccessApplicationAsync(
            request.ApplicationId, request.ApplicantToReturnTo, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(
            request.ApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.AddStatusHistory(
            request.PerformingUserId, request.ApplicationId, FellingLicenceStatus.ReturnedToApplicant, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.UpdateApplicationStepStatusAsync(
            request.ApplicationId, request.SectionsRequiringAttention, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.AssignFellingLicenceApplicationToStaffMemberAsync(
            request.ApplicationId, request.ApplicantToReturnTo.UserAccountId, AssignedUserRole.Applicant, now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(request.ApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.Verify(x => x.RemoveAssignedRolesFromApplicationAsync(
            request.ApplicationId, new[] { AssignedUserRole.AdminOfficer, AssignedUserRole.WoodlandOfficer }, now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        _internalFlaRepository.VerifyNoOtherCalls();

        _mockCaseNotes.VerifyNoOtherCalls();

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task AddsDecisionPublicRegisterDetails_WhenValidApplicationWithCorrectDetailsWasApproved(
        FellingLicenceApplication fellingLicenceApplication)
    {
        foreach (var assigneeHistory in fellingLicenceApplication.AssigneeHistories)
        {
            TestUtils.SetProtectedProperty(assigneeHistory, nameof(assigneeHistory.Id), Guid.NewGuid());
            if (assigneeHistory.Role is AssignedUserRole.Applicant or AssignedUserRole.Author)
            {
                assigneeHistory.Role = AssignedUserRole.FieldManager;
            }

            assigneeHistory.TimestampUnassigned = null;
        }

        var sut = CreateSut();

        var statuses = new List<StatusHistory>
        {
            new()
            {
                Created = DateTime.Today.AddHours(12),
                Status = FellingLicenceStatus.Approved,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today, 
                Status = FellingLicenceStatus.SentForApproval, 
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-1),
                Status = FellingLicenceStatus.WoodlandOfficerReview,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-2),
                Status = FellingLicenceStatus.AdminOfficerReview,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-3),
                Status = FellingLicenceStatus.Submitted,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-4),
                Status = FellingLicenceStatus.Draft,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            }
        };

        var publishedDate = DateTime.UtcNow;
        var expiryDate = publishedDate.AddDays(28);

        _internalFlaRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statuses);

        _internalFlaRepository
            .Setup(x => x.AddDecisionPublicRegisterDetailsAsync(It.IsAny<Guid>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.AddDecisionPublicRegisterDetailsAsync(
            fellingLicenceApplication.Id, 
            publishedDate, 
            expiryDate, 
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        _internalFlaRepository
            .Verify(x=>x.GetStatusHistoryForApplicationAsync(
                fellingLicenceApplication.Id, 
                It.IsAny<CancellationToken>()), 
                Times.Once);

        _internalFlaRepository
            .Verify(x => x.AddDecisionPublicRegisterDetailsAsync(
                    fellingLicenceApplication.Id,
                    publishedDate,
                    expiryDate,
                    It.IsAny<CancellationToken>()),
                Times.Once);

        _internalFlaRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task AddsDecisionPublicRegisterDetails_WhenValidApplicationWithCorrectDetailsWasRefused(
      FellingLicenceApplication fellingLicenceApplication)
    {
        foreach (var assigneeHistory in fellingLicenceApplication.AssigneeHistories)
        {
            TestUtils.SetProtectedProperty(assigneeHistory, nameof(assigneeHistory.Id), Guid.NewGuid());
            if (assigneeHistory.Role is AssignedUserRole.Applicant or AssignedUserRole.Author)
            {
                assigneeHistory.Role = AssignedUserRole.FieldManager;
            }

            assigneeHistory.TimestampUnassigned = null;
        }

        var sut = CreateSut();

        var statuses = new List<StatusHistory>
        {
            new()
            {
                Created = DateTime.Today.AddHours(12),
                Status = FellingLicenceStatus.Refused,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today,
                Status = FellingLicenceStatus.SentForApproval,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-1),
                Status = FellingLicenceStatus.WoodlandOfficerReview,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-2),
                Status = FellingLicenceStatus.AdminOfficerReview,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-3),
                Status = FellingLicenceStatus.Submitted,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-4),
                Status = FellingLicenceStatus.Draft,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            }
        };

        var publishedDate = DateTime.UtcNow;
        var expiryDate = publishedDate.AddDays(28);

        _internalFlaRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statuses);

        _internalFlaRepository
            .Setup(x => x.AddDecisionPublicRegisterDetailsAsync(It.IsAny<Guid>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.AddDecisionPublicRegisterDetailsAsync(
            fellingLicenceApplication.Id,
            publishedDate,
            expiryDate,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        _internalFlaRepository
            .Verify(x => x.GetStatusHistoryForApplicationAsync(
                fellingLicenceApplication.Id,
                It.IsAny<CancellationToken>()),
                Times.Once);

        _internalFlaRepository
            .Verify(x => x.AddDecisionPublicRegisterDetailsAsync(
                    fellingLicenceApplication.Id,
                    publishedDate,
                    expiryDate,
                    It.IsAny<CancellationToken>()),
                Times.Once);

        _internalFlaRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task AddsDecisionPublicRegisterDetails_WhenValidApplicationWithCorrectDetailsWasReferredToLocalAuthority(
    FellingLicenceApplication fellingLicenceApplication)
    {
        foreach (var assigneeHistory in fellingLicenceApplication.AssigneeHistories)
        {
            TestUtils.SetProtectedProperty(assigneeHistory, nameof(assigneeHistory.Id), Guid.NewGuid());
            if (assigneeHistory.Role is AssignedUserRole.Applicant or AssignedUserRole.Author)
            {
                assigneeHistory.Role = AssignedUserRole.FieldManager;
            }

            assigneeHistory.TimestampUnassigned = null;
        }

        var sut = CreateSut();

        var statuses = new List<StatusHistory>
        {
            new()
            {
                Created = DateTime.Today.AddHours(12),
                Status = FellingLicenceStatus.ReferredToLocalAuthority,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today,
                Status = FellingLicenceStatus.SentForApproval,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-1),
                Status = FellingLicenceStatus.WoodlandOfficerReview,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-2),
                Status = FellingLicenceStatus.AdminOfficerReview,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-3),
                Status = FellingLicenceStatus.Submitted,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-4),
                Status = FellingLicenceStatus.Draft,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            }
        };

        var publishedDate = DateTime.UtcNow;
        var expiryDate = publishedDate.AddDays(28);

        _internalFlaRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statuses);

        _internalFlaRepository
            .Setup(x => x.AddDecisionPublicRegisterDetailsAsync(It.IsAny<Guid>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.AddDecisionPublicRegisterDetailsAsync(
            fellingLicenceApplication.Id,
            publishedDate,
            expiryDate,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        _internalFlaRepository
            .Verify(x => x.GetStatusHistoryForApplicationAsync(
                fellingLicenceApplication.Id,
                It.IsAny<CancellationToken>()),
                Times.Once);

        _internalFlaRepository
            .Verify(x => x.AddDecisionPublicRegisterDetailsAsync(
                    fellingLicenceApplication.Id,
                    publishedDate,
                    expiryDate,
                    It.IsAny<CancellationToken>()),
                Times.Once);

        _internalFlaRepository.VerifyNoOtherCalls();
    }


    [Theory, AutoMoqData]
    public async Task AddsDecisionPublicRegisterDetails_DoesNotAddIfRequiredStatusIsNotTheCurrentStatus(
       FellingLicenceApplication fellingLicenceApplication)
    {
        foreach (var assigneeHistory in fellingLicenceApplication.AssigneeHistories)
        {
            TestUtils.SetProtectedProperty(assigneeHistory, nameof(assigneeHistory.Id), Guid.NewGuid());
            if (assigneeHistory.Role is AssignedUserRole.Applicant or AssignedUserRole.Author)
            {
                assigneeHistory.Role = AssignedUserRole.FieldManager;
            }

            assigneeHistory.TimestampUnassigned = null;
        }

        var sut = CreateSut();

        var statuses = new List<StatusHistory>
        {
            new()
            {
                // technically cannot go to this status from SentFromApproval, but satisfies the test.
                Created = DateTime.Today,
                Status = FellingLicenceStatus.WoodlandOfficerReview,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-1),
                Status = FellingLicenceStatus.SentForApproval,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-2),
                Status = FellingLicenceStatus.WoodlandOfficerReview,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-3),
                Status = FellingLicenceStatus.AdminOfficerReview,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-4),
                Status = FellingLicenceStatus.Submitted,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-5),
                Status = FellingLicenceStatus.Draft,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            }
        };

        var publishedDate = DateTime.UtcNow;
        var expiryDate = publishedDate.AddDays(28);

        _internalFlaRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statuses);

        var result = await sut.AddDecisionPublicRegisterDetailsAsync(
            fellingLicenceApplication.Id,
            publishedDate,
            expiryDate,
            CancellationToken.None);

        Assert.False(result.IsSuccess);

        _internalFlaRepository
            .Verify(x => x.GetStatusHistoryForApplicationAsync(
                fellingLicenceApplication.Id,
                It.IsAny<CancellationToken>()),
                Times.Once);

        _internalFlaRepository
            .Verify(x => x.AddDecisionPublicRegisterDetailsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

        _internalFlaRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task AddsDecisionPublicRegisterDetails_WhenRepositoryAddCallFails(
      FellingLicenceApplication fellingLicenceApplication)
    {
        foreach (var assigneeHistory in fellingLicenceApplication.AssigneeHistories)
        {
            TestUtils.SetProtectedProperty(assigneeHistory, nameof(assigneeHistory.Id), Guid.NewGuid());
            if (assigneeHistory.Role is AssignedUserRole.Applicant or AssignedUserRole.Author)
            {
                assigneeHistory.Role = AssignedUserRole.FieldManager;
            }

            assigneeHistory.TimestampUnassigned = null;
        }

        var sut = CreateSut();

        var statuses = new List<StatusHistory>
        {
            new()
            {
                Created = DateTime.Today,
                Status = FellingLicenceStatus.Approved,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today,
                Status = FellingLicenceStatus.SentForApproval,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-1),
                Status = FellingLicenceStatus.WoodlandOfficerReview,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-2),
                Status = FellingLicenceStatus.AdminOfficerReview,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-3),
                Status = FellingLicenceStatus.Submitted,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-4),
                Status = FellingLicenceStatus.Draft,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            }
        };

        var publishedDate = DateTime.UtcNow;
        var expiryDate = publishedDate.AddDays(28);

        _internalFlaRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statuses);

        _internalFlaRepository
            .Setup(x => x.AddDecisionPublicRegisterDetailsAsync(It.IsAny<Guid>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.NotFound));

        var result = await sut.AddDecisionPublicRegisterDetailsAsync(
            fellingLicenceApplication.Id,
            publishedDate,
            expiryDate,
            CancellationToken.None);

        Assert.False(result.IsSuccess);

        _internalFlaRepository
            .Verify(x => x.GetStatusHistoryForApplicationAsync(
                fellingLicenceApplication.Id,
                It.IsAny<CancellationToken>()),
                Times.Once);

        _internalFlaRepository
            .Verify(x => x.AddDecisionPublicRegisterDetailsAsync(
                    fellingLicenceApplication.Id,
                    publishedDate,
                    expiryDate,
                    It.IsAny<CancellationToken>()),
                Times.Once);

        _internalFlaRepository.VerifyNoOtherCalls();
    }


    [Theory]
    [InlineData(FellingLicenceStatus.Draft)]
    [InlineData(FellingLicenceStatus.AdminOfficerReview)]
    [InlineData(FellingLicenceStatus.WoodlandOfficerReview)]
    [InlineData(FellingLicenceStatus.ReturnedToApplicant)]
    [InlineData(FellingLicenceStatus.Submitted)]
    [InlineData(FellingLicenceStatus.Received)]
    [InlineData(FellingLicenceStatus.WithApplicant)]
    [InlineData(FellingLicenceStatus.Withdrawn)]
    public async Task Cannot_AddDecisionPublicRegisterDetails_WhenInvalidCurrentStatus(FellingLicenceStatus invalidStatus)
    {
        FixtureInstance.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => FixtureInstance.Behaviors.Remove(b));
        FixtureInstance.Behaviors.Add(new OmitOnRecursionBehavior());

        var fellingLicenceApplication = FixtureInstance.Create<FellingLicenceApplication>();
        foreach (var assigneeHistory in fellingLicenceApplication.AssigneeHistories)
        {
            TestUtils.SetProtectedProperty(assigneeHistory, nameof(assigneeHistory.Id), Guid.NewGuid());
            if (assigneeHistory.Role is AssignedUserRole.Applicant or AssignedUserRole.Author)
            {
                assigneeHistory.Role = AssignedUserRole.FieldManager;
            }

            assigneeHistory.TimestampUnassigned = null;
        }

        var sut = CreateSut();

        var statuses = new List<StatusHistory>
        {
            new()
            {
                Created = DateTime.Today,
                Status = invalidStatus,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-1),
                Status = FellingLicenceStatus.WoodlandOfficerReview,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-2),
                Status = FellingLicenceStatus.AdminOfficerReview,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-3),
                Status = FellingLicenceStatus.Submitted,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            },
            new()
            {
                Created = DateTime.Today.AddDays(-4),
                Status = FellingLicenceStatus.Draft,
                FellingLicenceApplicationId = fellingLicenceApplication.Id
            }
        };

        var publishedDate = DateTime.UtcNow;
        var expiryDate = publishedDate.AddDays(28);

        _internalFlaRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statuses);
        
        var result = await sut.AddDecisionPublicRegisterDetailsAsync(
            fellingLicenceApplication.Id,
            publishedDate,
            expiryDate,
            CancellationToken.None);

        Assert.False(result.IsSuccess);

        _internalFlaRepository
            .Verify(x => x.GetStatusHistoryForApplicationAsync(
                fellingLicenceApplication.Id,
                It.IsAny<CancellationToken>()),
                Times.Once);

        _internalFlaRepository
            .Verify(x => x.AddDecisionPublicRegisterDetailsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

        _internalFlaRepository.VerifyNoOtherCalls();
    }



    private UpdateFellingLicenceApplicationService CreateSut()
    {
        _internalFlaRepository.Reset();
        _mockCaseNotes.Reset();
        _mockClock.Reset();

        return new UpdateFellingLicenceApplicationService(
            _internalFlaRepository.Object,
            _mockCaseNotes.Object,
            _mockGetConfiguredFcAreas.Object,
            _mockClock.Object,
            new NullLogger<UpdateFellingLicenceApplicationService>(),
            new Mock<IOptions<FellingLicenceApplicationOptions>>().Object);
    }
}