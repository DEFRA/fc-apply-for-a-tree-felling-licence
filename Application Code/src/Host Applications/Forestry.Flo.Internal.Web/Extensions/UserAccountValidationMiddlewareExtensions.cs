using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Middleware;
using Forestry.Flo.Services.InternalUsers.Configuration;

namespace Forestry.Flo.Internal.Web.Extensions
{
    public static class UserAccountValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserAccountValidation(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserAccountValidationMiddleware>();
        }

        public static IServiceCollection AddValidationMiddlewareOptions(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<PermittedRegisteredUserOptions>(
                configuration.GetSection(PermittedRegisteredUserOptions.ConfigurationKey));

            return services;
        }
    }
}
