using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services.AdminOfficerReview;

public class UpdateAdminOfficerReviewServiceTests : UpdateAdminOfficerReviewServiceTestsBase
{
    [Theory, AutoData]
    public async Task WhenApplicationNotFound(
        Guid applicationId,
        Guid performingUserId,
        DateTime completedDateTime)
    {
        var result = await Sut.CompleteAdminOfficerReviewAsync(
            applicationId, 
            performingUserId, 
            completedDateTime,
            false,
            true,
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationNotInAdminOfficerReviewState(
        Guid performingUserId,
        DateTime completedDateTime,
        FellingLicenceApplication application)
    {
        foreach (var applicationStatusHistory in application.StatusHistories)
        {
            if (applicationStatusHistory.Status == FellingLicenceStatus.AdminOfficerReview)
            {
                applicationStatusHistory.Status = FellingLicenceStatus.Submitted;
            }
        }

        CompletableAdminOfficerReview(application, true);

        FellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await FellingLicenceApplicationsContext.SaveEntitiesAsync().ConfigureAwait(false);
        
        var result = await Sut.CompleteAdminOfficerReviewAsync(
            application.Id, 
            performingUserId,
            completedDateTime, 
            false,
            true,
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory, AutoMoqData]
    public async Task WhenPerformingUserNotAssignedAdminOfficer(
        Guid performingUserId,
        Guid adminOfficerId,
        Guid woodlandOfficerId,
        DateTime completedDateTime,
        FellingLicenceApplication application)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new()
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new()
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = adminOfficerId,
                Role = AssignedUserRole.AdminOfficer
            },
            new()
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = woodlandOfficerId,
                Role = AssignedUserRole.WoodlandOfficer
            }
        };

        CompletableAdminOfficerReview(application, true);

        FellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await FellingLicenceApplicationsContext.SaveEntitiesAsync().ConfigureAwait(false);

        var result = await Sut.CompleteAdminOfficerReviewAsync(
            application.Id,
            performingUserId,
            completedDateTime, 
            false,
            true,
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory, AutoMoqData]
    public async Task WhenNoAssignedWoodlandOfficer(
        Guid performingUserId,
        DateTime completedDateTime,
        FellingLicenceApplication application)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new()
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new()
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.AdminOfficer
            }
        };

        CompletableAdminOfficerReview(application, true);

        FellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await FellingLicenceApplicationsContext.SaveEntitiesAsync().ConfigureAwait(false);

        var result = await Sut.CompleteAdminOfficerReviewAsync(
            application.Id,
            performingUserId, 
            completedDateTime, 
            false,
            true,
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory, CombinatorialData]
    public async Task WhenTaskNotComplete(
        bool agentAuthorityComplete,
        bool mappingComplete, 
        bool constraintsComplete,
        bool isTreeHealthComplete)
    {
        if (agentAuthorityComplete && mappingComplete && constraintsComplete)
        {
            return;
        }

        var performingUserId = Guid.NewGuid();

        var completedDateTime = DateTime.UtcNow;

        var adminOfficerReview = new Entities.AdminOfficerReview
        {
            LastUpdatedDate = DateTime.UtcNow,
            LastUpdatedById = performingUserId,
            AgentAuthorityFormChecked = agentAuthorityComplete,
            MappingChecked = mappingComplete,
            ConstraintsChecked = constraintsComplete,
            IsTreeHealthAnswersChecked = isTreeHealthComplete,
            AdminOfficerReviewComplete = false
        };

        var application = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview, adminOfficerReview, performingUserId, isTreeHealthApplication: !isTreeHealthComplete);

        var result = await Sut.CompleteAdminOfficerReviewAsync(
            application.Id,
            performingUserId,
            completedDateTime,
            false,
            true,
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task NonAgentApplicationsDoNotRequireAuthorityFormCompletion()
    {
        var performingUserId = Guid.NewGuid();
        var completedDateTime = DateTime.UtcNow;
        var adminOfficerReview = new Entities.AdminOfficerReview
        {
            LastUpdatedDate = DateTime.UtcNow,
            LastUpdatedById = performingUserId,
            AgentAuthorityFormChecked = null,
            MappingChecked = true,
            ConstraintsChecked = true,
            AdminOfficerReviewComplete = false
        };

        var application = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview, adminOfficerReview, performingUserId, Guid.NewGuid());

        var result = await Sut.CompleteAdminOfficerReviewAsync(
            application.Id,
            performingUserId,
            completedDateTime,
            false,
            false,      // no CBW do not skip WO
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationDoesNotHaveWoodlandOfficerYet(
        Guid performingUserId,
        DateTime completedDateTime,
        FellingLicenceApplication application)
    {
        application.CitizensCharterDate = null;
        application.StatusHistories = new List<StatusHistory>
        {
            new()
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new()
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.AdminOfficer
            }
        };

        CompletableAdminOfficerReview(application, true);

        FellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await FellingLicenceApplicationsContext.SaveEntitiesAsync().ConfigureAwait(false);

        var result = await Sut.CompleteAdminOfficerReviewAsync(
            application.Id,
            performingUserId,
            completedDateTime,
            false,
            true,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        Assert.Single(application.StatusHistories);
        Assert.Equal(FellingLicenceStatus.AdminOfficerReview, application.StatusHistories.Last().Status);
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationUpdated(
        Guid performingUserId,
        Guid woodlandOfficerId,
        DateTime completedDateTime,
        FellingLicenceApplication application)
    {
        application.CitizensCharterDate = null;
        application.StatusHistories = new List<StatusHistory>
        {
            new()
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new()
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.AdminOfficer
            },
            new()
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = woodlandOfficerId,
                Role = AssignedUserRole.WoodlandOfficer
            }
        };
        application.IsTreeHealthIssue = true;

        CompletableAdminOfficerReview(application, true);

        FellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await FellingLicenceApplicationsContext.SaveEntitiesAsync().ConfigureAwait(false);
        
        var result = await Sut.CompleteAdminOfficerReviewAsync(
            application.Id,
            performingUserId, 
            completedDateTime, 
            false,
            false,      // no CBW do not skip WO
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(woodlandOfficerId, result.Value.WoodlandOfficerId);
        Assert.Equal(application.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(application.CreatedById, result.Value.ApplicantId);
        Assert.Equal(2, application.StatusHistories.Count);
        Assert.Equal(FellingLicenceStatus.WoodlandOfficerReview, application.StatusHistories.Last().Status);
        Assert.Equal(completedDateTime, application.StatusHistories.Last().Created);
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationUpdatedForNonTreeHealthApplication(
        Guid performingUserId,
        Guid woodlandOfficerId,
        DateTime completedDateTime,
        FellingLicenceApplication application)
    {
        application.CitizensCharterDate = null;
        application.StatusHistories = new List<StatusHistory>
        {
            new()
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new()
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.AdminOfficer
            },
            new()
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = woodlandOfficerId,
                Role = AssignedUserRole.WoodlandOfficer
            }
        };

        application.IsTreeHealthIssue = false;

        CompletableAdminOfficerReview(application, true);
        application.AdminOfficerReview.IsTreeHealthAnswersChecked = null;

        FellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await FellingLicenceApplicationsContext.SaveEntitiesAsync().ConfigureAwait(false);

        var result = await Sut.CompleteAdminOfficerReviewAsync(
            application.Id,
            performingUserId,
            completedDateTime,
            false,
            false,      // no CBW do not skip WO
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(woodlandOfficerId, result.Value.WoodlandOfficerId);
        Assert.Equal(application.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(application.CreatedById, result.Value.ApplicantId);
        Assert.Equal(2, application.StatusHistories.Count);
        Assert.Equal(FellingLicenceStatus.WoodlandOfficerReview, application.StatusHistories.Last().Status);
        Assert.Equal(completedDateTime, application.StatusHistories.Last().Created);
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationUpdatedStraightToApproval(
        Guid performingUserId,
        Guid fieldManagerId,
        DateTime completedDateTime,
        FellingLicenceApplication application)
    {
        application.CitizensCharterDate = null;
        application.StatusHistories = new List<StatusHistory>
        {
            new()
            {
                Created = DateTime.UtcNow,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        };
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new()
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = performingUserId,
                Role = AssignedUserRole.AdminOfficer
            },
            new()
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = fieldManagerId,
                Role = AssignedUserRole.FieldManager
            }
        };

        CompletableAdminOfficerReview(application, true);

        FellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await FellingLicenceApplicationsContext.SaveEntitiesAsync().ConfigureAwait(false);

        var result = await Sut.CompleteAdminOfficerReviewAsync(
            application.Id,
            performingUserId,
            completedDateTime,
            false,
            true,       // CBW do skip WO
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(fieldManagerId, result.Value.WoodlandOfficerId);
        Assert.Equal(application.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(application.CreatedById, result.Value.ApplicantId);
        Assert.Equal(2, application.StatusHistories.Count);
        Assert.Equal(FellingLicenceStatus.SentForApproval, application.StatusHistories.Last().Status);
        Assert.Equal(completedDateTime, application.StatusHistories.Last().Created);
    }

    private static void CompletableAdminOfficerReview(FellingLicenceApplication application, bool completable)
    {
        application.AdminOfficerReview = new Entities.AdminOfficerReview
        {
            AgentAuthorityFormChecked = completable,
            MappingChecked = completable,
            ConstraintsChecked = completable,
            IsTreeHealthAnswersChecked = true,
            AdminOfficerReviewComplete = false
        };
    }
}