using System.Security.Claims;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Forestry.Flo.Services.Common
{
    public static class RequestContextInstaller
    {
        private const string RequestItemKey = nameof(RequestContextInstaller);
        
        public static IServiceCollection AddRequestContext(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<RequestContext>(sp =>
            {
                var requestContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;

                if (requestContext is null)
                {
                    return new RequestContext(string.Empty, new RequestUserModel(new ClaimsPrincipal()));
                }

                if (requestContext.Items.TryGetValue(RequestItemKey, out var item))
                {
                    // load from request context items if we have read this already and cached it on the request items collection
                    return (RequestContext) item;
                }

                // if we have got here then we have not found it in the request items cache
                var requestId = requestContext.TraceIdentifier;

                var requestUserModel = new RequestUserModel(requestContext.User);
                var result = new RequestContext(requestId, requestUserModel);

                requestContext.Items.Add(RequestItemKey, result);

                return result;
            });

            return serviceCollection;
        }
    }
}
