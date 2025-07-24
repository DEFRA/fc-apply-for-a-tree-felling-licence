using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Forestry.Flo.Internal.Web.Infrastructure;

public class RequireUserAccountTypeTagHelper : TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountTypeInternal RequiredAccountType { get; set; }

    public RequireUserAccountTypeTagHelper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user == null)
        {
            output.SuppressOutput();
        }
        else
        {
            var internalUser = new InternalUser(user);
            if (internalUser.AccountType == null || internalUser.AccountType!.Value != RequiredAccountType)
            {
                output.SuppressOutput();
            }
        }
    }
}