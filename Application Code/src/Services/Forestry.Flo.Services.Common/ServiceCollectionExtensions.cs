using Ardalis.GuardClauses;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace Forestry.Flo.Services.Common;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommonServices(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> options)
    {
        Guard.Against.Null(services);
        Guard.Against.Null(options);

        services.AddDbContextFactory<AuditDataContext>(options);
        services.AddSingleton<IDbContextFactorySource<AuditDataContext>, CustomDbContextFactorySource<AuditDataContext>>();
        services.AddScoped<IActivityFeedItemProvider, ActivityFeedItemProvider>();

        services.AddSingleton<IClock>(SystemClock.Instance);
        services.AddSingleton(typeof(IAuditService<>), typeof(EfAuditService<>));

        return services;
    }
}