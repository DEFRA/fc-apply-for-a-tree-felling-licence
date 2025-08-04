using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Domain.V1;
using FakeItEasy;
using MigrationHostApp.Validation;
using Xunit;

namespace Forestry.FloV1.DataMigration.MigrationHostApp.Tests.MigrationModules.ExternalUsersMigrator;

public class PreValidateTests : ExternalUsersMigratorTestsBase
{
    [Fact]
    public async Task ShouldReturnFailure_WhenUsersCannotBeLoadedFromFlo1()
    {
        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Failure<List<FloUser>>("error"));

        var result = await ExternalUsersMigrator.PreValidateAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Could not load users from FLOv1 user data set", result.Error.ExceptionMessage);

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ShouldReturnFailure_WhenZeroUsersLoadedFromFlo1()
    {
        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Success(new List<FloUser>(0)));

        var result = await ExternalUsersMigrator.PreValidateAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("No users found in FLOv1 user data set", result.Error.ExceptionMessage);

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailure_WhenUsersWithDuplicateEmailsLoadedFromFlo1(List<FloUser> users, string duplicateEmail)
    {
        for (int i = 0; i < 3; i++)
        {
            users.Add(new FloUser(
                Fixture.Create<long>(),
                Fixture.Create<string>(),
                duplicateEmail,
                Fixture.Create<string>(),
                Fixture.Create<string>(),
                Fixture.Create<string>(),
                Fixture.Create<string>(),
                Fixture.Create<string>(),
                Fixture.Create<string>(),
                Fixture.Create<string>(),
                Fixture.Create<string>(),
                Fixture.Create<string>(),
                Fixture.Create<string>(),
                Fixture.Create<string>(),
                Fixture.Create<string>(),
                Fixture.Create<string>()));
        }

        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Success(users));

        var result = await ExternalUsersMigrator.PreValidateAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Duplicate email usage was found in FLOv1 user data set", result.Error.ExceptionMessage);

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailure_WhenUserFoundWithNoFirstName(List<FloUser> users)
    {
        users.Add(new FloUser(
            Fixture.Create<long>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            null,
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>()));

        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Success(users));

        var result = await ExternalUsersMigrator.PreValidateAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error.ValidationResultFailures);
        var validationError = result.Error.ValidationResultFailures.Single();
        Assert.Equal(DataItemValidationIssue.DataMissing, validationError.ItemValidationIssue);
        Assert.Equal("FirstName", validationError.FieldName);

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailure_WhenUserFoundWithNoLastName(List<FloUser> users)
    {
        users.Add(new FloUser(
            Fixture.Create<long>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            null,
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>()));

        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Success(users));

        var result = await ExternalUsersMigrator.PreValidateAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error.ValidationResultFailures);
        var validationError = result.Error.ValidationResultFailures.Single();
        Assert.Equal(DataItemValidationIssue.DataMissing, validationError.ItemValidationIssue);
        Assert.Equal("LastName", validationError.FieldName);

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailure_WhenUserFoundWithNoAddressLine1(List<FloUser> users)
    {
        users.Add(new FloUser(
            Fixture.Create<long>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            null,
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>()));

        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Success(users));

        var result = await ExternalUsersMigrator.PreValidateAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error.ValidationResultFailures);
        var validationError = result.Error.ValidationResultFailures.Single();
        Assert.Equal(DataItemValidationIssue.DataMissing, validationError.ItemValidationIssue);
        Assert.Equal("AddressLine1", validationError.FieldName);

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailure_WhenUserFoundWithNoTelephoneOrMobile(List<FloUser> users)
    {
        users.Add(new FloUser(
            Fixture.Create<long>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            null,
            null,
            Fixture.Create<string>()));

        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Success(users));

        var result = await ExternalUsersMigrator.PreValidateAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(2, result.Error.ValidationResultFailures.Count);
        
        Assert.Contains(result.Error.ValidationResultFailures, v => v is { ItemValidationIssue: DataItemValidationIssue.DataMissing, FieldName: "TelephoneNumber" });
        Assert.Contains(result.Error.ValidationResultFailures, v => v is { ItemValidationIssue: DataItemValidationIssue.DataMissing, FieldName: "MobileTelephoneNumber" });

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_WhenAllUsersAreValid(List<FloUser> users)
    {
        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Success(users));

        var result = await ExternalUsersMigrator.PreValidateAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        
        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}