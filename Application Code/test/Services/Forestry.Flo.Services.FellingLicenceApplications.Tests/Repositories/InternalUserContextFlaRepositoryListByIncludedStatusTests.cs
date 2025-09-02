using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Tests.Common;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public class InternalUserContextFlaRepositoryListByIncludedStatusTests
{
    private readonly FellingLicenceApplicationsContext _ctx;
    private readonly InternalUserContextFlaRepository _sut;

    public InternalUserContextFlaRepositoryListByIncludedStatusTests()
    {
        _ctx = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _sut = new InternalUserContextFlaRepository(_ctx);
    }

    [Fact]
    public async Task Paginates_By_FinalActionDate_DefaultAscending()
    {
        // Arrange
        var includeStatuses = SeedApplicationsForOrdering();

        // Act
        var page1 = await _sut.ListByIncludedStatus(false, Guid.NewGuid(), includeStatuses, CancellationToken.None,
            pageNumber: 1, pageSize: 3, sortColumn: "FinalActionDate", sortDirection: "asc");
        var page2 = await _sut.ListByIncludedStatus(false, Guid.NewGuid(), includeStatuses, CancellationToken.None,
            pageNumber: 2, pageSize: 3, sortColumn: "FinalActionDate", sortDirection: "asc");

        // Assert
        Assert.Equal(3, page1.Count);
        Assert.Equal(2, page2.Count);

        // By design we created FAD in increasing order A..E, so page1 are first three, page2 are next two
        Assert.Collection(page1.Select(x => x.ApplicationReference),
            s => Assert.Equal("A", s),
            s => Assert.Equal("B", s),
            s => Assert.Equal("C", s));

        Assert.Collection(page2.Select(x => x.ApplicationReference),
            s => Assert.Equal("D", s),
            s => Assert.Equal("E", s));
    }

    [Theory]
    [InlineData("Reference", "asc", new[] { "A", "B", "C", "D", "E" })]
    [InlineData("Reference", "desc", new[] { "E", "D", "C", "B", "A" })]
    [InlineData("Property", "asc", new[] { "B", "C", "A", "E", "D" })] // P1..P5 mapped to B,C,A,E,D
    [InlineData("Property", "desc", new[] { "D", "E", "A", "C", "B" })]
    [InlineData("SubmittedDate", "desc", new[] { "D", "E", "A", "C", "B" })]
    [InlineData("CitizensCharterDate", "asc", new[] { "C", "A", "B", "E", "D" })] // C1..C5 mapped to C,A,B,E,D
    [InlineData("CitizensCharterDate", "desc", new[] { "D", "E", "B", "A", "C" })]
    [InlineData("FinalActionDate", "asc", new[] { "A", "B", "C", "D", "E" })]
    [InlineData("FinalActionDate", "desc", new[] { "E", "D", "C", "B", "A" })]
    public async Task Orders_Dynamically_By_Specified_Column(string column, string dir, string[] expectedOrder)
    {
        // Arrange
        var includeStatuses = SeedApplicationsForOrdering();

        // Act
        var result = await _sut.ListByIncludedStatus(false, Guid.NewGuid(), includeStatuses, CancellationToken.None,
            pageNumber: 1, pageSize: 100, sortColumn: column, sortDirection: dir);

        // Assert
        Assert.Equal(expectedOrder.Length, result.Count);
        Assert.Equal(expectedOrder, result.Select(x => x.ApplicationReference).ToArray());
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public async Task Orders_By_Status_Using_Latest_Status(string dir)
    {
        // Arrange
        var includeStatuses = SeedApplicationsForOrdering();

        // Act
        var result = await _sut.ListByIncludedStatus(false, Guid.NewGuid(), includeStatuses, CancellationToken.None,
            pageNumber: 1, pageSize: 100, sortColumn: "Status", sortDirection: dir);

        // Assert latest status is monotonic according to direction
        var latestStatuses = result
            .Select(x => x.StatusHistories.OrderByDescending(sh => sh.Created).First().Status)
            .ToArray();

        var sorted = dir == "asc"
            ? latestStatuses.OrderBy(s => s).ToArray()
            : latestStatuses.OrderByDescending(s => s).ToArray();

        Assert.Equal(sorted, latestStatuses);
    }

    private List<FellingLicenceStatus> SeedApplicationsForOrdering()
    {
        // Clear DB
        foreach (var entity in _ctx.FellingLicenceApplications)
        {
            _ctx.FellingLicenceApplications.Remove(entity);
        }
        _ctx.SaveChanges();

        var includeStatuses = new List<FellingLicenceStatus>
        {
            FellingLicenceStatus.Submitted,
            FellingLicenceStatus.AdminOfficerReview,
            FellingLicenceStatus.SentForApproval,
            FellingLicenceStatus.Approved,
            FellingLicenceStatus.Refused
        };

        var baseDate = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc);

        // Map for deterministic ordering
        var seeds = new[]
        {
            new { Ref = "A", Prop = "P3", SubIdx = 3, CharIdx = 2, FinalIdx = 1, Latest = FellingLicenceStatus.AdminOfficerReview },
            new { Ref = "B", Prop = "P1", SubIdx = 1, CharIdx = 3, FinalIdx = 2, Latest = FellingLicenceStatus.SentForApproval },
            new { Ref = "C", Prop = "P2", SubIdx = 2, CharIdx = 1, FinalIdx = 3, Latest = FellingLicenceStatus.Submitted },
            new { Ref = "D", Prop = "P5", SubIdx = 5, CharIdx = 5, FinalIdx = 4, Latest = FellingLicenceStatus.Refused },
            new { Ref = "E", Prop = "P4", SubIdx = 4, CharIdx = 4, FinalIdx = 5, Latest = FellingLicenceStatus.Approved },
        };

        foreach (var s in seeds)
        {
            var app = new FellingLicenceApplication
            {
                Id = Guid.NewGuid(),
                ApplicationReference = s.Ref,
                CitizensCharterDate = baseDate.AddDays(s.CharIdx),
                FinalActionDate = baseDate.AddDays(s.FinalIdx),
            };

            app.SubmittedFlaPropertyDetail = new SubmittedFlaPropertyDetail
            {
                FellingLicenceApplicationId = app.Id,
                Name = s.Prop
            };

            app.StatusHistories = new List<StatusHistory>();

            // Submitted at SubIdx
            app.StatusHistories.Add(new StatusHistory
            {
                Created = baseDate.AddDays(s.SubIdx),
                FellingLicenceApplication = app,
                Status = FellingLicenceStatus.Submitted
            });
            // Latest status at SubIdx + 1
            app.StatusHistories.Add(new StatusHistory
            {
                Created = baseDate.AddDays(s.SubIdx + 1),
                FellingLicenceApplication = app,
                Status = s.Latest
            });

            _ctx.FellingLicenceApplications.Add(app);
        }

        _ctx.SaveEntitiesAsync(CancellationToken.None).GetAwaiter().GetResult();
        return includeStatuses;
    }
}
