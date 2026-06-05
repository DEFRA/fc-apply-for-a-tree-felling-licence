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
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.EntityFrameworkCore.Storage;
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
    private readonly Mock<IDbContextTransaction> _mockTransaction = new();
    private readonly Mock<IUnitOfWork> _mockUow = new();

    private static readonly Fixture FixtureInstance = new();

    [Theory, AutoMoqData]
    public async Task WhenCannotRetrieveApplication(AssignToUserRequest request)
    {
        var sut = CreateSut();

        _internalFlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.None);

        var result = await sut.AssignToInternalUserAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _internalFlaRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _internalFlaRepository
            .Verify(x => x.GetAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                Times.Once);
        _internalFlaRepository.VerifyNoOtherCalls();
        _mockUow.VerifyNoOtherCalls();
        _mockTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _mockTransaction.VerifyNoOtherCalls();

        _mockGetConfiguredFcAreas.VerifyNoOtherCalls();

        _mockCaseNotes.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotAddCaseNote(
        AssignToUserRequest request, 
        FellingLicenceApplication application, 
        string error)
    {
        application.ApplicationReference = "018/026/2026/Test";
        var sut = CreateSut();

        var expectedConfiguredArea = FixtureInstance.Build<ConfiguredFcArea>().With(x => x.AreaCostCode, request.FcAreaCostCode).Create();
        var configuredAreas = FixtureInstance.CreateMany<ConfiguredFcArea>().ToList();

        _mockGetConfiguredFcAreas.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredAreas.Append(expectedConfiguredArea).ToList());

        _internalFlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));

        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.AssignToInternalUserAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _internalFlaRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _internalFlaRepository
            .Verify(x => x.GetAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                Times.Once);
        _internalFlaRepository.VerifyNoOtherCalls();
        _mockUow.VerifyNoOtherCalls();
        _mockTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _mockTransaction.VerifyNoOtherCalls();

        _mockGetConfiguredFcAreas.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockGetConfiguredFcAreas.VerifyNoOtherCalls();

        _mockCaseNotes
            .Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(r => r.FellingLicenceApplicationId == request.ApplicationId
                && r.Type == CaseNoteType.CaseNote
                && r.Text == request.CaseNoteContent
                && r.VisibleToApplicant == true
                && r.VisibleToConsultee == false), request.PerformingUserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseNotes.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotSaveChanges(
        AssignToUserRequest request,
        FellingLicenceApplication application,
        UserDbErrorReason error)
    {
        application.ApplicationReference = "018/026/2026/Test";
        var sut = CreateSut();

        var expectedConfiguredArea = FixtureInstance.Build<ConfiguredFcArea>().With(x => x.AreaCostCode, request.FcAreaCostCode).Create();
        var configuredAreas = FixtureInstance.CreateMany<ConfiguredFcArea>().ToList();

        _mockGetConfiguredFcAreas.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredAreas.Append(expectedConfiguredArea).ToList());

        _internalFlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));

        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockUow
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(error));

        var result = await sut.AssignToInternalUserAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _internalFlaRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _internalFlaRepository
            .Verify(x => x.GetAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                Times.Once);
        _mockUow.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()));

        _internalFlaRepository.VerifyNoOtherCalls();
        _mockUow.VerifyNoOtherCalls();

        _mockTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _mockTransaction.VerifyNoOtherCalls();

        _mockGetConfiguredFcAreas.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockGetConfiguredFcAreas.VerifyNoOtherCalls();

        _mockCaseNotes
            .Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(r => r.FellingLicenceApplicationId == request.ApplicationId
                && r.Type == CaseNoteType.CaseNote
                && r.Text == request.CaseNoteContent
                && r.VisibleToApplicant == true
                && r.VisibleToConsultee == false), request.PerformingUserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseNotes.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSuccessfulWithNoExistingAssignmentAndSetsToAoReview(
        FellingLicenceApplication application)
    {
        var request = FixtureInstance.Build<AssignToUserRequest>()
            .With(x => x.AssignedRole, AssignedUserRole.AdminOfficer)
            .Create();

        application.ApplicationReference = "018/026/2026/Test";
        application.StatusHistories = 
        [
            new StatusHistory
            {
                CreatedById = Guid.NewGuid(),
                Created = DateTime.Today,
                Status = FellingLicenceStatus.Submitted
            }
        ];
        application.AssigneeHistories = [];

        var sut = CreateSut();

        var expectedConfiguredArea = FixtureInstance.Build<ConfiguredFcArea>().With(x => x.AreaCostCode, request.FcAreaCostCode).Create();
        var configuredAreas = FixtureInstance.CreateMany<ConfiguredFcArea>().ToList();

        _mockGetConfiguredFcAreas.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredAreas.Append(expectedConfiguredArea).ToList());

        _internalFlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));

        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockUow
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.AssignToInternalUserAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal("018/026/2026/Test", result.Value.OriginalApplicationReference);
        Assert.Equal($"{request.FcAreaCostCode}/026/2026/Test", result.Value.UpdatedApplicationReference);
        Assert.False(result.Value.ApplicationAlreadyAssignedToThisUser);
        Assert.Null(result.Value.IdOfUnassignedUser);
        Assert.Null(result.Value.LinkedPropertyProfileId);
        Assert.Equal(application.SubmittedFlaPropertyDetail.Name, result.Value.PropertyName);
        Assert.Equal(application.CreatedById, result.Value.ApplicationAuthorId);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _internalFlaRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _internalFlaRepository
            .Verify(x => x.GetAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                Times.Once);
        _mockUow.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()));

        _internalFlaRepository.VerifyNoOtherCalls();
        _mockUow.VerifyNoOtherCalls();

        _mockTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _mockTransaction.VerifyNoOtherCalls();

        _mockGetConfiguredFcAreas.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockGetConfiguredFcAreas.VerifyNoOtherCalls();

        _mockCaseNotes
            .Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(r => r.FellingLicenceApplicationId == request.ApplicationId
                && r.Type == CaseNoteType.CaseNote
                && r.Text == request.CaseNoteContent
                && r.VisibleToApplicant == true
                && r.VisibleToConsultee == false), request.PerformingUserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseNotes.VerifyNoOtherCalls();

        //assert application changes
        Assert.Equal(FellingLicenceStatus.AdminOfficerReview, application.GetCurrentStatus()); 
        Assert.Contains(application.AssigneeHistories, x => x.Role == AssignedUserRole.AdminOfficer && x.AssignedUserId == request.AssignToUserId && x.TimestampUnassigned == null);
        Assert.Equal($"{request.FcAreaCostCode}/026/2026/Test", application.ApplicationReference);

    }

    [Theory, AutoMoqData]
    public async Task WhenSuccessfulWithUserAlreadyAssigned(
        FellingLicenceApplication application)
    {
        var request = FixtureInstance.Build<AssignToUserRequest>()
            .With(x => x.AssignedRole, AssignedUserRole.AdminOfficer)
            .With(x => x.FcAreaCostCode, "018")
            .Create();

        application.ApplicationReference = "018/026/2026/Test";
        application.StatusHistories =
        [
            new StatusHistory
            {
                CreatedById = Guid.NewGuid(),
                Created = DateTime.Today,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        ];
        application.AssigneeHistories =
        [
            new AssigneeHistory
            {
                Role = AssignedUserRole.AdminOfficer,
                AssignedUserId = request.AssignToUserId,
                FellingLicenceApplication = application,
                TimestampUnassigned = null,
                TimestampAssigned = DateTime.Today
            }
        ];

        var sut = CreateSut();

        var expectedConfiguredArea = FixtureInstance.Build<ConfiguredFcArea>().With(x => x.AreaCostCode, request.FcAreaCostCode).Create();
        var configuredAreas = FixtureInstance.CreateMany<ConfiguredFcArea>().ToList();

        _mockGetConfiguredFcAreas.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredAreas.Append(expectedConfiguredArea).ToList());

        _internalFlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));

        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockUow
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.AssignToInternalUserAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal("018/026/2026/Test", result.Value.OriginalApplicationReference);
        Assert.Equal("018/026/2026/Test", result.Value.UpdatedApplicationReference);
        Assert.True(result.Value.ApplicationAlreadyAssignedToThisUser);
        Assert.Null(result.Value.IdOfUnassignedUser);
        Assert.Null(result.Value.LinkedPropertyProfileId);
        Assert.Equal(application.SubmittedFlaPropertyDetail.Name, result.Value.PropertyName);
        Assert.Equal(application.CreatedById, result.Value.ApplicationAuthorId);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _internalFlaRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _internalFlaRepository
            .Verify(x => x.GetAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                Times.Once);
        _mockUow.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()));

        _internalFlaRepository.VerifyNoOtherCalls();
        _mockUow.VerifyNoOtherCalls();

        _mockTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _mockTransaction.VerifyNoOtherCalls();

        _mockGetConfiguredFcAreas.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockGetConfiguredFcAreas.VerifyNoOtherCalls();

        _mockCaseNotes
            .Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(r => r.FellingLicenceApplicationId == request.ApplicationId
                && r.Type == CaseNoteType.CaseNote
                && r.Text == request.CaseNoteContent
                && r.VisibleToApplicant == true
                && r.VisibleToConsultee == false), request.PerformingUserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseNotes.VerifyNoOtherCalls();

        //assert application changes
        Assert.Equal(1, application.StatusHistories.Count);  // no new history added
        Assert.Equal(1, application.AssigneeHistories.Count);  // no new assignee history added
        Assert.Equal("018/026/2026/Test", application.ApplicationReference);  // ref stayed same
    }

    [Theory, AutoMoqData]
    public async Task WhenSuccessfulWithUserAlreadyAssignedAndApplicationWithApplicant(
    FellingLicenceApplication application)
    {
        var request = FixtureInstance.Build<AssignToUserRequest>()
            .With(x => x.AssignedRole, AssignedUserRole.AdminOfficer)
            .With(x => x.FcAreaCostCode, "018")
            .Create();

        application.ApplicationReference = "018/026/2026/Test";
        application.StatusHistories =
        [
            new StatusHistory
            {
                CreatedById = Guid.NewGuid(),
                Created = DateTime.Today,
                Status = FellingLicenceStatus.WithApplicant
            }
        ];
        application.AssigneeHistories =
        [
            new AssigneeHistory
            {
                Role = AssignedUserRole.AdminOfficer,
                AssignedUserId = request.AssignToUserId,
                FellingLicenceApplication = application,
                TimestampUnassigned = null,
                TimestampAssigned = DateTime.Today
            }
        ];

        var sut = CreateSut();

        var expectedConfiguredArea = FixtureInstance.Build<ConfiguredFcArea>().With(x => x.AreaCostCode, request.FcAreaCostCode).Create();
        var configuredAreas = FixtureInstance.CreateMany<ConfiguredFcArea>().ToList();

        _mockGetConfiguredFcAreas.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredAreas.Append(expectedConfiguredArea).ToList());

        _internalFlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));

        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockUow
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.AssignToInternalUserAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal("018/026/2026/Test", result.Value.OriginalApplicationReference);
        Assert.Equal("018/026/2026/Test", result.Value.UpdatedApplicationReference);
        Assert.True(result.Value.ApplicationAlreadyAssignedToThisUser);
        Assert.Null(result.Value.IdOfUnassignedUser);
        Assert.Equal(application.LinkedPropertyProfile.PropertyProfileId, result.Value.LinkedPropertyProfileId);
        Assert.Null(result.Value.PropertyName);
        Assert.Equal(application.CreatedById, result.Value.ApplicationAuthorId);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _internalFlaRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _internalFlaRepository
            .Verify(x => x.GetAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                Times.Once);
        _mockUow.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()));

        _internalFlaRepository.VerifyNoOtherCalls();
        _mockUow.VerifyNoOtherCalls();

        _mockTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _mockTransaction.VerifyNoOtherCalls();

        _mockGetConfiguredFcAreas.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockGetConfiguredFcAreas.VerifyNoOtherCalls();

        _mockCaseNotes
            .Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(r => r.FellingLicenceApplicationId == request.ApplicationId
                && r.Type == CaseNoteType.CaseNote
                && r.Text == request.CaseNoteContent
                && r.VisibleToApplicant == true
                && r.VisibleToConsultee == false), request.PerformingUserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseNotes.VerifyNoOtherCalls();

        //assert application changes
        Assert.Equal(1, application.StatusHistories.Count);  // no new history added
        Assert.Equal(1, application.AssigneeHistories.Count);  // no new assignee history added
        Assert.Equal("018/026/2026/Test", application.ApplicationReference);  // ref stayed same
    }

    [Theory, AutoMoqData]
    public async Task WhenSuccessfulWithUserAlreadyAssignedAndNoCaseNote(
        FellingLicenceApplication application)
    {
        var request = FixtureInstance.Build<AssignToUserRequest>()
            .With(x => x.AssignedRole, AssignedUserRole.AdminOfficer)
            .With(x => x.FcAreaCostCode, "018")
            .With(x => x.CaseNoteContent, string.Empty)
            .Create();

        application.ApplicationReference = "018/026/2026/Test";
        application.StatusHistories =
        [
            new StatusHistory
            {
                CreatedById = Guid.NewGuid(),
                Created = DateTime.Today,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        ];
        application.AssigneeHistories =
        [
            new AssigneeHistory
            {
                Role = AssignedUserRole.AdminOfficer,
                AssignedUserId = request.AssignToUserId,
                FellingLicenceApplication = application,
                TimestampUnassigned = null,
                TimestampAssigned = DateTime.Today
            }
        ];

        var sut = CreateSut();

        var expectedConfiguredArea = FixtureInstance.Build<ConfiguredFcArea>().With(x => x.AreaCostCode, request.FcAreaCostCode).Create();
        var configuredAreas = FixtureInstance.CreateMany<ConfiguredFcArea>().ToList();

        _mockGetConfiguredFcAreas.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredAreas.Append(expectedConfiguredArea).ToList());

        _internalFlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));

        _mockUow
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.AssignToInternalUserAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal("018/026/2026/Test", result.Value.OriginalApplicationReference);
        Assert.Equal("018/026/2026/Test", result.Value.UpdatedApplicationReference);
        Assert.True(result.Value.ApplicationAlreadyAssignedToThisUser);
        Assert.Null(result.Value.IdOfUnassignedUser);
        Assert.Null(result.Value.LinkedPropertyProfileId);
        Assert.Equal(application.SubmittedFlaPropertyDetail.Name, result.Value.PropertyName);
        Assert.Equal(application.CreatedById, result.Value.ApplicationAuthorId);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _internalFlaRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _internalFlaRepository
            .Verify(x => x.GetAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                Times.Once);
        _mockUow.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()));

        _internalFlaRepository.VerifyNoOtherCalls();
        _mockUow.VerifyNoOtherCalls();

        _mockTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _mockTransaction.VerifyNoOtherCalls();

        _mockGetConfiguredFcAreas.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockGetConfiguredFcAreas.VerifyNoOtherCalls();

        _mockCaseNotes.VerifyNoOtherCalls();

        //assert application changes
        Assert.Equal(1, application.StatusHistories.Count);  // no new history added
        Assert.Equal(1, application.AssigneeHistories.Count);  // no new assignee history added
        Assert.Equal("018/026/2026/Test", application.ApplicationReference);  // ref stayed same
    }

    [Theory, AutoMoqData]
    public async Task WhenSuccessfulWithExistingUserUnassigned(
        FellingLicenceApplication application,
        Guid unassignedUserId)
    {
        var request = FixtureInstance.Build<AssignToUserRequest>()
            .With(x => x.AssignedRole, AssignedUserRole.AdminOfficer)
            .With(x => x.FcAreaCostCode, "018")
            .Create();

        application.ApplicationReference = "018/026/2026/Test";
        application.StatusHistories =
        [
            new StatusHistory
            {
                CreatedById = Guid.NewGuid(),
                Created = DateTime.Today,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        ];
        application.AssigneeHistories =
        [
            new AssigneeHistory
            {
                Role = AssignedUserRole.AdminOfficer,
                AssignedUserId = unassignedUserId,
                FellingLicenceApplication = application,
                TimestampUnassigned = null,
                TimestampAssigned = DateTime.Today
            }
        ];

        var sut = CreateSut();

        var expectedConfiguredArea = FixtureInstance.Build<ConfiguredFcArea>().With(x => x.AreaCostCode, request.FcAreaCostCode).Create();
        var configuredAreas = FixtureInstance.CreateMany<ConfiguredFcArea>().ToList();

        _mockGetConfiguredFcAreas.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredAreas.Append(expectedConfiguredArea).ToList());

        _internalFlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));

        _mockCaseNotes
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockUow
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.AssignToInternalUserAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal("018/026/2026/Test", result.Value.OriginalApplicationReference);
        Assert.Equal("018/026/2026/Test", result.Value.UpdatedApplicationReference);
        Assert.False(result.Value.ApplicationAlreadyAssignedToThisUser);
        Assert.Equal(unassignedUserId, result.Value.IdOfUnassignedUser);
        Assert.Null(result.Value.LinkedPropertyProfileId);
        Assert.Equal(application.SubmittedFlaPropertyDetail.Name, result.Value.PropertyName);
        Assert.Equal(application.CreatedById, result.Value.ApplicationAuthorId);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _internalFlaRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _internalFlaRepository
            .Verify(x => x.GetAsync(request.ApplicationId, It.IsAny<CancellationToken>()),
                Times.Once);
        _mockUow.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()));

        _internalFlaRepository.VerifyNoOtherCalls();
        _mockUow.VerifyNoOtherCalls();

        _mockTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _mockTransaction.VerifyNoOtherCalls();

        _mockGetConfiguredFcAreas.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockGetConfiguredFcAreas.VerifyNoOtherCalls();

        _mockCaseNotes
            .Verify(x => x.AddCaseNoteAsync(It.Is<AddCaseNoteRecord>(r => r.FellingLicenceApplicationId == request.ApplicationId
                && r.Type == CaseNoteType.CaseNote
                && r.Text == request.CaseNoteContent
                && r.VisibleToApplicant == true
                && r.VisibleToConsultee == false), request.PerformingUserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseNotes.VerifyNoOtherCalls();

        //assert application changes
        Assert.Equal(1, application.StatusHistories.Count);  // no new history added
        Assert.Equal(2, application.AssigneeHistories.Count);  // new assignee history added
        Assert.Contains(application.AssigneeHistories,
            x => x.AssignedUserId == request.AssignToUserId && x is
                { Role: AssignedUserRole.AdminOfficer, TimestampUnassigned: null });
        Assert.Contains(application.AssigneeHistories,
            x => x.AssignedUserId == unassignedUserId && x is
                { Role: AssignedUserRole.AdminOfficer, TimestampUnassigned: not null });
        Assert.Equal("018/026/2026/Test", application.ApplicationReference);  // ref stayed same
    }

    private UpdateFellingLicenceApplicationService CreateSut()
    {
        _internalFlaRepository.Reset();
        _mockCaseNotes.Reset();
        _mockClock.Reset();
        _mockGetConfiguredFcAreas.Reset();
        _mockTransaction.Reset();
        _mockUow.Reset();
        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.Now.ToUniversalTime()));

        _internalFlaRepository.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockTransaction.Object);
        
        _internalFlaRepository.SetupGet(x => x.UnitOfWork).Returns(_mockUow.Object);

        return new UpdateFellingLicenceApplicationService(
            _internalFlaRepository.Object,
            _mockCaseNotes.Object,
            _mockGetConfiguredFcAreas.Object,
            _mockClock.Object,
            new NullLogger<UpdateFellingLicenceApplicationService>(),
            new Mock<IOptions<FellingLicenceApplicationOptions>>().Object);
    }
}