using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Forestry.Flo.HostApplicationsCommon.Infrastructure;

[HtmlTargetElement("govuk-form-group", Attributes = ForAttributeName)]
public class GovUkFormGroupTagHelper : TagHelper
{
    private const string ForAttributeName = "asp-for";

    [HtmlAttributeName(ForAttributeName)]
    public ModelExpression For { get; set; }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // Always render as a <div>
        output.TagName = "div";

        // Base GOV.UK class
        string cssClass = "govuk-form-group";

        // Check ModelState for errors
        var modelState = ViewContext?.ViewData?.ModelState;
        if (modelState != null && modelState.TryGetValue(For.Name, out ModelStateEntry? entry))
        {
            if (entry.Errors.Any())
            {
                cssClass += " govuk-form-group--error";
            }
        }

        // Merge with existing classes, if any
        var existingClass = output.Attributes["class"]?.Value?.ToString();
        if (!string.IsNullOrEmpty(existingClass))
        {
            cssClass = $"{existingClass} {cssClass}";
        }

        output.Attributes.SetAttribute("class", cssClass);
    }
}