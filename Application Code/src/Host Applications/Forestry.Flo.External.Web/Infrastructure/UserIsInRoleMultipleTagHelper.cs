using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Forestry.Flo.External.Web.Infrastructure;

/// <summary>
/// A tag that only shows if the user is one of the several roles that is given as a value
/// </summary>
public class UserIsInRoleMultipleTagHelper : TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountTypeExternal[] RoleNames { get; set; }

    public UserIsInRoleMultipleTagHelper(IHttpContextAccessor httpContextAccessor)
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
            || !RoleNames.Any(role => user.HasClaim(FloClaimTypes.AccountType, role.ToString())))
        {
            output.SuppressOutput();
        }
    }
}