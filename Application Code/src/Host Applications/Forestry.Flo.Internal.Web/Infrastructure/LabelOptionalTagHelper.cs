using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Forestry.Flo.Internal.Web.Infrastructure;

[HtmlTargetElement("label", Attributes = "asp-for")]
public class LabelOptionalTagHelper : LabelTagHelper
{
    public LabelOptionalTagHelper(IHtmlGenerator htmlGenerator)
        : base(htmlGenerator)
    {
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        await base.ProcessAsync(context, output);

        var metadata = For.Metadata as DefaultModelMetadata;
        var hasRequiredAttribute = metadata
            ?.Attributes
            .PropertyAttributes != null && (metadata.Attributes.PropertyAttributes
            .Any(i => i.GetType() == typeof(DisplayAsOptionalAttribute)));
        if (hasRequiredAttribute)
        {
            output.PostContent.AppendHtml(" (optional)");
        }
    }
}