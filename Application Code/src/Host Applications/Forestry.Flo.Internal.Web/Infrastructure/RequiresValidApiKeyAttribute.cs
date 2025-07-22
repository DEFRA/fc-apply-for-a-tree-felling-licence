using Forestry.Flo.Services.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Internal.Web.Infrastructure
{
    /// <summary>
    /// Attribute class which enforces the presence of the required HTTP Header Key and expected value
    /// in order for a request to be permitted. Attribute should be used on all end-points used
    /// by external connecting systems.
    /// <para>
    /// Can be used at Controller class and/or Action method level.
    /// </para>
    /// <para>
    /// See <see cref="ApiSecurityOptions"/> for asserted configuration values.
    /// </para>
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequiresValidApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        private readonly ApiSecurityOptions _apiSecurityOptions = new();
        private ILogger<RequiresValidApiKeyAttribute> _logger = new NullLogger<RequiresValidApiKeyAttribute>();

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequiresValidApiKeyAttribute>>();

            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            configuration.Bind("ApiSecurityOptions", _apiSecurityOptions);

            var hasValidKey = HasValidApiKey(context.HttpContext);

            if (!hasValidKey)
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            await next();
        }
        
        private bool HasValidApiKey(HttpContext httpContext)
        {
            var expectedHeaderValue = _apiSecurityOptions.AuthenticationHeaderValue;

            if (string.IsNullOrWhiteSpace(expectedHeaderValue))
            {
                _logger.LogWarning("No expected security header value has been set so allowing API request.");
                return true;
            }

            var headerValue = httpContext.Request.Headers[_apiSecurityOptions.AuthenticationHeaderKey].SingleOrDefault();

            if (expectedHeaderValue.Equals(headerValue, StringComparison.Ordinal)) 
                return true;

            _logger.LogWarning(
                "Value '{HeaderValue}' read from HTTP Header {HeaderKey} does not match expected configured value",
                headerValue, _apiSecurityOptions.AuthenticationHeaderKey);

            _logger.LogError("Unauthorized API request blocked from remote IP address {RemoteIpAddress}"
                , httpContext.Connection.RemoteIpAddress);

            return false;
        }
    }
}
