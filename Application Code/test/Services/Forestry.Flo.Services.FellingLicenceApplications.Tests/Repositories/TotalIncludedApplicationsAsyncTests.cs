using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Tests.Common;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public class TotalIncludedApplicationsAsyncTests
{
    private readonly FellingLicenceApplicationsContext _ctx;
    private readonly InternalUserContextFlaRepository _sut;

    public TotalIncludedApplicationsAsyncTests()
    {
        _ctx = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _sut = new InternalUserContextFlaRepository(_ctx);
    }

    [Fact]
    public async Task Returns_Total_Assigned_Counts_And_Status_Distribution()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var statuses = new List<FellingLicenceStatus>
        {
            FellingLicenceStatus.Submitted,
            FellingLicenceStatus.AdminOfficerReview,
            FellingLicenceStatus.SentForApproval
        };

        Seed(statuses, userId);

        // Act
        var summary = await _sut.TotalIncludedApplicationsAsync(false, userId, statuses, CancellationToken.None);

        // Assert
        Assert.Equal(5, summary.TotalCount); // seeded 5 apps in status scope
        Assert.Equal(2, summary.AssignedToUserCount); // two assigned to user

        var dict = summary.StatusCounts.ToDictionary(x => x.Status, x => x.Count);
        Assert.Equal(2, dict[FellingLicenceStatus.Submitted]);
        Assert.Equal(2, dict[FellingLicenceStatus.AdminOfficerReview]);
        Assert.Equal(1, dict[FellingLicenceStatus.SentForApproval]);
    }

    [Fact]
    public async Task Applies_Assigned_Filter_To_Total_And_StatusCounts_When_Requested()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var statuses = new List<FellingLicenceStatus>
        {
            FellingLicenceStatus.Submitted,
            FellingLicenceStatus.AdminOfficerReview,
            FellingLicenceStatus.SentForApproval
        };

        Seed(statuses, userId);

        // Act: apply assigned filter
        var summary = await _sut.TotalIncludedApplicationsAsync(true, userId, statuses, CancellationToken.None);

        // Assert: only the two assigned items should be counted in total and per-status
        Assert.Equal(2, summary.TotalCount);
        Assert.Equal(2, summary.AssignedToUserCount); // AssignedToUserCount remains overall assigned within status scope

        var dict = summary.StatusCounts.ToDictionary(x => x.Status, x => x.Count);
        // In seeding, exactly 1 Submitted and 1 AdminOfficerReview are assigned to the user
        Assert.Equal(1, dict[FellingLicenceStatus.Submitted]);
        Assert.Equal(1, dict[FellingLicenceStatus.AdminOfficerReview]);
        Assert.False(dict.ContainsKey(FellingLicenceStatus.SentForApproval));
    }

    private void Seed(List<FellingLicenceStatus> allowedStatuses, Guid userId)
    {
        // Clear
        foreach (var e in _ctx.FellingLicenceApplications) _ctx.FellingLicenceApplications.Remove(e);
        _ctx.SaveChanges();

        var baseDate = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc);

        // Create 5 apps with latest statuses: 2 Submitted, 2 AdminOfficerReview, 1 SentForApproval
        var seeds = new[]
        {
            new { Ref = "A", Latest = FellingLicenceStatus.Submitted, AssignToUser = true },
            new { Ref = "B", Latest = FellingLicenceStatus.Submitted, AssignToUser = false },
            new { Ref = "C", Latest = FellingLicenceStatus.AdminOfficerReview, AssignToUser = true },
            new { Ref = "D", Latest = FellingLicenceStatus.AdminOfficerReview, AssignToUser = false },
            new { Ref = "E", Latest = FellingLicenceStatus.SentForApproval, AssignToUser = false },
        };

        int i = 0;
        foreach (var s in seeds)
        {
            var app = new FellingLicenceApplication
            {
                Id = Guid.NewGuid(),
                ApplicationReference = s.Ref,
                StatusHistories = new List<StatusHistory>()
            };

            // Initial Submitted
            app.StatusHistories.Add(new StatusHistory
            {
                Created = baseDate.AddDays(i),
                FellingLicenceApplication = app,
                Status = FellingLicenceStatus.Submitted
            });
            // Latest
            app.StatusHistories.Add(new StatusHistory
            {
                Created = baseDate.AddDays(i + 1),
                FellingLicenceApplication = app,
                Status = s.Latest
            });

            app.AssigneeHistories = new List<AssigneeHistory>();
            if (s.AssignToUser)
            {
                app.AssigneeHistories.Add(new AssigneeHistory
                {
                    FellingLicenceApplicationId = app.Id,
                    FellingLicenceApplication = app,
                    AssignedUserId = userId,
                    TimestampAssigned = baseDate.AddDays(i),
                    TimestampUnassigned = null,
                    Role = AssignedUserRole.AdminOfficer
                });
            }

            _ctx.FellingLicenceApplications.Add(app);
            i += 2;
        }

        _ctx.SaveEntitiesAsync(CancellationToken.None).GetAwaiter().GetResult();
    }
}
