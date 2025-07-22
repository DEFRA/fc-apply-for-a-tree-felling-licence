using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Forestry.Flo.External.Web.Infrastructure;

[HtmlTargetElement(Attributes="condition-disabled")]
public class ConditionalDisabledTagHelper : TagHelper
{
    public bool ConditionDisabled { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (ConditionDisabled)
        {
            output.Attributes.Add("disabled", "disabled");
        }
        base.Process(context, output);
    }
}