using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Forestry.Flo.HostApplicationsCommon.Infrastructure;

/// <summary>
/// Extension of the InputTagHelper to add GOV.UK error styling when there are validation errors.
/// </summary>
[HtmlTargetElement("input", Attributes = ForAttributeName)]
public class GdsInputTagHelper(IHtmlGenerator generator) : InputTagHelper(generator)
{
    private const string ForAttributeName = "asp-for";

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Let the base class generate the standard <input> markup
        await base.ProcessAsync(context, output);

        // Now check for validation errors
        var modelName = For.Name;
        var modelState = ViewContext.ViewData.ModelState;

        GdsErrorTagHelper.ProcessOutput(modelName, modelState, "govuk-input--error", output);
    }
}