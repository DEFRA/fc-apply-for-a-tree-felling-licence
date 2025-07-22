using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace Forestry.Flo.Tests.Common;

public static class TestDatabaseContextFactory<T> where T : DbContext, new()
{
    public static DbContextOptions<T> CreateDefaultTestContextOption() =>
        new DbContextOptionsBuilder<T>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            // don't raise the error warning us that the in memory db doesn't support transactions
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    public static IDbContextFactory<T> CreateDefaultTestContextFactory(T? database)
    {
        var mockDbContextFactory = new Mock<IDbContextFactory<T>>();
        mockDbContextFactory
            .Setup(x => x.CreateDbContext())
            .Returns(database!);
        mockDbContextFactory
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(database!);

        return mockDbContextFactory.Object;
    }
}