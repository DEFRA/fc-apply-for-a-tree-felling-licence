using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Forestry.Flo.HostApplicationsCommon.Infrastructure;

/// <summary>
/// Base class for TagHelpers that add GOV.UK error styling based on ModelState validation errors.
/// </summary>
public static class GdsErrorTagHelper
{
    public static void ProcessOutput(
        string? modelName, 
        ModelStateDictionary? modelState,
        string errorClassToApply,
        TagHelperOutput input)
    {
        if (string.IsNullOrWhiteSpace(modelName) || modelState == null)
        {
            return;
        }

        // Check for validation errors
        if (modelState.TryGetValue(modelName, out var entry) && entry.Errors.Count > 0)
        {
            // Add a CSS class e.g. 'govuk-input--error'
            var existingClass = GetExistingClass(input);
            var newClass = string.IsNullOrEmpty(existingClass)
                ? errorClassToApply
                : $"{existingClass} {errorClassToApply}";

            input.Attributes.SetAttribute("class", newClass);
        }
    }

    private static string? GetExistingClass(TagHelperOutput output)
    {
        var classAttr = output.Attributes.FirstOrDefault(a => a.Name == "class");
        if (classAttr == null) return null;

        switch (classAttr.Value)
        {
            case string s:
                return s;
            case Microsoft.AspNetCore.Html.IHtmlContent htmlContent:
            {
                // Render IHtmlContent to string
                using var writer = new StringWriter();
                htmlContent.WriteTo(writer, HtmlEncoder.Default);
                return writer.ToString();
            }
            default:
                // Fallback
                return classAttr.Value?.ToString();
        }
    }
}