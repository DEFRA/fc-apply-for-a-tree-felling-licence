using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using FakeItEasy;
using Xunit;

namespace Forestry.FloV1.DataMigration.MigrationHostApp.Tests.MigrationModules.ExternalUsersMigrator;

public class CanBeExecutedTests : ExternalUsersMigratorTestsBase
{
    [Theory, AutoData]
    public async Task ShouldReturnFailure_WhenUsersExistInFlo2(string error)
    {
        CreateSut();

        A.CallTo(() => DatabaseServiceV2.EnsureNoFlo2UsersExistAsync(A<CancellationToken>._))
            .Returns(Result.Failure(error));
        A.CallTo(() => DatabaseServiceV2.EnsureFlov1IdOnUserAccountTableAsync(A<CancellationToken>._))
            .Returns(Result.Success());
        A.CallTo(() => DatabaseServiceV2.EnsureFlov1ManagedOwnerIdOnWoodlandOwnerTableAsync(A<CancellationToken>._))
            .Returns(Result.Success());


        var result = await ExternalUsersMigrator.CanBeExecutedAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);

        A.CallTo(() => DatabaseServiceV2.EnsureNoFlo2UsersExistAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailure_WhenFlov1IdCannotBeAddedToUserAccount(string error)
    {
        CreateSut();

        A.CallTo(() => DatabaseServiceV2.EnsureNoFlo2UsersExistAsync(A<CancellationToken>._))
            .Returns(Result.Success());
        A.CallTo(() => DatabaseServiceV2.EnsureFlov1IdOnUserAccountTableAsync(A<CancellationToken>._))
            .Returns(Result.Failure(error));
        A.CallTo(() => DatabaseServiceV2.EnsureFlov1ManagedOwnerIdOnWoodlandOwnerTableAsync(A<CancellationToken>._))
            .Returns(Result.Success());


        var result = await ExternalUsersMigrator.CanBeExecutedAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);

        A.CallTo(() => DatabaseServiceV2.EnsureNoFlo2UsersExistAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.EnsureFlov1IdOnUserAccountTableAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailure_WhenFlov1ManagedOwnerIdCannotBeAddedToWoodlandOwner(string error)
    {
        CreateSut();

        A.CallTo(() => DatabaseServiceV2.EnsureNoFlo2UsersExistAsync(A<CancellationToken>._))
            .Returns(Result.Success());
        A.CallTo(() => DatabaseServiceV2.EnsureFlov1IdOnUserAccountTableAsync(A<CancellationToken>._))
            .Returns(Result.Success());
        A.CallTo(() => DatabaseServiceV2.EnsureFlov1ManagedOwnerIdOnWoodlandOwnerTableAsync(A<CancellationToken>._))
            .Returns(Result.Failure(error));


        var result = await ExternalUsersMigrator.CanBeExecutedAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);

        A.CallTo(() => DatabaseServiceV2.EnsureNoFlo2UsersExistAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.EnsureFlov1IdOnUserAccountTableAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.EnsureFlov1ManagedOwnerIdOnWoodlandOwnerTableAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}