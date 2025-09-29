using AutoFixture;
using Forestry.Flo.Services.Applicants;
using Forestry.Flo.Services.Applicants.Entities;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Common.User;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Tests.Common;

public static class TestApplicantsDatabaseFactory
{
    public static ApplicantsContext CreateDefaultTestContext()
    {
        var databaseOptions = TestDatabaseContextFactory<ApplicantsContext>.CreateDefaultTestContextOption();
        return new TestApplicantsContext(databaseOptions);
    }
    
    public static async Task<UserAccount> AddTestWoodlandOwnerAccount(
        ApplicantsContext dbContext,
        Fixture fixture,
        string? identityProviderId = null,
        string? email = null,
        bool? isOrganisation = null,
        bool acceptedTAndCs = true,
        bool acceptedPrivacyPolicy = true,
        UserAccountStatus? userAccountStatus = UserAccountStatus.Active)
    {
        DateTime? acceptedTAndCsDate = acceptedTAndCs ? fixture.Create<DateTime>() : null;
        DateTime? acceptedPrivacyPolicyDate = acceptedPrivacyPolicy ? fixture.Create<DateTime>() : null;

        var account = new UserAccount
        {
            AccountType = AccountTypeExternal.WoodlandOwnerAdministrator,
            IdentityProviderId = identityProviderId ?? fixture.Create<string>(),
            Email = email ?? fixture.Create<string>(),
            ContactAddress = fixture.Create<Address>(),
            FirstName = fixture.Create<string>(),
            LastName = fixture.Create<string>(),
            Title = fixture.Create<string>(),
            Status = userAccountStatus?? UserAccountStatus.Active,
            ContactTelephone = fixture.Create<string>(),
            ContactMobileTelephone = fixture.Create<string>(),
            DateAcceptedTermsAndConditions = acceptedTAndCsDate,
            DateAcceptedPrivacyPolicy = acceptedPrivacyPolicyDate,
            PreferredContactMethod = fixture.Create<PreferredContactMethod>(),
            WoodlandOwner = new WoodlandOwner
            {
                IsOrganisation = isOrganisation ?? fixture.Create<bool>(),
                ContactAddress = fixture.Create<Address>(),
                ContactName = fixture.Create<string>(),
                OrganisationAddress = fixture.Create<Address>(),
                OrganisationName = fixture.Create<string>(),
                ContactEmail = fixture.Create<string>()
            }
        };

        dbContext.UserAccounts.Add(account);
        await dbContext.SaveChangesAsync();

        return account;
    }

    public static async Task<UserAccount> AddTestAgentAccount(
    ApplicantsContext dbContext,
    Fixture fixture,
    string? identityProviderId = null,
    string? email = null,
    bool? isOrganisation = null,
    bool acceptedTAndCs = true,
    bool acceptedPrivacyPolicy = true,
    bool agentAdministrator = true,
    bool invited = false)
    {
        DateTime? acceptedTAndCsDate = acceptedTAndCs ? fixture.Create<DateTime>() : null;
        DateTime? acceptedPrivacyPolicyDate = acceptedPrivacyPolicy ? fixture.Create<DateTime>() : null;

        var account = new UserAccount
        {
            AccountType = agentAdministrator 
                ? AccountTypeExternal.AgentAdministrator 
                : AccountTypeExternal.Agent,
            IdentityProviderId = identityProviderId ?? fixture.Create<string>(),
            Email = email ?? fixture.Create<string>(),
            ContactAddress = fixture.Create<Address>(),
            FirstName = fixture.Create<string>(),
            LastName = fixture.Create<string>(),
            Title = fixture.Create<string>(),
            Status = UserAccountStatus.Active,
            ContactTelephone = fixture.Create<string>(),
            ContactMobileTelephone = fixture.Create<string>(),
            DateAcceptedTermsAndConditions = acceptedTAndCsDate,
            DateAcceptedPrivacyPolicy = acceptedPrivacyPolicyDate,
            PreferredContactMethod = fixture.Create<PreferredContactMethod>(),
            InviteToken = invited ? fixture.Create<Guid>() : null,
            InviteTokenExpiry = invited ? DateTime.UtcNow.AddDays(5) : null, 
            Agency = new Agency
            {
                Address = fixture.Create<Address>(),
                ContactEmail = fixture.Create<string>(),
                ContactName = fixture.Create<string>(),
                Id = Guid.NewGuid(),
                OrganisationName = fixture.Create<string>(),
                IsOrganisation = isOrganisation ?? false,
                ShouldAutoApproveThinningApplications = fixture.Create<bool>()
            }
        };

        dbContext.UserAccounts.Add(account);
        await dbContext.SaveChangesAsync();

        return account;
    }

    internal class TestApplicantsContext : ApplicantsContext
    {
        public TestApplicantsContext(DbContextOptions<ApplicantsContext> options) : base(options)
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