using Forestry.Flo.Services.InternalUsers;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Tests.Common;

public static class TestInternalUserDatabaseFactory
{
    public static InternalUsersContext CreateDefaultTestContext()
    {
        var databaseOptions = TestDatabaseContextFactory<InternalUsersContext>.CreateDefaultTestContextOption();
        return new InternalUsersContext(databaseOptions);
    }

    private class TestApplicantsContext : InternalUsersContext
    {
        public TestApplicantsContext(DbContextOptions<InternalUsersContext> options) : base(options)
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