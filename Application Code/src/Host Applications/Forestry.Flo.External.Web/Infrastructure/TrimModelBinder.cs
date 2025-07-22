using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace Forestry.Flo.External.Web.Infrastructure;
public class TrimmedFormValueProvider
    : FormValueProvider
{
    public TrimmedFormValueProvider(IFormCollection values)
        : base(BindingSource.Form, values, CultureInfo.InvariantCulture)
    { }

    public override ValueProviderResult GetValue(string key)
    {
        var baseResult = base.GetValue(key);
        string[] trimmedValues = baseResult.Values.Select(v => v?.Trim()).ToArray()!;
        return new ValueProviderResult(new StringValues(trimmedValues));
    }
}

public class TrimmedFormValueProviderFactory
    : IValueProviderFactory
{
    public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
    {
        if (context.ActionContext.HttpContext.Request.HasFormContentType)
            context.ValueProviders.Add(new TrimmedFormValueProvider(context.ActionContext.HttpContext.Request.Form));
        return Task.CompletedTask;
    }
}
