using AutoFixture;
using AutoFixture.AutoMoq;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqKit;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateFellingLicenceApplicationForExternalUsersServiceTests
{
    private readonly Mock<IFellingLicenceApplicationExternalRepository> _mockRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitofWork = new();
    private readonly Mock<IClock> _mockClock = new();
    private readonly Instant _now = Instant.FromDateTimeUtc(DateTime.Today.AddDays(10).ToUniversalTime());

    private readonly IFixture FixtureInstance = CreateFixture();

    private readonly FellingLicenceApplicationOptions _options = new()
    {
        CitizensCharterDateLength = TimeSpan.FromDays(7),
        FinalActionDateDaysFromSubmission = 30
    };

    [Theory, AutoMoqData]
    public async Task WhenApplicationWithGivenIdIsNotFound(
        Guid applicationId, 
        UserAccessModel userAccessModel, 
        SubmittedFlaPropertyDetail submittedFlaPropertyDetail)
    {
        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result =
            await sut.SubmitFellingLicenceApplicationAsync(applicationId, userAccessModel, submittedFlaPropertyDetail, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();

        _mockUnitofWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasNoLinkedPropertyProfile(
        Guid applicationId,
        FellingLicenceApplication application,
        UserAccessModel userAccessModel,
        SubmittedFlaPropertyDetail submittedFlaPropertyDetail)
    {
        TestUtils.SetProtectedProperty(application, nameof(application.Id), applicationId);
        application.LinkedPropertyProfile = null;
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Status = FellingLicenceStatus.Draft,
                Created = DateTime.Today,
                CreatedById = Guid.NewGuid()
            }
        };

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));

        var result = await sut.SubmitFellingLicenceApplicationAsync(applicationId, userAccessModel, submittedFlaPropertyDetail, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();

        _mockUnitofWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUserHasNoAccessForTheApplication(
        Guid applicationId,
        FellingLicenceApplication application,
        SubmittedFlaPropertyDetail submittedFlaPropertyDetail)
    {
        TestUtils.SetProtectedProperty(application, nameof(application.Id), applicationId);
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Status = FellingLicenceStatus.Draft,
                Created = DateTime.Today,
                CreatedById = Guid.NewGuid()
            }
        };

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { Guid.NewGuid() }
        };

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));

        var result = await sut.SubmitFellingLicenceApplicationAsync(applicationId, userAccessModel, submittedFlaPropertyDetail, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();

        _mockUnitofWork.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(FellingLicenceStatus.AdminOfficerReview)]
    [InlineData(FellingLicenceStatus.Submitted)]
    [InlineData(FellingLicenceStatus.Approved)]
    [InlineData(FellingLicenceStatus.Refused)]
    [InlineData(FellingLicenceStatus.Received)]
    [InlineData(FellingLicenceStatus.SentForApproval)]
    [InlineData(FellingLicenceStatus.Withdrawn)]
    [InlineData(FellingLicenceStatus.WoodlandOfficerReview)]
    public async Task WhenApplicationIsNotInASubmittableState(FellingLicenceStatus currentState)
    {
        var applicationId = Guid.NewGuid();
        var application = FixtureInstance.Create<FellingLicenceApplication>();
        var submittedFlaPropertyDetail = FixtureInstance.Create<SubmittedFlaPropertyDetail>();

        TestUtils.SetProtectedProperty(application, nameof(application.Id), applicationId);
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Status = currentState,
                Created = DateTime.Today,
                CreatedById = Guid.NewGuid()
            }
        };

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
        };

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));

        var result = await sut.SubmitFellingLicenceApplicationAsync(applicationId, userAccessModel, submittedFlaPropertyDetail, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();

        _mockUnitofWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUnableToRemoveExistingSubmittedFlaPropertyDetails(
        Guid applicationId,
        FellingLicenceApplication application,
        SubmittedFlaPropertyDetail submittedFlaPropertyDetail)
    {
        TestUtils.SetProtectedProperty(application, nameof(application.Id), applicationId);
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Status = FellingLicenceStatus.Draft,
                Created = DateTime.Today,
                CreatedById = Guid.NewGuid()
            }
        };
        application.ApplicationReference = "---/022/1234";

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
        };

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));
        _mockRepository
            .Setup(x => x.DeleteSubmittedFlaPropertyDetailForApplicationAsync(It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.SubmitFellingLicenceApplicationAsync(applicationId, userAccessModel, submittedFlaPropertyDetail, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.DeleteSubmittedFlaPropertyDetailForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUnableToSaveSubmittedFlaPropertyDetails(
        Guid applicationId,
        FellingLicenceApplication application,
        SubmittedFlaPropertyDetail submittedFlaPropertyDetail)  // the fandr details in application won't match compartments in submittedFlaPropertyDetail, so converting will fail
    {
        TestUtils.SetProtectedProperty(application, nameof(application.Id), applicationId);
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Status = FellingLicenceStatus.Draft,
                Created = DateTime.Today,
                CreatedById = Guid.NewGuid()
            }
        };
        application.ApplicationReference = "---/022/1234";

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
        };

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));
        _mockRepository
            .Setup(x => x.DeleteSubmittedFlaPropertyDetailForApplicationAsync(It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _mockUnitofWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.SubmitFellingLicenceApplicationAsync(applicationId, userAccessModel, submittedFlaPropertyDetail, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.DeleteSubmittedFlaPropertyDetailForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        _mockRepository.VerifyGet(x => x.UnitOfWork, Times.Once);
        _mockUnitofWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUnableToConvertFAndR(
    Guid applicationId,
    FellingLicenceApplication application,
    SubmittedFlaPropertyDetail submittedFlaPropertyDetail)  // the fandr details in application won't match compartments in submittedFlaPropertyDetail, so converting will fail
    {
        TestUtils.SetProtectedProperty(application, nameof(application.Id), applicationId);
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Status = FellingLicenceStatus.Draft,
                Created = DateTime.Today,
                CreatedById = Guid.NewGuid()
            }
        };
        application.ApplicationReference = "---/022/1234";

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
        };

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));
        _mockRepository
            .Setup(x => x.DeleteSubmittedFlaPropertyDetailForApplicationAsync(It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _mockUnitofWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.SubmitFellingLicenceApplicationAsync(applicationId, userAccessModel, submittedFlaPropertyDetail, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.DeleteSubmittedFlaPropertyDetailForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyGet(x => x.UnitOfWork, Times.Once);
        _mockUnitofWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();
    }

    // fandr conversion tested in UpdateFellingLicenceApplicationForExternalUsersServiceConvertFAndRTests

    [Theory, AutoMoqData]
    public async Task WhenSavingChangesToApplicationFails(
        Guid applicationId,
        FellingLicenceApplication application,
        SubmittedFlaPropertyDetail submittedFlaPropertyDetail)
    {
        TestUtils.SetProtectedProperty(application, nameof(application.Id), applicationId);
        application.LinkedPropertyProfile.ProposedFellingDetails = [];
        application.LinkedPropertyProfile.ProposedCompartmentDesignations = [];
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Status = FellingLicenceStatus.Draft,
                Created = DateTime.Today,
                CreatedById = Guid.NewGuid()
            }
        };
        application.ApplicationReference = "---/022/1234";

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
        };

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));
        _mockRepository
            .Setup(x => x.DeleteSubmittedFlaPropertyDetailForApplicationAsync(It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _mockUnitofWork
            .SetupSequence(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>())
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.SubmitFellingLicenceApplicationAsync(applicationId, userAccessModel, submittedFlaPropertyDetail, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.DeleteSubmittedFlaPropertyDetailForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyGet(x => x.UnitOfWork, Times.Exactly(2));

        _mockUnitofWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitofWork.VerifyNoOtherCalls();

        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSubmittingDraftApplication(
        Guid applicationId,
        FellingLicenceApplication application,
        SubmittedFlaPropertyDetail submittedFlaPropertyDetail)
    {
        TestUtils.SetProtectedProperty(application, nameof(application.Id), applicationId);
        application.LinkedPropertyProfile.ProposedFellingDetails = [];
        application.LinkedPropertyProfile.ProposedCompartmentDesignations = [];
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Status = FellingLicenceStatus.Draft,
                Created = DateTime.Today,
                CreatedById = Guid.NewGuid()
            }
        };
        
        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
        };

        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                Role = AssignedUserRole.Author,
                TimestampAssigned = DateTime.Today,
                AssignedUserId = userAccessModel.UserAccountId
            }
        };
        application.AreaCode = "123";
        application.ApplicationReference = "---/022/1234";


        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));
        _mockUnitofWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.SubmitFellingLicenceApplicationAsync(applicationId, userAccessModel, submittedFlaPropertyDetail, CancellationToken.None);

        Assert.True(result.IsSuccess);

        // assert response model
        Assert.Equal("123/022/1234", result.Value.ApplicationReference);
        Assert.Equal(application.WoodlandOwnerId, result.Value.WoodlandOwnerId);
        Assert.Equal(application.LinkedPropertyProfile.PropertyProfileId, result.Value.PropertyProfileId);
        Assert.Equal(FellingLicenceStatus.Draft, result.Value.PreviousStatus);
        Assert.Empty(result.Value.AssignedInternalUsers);

        // assert updated reference
        Assert.Equal("123/022/1234", application.ApplicationReference);

        // assert updated assignee history
        Assert.Equal(_now.ToDateTimeUtc(), application.AssigneeHistories.Single().TimestampUnassigned);

        // assert new status
        Assert.Equal(FellingLicenceStatus.Submitted, application.StatusHistories.MaxBy(x => x.Created).Status);

        // assert timestamps
        Assert.Equal(_now.ToDateTimeUtc(), application.DateReceived);
        Assert.Equal(_now.ToDateTimeUtc().AddDays(_options.FinalActionDateDaysFromSubmission), application.FinalActionDate);
        Assert.Equal(_now.ToDateTimeUtc().Add(_options.CitizensCharterDateLength), application.CitizensCharterDate);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.DeleteSubmittedFlaPropertyDetailForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyGet(x => x.UnitOfWork, Times.Exactly(2));

        _mockUnitofWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitofWork.VerifyNoOtherCalls();

        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSubmittingReturnedToApplicantApplication(
        Guid applicationId,
        FellingLicenceApplication application,
        DateTime originalDateReceived,
        SubmittedFlaPropertyDetail submittedFlaPropertyDetail)
    {
        TestUtils.SetProtectedProperty(application, nameof(application.Id), applicationId);
        application.LinkedPropertyProfile.ProposedFellingDetails = [];
        application.LinkedPropertyProfile.ProposedCompartmentDesignations = [];
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Status = FellingLicenceStatus.ReturnedToApplicant,
                Created = DateTime.Today,
                CreatedById = Guid.NewGuid()
            }
        };

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
        };

        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                Role = AssignedUserRole.Author,
                TimestampAssigned = DateTime.Today,
                AssignedUserId = userAccessModel.UserAccountId
            },
            new AssigneeHistory
            {
                Role = AssignedUserRole.AdminOfficer,
                TimestampAssigned = DateTime.Today,
                TimestampUnassigned = DateTime.Today,
                AssignedUserId = Guid.NewGuid()
            }
        };
        application.AreaCode = "123";
        application.ApplicationReference = "---/022/1234";
        application.DateReceived = originalDateReceived;

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));
        _mockUnitofWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _mockRepository
            .Setup(x => x.UpdateExistingWoodlandOfficerReviewFlagsForResubmission(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.SubmitFellingLicenceApplicationAsync(applicationId, userAccessModel, submittedFlaPropertyDetail, CancellationToken.None);

        Assert.True(result.IsSuccess);

        // assert response model
        Assert.Equal("123/022/1234", result.Value.ApplicationReference);
        Assert.Equal(application.WoodlandOwnerId, result.Value.WoodlandOwnerId);
        Assert.Equal(application.LinkedPropertyProfile.PropertyProfileId, result.Value.PropertyProfileId);
        Assert.Equal(FellingLicenceStatus.ReturnedToApplicant, result.Value.PreviousStatus);
        Assert.Empty(result.Value.AssignedInternalUsers);

        // assert updated reference
        Assert.Equal("123/022/1234", application.ApplicationReference);

        // assert updated assignee history
        Assert.Equal(_now.ToDateTimeUtc(), application.AssigneeHistories.First().TimestampUnassigned);

        // assert new status
        Assert.Equal(FellingLicenceStatus.Submitted, application.StatusHistories.MaxBy(x => x.Created).Status);

        // assert timestamps
        Assert.Equal(originalDateReceived, application.DateReceived);
        Assert.Equal(_now.ToDateTimeUtc().AddDays(_options.FinalActionDateDaysFromSubmission), application.FinalActionDate);
        Assert.Equal(_now.ToDateTimeUtc().Add(_options.CitizensCharterDateLength), application.CitizensCharterDate);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.DeleteSubmittedFlaPropertyDetailForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyGet(x => x.UnitOfWork, Times.Exactly(2));
        _mockRepository.Verify(x => x
                .DeleteAdminOfficerReviewForApplicationAsync(applicationId, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockRepository.Verify(x => x.UpdateExistingWoodlandOfficerReviewFlagsForResubmission(
            applicationId, _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitofWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitofWork.VerifyNoOtherCalls();

        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSubmittingWithApplicantApplication(
        Guid applicationId,
        FellingLicenceApplication application,
        DateTime originalDateReceived,
        DateTime originalFinalActionDate,
        DateTime originalCitizensCharterDate,
        SubmittedFlaPropertyDetail submittedFlaPropertyDetail)
    {
        TestUtils.SetProtectedProperty(application, nameof(application.Id), applicationId);
        application.LinkedPropertyProfile.ProposedFellingDetails = [];
        application.LinkedPropertyProfile.ProposedCompartmentDesignations = [];
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Status = FellingLicenceStatus.WithApplicant,
                Created = DateTime.Today,
                CreatedById = Guid.NewGuid()
            }
        };

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
        };

        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                Role = AssignedUserRole.Author,
                TimestampAssigned = DateTime.Today,
                AssignedUserId = userAccessModel.UserAccountId
            },
            new AssigneeHistory
            {
                Role = AssignedUserRole.AdminOfficer,
                TimestampAssigned = DateTime.Today,
                AssignedUserId = Guid.NewGuid()
            },
            new AssigneeHistory
            {
                Role = AssignedUserRole.WoodlandOfficer,
                TimestampAssigned = DateTime.Today,
                AssignedUserId = Guid.NewGuid()
            }
        };
        application.AreaCode = "123";
        application.ApplicationReference = "---/022/1234";
        application.DateReceived = originalDateReceived;
        application.CitizensCharterDate = originalCitizensCharterDate;
        application.FinalActionDate = originalFinalActionDate;

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));
        _mockUnitofWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _mockRepository
            .Setup(x => x.UpdateExistingWoodlandOfficerReviewFlagsForResubmission(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.SubmitFellingLicenceApplicationAsync(applicationId, userAccessModel, submittedFlaPropertyDetail, CancellationToken.None);

        Assert.True(result.IsSuccess);

        // assert response model
        Assert.Equal("123/022/1234", result.Value.ApplicationReference);
        Assert.Equal(application.WoodlandOwnerId, result.Value.WoodlandOwnerId);
        Assert.Equal(application.LinkedPropertyProfile.PropertyProfileId, result.Value.PropertyProfileId);
        Assert.Equal(FellingLicenceStatus.WithApplicant, result.Value.PreviousStatus);
        Assert.Equal(2, result.Value.AssignedInternalUsers.Count);
        Assert.Contains(application.AssigneeHistories[1].AssignedUserId, result.Value.AssignedInternalUsers);
        Assert.Contains(application.AssigneeHistories[2].AssignedUserId, result.Value.AssignedInternalUsers);

        // assert updated reference
        Assert.Equal("123/022/1234", application.ApplicationReference);

        // assert updated assignee history
        Assert.Equal(_now.ToDateTimeUtc(), application.AssigneeHistories.First().TimestampUnassigned);

        // assert new status
        Assert.Equal(FellingLicenceStatus.WoodlandOfficerReview, application.StatusHistories.MaxBy(x => x.Created).Status);

        // assert timestamps
        Assert.Equal(originalDateReceived, application.DateReceived);
        Assert.Equal(originalFinalActionDate, application.FinalActionDate);
        Assert.Equal(originalCitizensCharterDate, application.CitizensCharterDate);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.DeleteSubmittedFlaPropertyDetailForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyGet(x => x.UnitOfWork, Times.Exactly(2));
        _mockRepository.Verify(x => x.UpdateExistingWoodlandOfficerReviewFlagsForResubmission(
            applicationId, _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitofWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitofWork.VerifyNoOtherCalls();

        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSubmittingDraftApplicationWithDesignations(
        Guid applicationId,
        FellingLicenceApplication application,
        SubmittedFlaPropertyDetail submittedFlaPropertyDetail)
    {
        TestUtils.SetProtectedProperty(application, nameof(application.Id), applicationId);
        application.LinkedPropertyProfile.ProposedFellingDetails = [];

        var cptWithDesignations =
            application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.First().CompartmentId;

        application.LinkedPropertyProfile.ProposedCompartmentDesignations =
        [
            new ProposedCompartmentDesignations
            {
                LinkedPropertyProfile = application.LinkedPropertyProfile,
                LinkedPropertyProfileId = application.LinkedPropertyProfile.Id,
                CrossesPawsZones = ["ARW"],
                Id = Guid.NewGuid(),
                PropertyProfileCompartmentId = cptWithDesignations
            }
        ];

        submittedFlaPropertyDetail.SubmittedFlaPropertyCompartments
            .ForEach(x => x.SubmittedCompartmentDesignations = null);

        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Status = FellingLicenceStatus.Draft,
                Created = DateTime.Today,
                CreatedById = Guid.NewGuid()
            }
        };

        var userAccessModel = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
        };

        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                Role = AssignedUserRole.Author,
                TimestampAssigned = DateTime.Today,
                AssignedUserId = userAccessModel.UserAccountId
            }
        };
        application.AreaCode = "123";
        application.ApplicationReference = "---/022/1234";


        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));
        _mockUnitofWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.SubmitFellingLicenceApplicationAsync(applicationId, userAccessModel, submittedFlaPropertyDetail, CancellationToken.None);

        Assert.True(result.IsSuccess);

        // assert updated entity submitted designations
        foreach (var submittedCompartment in
                 application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments)
        {
            if (submittedCompartment.CompartmentId == cptWithDesignations)
            {
                Assert.NotNull(submittedCompartment.SubmittedCompartmentDesignations);
                var designation = submittedCompartment.SubmittedCompartmentDesignations;
                var proposedDesignation =
                    application.LinkedPropertyProfile.ProposedCompartmentDesignations
                        .First(x => x.PropertyProfileCompartmentId == cptWithDesignations);
                Assert.Equal(proposedDesignation.CrossesPawsZones.Any(), designation.Paws);
                Assert.Equal(proposedDesignation.ProportionAfterFelling, designation.ProportionAfterFelling);
                Assert.Equal(proposedDesignation.ProportionBeforeFelling, designation.ProportionBeforeFelling);
            }
            else
            {
                Assert.Null(submittedCompartment.SubmittedCompartmentDesignations);
            }
        }

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.DeleteSubmittedFlaPropertyDetailForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyGet(x => x.UnitOfWork, Times.Exactly(2));

        _mockUnitofWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitofWork.VerifyNoOtherCalls();

        _mockRepository.VerifyNoOtherCalls();
    }
    private UpdateFellingLicenceApplicationForExternalUsersService CreateSut()
    {
        _mockClock.Reset();
        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(_now);

        _mockRepository.Reset();
        _mockUnitofWork.Reset();
        _mockRepository.SetupGet(x => x.UnitOfWork).Returns(_mockUnitofWork.Object);

        return new UpdateFellingLicenceApplicationForExternalUsersService(
            _mockRepository.Object,
            _mockClock.Object,
            new OptionsWrapper<FellingLicenceApplicationOptions>(_options),
            new NullLogger<UpdateFellingLicenceApplicationForExternalUsersService>());
    }

    private static IFixture CreateFixture()
    {
        var fixture = new Fixture().Customize(new CompositeCustomization(
            new AutoMoqCustomization(),
            new SupportMutableValueTypesCustomization()));

        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        fixture.Customize<DateOnly>(composer => composer.FromFactory<DateTime>(DateOnly.FromDateTime));

        return fixture;
    }
}