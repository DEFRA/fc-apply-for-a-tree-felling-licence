using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Forestry.Flo.HostApplicationsCommon.Infrastructure;

/// <summary>
/// Extension of the SelectTagHelper to add GOV.UK error styling when there are validation errors.
/// </summary>
[HtmlTargetElement("select", Attributes = ForAttributeName)]
public class GdsSelectTagHelper(IHtmlGenerator generator) : SelectTagHelper(generator)
{
    private const string ForAttributeName = "asp-for";

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Let the base class generate the standard <select> markup
        await base.ProcessAsync(context, output);

        // Now check for validation errors
        var modelName = For.Name;
        var modelState = ViewContext.ViewData.ModelState;

        GdsErrorTagHelper.ProcessOutput(modelName, modelState, "govuk-select--error", output);

        //reinitialize the PostContent so only the standard framework SelectTagHelper applies options
        output.PostContent.Reinitialize();
    }
}