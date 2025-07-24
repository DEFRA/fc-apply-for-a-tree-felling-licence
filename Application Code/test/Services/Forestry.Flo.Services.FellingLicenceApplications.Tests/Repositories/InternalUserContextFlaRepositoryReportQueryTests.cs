using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Tests.Common;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

/// <summary>
/// This effectively tests the construction of the query being built in <see cref="FellingLicenceApplicationsReportQuery"/>
///
/// todo create additional tests, across all properties in the view model to cover logic in the FellingLicenceApplicationsReportQuery
/// 
/// </summary>
public class InternalUserContextFlaRepositoryReportQueryTests
{
    private readonly InternalUserContextFlaRepository _sut;
    private readonly FellingLicenceApplicationsContext _fellingLicenceApplicationsContext;

    public InternalUserContextFlaRepositoryReportQueryTests()
    {
        _fellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _sut = new InternalUserContextFlaRepository(_fellingLicenceApplicationsContext);
    }
    
    [Theory, AutoMoqData]
    public async Task ReturnsData_ForSpecifiedCurrentStatus_WithinDateRange_ForDateRangeType(List<FellingLicenceApplication> applications)
    {
        var daysOffSet = 0;

        var queryModel = new FellingLicenceApplicationsReportQuery
        {
            DateFrom = DateTime.UtcNow.Date.AddDays(-10).Date,
            DateTo = DateTime.UtcNow.AddDays(10).Date,
            CurrentStatus = FellingLicenceStatus.Submitted,
            DateRangeTypeForReport = ReportDateRangeType.Submitted
        };
        
        for (var index = 0; index < applications.Count; index++)
        {
            if (index == applications.Count-1)
            {
                daysOffSet = 100;
            }
            var application = applications[index];

            application.CreatedTimestamp = DateTime.UtcNow.AddDays(daysOffSet);
            application.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = DateTime.UtcNow.AddDays(daysOffSet),
                    FellingLicenceApplication = application,
                    Status = FellingLicenceStatus.Draft
                },
                new()
                {
                    Created = DateTime.UtcNow.AddDays(daysOffSet).AddHours(5),
                    FellingLicenceApplication = application,
                    Status = FellingLicenceStatus.Submitted
                },

            };
            _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        }
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.ExecuteFellingLicenceApplicationsReportQueryAsync(queryModel, CancellationToken.None);

        Assert.Equal(applications.Count-1, result.Count);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsData_ForAnyCurrentStatus_WithinDateRange_ForDateRangeType(List<FellingLicenceApplication> applications)
    {
        var daysOffSet = 0;

        var queryModel = new FellingLicenceApplicationsReportQuery
        {
            DateFrom = DateTime.UtcNow.Date.AddDays(-10).Date,
            DateTo = DateTime.UtcNow.AddDays(10).Date,
            DateRangeTypeForReport = ReportDateRangeType.Submitted
        };

        for (var index = 0; index < applications.Count; index++)
        {
            if (index == applications.Count - 1)
            {
                daysOffSet = 100;
            }
            var application = applications[index];

            application.CreatedTimestamp = queryModel.DateFrom;

            for (var i = 0; i < application.StatusHistories.Count; i++)
            {
                if (application.StatusHistories.NotAny(c => c.Status == FellingLicenceStatus.Submitted))
                {
                    application.StatusHistories.Add(new StatusHistory
                    {
                        Created = DateTime.UtcNow.AddDays(daysOffSet).AddHours(5),
                        FellingLicenceApplication = application,
                        Status = FellingLicenceStatus.Submitted
                    });
                }
                
                var applicationStatusHistory = application.StatusHistories[i];
                applicationStatusHistory.Created = DateTime.Now.AddDays(daysOffSet).AddDays(5);
            }

            _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        }
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.ExecuteFellingLicenceApplicationsReportQueryAsync(queryModel, CancellationToken.None);

        Assert.Equal(applications.Count - 1, result.Count);
    }
}
