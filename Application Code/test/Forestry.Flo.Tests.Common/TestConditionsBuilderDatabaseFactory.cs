using Forestry.Flo.Services.ConditionsBuilder;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Tests.Common;

public class TestConditionsBuilderDatabaseFactory
{
    public static ConditionsBuilderContext CreateDefaultTestContext()
    {
        var databaseOptions = TestDatabaseContextFactory<ConditionsBuilderContext>.CreateDefaultTestContextOption();
        return new TestConditionsBuilderContext(databaseOptions);
    }

    private class TestConditionsBuilderContext : ConditionsBuilderContext
    {
        public TestConditionsBuilderContext(DbContextOptions<ConditionsBuilderContext> options) : base(options)
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