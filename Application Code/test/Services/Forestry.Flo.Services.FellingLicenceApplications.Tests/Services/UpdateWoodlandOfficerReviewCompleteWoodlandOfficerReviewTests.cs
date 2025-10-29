using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateWoodlandOfficerReviewCompleteWoodlandOfficerReviewTests : UpdateWoodlandOfficerReviewServiceTestsBase
{
    [Theory, AutoData]
    public async Task WhenApplicationNotFound(
        Guid applicationId,
        Guid performingUserId,
        RecommendedLicenceDuration recommendedLicenceDuration,
        bool recommendationForPublicRegister,
        string recommendationForPublicRegisterReason,
        DateTime completedDateTime)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationNotInWoodlandOfficerReviewState(
        Guid applicationId,
        Guid performingUserId,
        DateTime completedDateTime,
        RecommendedLicenceDuration recommendedLicenceDuration,
        bool recommendationForPublicRegister,
        string recommendationForPublicRegisterReason,
        FellingLicenceApplication application)
    {
        foreach (var applicationStatusHistory in application.StatusHistories)
        {
            if (applicationStatusHistory.Status == FellingLicenceStatus.WoodlandOfficerReview)
            {
                applicationStatusHistory.Status = FellingLicenceStatus.Draft;
            }
        }

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenPerformingUserNotAssignedWoodlandOfficer(
        Guid applicationId,
        Guid performingUserId,
        Guid woodlandOfficerId,
        DateTime completedDateTime,
        RecommendedLicenceDuration recommendedLicenceDuration,
        bool recommendationForPublicRegister,
        string recommendationForPublicRegisterReason,
        FellingLicenceApplication application)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.WoodlandOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = woodlandOfficerId,
                Role = AssignedUserRole.WoodlandOfficer
            },
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = Guid.NewGuid(),
                Role = AssignedUserRole.FieldManager
            }
        };

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenNoAssignedFieldManager(
        Guid applicationId,
        Guid performingUserId,
        DateTime completedDateTime,
        RecommendedLicenceDuration recommendedLicenceDuration,
        bool recommendationForPublicRegister,
        string recommendationForPublicRegisterReason,
        FellingLicenceApplication application)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.WoodlandOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.WoodlandOfficer
            }
        };

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenNoWoodlandOfficerReviewEntityFound(
        Guid applicationId,
        Guid performingUserId,
        DateTime completedDateTime,
        RecommendedLicenceDuration recommendedLicenceDuration,
        bool recommendationForPublicRegister,
        string recommendationForPublicRegisterReason,
        FellingLicenceApplication application)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.WoodlandOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.WoodlandOfficer
            },
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = Guid.NewGuid(),
                Role = AssignedUserRole.FieldManager
            }
        };

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenNoPublicRegisterEntityFound(
     Guid applicationId,
     Guid performingUserId,
     DateTime completedDateTime,
     FellingLicenceApplication application,
     RecommendedLicenceDuration recommendedLicenceDuration,
     bool recommendationForPublicRegister,
     string recommendationForPublicRegisterReason,
     WoodlandOfficerReview woodlandOfficerReview)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.WoodlandOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.WoodlandOfficer
            },
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = Guid.NewGuid(),
                Role = AssignedUserRole.FieldManager
            }
        };

        // completed site visit
        woodlandOfficerReview.SiteVisitNeeded = true;
        woodlandOfficerReview.SiteVisitArrangementsMade = true;
        woodlandOfficerReview.SiteVisitComplete = true;

        // complete PW14 checks
        woodlandOfficerReview.Pw14ChecksComplete = true;

        // completed confirmed f&r
        woodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(woodlandOfficerReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenPublicRegisterStageInProgress(
        Guid applicationId,
        Guid performingUserId,
        DateTime completedDateTime,
        FellingLicenceApplication application,
        RecommendedLicenceDuration recommendedLicenceDuration,
        bool recommendationForPublicRegister,
        string recommendationForPublicRegisterReason,
        WoodlandOfficerReview woodlandOfficerReview)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.WoodlandOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.WoodlandOfficer
            },
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = Guid.NewGuid(),
                Role = AssignedUserRole.FieldManager
            }
        };

        // completed site visit
        woodlandOfficerReview.SiteVisitNeeded = true;
        woodlandOfficerReview.SiteVisitArrangementsMade = true;
        woodlandOfficerReview.SiteVisitComplete = true;

        // complete PW14 checks
        woodlandOfficerReview.Pw14ChecksComplete = true;

        // pr in progress
        var publicRegister = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow
        };

        // completed confirmed f&r
        woodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(woodlandOfficerReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(publicRegister));

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSiteVisitStageNotStarted(
        Guid applicationId,
        Guid performingUserId,
        DateTime completedDateTime,
        FellingLicenceApplication application,
        RecommendedLicenceDuration recommendedLicenceDuration,
        bool recommendationForPublicRegister,
        string recommendationForPublicRegisterReason,
        WoodlandOfficerReview woodlandOfficerReview)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.WoodlandOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.WoodlandOfficer
            },
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = Guid.NewGuid(),
                Role = AssignedUserRole.FieldManager
            }
        };

        // site visit not started
        woodlandOfficerReview.SiteVisitNeeded = null;
        woodlandOfficerReview.SiteVisitArrangementsMade = null;
        woodlandOfficerReview.SiteVisitComplete = false;

        // complete PW14 checks
        woodlandOfficerReview.Pw14ChecksComplete = true;

        //completed pr
        var publicRegister = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow,
            ConsultationPublicRegisterRemovedTimestamp = DateTime.UtcNow
        };

        // completed confirmed f&r
        woodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(woodlandOfficerReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(publicRegister));

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSiteVisitStageInProgress(
        Guid applicationId,
        Guid performingUserId,
        DateTime completedDateTime,
        FellingLicenceApplication application,
        RecommendedLicenceDuration recommendedLicenceDuration,
        bool recommendationForPublicRegister,
        string recommendationForPublicRegisterReason,
        WoodlandOfficerReview woodlandOfficerReview)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.WoodlandOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.WoodlandOfficer
            },
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = Guid.NewGuid(),
                Role = AssignedUserRole.FieldManager
            }
        };

        // site visit in progress
        woodlandOfficerReview.SiteVisitNeeded = true;
        woodlandOfficerReview.SiteVisitArrangementsMade = true;
        woodlandOfficerReview.SiteVisitComplete = false;

        // complete PW14 checks
        woodlandOfficerReview.Pw14ChecksComplete = true;

        //completed pr
        var publicRegister = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow,
            ConsultationPublicRegisterRemovedTimestamp = DateTime.UtcNow
        };

        // completed confirmed f&r
        woodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(woodlandOfficerReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(publicRegister));

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenPw14ChecksStageInProgress(
        Guid applicationId,
        Guid performingUserId,
        DateTime completedDateTime,
        FellingLicenceApplication application,
        RecommendedLicenceDuration recommendedLicenceDuration,
        bool recommendationForPublicRegister,
        string recommendationForPublicRegisterReason,
        WoodlandOfficerReview woodlandOfficerReview)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.WoodlandOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.WoodlandOfficer
            },
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = Guid.NewGuid(),
                Role = AssignedUserRole.FieldManager
            }
        };

        // completed site visit
        woodlandOfficerReview.SiteVisitNeeded = true;
        woodlandOfficerReview.SiteVisitArrangementsMade = true;
        woodlandOfficerReview.SiteVisitComplete = true;

        // incomplete PW14 checks
        woodlandOfficerReview.Pw14ChecksComplete = false;
        
        //completed pr
        var publicRegister = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow,
            ConsultationPublicRegisterRemovedTimestamp = DateTime.UtcNow
        };

        // completed confirmed f&r
        woodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(woodlandOfficerReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(publicRegister));

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenConfirmedFellingAndRestockingStageInProgress(
        Guid applicationId,
        Guid performingUserId,
        DateTime completedDateTime,
        FellingLicenceApplication application,
        RecommendedLicenceDuration recommendedLicenceDuration,
        bool recommendationForPublicRegister,
        string recommendationForPublicRegisterReason,
        WoodlandOfficerReview woodlandOfficerReview)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.WoodlandOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.WoodlandOfficer
            },
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = Guid.NewGuid(),
                Role = AssignedUserRole.FieldManager
            }
        };

        // completed site visit
        woodlandOfficerReview.SiteVisitNeeded = true;
        woodlandOfficerReview.SiteVisitArrangementsMade = true;
        woodlandOfficerReview.SiteVisitComplete = true;

        // completed PW14 checks
        woodlandOfficerReview.Pw14ChecksComplete = true;

        //completed pr
        var publicRegister = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow,
            ConsultationPublicRegisterRemovedTimestamp = DateTime.UtcNow
        };

        // incomplete confirmed f&r
        woodlandOfficerReview.ConfirmedFellingAndRestockingComplete = false;

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(woodlandOfficerReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(publicRegister));

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenConditionalNotSet(
        Guid applicationId,
        Guid performingUserId,
        DateTime completedDateTime,
        FellingLicenceApplication application,
        RecommendedLicenceDuration recommendedLicenceDuration,
        bool recommendationForPublicRegister,
        string recommendationForPublicRegisterReason,
        WoodlandOfficerReview woodlandOfficerReview)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.WoodlandOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.WoodlandOfficer
            },
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = Guid.NewGuid(),
                Role = AssignedUserRole.FieldManager
            }
        };

        // completed site visit
        woodlandOfficerReview.SiteVisitNeeded = true;
        woodlandOfficerReview.SiteVisitArrangementsMade = true;
        woodlandOfficerReview.SiteVisitComplete = true;

        // completed PW14 checks
        woodlandOfficerReview.Pw14ChecksComplete = true;

        //completed pr
        var publicRegister = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow,
            ConsultationPublicRegisterRemovedTimestamp = DateTime.UtcNow
        };

        // complete confirmed f&r
        woodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;

        //not started conditions
        woodlandOfficerReview.IsConditional = null;

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(woodlandOfficerReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(publicRegister));

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenConditionalButNotificationNotSent(
        Guid applicationId,
        Guid performingUserId,
        DateTime completedDateTime,
        FellingLicenceApplication application,
        RecommendedLicenceDuration recommendedLicenceDuration,
        bool recommendationForPublicRegister,
        string recommendationForPublicRegisterReason,
        WoodlandOfficerReview woodlandOfficerReview)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.WoodlandOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.WoodlandOfficer
            },
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = Guid.NewGuid(),
                Role = AssignedUserRole.FieldManager
            }
        };

        // completed site visit
        woodlandOfficerReview.SiteVisitNeeded = true;
        woodlandOfficerReview.SiteVisitArrangementsMade = true;
        woodlandOfficerReview.SiteVisitComplete = true;

        // completed PW14 checks
        woodlandOfficerReview.Pw14ChecksComplete = true;

        //completed pr
        var publicRegister = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow,
            ConsultationPublicRegisterRemovedTimestamp = DateTime.UtcNow
        };

        // complete confirmed f&r
        woodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;

        //incomplete conditions
        woodlandOfficerReview.IsConditional = true;
        woodlandOfficerReview.ConditionsToApplicantDate = null;

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(woodlandOfficerReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(publicRegister));

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenRepositoryUpdateFails(
        Guid applicationId,
        Guid performingUserId,
        Guid fieldManagerId,
        DateTime completedDateTime,
        FellingLicenceApplication application,
        RecommendedLicenceDuration recommendedLicenceDuration,
        bool recommendationForPublicRegister,
        string recommendationForPublicRegisterReason,
        WoodlandOfficerReview woodlandOfficerReview)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.WoodlandOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.WoodlandOfficer
            },
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = fieldManagerId,
                Role = AssignedUserRole.FieldManager
            }
        };

        // completed site visit
        woodlandOfficerReview.SiteVisitNeeded = true;
        woodlandOfficerReview.SiteVisitArrangementsMade = true;
        woodlandOfficerReview.SiteVisitComplete = true;

        // completed PW14 checks
        woodlandOfficerReview.Pw14ChecksComplete = true;

        //completed pr
        var publicRegister = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow,
            ConsultationPublicRegisterRemovedTimestamp = DateTime.UtcNow
        };

        // completed confirmed f&r
        woodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;
        woodlandOfficerReview.LarchCheckComplete = true;
        woodlandOfficerReview.EiaScreeningComplete = true;

        //completed conditions
        woodlandOfficerReview.IsConditional = false;

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(woodlandOfficerReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(publicRegister));
        UnitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUpdateCompletesSuccessfully(
        Guid applicationId,
        Guid performingUserId,
        Guid fieldManagerId,
        DateTime completedDateTime,
        FellingLicenceApplication application,
        RecommendedLicenceDuration recommendedLicenceDuration,
        bool recommendationForPublicRegister,
        string recommendationForPublicRegisterReason,
        WoodlandOfficerReview woodlandOfficerReview)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.WoodlandOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.WoodlandOfficer
            },
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = fieldManagerId,
                Role = AssignedUserRole.FieldManager
            }
        };

        // completed site visit
        woodlandOfficerReview.SiteVisitNeeded = true;
        woodlandOfficerReview.SiteVisitArrangementsMade = true;
        woodlandOfficerReview.SiteVisitComplete = true;

        // completed PW14 checks
        woodlandOfficerReview.Pw14ChecksComplete = true;

        //completed pr
        var publicRegister = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow,
            ConsultationPublicRegisterRemovedTimestamp = DateTime.UtcNow
        };

        // completed confirmed f&r
        woodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;
        woodlandOfficerReview.LarchCheckComplete = true;

        //completed conditions
        woodlandOfficerReview.IsConditional = false;
        woodlandOfficerReview.EiaScreeningComplete = true;

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(woodlandOfficerReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(publicRegister));
        UnitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsSuccess);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();

        Assert.Equal(2, application.StatusHistories.Count);
        Assert.Equal(FellingLicenceStatus.SentForApproval, application.StatusHistories.Last().Status);

        Assert.Equal(application.CreatedById, result.Value.ApplicantId);
        Assert.Equal(application.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(fieldManagerId, result.Value.FieldManagerId);

        Assert.Equal(completedDateTime, woodlandOfficerReview.LastUpdatedDate);
        Assert.Equal(performingUserId, woodlandOfficerReview.LastUpdatedById);
        Assert.Equal(recommendedLicenceDuration, woodlandOfficerReview.RecommendedLicenceDuration);
        Assert.Equal(recommendationForPublicRegister, woodlandOfficerReview.RecommendationForDecisionPublicRegister);
        Assert.Equal(recommendationForPublicRegisterReason, woodlandOfficerReview.RecommendationForDecisionPublicRegisterReason);
    }

    [Theory, AutoMoqData]
    public async Task WhenEiaScreeningRequiredAndIncomplete(
        Guid applicationId,
        Guid performingUserId,
        Guid fieldManagerId,
        DateTime completedDateTime,
        FellingLicenceApplication application,
        RecommendedLicenceDuration recommendedLicenceDuration,
        bool recommendationForPublicRegister,
        string recommendationForPublicRegisterReason,
        WoodlandOfficerReview woodlandOfficerReview)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.WoodlandOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.WoodlandOfficer
            },
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = fieldManagerId,
                Role = AssignedUserRole.FieldManager
            }
        };

        var deforestationProposed = application.LinkedPropertyProfile!.ProposedFellingDetails!.First();

        deforestationProposed.ProposedRestockingDetails!.Clear();
        deforestationProposed.IsRestocking = false;
        deforestationProposed.NoRestockingReason = "not required";

        // completed site visit
        woodlandOfficerReview.SiteVisitNeeded = true;
        woodlandOfficerReview.SiteVisitArrangementsMade = true;
        woodlandOfficerReview.SiteVisitComplete = true;

        // completed PW14 checks
        woodlandOfficerReview.Pw14ChecksComplete = true;

        //completed pr
        var publicRegister = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow,
            ConsultationPublicRegisterRemovedTimestamp = DateTime.UtcNow
        };

        // completed confirmed f&r
        woodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;
        woodlandOfficerReview.LarchCheckComplete = true;

        //completed conditions
        woodlandOfficerReview.IsConditional = false;
        woodlandOfficerReview.EiaScreeningComplete = false;

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(woodlandOfficerReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(publicRegister));
        UnitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenEiaScreeningRequiredAndComplete(
        Guid applicationId,
        Guid performingUserId,
        Guid fieldManagerId,
        DateTime completedDateTime,
        FellingLicenceApplication application,
        RecommendedLicenceDuration recommendedLicenceDuration,
        bool recommendationForPublicRegister,
        string recommendationForPublicRegisterReason,
        WoodlandOfficerReview woodlandOfficerReview)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.WoodlandOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.WoodlandOfficer
            },
            new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = fieldManagerId,
                Role = AssignedUserRole.FieldManager
            }
        };

        var deforestationProposed = application.LinkedPropertyProfile!.ProposedFellingDetails!.First();

        deforestationProposed.ProposedRestockingDetails!.Clear();
        deforestationProposed.IsRestocking = false;
        deforestationProposed.NoRestockingReason = "not required";

        // completed site visit
        woodlandOfficerReview.SiteVisitNeeded = true;
        woodlandOfficerReview.SiteVisitArrangementsMade = true;
        woodlandOfficerReview.SiteVisitComplete = true;

        // completed PW14 checks
        woodlandOfficerReview.Pw14ChecksComplete = true;

        //completed pr
        var publicRegister = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow,
            ConsultationPublicRegisterRemovedTimestamp = DateTime.UtcNow
        };

        // completed confirmed f&r
        woodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;
        woodlandOfficerReview.LarchCheckComplete = true;

        //completed conditions
        woodlandOfficerReview.IsConditional = false;
        woodlandOfficerReview.EiaScreeningComplete = true;

        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(woodlandOfficerReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(publicRegister));
        UnitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId, performingUserId, recommendedLicenceDuration, recommendationForPublicRegister, recommendationForPublicRegisterReason, completedDateTime, CancellationToken.None);

        Assert.True(result.IsSuccess);

        FellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();

        Assert.Equal(2, application.StatusHistories.Count);
        Assert.Equal(FellingLicenceStatus.SentForApproval, application.StatusHistories.Last().Status);

        Assert.Equal(application.CreatedById, result.Value.ApplicantId);
        Assert.Equal(application.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(fieldManagerId, result.Value.FieldManagerId);

        Assert.Equal(completedDateTime, woodlandOfficerReview.LastUpdatedDate);
        Assert.Equal(performingUserId, woodlandOfficerReview.LastUpdatedById);
        Assert.Equal(recommendedLicenceDuration, woodlandOfficerReview.RecommendedLicenceDuration);
        Assert.Equal(recommendationForPublicRegister, woodlandOfficerReview.RecommendationForDecisionPublicRegister);
        Assert.Equal(recommendationForPublicRegisterReason, woodlandOfficerReview.RecommendationForDecisionPublicRegisterReason);
    }
}