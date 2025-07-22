using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
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

public class UpdateFellingLicenceApplicationServiceAssignToInternalUserTests
{
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _internalFlaRepository = new();
    private readonly Mock<IAmendCaseNotes> _mockCaseNotes = new();
    private readonly Mock<IGetConfiguredFcAreas> _mockGetConfiguredFcAreas = new();
    private readonly Mock<IClock> _mockClock = new();

    private static readonly Fixture FixtureInstance = new();

    [Theory, AutoMoqData]
    public async Task WhenCannotRetrieveOriginalReference(AssignToUserRequest request, string error)
    {
        var sut = CreateSut();

        _internalFlaRepository
            .Setup(x => x.GetApplicationReferenceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<string>(error));

        var result = await sut.AssignToInternalUserAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);

        _internalFlaRepository
            .Verify(x => x.GetApplicationReferenceAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                Times.Once);
        _internalFlaRepository.VerifyNoOtherCalls();

        _mockCaseNotes.VerifyNoOtherCalls();

        _mockClock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(AssignedUserRole.AdminOfficer)]
    [InlineData(AssignedUserRole.WoodlandOfficer)]
    public async Task WhenCannotUpdateAreaCode(AssignedUserRole validRoleForAreaCode)
    {
        var reference = FixtureInstance.Create<string>();
        var request = new AssignToUserRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            validRoleForAreaCode,
            FixtureInstance.Create<string>(),
            FixtureInstance.Create<string>());

        var sut = CreateSut();

        var expectedConfiguredArea = FixtureInstance.Build<ConfiguredFcArea>().With(x => x.AreaCostCode, request.FcAreaCostCode).Create();
        var configuredAreas = FixtureInstance.CreateMany<ConfiguredFcArea>().ToList();

        _mockGetConfiguredFcAreas.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredAreas.Append(expectedConfiguredArea).ToList());

        _internalFlaRepository
            .Setup(x => x.GetApplicationReferenceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(reference));
        _internalFlaRepository
            .Setup(x => x.UpdateAreaCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.NotFound));
        if (validRoleForAreaCode == AssignedUserRole.AdminOfficer)
        {
            _internalFlaRepository
                .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StatusHistory>(0));
        }

        var result = await sut.AssignToInternalUserAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _internalFlaRepository
            .Verify(x => x.GetApplicationReferenceAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                Times.Once);
        if (validRoleForAreaCode == AssignedUserRole.AdminOfficer)
        {
            _internalFlaRepository
                .Verify(x => x.GetStatusHistoryForApplicationAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                    Times.Once);
        }
        _internalFlaRepository
            .Verify(x => x.UpdateAreaCodeAsync(request.ApplicationId, request.FcAreaCostCode, expectedConfiguredArea.AdminHubName, It.IsAny<CancellationToken>()),
                Times.Once);
        _internalFlaRepository.VerifyNoOtherCalls();

        _mockCaseNotes.VerifyNoOtherCalls();

        _mockClock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotAddCaseNote(AssignToUserRequest request, string error)
    {
        var reference = FixtureInstance.Create<string>();

        var sut = CreateSut();

        var expectedConfiguredArea = FixtureInstance.Build<ConfiguredFcArea>().With(x => x.AreaCostCode, request.FcAreaCostCode).Create();
        var configuredAreas = FixtureInstance.CreateMany<ConfiguredFcArea>().ToList();

        _mockGetConfiguredFcAreas.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredAreas.Append(expectedConfiguredArea).ToList());

        _internalFlaRepository
            .Setup(x => x.GetApplicationReferenceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(reference));
        if (request.AssignedRole == AssignedUserRole.AdminOfficer)
        {
            _internalFlaRepository
                .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StatusHistory>(0));
        }
        if (request.AssignedRole is AssignedUserRole.AdminOfficer or AssignedUserRole.WoodlandOfficer)
        {
            _internalFlaRepository
                .Setup(x => x.UpdateAreaCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        }

        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.AssignToInternalUserAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);

        _internalFlaRepository
            .Verify(x => x.GetApplicationReferenceAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                Times.Once);
        if (request.AssignedRole == AssignedUserRole.AdminOfficer)
        {
            _internalFlaRepository
                .Verify(x => x.GetStatusHistoryForApplicationAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                    Times.Once);
        }
        if (request.AssignedRole is AssignedUserRole.AdminOfficer or AssignedUserRole.WoodlandOfficer)
        {
            _internalFlaRepository
                .Verify(x => x.UpdateAreaCodeAsync(request.ApplicationId, request.FcAreaCostCode,expectedConfiguredArea.AdminHubName, It.IsAny<CancellationToken>()),
                    Times.Once);
        }

        _internalFlaRepository.VerifyNoOtherCalls();

        _mockCaseNotes
            .Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(r => r.FellingLicenceApplicationId == request.ApplicationId
                && r.Type == CaseNoteType.CaseNote
                && r.Text == request.CaseNoteContent
                && r.VisibleToApplicant == true
                && r.VisibleToConsultee == false), request.PerformingUserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseNotes.VerifyNoOtherCalls();

        _mockClock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotGetUpdatedReference(AssignToUserRequest request, string error)
    {
        var reference = FixtureInstance.Create<string>();
        var referenceCalls = new Queue<Result<string>>(2);
        referenceCalls.Enqueue(Result.Success(reference));
        referenceCalls.Enqueue(Result.Failure<string>(error));

        var expectedConfiguredArea = FixtureInstance.Build<ConfiguredFcArea>().With(x => x.AreaCostCode, request.FcAreaCostCode).Create();
        var configuredAreas = FixtureInstance.CreateMany<ConfiguredFcArea>().ToList();
        var now = Instant.FromDateTimeUtc(DateTime.UtcNow);

        var sut = CreateSut();

        _mockGetConfiguredFcAreas.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredAreas.Append(expectedConfiguredArea).ToList());

        _internalFlaRepository
            .Setup(x => x.GetApplicationReferenceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => referenceCalls.Dequeue());
        if (request.AssignedRole == AssignedUserRole.AdminOfficer)
        {
            _internalFlaRepository
                .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StatusHistory>(0));
        }
        if (request.AssignedRole is AssignedUserRole.AdminOfficer or AssignedUserRole.WoodlandOfficer)
        {
            _internalFlaRepository
                .Setup(x => x.UpdateAreaCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        }

        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(now);

        var result = await sut.AssignToInternalUserAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);

        _internalFlaRepository
            .Verify(x => x.GetApplicationReferenceAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        if (request.AssignedRole == AssignedUserRole.AdminOfficer)
        {
            _internalFlaRepository
                .Verify(x => x.GetStatusHistoryForApplicationAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                    Times.Once);
        }
        if (request.AssignedRole is AssignedUserRole.AdminOfficer or AssignedUserRole.WoodlandOfficer)
        {
            _internalFlaRepository
                .Verify(x => x.UpdateAreaCodeAsync(request.ApplicationId, request.FcAreaCostCode, expectedConfiguredArea.AdminHubName, It.IsAny<CancellationToken>()),
                    Times.Once);
        }
        _internalFlaRepository
            .Verify(x => x.AssignFellingLicenceApplicationToStaffMemberAsync(
                request.ApplicationId, request.AssignToUserId, request.AssignedRole, now.ToDateTimeUtc(), It.IsAny<CancellationToken>()),
                Times.Once);

        _internalFlaRepository.VerifyNoOtherCalls();

        _mockCaseNotes
            .Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(r => r.FellingLicenceApplicationId == request.ApplicationId
                && r.Type == CaseNoteType.CaseNote
                && r.Text == request.CaseNoteContent
                && r.VisibleToApplicant == true
                && r.VisibleToConsultee == false), request.PerformingUserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseNotes.VerifyNoOtherCalls();

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenAssignmentSuccessWithOtherUserUnassigned(AssignToUserRequest request, Guid unassignedUserId)
    {
        var originalReference = FixtureInstance.Create<string>();
        var newReference = FixtureInstance.Create<string>();
        var referenceQueue = new Queue<Result<string>>();
        referenceQueue.Enqueue(Result.Success(originalReference));
        referenceQueue.Enqueue(Result.Success(newReference));

        var expectedConfiguredArea = FixtureInstance.Build<ConfiguredFcArea>().With(x => x.AreaCostCode, request.FcAreaCostCode).Create();
        var configuredAreas = FixtureInstance.CreateMany<ConfiguredFcArea>().ToList();
        var now = Instant.FromDateTimeUtc(DateTime.UtcNow);

        var sut = CreateSut();

        _mockGetConfiguredFcAreas.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredAreas.Append(expectedConfiguredArea).ToList());

        _internalFlaRepository
            .Setup(x => x.GetApplicationReferenceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => referenceQueue.Dequeue());
        if (request.AssignedRole == AssignedUserRole.AdminOfficer)
        {
            _internalFlaRepository
                .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StatusHistory>(0));
        }
        if (request.AssignedRole is AssignedUserRole.AdminOfficer or AssignedUserRole.WoodlandOfficer)
        {
            _internalFlaRepository
                .Setup(x => x.UpdateAreaCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        }

        _internalFlaRepository
            .Setup(x => x.AssignFellingLicenceApplicationToStaffMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<AssignedUserRole>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, Maybe<Guid>.From(unassignedUserId)));

        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(now);

        var result = await sut.AssignToInternalUserAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(originalReference, result.Value.OriginalApplicationReference);
        Assert.Equal(newReference, result.Value.UpdatedApplicationReference);
        Assert.True(result.Value.IdOfUnassignedUser.HasValue);
        Assert.Equal(unassignedUserId, result.Value.IdOfUnassignedUser.Value);
        Assert.False(result.Value.ApplicationAlreadyAssignedToThisUser);

        _internalFlaRepository
            .Verify(x => x.GetApplicationReferenceAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        if (request.AssignedRole == AssignedUserRole.AdminOfficer)
        {
            _internalFlaRepository
                .Verify(x => x.GetStatusHistoryForApplicationAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                    Times.Once);
        }
        if (request.AssignedRole is AssignedUserRole.AdminOfficer or AssignedUserRole.WoodlandOfficer)
        {
            _internalFlaRepository
                .Verify(x => x.UpdateAreaCodeAsync(request.ApplicationId, request.FcAreaCostCode, expectedConfiguredArea.AdminHubName, It.IsAny<CancellationToken>()),
                    Times.Once);
        }
        _internalFlaRepository
            .Verify(x => x.AssignFellingLicenceApplicationToStaffMemberAsync(
                request.ApplicationId, request.AssignToUserId, request.AssignedRole, now.ToDateTimeUtc(), It.IsAny<CancellationToken>()),
                Times.Once);

        _internalFlaRepository.VerifyNoOtherCalls();

        _mockCaseNotes
            .Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(r => r.FellingLicenceApplicationId == request.ApplicationId
                && r.Type == CaseNoteType.CaseNote
                && r.Text == request.CaseNoteContent
                && r.VisibleToApplicant == true
                && r.VisibleToConsultee == false), request.PerformingUserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseNotes.VerifyNoOtherCalls();

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenAssignmentSuccessAlreadyAssignedToSameUser(AssignToUserRequest request)
    {
        var originalReference = FixtureInstance.Create<string>();
        var newReference = FixtureInstance.Create<string>();
        var referenceQueue = new Queue<Result<string>>();
        referenceQueue.Enqueue(Result.Success(originalReference));
        referenceQueue.Enqueue(Result.Success(newReference));

        var expectedConfiguredArea = FixtureInstance.Build<ConfiguredFcArea>().With(x => x.AreaCostCode, request.FcAreaCostCode).Create();
        var configuredAreas = FixtureInstance.CreateMany<ConfiguredFcArea>().ToList();
        var now = Instant.FromDateTimeUtc(DateTime.UtcNow);

        var sut = CreateSut();

        _mockGetConfiguredFcAreas.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredAreas.Append(expectedConfiguredArea).ToList());

        _internalFlaRepository
            .Setup(x => x.GetApplicationReferenceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => referenceQueue.Dequeue());
        if (request.AssignedRole == AssignedUserRole.AdminOfficer)
        {
            _internalFlaRepository
                .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StatusHistory>(0));
        }
        if (request.AssignedRole is AssignedUserRole.AdminOfficer or AssignedUserRole.WoodlandOfficer)
        {
            _internalFlaRepository
                .Setup(x => x.UpdateAreaCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        }

        _internalFlaRepository
            .Setup(x => x.AssignFellingLicenceApplicationToStaffMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<AssignedUserRole>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, Maybe<Guid>.None));

        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(now);

        var result = await sut.AssignToInternalUserAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(originalReference, result.Value.OriginalApplicationReference);
        Assert.Equal(newReference, result.Value.UpdatedApplicationReference);
        Assert.False(result.Value.IdOfUnassignedUser.HasValue);
        Assert.True(result.Value.ApplicationAlreadyAssignedToThisUser);

        _internalFlaRepository
            .Verify(x => x.GetApplicationReferenceAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        if (request.AssignedRole == AssignedUserRole.AdminOfficer)
        {
            _internalFlaRepository
                .Verify(x => x.GetStatusHistoryForApplicationAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                    Times.Once);
        }
        if (request.AssignedRole is AssignedUserRole.AdminOfficer or AssignedUserRole.WoodlandOfficer)
        {
            _internalFlaRepository
                .Verify(x => x.UpdateAreaCodeAsync(request.ApplicationId, request.FcAreaCostCode, expectedConfiguredArea.AdminHubName, It.IsAny<CancellationToken>()),
                    Times.Once);
        }
        _internalFlaRepository
            .Verify(x => x.AssignFellingLicenceApplicationToStaffMemberAsync(
                request.ApplicationId, request.AssignToUserId, request.AssignedRole, now.ToDateTimeUtc(), It.IsAny<CancellationToken>()),
                Times.Once);

        _internalFlaRepository.VerifyNoOtherCalls();

        _mockCaseNotes
            .Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(r => r.FellingLicenceApplicationId == request.ApplicationId
                && r.Type == CaseNoteType.CaseNote
                && r.Text == request.CaseNoteContent
                && r.VisibleToApplicant == true
                && r.VisibleToConsultee == false), request.PerformingUserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseNotes.VerifyNoOtherCalls();

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdatesStateIfAssigningSubmittedApplicationToAdminOfficer()
    {
        var request = new AssignToUserRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            AssignedUserRole.AdminOfficer,
            FixtureInstance.Create<string>(),
            FixtureInstance.Create<string>(),
            true,
            false);

        var expectedConfiguredArea = FixtureInstance.Build<ConfiguredFcArea>().With(x => x.AreaCostCode, request.FcAreaCostCode).Create();
        var configuredAreas = FixtureInstance.CreateMany<ConfiguredFcArea>().ToList();

        var reference = FixtureInstance.Create<string>();

        var now = Instant.FromDateTimeUtc(DateTime.UtcNow);

        var sut = CreateSut();

        _mockGetConfiguredFcAreas.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredAreas.Append(expectedConfiguredArea).ToList());

        _internalFlaRepository
            .Setup(x => x.GetApplicationReferenceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(reference));
        if (request.AssignedRole == AssignedUserRole.AdminOfficer)
        {
            _internalFlaRepository
                .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StatusHistory> {new() {Status = FellingLicenceStatus.Submitted, Created = DateTime.Today.ToUniversalTime()}});
        }
        if (request.AssignedRole is AssignedUserRole.AdminOfficer or AssignedUserRole.WoodlandOfficer)
        {
            _internalFlaRepository
                .Setup(x => x.UpdateAreaCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        }

        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(now);

        var result = await sut.AssignToInternalUserAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _internalFlaRepository
            .Verify(x => x.GetApplicationReferenceAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        _internalFlaRepository
            .Verify(x => x.GetStatusHistoryForApplicationAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                Times.Once);
        _internalFlaRepository
            .Verify(x => x.AddStatusHistory(request.PerformingUserId, request.ApplicationId, FellingLicenceStatus.AdminOfficerReview, It.IsAny<CancellationToken>()),
                Times.Once);
        _internalFlaRepository
            .Verify(x => x.UpdateAreaCodeAsync(request.ApplicationId, request.FcAreaCostCode, expectedConfiguredArea.AdminHubName, It.IsAny<CancellationToken>()),
                Times.Once);
        _internalFlaRepository
            .Verify(x => x.AssignFellingLicenceApplicationToStaffMemberAsync(
                request.ApplicationId, request.AssignToUserId, request.AssignedRole, now.ToDateTimeUtc(), It.IsAny<CancellationToken>()),
                Times.Once);

        _internalFlaRepository.VerifyNoOtherCalls();

        _mockCaseNotes
            .Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(r => r.FellingLicenceApplicationId == request.ApplicationId
                && r.Type == CaseNoteType.CaseNote
                && r.Text == request.CaseNoteContent
                && r.VisibleToApplicant == true
                && r.VisibleToConsultee == false), request.PerformingUserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseNotes.VerifyNoOtherCalls();

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();
    }

    private UpdateFellingLicenceApplicationService CreateSut()
    {
        _internalFlaRepository.Reset();
        _mockCaseNotes.Reset();
        _mockClock.Reset();
        _mockGetConfiguredFcAreas.Reset();

        return new UpdateFellingLicenceApplicationService(
            _internalFlaRepository.Object,
            _mockCaseNotes.Object,
            _mockGetConfiguredFcAreas.Object,
            _mockClock.Object,
            new NullLogger<UpdateFellingLicenceApplicationService>(),
            new Mock<IOptions<FellingLicenceApplicationOptions>>().Object);
    }
}