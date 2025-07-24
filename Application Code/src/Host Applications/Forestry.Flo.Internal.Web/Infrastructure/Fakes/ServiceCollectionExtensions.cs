using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.Gis.Infrastructure;
using Forestry.Flo.Services.Gis.Services;
using Forestry.Flo.Services.Gis.Interfaces;

namespace Forestry.Flo.Internal.Web.Infrastructure.Fakes;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFakeServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var woReviewOptions = configuration.GetSection("WoodlandOfficerReview").Get<WoodlandOfficerReviewOptions>();
        if (woReviewOptions.UseDevPublicRegister)
        {
            services.RemoveService<IPublicRegister>();
            services.AddScoped<IPublicRegister, DevelopmentPublicRegister>();
        }
        else
        {
            services.AddScoped<IPublicRegister, PublicRegister>();
        }

        return services;
    }

    /// <summary>
    /// Removes all registered registrations of <see cref="TService"/> from an <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TService">The type of service interface which needs to be removed.</typeparam>
    /// <param name="services"></param>
    private static void RemoveService<TService>(this IServiceCollection services)
    {
        if (services.Any(x => x.ServiceType == typeof(TService)))
        {
            var serviceDescriptors = services.Where(x => x.ServiceType == typeof(TService)).ToList();
            foreach (var serviceDescriptor in serviceDescriptors)
            {
                services.Remove(serviceDescriptor);
            }
        }
    }
}