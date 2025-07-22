using Forestry.Flo.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Tests.Common;

public class TestNotificationHistoryDatabaseFactory
{
    public static NotificationsContext CreateDefaultTestContext()
    {
        var databaseOptions = TestDatabaseContextFactory<NotificationsContext>.CreateDefaultTestContextOption();
        return new TestNotificationHistories(databaseOptions);
    }

    private class TestNotificationHistories : NotificationsContext
    {
        public TestNotificationHistories(DbContextOptions<NotificationsContext> options) : base(options)
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