﻿using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Forestry.Flo.Internal.Web.Infrastructure;

[HtmlTargetElement(Attributes = nameof(Condition))]
public class ConditionTagHelper : TagHelper
{
    public bool Condition { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Condition)
        {
            output.SuppressOutput();
        }
    }
}