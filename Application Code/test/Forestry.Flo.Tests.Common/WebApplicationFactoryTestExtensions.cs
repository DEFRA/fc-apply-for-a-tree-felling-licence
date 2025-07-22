using Forestry.Flo.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Forestry.Flo.Tests.Common
{
    public static class WebApplicationFactoryTestExtensions
    {
        public static void ConfigureInMemoryDatabase<TContext>(IServiceCollection services, string dbInMemoryDatabaseName) where TContext : DbContext
        {
            // AddDbContext ultimately results in a TryAdd operation on the services collection, thus an existing DbContext
            // registration must be removed before we try to register the new one
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            
            services.AddDbContextFactory<TContext>(options =>
            {
                options.UseInMemoryDatabase(dbInMemoryDatabaseName);
                options.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            services.AddSingleton<IDbContextFactorySource<TContext>, CustomDbContextFactorySource<TContext>>();
        }

        /// <summary>
        /// Removes all registered registrations of <see cref="TService"/> and adds in replacement Mock service as a Singleton.
        /// </summary>
        /// <typeparam name="TService">The type of service interface which needs to be placed.</typeparam>
        /// <param name="mockObject">The mock implementation of <see cref="TService"/> to add into <see cref="services"/>.</param>
        /// <param name="services"></param>
        public static void SwapService<TService>(this IServiceCollection services, object mockObject) 
        {
            if (services.Any(x => x.ServiceType == typeof(TService)))
            {
                var serviceDescriptors = services.Where(x => x.ServiceType == typeof(TService)).ToList();
                foreach (var serviceDescriptor in serviceDescriptors)
                {
                    services.Remove(serviceDescriptor);
                }
            }

            services.AddSingleton(typeof(TService), mockObject);
        }
    }
}
