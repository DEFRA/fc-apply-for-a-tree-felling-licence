using Forestry.Flo.Services.Common;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Forestry.Flo.External.Web.Infrastructure;

public class UserIsFcUserTagHelper : TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserIsFcUserTagHelper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        var user = _httpContextAccessor.HttpContext?.User;

        if (user == null 
            || user.IsNotLoggedIn()
            || !user.HasClaim(FloClaimTypes.FcUser, "true"))
        {
            output.SuppressOutput();
        }
    }
}