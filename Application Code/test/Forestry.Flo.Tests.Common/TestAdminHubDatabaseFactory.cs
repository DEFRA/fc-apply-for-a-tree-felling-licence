using AutoFixture;
using Forestry.Flo.Services.AdminHubs;
using Forestry.Flo.Services.AdminHubs.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Tests.Common;

public static class TestAdminHubDatabaseFactory
{
    public static AdminHubContext CreateDefaultTestContext()
    {
        var databaseOptions = TestDatabaseContextFactory<AdminHubContext>.CreateDefaultTestContextOption();
        return new TestAdminHubContext(databaseOptions);
    }
    
    public static async Task<AdminHub> CreateTestAdminHub(
        AdminHubContext dbContext, 
        Fixture fixture,
        bool save = true)
    {
        var adminHub = new AdminHub
        {
            Id = Guid.NewGuid(),
            AdminManagerId = Guid.NewGuid(),
            Name = fixture.Create<string>()
        };
        
        adminHub.AdminOfficers = new List<AdminHubOfficer>
        {
            new(adminHub, Guid.NewGuid()),
            new(adminHub, Guid.NewGuid()),
            new(adminHub, Guid.NewGuid()),
            new(adminHub, Guid.NewGuid()),
        }; 

        adminHub.Areas = new List<Area>
        {
            new(){AdminHub = adminHub,AdminHubId = adminHub.Id, Code="SW", Name="South West"},
            new(){AdminHub = adminHub,AdminHubId = adminHub.Id, Code="SE", Name="South East"}
        };

        if (save)
        {
            dbContext.AdminHubs.Add(adminHub);
            await dbContext.SaveChangesAsync();
        }
       
        return adminHub;
    }

    private class TestAdminHubContext : AdminHubContext
    {
        public TestAdminHubContext(DbContextOptions<AdminHubContext> options) : base(options)
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