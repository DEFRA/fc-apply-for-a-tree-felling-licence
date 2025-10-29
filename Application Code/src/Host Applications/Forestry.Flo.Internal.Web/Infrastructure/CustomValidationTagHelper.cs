using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Forestry.Flo.Internal.Web.Infrastructure;

    [HtmlTargetElement("validation")]
    public class CustomValidationTagHelper : TagHelper
    {
        private readonly IHtmlGenerator _htmlGenerator;

        public CustomValidationTagHelper(IHtmlGenerator htmlGenerator)
        {
            _htmlGenerator = htmlGenerator;
        }

        [HtmlAttributeName("asp-for")]
        public ModelExpression For { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (!ViewContext.ModelState.TryGetValue(For.Name, out var entry) || entry.Errors.Count == 0)
            {
                output.SuppressOutput();
                return;
            }

            var validationContext = CreateTagHelperContext();
            
            var validationOutput = CreateTagHelperOutput("p");

            var innerSpan = CreateTagHelperOutput("span");
            innerSpan.Attributes.SetAttribute("class","govuk-visually-hidden");
            innerSpan.Attributes.SetAttribute("aria-hidden", "true");
            innerSpan.Content.SetContent("Error:");

            validationOutput.PreContent.AppendHtml(innerSpan);

            var validation = new ValidationMessageTagHelper(_htmlGenerator)
            {
                For = For,
                ViewContext = ViewContext,

            };

            await validation.ProcessAsync(validationContext, validationOutput);
            validationOutput.Attributes.SetAttribute("class", "govuk-error-message");
            
            output.TagName = "";

            output.Content.AppendHtml(validationOutput);
        }

        private static TagHelperContext CreateTagHelperContext()
        {
            return new TagHelperContext(
                new TagHelperAttributeList(),
                new Dictionary<object, object>(),
                Guid.NewGuid().ToString("N"));
        }

        private static TagHelperOutput CreateTagHelperOutput(string tagName)
        {
            return new TagHelperOutput(
                tagName,
                new TagHelperAttributeList(),
                (a, b) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent(string.Empty);
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
        }
    }
