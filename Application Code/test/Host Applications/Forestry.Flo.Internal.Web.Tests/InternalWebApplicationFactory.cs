using AutoFixture;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.InternalUsers;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Tests.Common;
using Forestry.Flo.Tests.Common.Testing.AuthenticationHandlers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using IUserInternalAccountRepository = Forestry.Flo.Services.InternalUsers.Repositories.IUserAccountRepository;

namespace Forestry.Flo.Internal.Web.Tests;

public class InternalWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    public readonly IFixture FixtureInstance = new Fixture();
    private readonly string _inMemoryDatabaseName = Guid.NewGuid().ToString();

    //todo remove all the mocks regarding data retrieval, and replace with actual data setup in the in-memory databases in the test webapp factory.

    public readonly Mock<IFellingLicenceApplicationInternalRepository> FellingLicenceApplicationInternalRepositoryMock = new();
    public readonly Mock<IFellingLicenceApplicationExternalRepository> FellingLicenceApplicationRepositoryMock = new();
    public readonly Mock<IWoodlandOwnerRepository> WoodlandOwnerRepositoryMock = new();
    public readonly Mock<IUserInternalAccountRepository> InternalUserAccountRepositoryMock = new();
    public readonly Mock<IUserAccountRepository> ExternalUserAccountRepositoryMock = new();
    public readonly Mock<ILandInformationSearch> LandInformationSearchMock = new();
    public readonly Mock<IFileStorageService> FileStorageServiceMock = new();
    public readonly Mock<IViewCaseNotesService> ViewCaseNotesServiceMock = new();
    public readonly Mock<IActivityFeedItemProvider> ActivityFeedItemProviderMock = new();
    public readonly Mock<IActivityFeedService> ActivityFeedServiceMock = new();
    public readonly Mock<IAddDocumentService> AddDocumentServiceMock = new();
    public Mock<IUnitOfWork> UnitOfWorkMock = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        UnitOfWorkMock = new Mock<IUnitOfWork>();

        builder.ConfigureServices(services =>
        {
            services.SwapService<IFellingLicenceApplicationExternalRepository>(FellingLicenceApplicationRepositoryMock.Object);
            services.SwapService<IFellingLicenceApplicationInternalRepository>(FellingLicenceApplicationInternalRepositoryMock.Object);
            services.SwapService<IWoodlandOwnerRepository>(WoodlandOwnerRepositoryMock.Object);
            services.SwapService<IFileStorageService>(FileStorageServiceMock.Object);
            services.SwapService<ILandInformationSearch>(LandInformationSearchMock.Object);
            services.SwapService<IUserAccountRepository>(ExternalUserAccountRepositoryMock.Object);
            services.SwapService<IUserInternalAccountRepository>(InternalUserAccountRepositoryMock.Object);
            services.SwapService<IWoodlandOwnerRepository>(WoodlandOwnerRepositoryMock.Object);
            services.SwapService<IViewCaseNotesService>(ViewCaseNotesServiceMock.Object);
            services.SwapService<IActivityFeedItemProvider>(ActivityFeedItemProviderMock.Object);
            services.SwapService<IActivityFeedService>(ActivityFeedServiceMock.Object);
            services.SwapService<IAddDocumentService>(AddDocumentServiceMock.Object);
            WebApplicationFactoryTestExtensions.ConfigureInMemoryDatabase<FellingLicenceApplicationsContext>(services, _inMemoryDatabaseName);
            WebApplicationFactoryTestExtensions.ConfigureInMemoryDatabase<AuditDataContext>(services, _inMemoryDatabaseName);
            WebApplicationFactoryTestExtensions.ConfigureInMemoryDatabase<InternalUsersContext>(services, _inMemoryDatabaseName);
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(HeaderAuthenticationDefaults.AuthenticationScheme)
                .AddScheme<HeaderAuthenticationOptions, InternalUserHeaderAuthenticatorHandler>(HeaderAuthenticationDefaults.AuthenticationScheme,
                    _ => { });
        });
    }

    public async Task AddInternalUserAccountToDbAsync(UserAccount userAccount, CancellationToken cancellationToken)
    {
        using var scope = Services.CreateScope();

        await using var internalUserContext = GetDbContext<InternalUsersContext>();

        EnsureUsingInMemoryProvider(internalUserContext);

        await internalUserContext.UserAccounts.AddAsync(userAccount, cancellationToken);
        await internalUserContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Convenience method for getting a new <see cref="DbContext"/> implementation from it's
    /// <see cref="IDbContextFactory{TContext}"/>.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext"/> type to retrieve.</typeparam>
    /// <returns>A database context that can be disposed of when done with</returns>
    private TContext GetDbContext<TContext>() where TContext : DbContext
    {
        var dbContextFactory = Services.GetRequiredService<IDbContextFactory<TContext>>();
        return dbContextFactory.CreateDbContext();
    }

    private static void EnsureUsingInMemoryProvider(DbContext dbContext)
    {
        var providerName = dbContext.Database.ProviderName;

        if (!providerName.Equals("Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
            throw new InvalidOperationException("Unable/unwilling to perform operation on DbContext that is not in memory, provider name is " + providerName);
    }
}
