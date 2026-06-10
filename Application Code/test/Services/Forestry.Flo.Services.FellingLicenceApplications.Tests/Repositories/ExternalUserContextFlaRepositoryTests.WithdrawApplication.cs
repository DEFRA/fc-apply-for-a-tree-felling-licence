using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public partial class ExternalUserContextFlaRepositoryTests
{
    [Theory, AutoMoqData]
    public async Task WithdrawApplicationWhenApplicationNotFoundById(
        Guid applicationId,
        Guid userId,
        DateTime currentDateTime,
        List<WithdrawalReason> withdrawalReasons,
        string? withdrawalReasonOtherDetails)
    {
        var result = await _sut.WithdrawApplicationAsync(applicationId, userId, currentDateTime, withdrawalReasons,
            withdrawalReasonOtherDetails, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
    }

    [Theory, AutoMoqData]
    public async Task WithdrawApplicationSetsStatusAndReasons(
        FellingLicenceApplication application,
        Guid applicationId,
        Guid userId,
        DateTime currentDateTime,
        List<WithdrawalReason> withdrawalReasons,
        string? withdrawalReasonOtherDetails)
    {
        TestUtils.SetProtectedProperty(application, nameof(FellingLicenceApplication.Id), applicationId);
        application.AssigneeHistories = [];
        application.StatusHistories = [];
        application.WithdrawalReasons = [];
        application.WithdrawalReasonOtherDetails = null;
        application.WoodlandOfficerReview = null;

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.WithdrawApplicationAsync(applicationId, userId, currentDateTime, withdrawalReasons,
            withdrawalReasonOtherDetails, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(x => x.StatusHistories)
            .FirstOrDefaultAsync(w => w.Id == application.Id);
        
        Assert.NotNull(updated);
        Assert.Single(updated.StatusHistories);

        Assert.Equal(FellingLicenceStatus.Withdrawn, updated.StatusHistories[0].Status);
        Assert.Equal(userId, updated.StatusHistories[0].CreatedById);
        Assert.Equal(currentDateTime, updated.StatusHistories[0].Created);

        Assert.Equivalent(withdrawalReasons, updated.WithdrawalReasons);
        Assert.Equal(withdrawalReasonOtherDetails, updated.WithdrawalReasonOtherDetails);
        Assert.Equal(userId, updated.WithdrawnByUserId);
    }

    [Theory, AutoMoqData]
    public async Task WithdrawApplicationUnassignsInternalUsers(
        FellingLicenceApplication application,
        Guid applicationId,
        Guid userId,
        DateTime currentDateTime,
        List<WithdrawalReason> withdrawalReasons,
        string? withdrawalReasonOtherDetails)
    {
        TestUtils.SetProtectedProperty(application, nameof(FellingLicenceApplication.Id), applicationId);
        application.AssigneeHistories = [
            new AssigneeHistory
            {
                Role = AssignedUserRole.Author,
                AssignedUserId = Guid.NewGuid(),
                FellingLicenceApplication = application,
                TimestampAssigned = DateTime.Today
            },
            new AssigneeHistory
            {
                Role = AssignedUserRole.AdminOfficer,
                AssignedUserId = Guid.NewGuid(),
                FellingLicenceApplication = application,
                TimestampAssigned = DateTime.Today
            },
            new AssigneeHistory
            {
                Role = AssignedUserRole.WoodlandOfficer,
                AssignedUserId = Guid.NewGuid(),
                FellingLicenceApplication = application,
                TimestampAssigned = DateTime.Today
            },
        ];
        application.StatusHistories = [];
        application.WithdrawalReasons = [];
        application.WithdrawalReasonOtherDetails = null;
        application.WoodlandOfficerReview = null;

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.WithdrawApplicationAsync(applicationId, userId, currentDateTime, withdrawalReasons,
            withdrawalReasonOtherDetails, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(x => x.AssigneeHistories)
            .FirstOrDefaultAsync(w => w.Id == application.Id);

        Assert.NotNull(updated);

        var author = updated.AssigneeHistories.SingleOrDefault(x => x.Role == AssignedUserRole.Author);
        Assert.NotNull(author);
        Assert.Null(author.TimestampUnassigned);

        var adminOfficer = updated.AssigneeHistories.SingleOrDefault(x => x.Role == AssignedUserRole.AdminOfficer);
        Assert.NotNull(adminOfficer);
        Assert.Equal(currentDateTime, adminOfficer.TimestampUnassigned);

        var woodlandOfficer = updated.AssigneeHistories.SingleOrDefault(x => x.Role == AssignedUserRole.WoodlandOfficer);
        Assert.NotNull(woodlandOfficer);
        Assert.Equal(currentDateTime, woodlandOfficer.TimestampUnassigned);
    }

    [Theory, AutoMoqData]
    public async Task WithdrawApplicationCompletesOutstandingAmendmentReview(
        FellingLicenceApplication application,
        Guid applicationId,
        Guid userId,
        DateTime currentDateTime,
        List<WithdrawalReason> withdrawalReasons,
        string? withdrawalReasonOtherDetails)
    {
        TestUtils.SetProtectedProperty(application, nameof(FellingLicenceApplication.Id), applicationId);
        TestUtils.SetProtectedProperty(application.WoodlandOfficerReview, nameof(WoodlandOfficerReview.Id), Guid.NewGuid());
        application.AssigneeHistories = [];
        application.StatusHistories = [];
        application.WithdrawalReasons = [];
        application.WithdrawalReasonOtherDetails = null;
        application.WoodlandOfficerReview.FellingAndRestockingAmendmentReviews = [
            new FellingAndRestockingAmendmentReview(true)
            {
                WoodlandOfficerReview = application.WoodlandOfficerReview,
                WoodlandOfficerReviewId = application.WoodlandOfficerReview.Id,
            },
            new FellingAndRestockingAmendmentReview(false)
            {
                WoodlandOfficerReview = application.WoodlandOfficerReview,
                WoodlandOfficerReviewId = application.WoodlandOfficerReview.Id,
            }
        ];

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await _sut.WithdrawApplicationAsync(applicationId, userId, currentDateTime, withdrawalReasons,
            withdrawalReasonOtherDetails, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(x => x.WoodlandOfficerReview)
            .ThenInclude(x => x.FellingAndRestockingAmendmentReviews)
            .FirstOrDefaultAsync(w => w.Id == application.Id);

        Assert.NotNull(updated);

        Assert.All(updated.WoodlandOfficerReview.FellingAndRestockingAmendmentReviews, x =>
        {
            Assert.True(x.AmendmentReviewCompleted is true);
        });
    }
}