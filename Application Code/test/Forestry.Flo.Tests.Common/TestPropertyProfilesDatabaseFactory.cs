using AutoFixture;
using Forestry.Flo.Services.PropertyProfiles;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Tests.Common;

public static class TestPropertyProfilesDatabaseFactory
{
    public static PropertyProfilesContext CreateDefaultTestContext()
    {
        var databaseOptions = TestDatabaseContextFactory<PropertyProfilesContext>.CreateDefaultTestContextOption();
        return new TestPropertyProfiles(databaseOptions);
    }

    public static async Task<PropertyProfile> AddTestPropertyProfile(
        PropertyProfilesContext dbContext,
        Fixture fixture)
    {
        var propertyProfile = fixture.Build<PropertyProfile>()
            .Create();
        var compartment = fixture.Build<Compartment>()
            .With(c => c.PropertyProfileId, propertyProfile.Id)
            .Create();

        dbContext.PropertyProfiles.Add(propertyProfile);
        dbContext.Compartments.Add(compartment);
        await dbContext.SaveChangesAsync();

        return propertyProfile;
    }

    private class TestPropertyProfiles : PropertyProfilesContext
    {
        public TestPropertyProfiles(DbContextOptions<PropertyProfilesContext> options) : base(options)
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