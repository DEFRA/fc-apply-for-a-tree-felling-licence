using Microsoft.AspNetCore.Authorization;

namespace Forestry.Flo.Internal.Web.Infrastructure
{
    public static class PolicyServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthorization<TDep>(this IServiceCollection services, Action<AuthorizationOptions, TDep> configure) where TDep : class
        {
            services.AddOptions<AuthorizationOptions>().Configure<TDep>(configure);
            return services.AddAuthorization();
        }
    }
}
