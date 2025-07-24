using Forestry.Flo.Services.Common;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Forestry.Flo.External.Web.Infrastructure;

public class UserIsNotFcUserTagHelper : TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserIsNotFcUserTagHelper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        var user = _httpContextAccessor.HttpContext?.User;

        if (user != null && user.IsLoggedIn())
        {
            if (user.HasClaim(FloClaimTypes.FcUser, "true"))
            {
                output.SuppressOutput();
            }
        }
    }
}