using AutoFixture;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.PropertyProfiles;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Tests.Common;

public static class TestFellingLicenceApplicationsDatabaseFactory
{
    public static FellingLicenceApplicationsContext CreateDefaultTestContext()
    {
        var databaseOptions = TestDatabaseContextFactory<FellingLicenceApplicationsContext>.CreateDefaultTestContextOption();
        return new FellingLicenceApplicationsContext(databaseOptions);
    }

    public static async Task<FellingLicenceApplication> AddTestFellingLicenceApplication(
        FellingLicenceApplicationsContext dbContext,
        Fixture fixture)
    {
        var fellingLicenceApplication = fixture.Build<FellingLicenceApplication>().Create();

        dbContext.FellingLicenceApplications.Add(fellingLicenceApplication);
        await dbContext.SaveChangesAsync();

        return fellingLicenceApplication;
    }

    private class TestFellingLicenceApplications : FellingLicenceApplicationsContext
    {
        public TestFellingLicenceApplications(DbContextOptions<FellingLicenceApplicationsContext> options) : base(options)
        {
        }

        public override void Dispose()
        {
        }

        public override ValueTask DisposeAsync()
        {
            return new ValueTask(Task.CompletedTask);
        }
    }
}