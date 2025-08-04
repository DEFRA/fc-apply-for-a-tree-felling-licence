using AutoFixture.Xunit2;
using AutoFixture;
using CSharpFunctionalExtensions;
using Domain.V1;
using Domain.V2;
using FakeItEasy;
using Forestry.FloV1.DataMigration.MigrationHostApp.Tests.FixtureCustomizations;
using MigrationHostApp.CommandOptions;
using Xunit;

namespace Forestry.FloV1.DataMigration.MigrationHostApp.Tests.MigrationModules.ExternalUsersMigrator;

public class MigrateTests : ExternalUsersMigratorTestsBase
{
    private readonly MigrationOptions _defaultOptions = new MigrationOptions { };

    [Fact]
    public async Task ShouldReturnFailure_WhenUsersCannotBeLoadedFromFlo1()
    {
        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Failure<List<FloUser>>("error"));

        var result = await ExternalUsersMigrator.MigrateAsync(_defaultOptions,CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Could not load users from FLOv1 user data set", result.Error);

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
    
    [Fact]
    public async Task ShouldReturnFailure_WhenZeroUsersLoadedFromFlo1()
    {
        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Success(new List<FloUser>(0)));

        var result = await ExternalUsersMigrator.MigrateAsync(_defaultOptions, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("No users found in FLOv1 user data set", result.Error);

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

        var result = await ExternalUsersMigrator.MigrateAsync(_defaultOptions, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Duplicate email usage was found in FLOv1 user data set", result.Error);

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Theory, AutoDataWithSpecificFloUserRoleName("owner")]
    public async Task ShouldReturnFailure_WithOwnerAccount_WhenUnableToInsertWoodlandOwnerEntity(FloUser floUser)
    {
        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Success(new List<FloUser>{floUser}));

        A.CallTo(() => DatabaseServiceV2.AddWoodlandOwnerAsync(A<WoodlandOwner>._, A<long?>._, A<CancellationToken>._))
            .Returns(Result.Failure<Guid>("error"));

        var result = await ExternalUsersMigrator.MigrateAsync(_defaultOptions, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Failed to insert user accounts", result.Error);

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddWoodlandOwnerAsync(A<WoodlandOwner>.That.Matches(
                w => w.AddressLine1 == floUser.AddressLine1
                     && w.AddressLine2 == floUser.AddressLine2
                     && w.AddressLine3 == floUser.AddressLine3
                     && w.AddressLine4 == $"{floUser.AddressLine4} {floUser.AddressLine5}"
                     && w.PostalCode == floUser.PostalCode
                     && w.Email == floUser.Email
                     && w.ContactName == $"{floUser.FirstName} {floUser.LastName}"
                     && w.IsOrganisation == !string.IsNullOrWhiteSpace(floUser.CompanyName)
                     && w.OrganisationName == floUser.CompanyName),
            null,
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddUserAccountAsync(A<UserAccount>._, A<long>._, A<Guid?>._, A<Guid?>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => DatabaseServiceV2.AddAgencyAsync(A<Agency>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Theory, AutoDataWithSpecificFloUserRoleName("owner")]
    public async Task ShouldReturnFailure_WithOwnerAccount_WhenUnableToInsertUserAccountEntity(FloUser floUser, Guid woodlandOwnerId)
    {
        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Success(new List<FloUser> { floUser }));

        A.CallTo(() => DatabaseServiceV2.AddWoodlandOwnerAsync(A<WoodlandOwner>._, A<long?>._, A<CancellationToken>._))
            .Returns(Result.Success(woodlandOwnerId));

        A.CallTo(() => DatabaseServiceV2.AddUserAccountAsync(A<UserAccount>._, A<long>._, A<Guid?>._, A<Guid?>._, A<CancellationToken>._))
            .Returns(Result.Failure<Guid>("error"));

        var result = await ExternalUsersMigrator.MigrateAsync(_defaultOptions, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Failed to insert user accounts", result.Error);

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddWoodlandOwnerAsync(A<WoodlandOwner>.That.Matches(
                w => w.AddressLine1 == floUser.AddressLine1
                     && w.AddressLine2 == floUser.AddressLine2
                     && w.AddressLine3 == floUser.AddressLine3
                     && w.AddressLine4 == $"{floUser.AddressLine4} {floUser.AddressLine5}"
                     && w.PostalCode == floUser.PostalCode
                     && w.Email == floUser.Email
                     && w.ContactName == $"{floUser.FirstName} {floUser.LastName}"
                     && w.IsOrganisation == !string.IsNullOrWhiteSpace(floUser.CompanyName)
                     && w.OrganisationName == floUser.CompanyName),
            null,
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddUserAccountAsync(A<UserAccount>.That.Matches(
                a => a.AddressLine1 == floUser.AddressLine1
                && a.AddressLine2 == floUser.AddressLine2
                && a.AddressLine3 == floUser.AddressLine3
                && a.AddressLine4 == $"{floUser.AddressLine4} {floUser.AddressLine5}"
                && a.AddressPostcode == floUser.PostalCode
                && a.AccountType == AccountType.WoodlandOwnerAdministrator
                && a.Email == floUser.Email
                && a.FirstName == floUser.FirstName
                && a.LastName == floUser.LastName
                && a.Telephone == floUser.TelephoneNumber
                && a.MobileTelephone == floUser.MobileTelephoneNumber
            ), floUser.UserId, woodlandOwnerId, null, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddAgencyAsync(A<Agency>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Theory, AutoDataWithSpecificFloUserRoleName("owner")]
    public async Task ShouldReturnSuccess_WithOwnerAccount(FloUser floUser, Guid woodlandOwnerId)
    {
        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Success(new List<FloUser> { floUser }));

        A.CallTo(() => DatabaseServiceV2.AddWoodlandOwnerAsync(A<WoodlandOwner>._, A<long?>._, A<CancellationToken>._))
            .Returns(Result.Success(woodlandOwnerId));

        A.CallTo(() => DatabaseServiceV2.AddUserAccountAsync(A<UserAccount>._, A<long>._, A<Guid?>._, A<Guid?>._, A<CancellationToken>._))
            .Returns(Result.Success(Guid.NewGuid()));

        var result = await ExternalUsersMigrator.MigrateAsync(_defaultOptions, CancellationToken.None);

        Assert.True(result.IsSuccess);

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddWoodlandOwnerAsync(A<WoodlandOwner>.That.Matches(
                w => w.AddressLine1 == floUser.AddressLine1
                     && w.AddressLine2 == floUser.AddressLine2
                     && w.AddressLine3 == floUser.AddressLine3
                     && w.AddressLine4 == $"{floUser.AddressLine4} {floUser.AddressLine5}"
                     && w.PostalCode == floUser.PostalCode
                     && w.Email == floUser.Email
                     && w.ContactName == $"{floUser.FirstName} {floUser.LastName}"
                     && w.IsOrganisation == !string.IsNullOrWhiteSpace(floUser.CompanyName)
                     && w.OrganisationName == floUser.CompanyName),
            null,
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddUserAccountAsync(A<UserAccount>.That.Matches(
                a => a.AddressLine1 == floUser.AddressLine1
                && a.AddressLine2 == floUser.AddressLine2
                && a.AddressLine3 == floUser.AddressLine3
                && a.AddressLine4 == $"{floUser.AddressLine4} {floUser.AddressLine5}"
                && a.AddressPostcode == floUser.PostalCode
                && a.AccountType == AccountType.WoodlandOwnerAdministrator
                && a.Email == floUser.Email
                && a.FirstName == floUser.FirstName
                && a.LastName == floUser.LastName
                && a.Telephone == floUser.TelephoneNumber
                && a.MobileTelephone == floUser.MobileTelephoneNumber
            ), floUser.UserId, woodlandOwnerId, null, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddAgencyAsync(A<Agency>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Theory, AutoDataWithSpecificFloUserRoleName("agent")]
    public async Task ShouldReturnFailure_WithAgentAccount_WhenUnableToInsertAgencyEntity(FloUser floUser)
    {
        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Success(new List<FloUser> { floUser }));

        A.CallTo(() => DatabaseServiceV2.AddAgencyAsync(A<Agency>._, A<CancellationToken>._))
            .Returns(Result.Failure<Guid>("error"));

        var result = await ExternalUsersMigrator.MigrateAsync(_defaultOptions, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Failed to insert user accounts", result.Error);

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddAgencyAsync(A<Agency>.That.Matches(
                a => a.AddressLine1 == floUser.AddressLine1
                && a.AddressLine2 == floUser.AddressLine2
                && a.AddressLine3 == floUser.AddressLine3
                && a.AddressLine4 == $"{floUser.AddressLine4} {floUser.AddressLine5}"
                && a.PostalCode == floUser.PostalCode
                && a.Email == floUser.Email
                && a.ContactName == $"{floUser.FirstName} {floUser.LastName}"
                && a.IsOrganisation == !string.IsNullOrWhiteSpace(floUser.CompanyName)
                && a.OrganisationName == floUser.CompanyName
            ), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddUserAccountAsync(A<UserAccount>._, A<long>._, A<Guid?>._, A<Guid?>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => DatabaseServiceV2.AddWoodlandOwnerAsync(A<WoodlandOwner>._, A<long?>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Theory, AutoDataWithSpecificFloUserRoleName("agent")]
    public async Task ShouldReturnFailure_WithAgentAccount_WhenUnableToInsertUserAccountEntity(FloUser floUser, Guid agencyId)
    {
        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Success(new List<FloUser> { floUser }));

        A.CallTo(() => DatabaseServiceV2.AddAgencyAsync(A<Agency>._, A<CancellationToken>._))
            .Returns(Result.Success(agencyId));

        A.CallTo(() => DatabaseServiceV2.AddUserAccountAsync(A<UserAccount>._, A<long>._, A<Guid?>._, A<Guid?>._, A<CancellationToken>._))
            .Returns(Result.Failure<Guid>("error"));

        var result = await ExternalUsersMigrator.MigrateAsync(_defaultOptions, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Failed to insert user accounts", result.Error);

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddAgencyAsync(A<Agency>.That.Matches(
                a => a.AddressLine1 == floUser.AddressLine1
                     && a.AddressLine2 == floUser.AddressLine2
                     && a.AddressLine3 == floUser.AddressLine3
                     && a.AddressLine4 == $"{floUser.AddressLine4} {floUser.AddressLine5}"
                     && a.PostalCode == floUser.PostalCode
                     && a.Email == floUser.Email
                     && a.ContactName == $"{floUser.FirstName} {floUser.LastName}"
                     && a.IsOrganisation == !string.IsNullOrWhiteSpace(floUser.CompanyName)
                     && a.OrganisationName == floUser.CompanyName
            ), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddUserAccountAsync(A<UserAccount>.That.Matches(
                a => a.AddressLine1 == floUser.AddressLine1
                     && a.AddressLine2 == floUser.AddressLine2
                     && a.AddressLine3 == floUser.AddressLine3
                     && a.AddressLine4 == $"{floUser.AddressLine4} {floUser.AddressLine5}"
                     && a.AddressPostcode == floUser.PostalCode
                     && a.AccountType == AccountType.AgentAdministrator
                     && a.Email == floUser.Email
                     && a.FirstName == floUser.FirstName
                     && a.LastName == floUser.LastName
                     && a.Telephone == floUser.TelephoneNumber
                     && a.MobileTelephone == floUser.MobileTelephoneNumber
            ), floUser.UserId, null, agencyId, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddWoodlandOwnerAsync(A<WoodlandOwner>._, A<long?>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Theory, AutoDataWithSpecificFloUserRoleName("agent")]
    public async Task ShouldReturnSuccess_WithAgentAccount(FloUser floUser, Guid agencyId)
    {
        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Success(new List<FloUser> { floUser }));

        A.CallTo(() => DatabaseServiceV2.AddAgencyAsync(A<Agency>._, A<CancellationToken>._))
            .Returns(Result.Success(agencyId));

        A.CallTo(() => DatabaseServiceV2.AddUserAccountAsync(A<UserAccount>._, A<long>._, A<Guid?>._, A<Guid?>._, A<CancellationToken>._))
            .Returns(Result.Success(Guid.NewGuid()));

        var result = await ExternalUsersMigrator.MigrateAsync(_defaultOptions, CancellationToken.None);

        Assert.True(result.IsSuccess);

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddAgencyAsync(A<Agency>.That.Matches(
                a => a.AddressLine1 == floUser.AddressLine1
                     && a.AddressLine2 == floUser.AddressLine2
                     && a.AddressLine3 == floUser.AddressLine3
                     && a.AddressLine4 == $"{floUser.AddressLine4} {floUser.AddressLine5}"
                     && a.PostalCode == floUser.PostalCode
                     && a.Email == floUser.Email
                     && a.ContactName == $"{floUser.FirstName} {floUser.LastName}"
                     && a.IsOrganisation == !string.IsNullOrWhiteSpace(floUser.CompanyName)
                     && a.OrganisationName == floUser.CompanyName
            ), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddUserAccountAsync(A<UserAccount>.That.Matches(
                a => a.AddressLine1 == floUser.AddressLine1
                     && a.AddressLine2 == floUser.AddressLine2
                     && a.AddressLine3 == floUser.AddressLine3
                     && a.AddressLine4 == $"{floUser.AddressLine4} {floUser.AddressLine5}"
                     && a.AddressPostcode == floUser.PostalCode
                     && a.AccountType == AccountType.AgentAdministrator
                     && a.Email == floUser.Email
                     && a.FirstName == floUser.FirstName
                     && a.LastName == floUser.LastName
                     && a.Telephone == floUser.TelephoneNumber
                     && a.MobileTelephone == floUser.MobileTelephoneNumber
            ), floUser.UserId, null, agencyId, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddWoodlandOwnerAsync(A<WoodlandOwner>._, A<long?>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Theory, AutoDataWithSpecificFloUserRoleName("FC Internal Agent")]
    public async Task ShouldNotInsertFcUserAccount(FloUser floUser)
    {
        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Success(new List<FloUser> { floUser }));

        var result = await ExternalUsersMigrator.MigrateAsync(_defaultOptions, CancellationToken.None);

        Assert.True(result.IsSuccess);

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddWoodlandOwnerAsync(A<WoodlandOwner>._, A<long?>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => DatabaseServiceV2.AddAgencyAsync(A<Agency>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => DatabaseServiceV2.AddUserAccountAsync(A<UserAccount>._, A<long>._, A<Guid?>._, A<Guid?>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Theory, AutoDataWithValidFloUserRoleName]
    public async Task ShouldProcessMultipleUserAccounts(List<FloUser> userAccounts)
    {
        CreateSut();

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .Returns(Result.Success(userAccounts));
        A.CallTo(() => DatabaseServiceV2.AddWoodlandOwnerAsync(A<WoodlandOwner>._, A<long?>._, A<CancellationToken>._))
            .Returns(Result.Success(Guid.NewGuid()));
        A.CallTo(() => DatabaseServiceV2.AddAgencyAsync(A<Agency>._, A<CancellationToken>._))
            .Returns(Result.Success(Guid.NewGuid()));
        A.CallTo(() => DatabaseServiceV2.AddUserAccountAsync(A<UserAccount>._, A<long>._, A<Guid?>._, A<Guid?>._, A<CancellationToken>._))
            .Returns(Result.Success(Guid.NewGuid()));

        var result = await ExternalUsersMigrator.MigrateAsync(_defaultOptions, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var countOfWoodlandOwners = userAccounts.Count(x => x.RoleName == "owner");
        var countOfAgentAccounts = userAccounts.Count(x => x.RoleName == "agent");
        var countOfApplicantAccounts = userAccounts.Count(x => x.RoleName is "owner" or "agent");

        A.CallTo(() => DatabaseServiceV1.GetFloV1UsersAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => DatabaseServiceV2.AddWoodlandOwnerAsync(A<WoodlandOwner>._, A<long?>._, A<CancellationToken>._))
            .MustHaveHappenedANumberOfTimesMatching(a => a == countOfWoodlandOwners);
        A.CallTo(() => DatabaseServiceV2.AddAgencyAsync(A<Agency>._, A<CancellationToken>._))
            .MustHaveHappenedANumberOfTimesMatching(a => a == countOfAgentAccounts);
        A.CallTo(() => DatabaseServiceV2.AddUserAccountAsync(A<UserAccount>._, A<long>._, A<Guid?>._, A<Guid?>._, A<CancellationToken>._))
            .MustHaveHappenedANumberOfTimesMatching(a => a == countOfApplicantAccounts);
    }
}