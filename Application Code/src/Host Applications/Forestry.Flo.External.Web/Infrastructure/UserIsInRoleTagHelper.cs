using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Forestry.Flo.External.Web.Infrastructure;

public class UserIsInRoleTagHelper : TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountTypeExternal RoleName { get; set; }

    public UserIsInRoleTagHelper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null; // stops our own tag name <is-organiastion> being written to the browser
        var user = _httpContextAccessor.HttpContext?.User;

        if (user == null 
            || user.IsNotLoggedIn() 
            || !user.HasClaim(FloClaimTypes.AccountType, RoleName.ToString()))
        {
            output.SuppressOutput();
        }
    }
}